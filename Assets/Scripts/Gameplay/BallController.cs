using System;
using EulersFuneral.Audio;
using EulersFuneral.Core.Data;
using EulersFuneral.Core.MathParser;
using EulersFuneral.Core.Physics;
using EulersFuneral.Graphics;
using UnityEngine;

namespace EulersFuneral.Gameplay
{
   public class BallController : MonoBehaviour
    {
        public const float VisualScale = 0.6f;

        public SimulationStatus Status { get; private set; } = SimulationStatus.Ready;
        public BallState CurrentState => _currState;
        public int CollectedStarsCount { get; private set; }

        public event Action<int> OnStarCollected;
        public event Action OnVictory;
        public event Action<SimulationStatus> OnDefeat;

        private readonly RK4Integrator _integrator = new RK4Integrator();
        private MathSurfaceFunction _surface;
        private LevelData _level;

        private BallState _prevState;
        private BallState _currState;
        private double _accumulator;

        private readonly bool[] _starCollected = new bool[3];
        private GameObject[] _starVisuals;

        public void ConfigureForLevel(LevelData level, GameObject[] starVisuals)
        {
            _level = level;
            _starVisuals = starVisuals;

            _integrator.Gravity = level.Gravity;
            _integrator.Restitution = level.Restitution;
            _integrator.BallRadius = level.BallRadius;
            _integrator.MinX = level.MinX;
            _integrator.MaxX = level.MaxX;
            _integrator.MinY = level.MinY;
            _integrator.MaxY = level.MaxY;
            _integrator.HasCeiling = level.HasCeiling;
            _integrator.CeilingHeight = level.CeilingHeight;
            _integrator.CeilingFunction = !string.IsNullOrWhiteSpace(level.CeilingFormula)
                ? new MathSurfaceFunction(level.CeilingFormula)
                : null;

            transform.localScale = Vector3.one * (level.BallRadius * 2.0f * VisualScale);

            Renderer rend = GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                Color blueColor = new Color(0.15f, 0.55f, 1.0f, 1.0f);
                rend.sharedMaterial.color = blueColor;
                if (rend.sharedMaterial.HasProperty("_Color")) rend.sharedMaterial.SetColor("_Color", blueColor);
                if (rend.sharedMaterial.HasProperty("_BaseColor")) rend.sharedMaterial.SetColor("_BaseColor", blueColor);
            }

            ResetToStartPoint(null);
        }

        public void ResetToStartPoint(MathSurfaceFunction surface)
        {
            _surface = surface;
            Status = SimulationStatus.Ready;
            _accumulator = 0.0;
            CollectedStarsCount = 0;

            for (int i = 0; i < _starCollected.Length; i++)
            {
                _starCollected[i] = false;
                if (_starVisuals != null && i < _starVisuals.Length && _starVisuals[i] != null)
                {
                    _starVisuals[i].SetActive(true);
                }
            }

            if (_level != null)
            {
                double startX = _level.StartPointA.x;
                double startY = _level.StartPointA.y;
                double startZ = _level.StartPointA.z;

                if (_surface != null)
                {
                    double evaluatedZ = _surface.Evaluate(startX, startY);
                    if (!double.IsNaN(evaluatedZ))
                    {
                        startZ = evaluatedZ;
                    }
                }

                _currState = BallState.CreateOnSurface(startX, startY, startZ);
                _prevState = _currState;
                transform.position = MathCoords.ToUnity(_currState.X, _currState.Y, _currState.Z);
            }

            if (ProceduralAudioSystem.Instance != null)
            {
                ProceduralAudioSystem.Instance.StopRollingSound();
            }
        }

        public void LaunchSimulation(MathSurfaceFunction surface)
        {
            if (_level == null || surface == null)
                return;

            _surface = surface;
            ResetToStartPoint(surface);

            double zAtStart = _surface.Evaluate(_level.StartPointA.x, _level.StartPointA.y);
            if (double.IsNaN(zAtStart))
            {
                TriggerDefeat(SimulationStatus.DefeatFellIntoVoid);
                return;
            }

            double heightDiff = _level.StartPointA.z - zAtStart;
            if (heightDiff > _level.BallRadius * 1.5f)
            {
                _currState = new BallState
                {
                    X = _level.StartPointA.x,
                    Y = _level.StartPointA.y,
                    Z = _level.StartPointA.z,
                    Vx = 0,
                    Vy = 0,
                    Vz = 0,
                    IsAirborne = true,
                    ElapsedTime = 0
                };
            }
            else
            {
                _currState = BallState.CreateOnSurface(_level.StartPointA.x, _level.StartPointA.y, zAtStart);
            }

            _prevState = _currState;
            Status = SimulationStatus.Running;
        }

        private void Update()
        {
            if (Status != SimulationStatus.Running || _level == null)
                return;

            double frameDt = Math.Min(Time.deltaTime, 0.1);
            _accumulator += frameDt;

            const double fixedDt = LagrangianPhysics.FixedDeltaTime; // 1/120 s

            while (_accumulator >= fixedDt && Status == SimulationStatus.Running)
            {
                _prevState = _currState;
                StepResult stepRes = _integrator.Step(in _currState, _surface, fixedDt);
                _currState = stepRes.State;
                _accumulator -= fixedDt;

                if (stepRes.DidBounce && ProceduralAudioSystem.Instance != null)
                {
                    ProceduralAudioSystem.Instance.PlayBounce(stepRes.BounceImpactSpeed, _level.Restitution);
                }

                if (stepRes.OutOfDomain)
                {
                    TriggerDefeat(SimulationStatus.DefeatOutOfBounds);
                    return;
                }

                if (stepRes.FellIntoVoid)
                {
                    TriggerDefeat(SimulationStatus.DefeatFellIntoVoid);
                    return;
                }

                if (_currState.ElapsedTime >= _level.MaxTimeSeconds)
                {
                    TriggerDefeat(SimulationStatus.DefeatTimeOut);
                    return;
                }

                CheckStarsAndObjectiveSubsteps(in _prevState, in _currState);
            }

            double alpha = _accumulator / fixedDt;
            BallState renderState = BallState.Lerp(in _prevState, in _currState, alpha);
            transform.position = MathCoords.ToUnity(renderState.X, renderState.Y, renderState.Z);

            if (ProceduralAudioSystem.Instance != null)
            {
                bool rollingOnSurface = (Status == SimulationStatus.Running) && !_currState.IsAirborne;
                ProceduralAudioSystem.Instance.UpdateRollingSound(_currState.Speed, rollingOnSurface);
            }
        }

        private void CheckStarsAndObjectiveSubsteps(in BallState a, in BallState b)
        {
            const int subChecks = 4;
            for (int s = 1; s <= subChecks; s++)
            {
                double t = (double)s / subChecks;
                BallState sample = BallState.Lerp(in a, in b, t);

                // RF-09: 3 estrellas rojas sobre la trayectoria
                for (int i = 0; i < 3 && i < _level.StarPositions.Length; i++)
                {
                    if (_starCollected[i]) continue;

                    Vector3 starPos = _level.StarPositions[i];
                    if (LagrangianPhysics.DoesBallTouchStar(
                        sample.X, sample.Y, sample.Z, _level.BallRadius,
                        starPos.x, starPos.y, starPos.z, _level.StarRadius))
                    {
                        _starCollected[i] = true;
                        CollectedStarsCount++;

                        if (_starVisuals != null && i < _starVisuals.Length && _starVisuals[i] != null)
                        {
                            _starVisuals[i].SetActive(false);
                        }

                        if (ProceduralAudioSystem.Instance != null)
                        {
                            ProceduralAudioSystem.Instance.PlayStarPickup(CollectedStarsCount);
                        }

                        OnStarCollected?.Invoke(CollectedStarsCount);
                    }
                }

                // El nivel se completa inmediatamente al recoger las 3 estrellas rojas
                if (CollectedStarsCount >= 3)
                {
                    Status = SimulationStatus.Victory;
                    _currState = sample;
                    transform.position = MathCoords.ToUnity(sample.X, sample.Y, sample.Z);

                    if (ProceduralAudioSystem.Instance != null)
                    {
                        ProceduralAudioSystem.Instance.PlayVictory();
                    }

                    OnVictory?.Invoke();
                    return;
                }
            }
        }

        private void TriggerDefeat(SimulationStatus reason)
        {
            Status = reason;
            if (ProceduralAudioSystem.Instance != null)
            {
                ProceduralAudioSystem.Instance.PlayDefeat();
            }
            OnDefeat?.Invoke(reason);
        }
    }
}
