using System.Globalization;
using EulersFuneral.Core.Data;
using EulersFuneral.Core.MathParser;
using EulersFuneral.Core.Physics;
using EulersFuneral.Graphics;
using EulersFuneral.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EulersFuneral.Gameplay
{
    public enum LevelPhase
    {
        Menu,
        TitleAnimation,
        EditingFormula,
        AnimatingSurface,
        Simulating,
        Victory,
        Defeat
    }

    /// <summary>
    /// Orquesta el flujo completo de los niveles 0 a 5 en la escena Gameplay:
    /// Transición de título Manim (RF-04), Mesa 3D (RF-05), Entrada de fórmula (RF-06),
    /// Pistas (RF-07), Simulación RK4 (RF-08), Estrellas (RF-09),
    /// Derrota (RF-11) y Reinicio/Progreso (RF-12).
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Configuración de Niveles (Assets en Inspector)")]
        public LevelData[] ConfiguredLevels;
        public bool AutoStartOnSceneLoad = true;

        [Header("Componentes de Escena 3D")]
        public SurfaceMeshGenerator SurfaceGenerator;
        public CoordinateAxesVisualizer AxesVisualizer;
        public OrbitCameraController OrbitCamera;
        public BallController Ball;
        public GameObject StartMarkerA;
        public GameObject[] StarObjects = new GameObject[3];

        [Header("Controladores UI")]
        public LaTeXTitleAnimation TitleAnimator;
        public FormulaInputController FormulaInput;
        public HintsController HintsPanel;
        public GravityToast GravityNotification;
        public InitialHelpPopup InitialHelp;
        public SceneFadeTransition SceneFader;

        [Header("HUD y Paneles de Resultado")]
        public GameObject GameplayHudRoot;
        public TextMeshProUGUI LevelHeaderText;
        public TextMeshProUGUI StarsCounterText;
        public TextMeshProUGUI TimerText;
        public GameObject ResultPanelRoot;
        public TextMeshProUGUI ResultTitleText;
        public TextMeshProUGUI ResultDetailsText;
        public Button RetryButton;
        public Button EditFormulaButton;
        public Button NextLevelButton;
        public Button ReturnToMenuButton;

        public LevelData[] AllLevels => ConfiguredLevels;
        public LevelData CurrentLevel { get; private set; }
        public LevelPhase Phase { get; private set; } = LevelPhase.Menu;

        private float _previousGravity = 9.8f;
        private bool _eventsBound;

        private void Start()
        {
            InitializeBindings();

            if (AutoStartOnSceneLoad)
            {
                StartLevel(GameSessionContext.SelectedLevelIndex);
            }
        }

        public void InitializeBindings()
        {
            if (_eventsBound)
                return;
            _eventsBound = true;

            if (FormulaInput != null)
            {
                FormulaInput.OnFormulaConfirmed += HandleFormulaConfirmed;
                FormulaInput.OnLaunchRequested += HandleLaunchRequested;
                FormulaInput.OnResetRequested += EnterEditMode;
            }

            if (SurfaceGenerator != null)
            {
                SurfaceGenerator.OnSurfaceAppearFinished += HandleSurfaceAppearFinished;
            }

            if (Ball != null)
            {
                Ball.OnStarCollected += HandleStarCollected;
                Ball.OnVictory += HandleVictory;
                Ball.OnDefeat += HandleDefeat;
            }

            if (RetryButton != null)
                RetryButton.onClick.AddListener(RetrySimulation);

            if (EditFormulaButton != null)
                EditFormulaButton.onClick.AddListener(EnterEditMode);

            if (NextLevelButton != null)
                NextLevelButton.onClick.AddListener(AdvanceToNextLevel);

            if (ReturnToMenuButton != null)
                ReturnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        public void StartLevel(int levelIndex)
        {
            if (ConfiguredLevels == null || ConfiguredLevels.Length == 0)
                return;

            levelIndex = Mathf.Clamp(levelIndex, 0, ConfiguredLevels.Length - 1);
            GameSessionContext.SelectedLevelIndex = levelIndex;
            CurrentLevel = ConfiguredLevels[levelIndex];

            if (ResultPanelRoot != null)
                ResultPanelRoot.SetActive(false);

            if (SurfaceGenerator != null)
                SurfaceGenerator.ClearSurface();

            if (AxesVisualizer != null)
                AxesVisualizer.RebuildTable(CurrentLevel.MinX, CurrentLevel.MaxX, CurrentLevel.MinY, CurrentLevel.MaxY);

            PositionLevelEntities(CurrentLevel);

            if (Ball != null)
            {
                Ball.gameObject.SetActive(true);
                Ball.ConfigureForLevel(CurrentLevel, StarObjects);
            }

            if (FormulaInput != null)
            {
                FormulaInput.ResetForLevel("");
                FormulaInput.SetInteractable(false);
            }

            if (HintsPanel != null)
            {
                HintsPanel.SetContext(CurrentLevel, null);
            }

            UpdateHudCounters(0, 0f);
            if (LevelHeaderText != null)
            {
                LevelHeaderText.text = CurrentLevel.LevelName;
            }

            // RF-04: Reproducir transición de video Manim antes de pasar a la fase de edición
            Phase = LevelPhase.TitleAnimation;
            if (GameplayHudRoot != null)
                GameplayHudRoot.SetActive(false);

            if (TitleAnimator != null)
            {
                TitleAnimator.PlayLevelTitle(CurrentLevel, OnTitleAnimationComplete);
            }
            else
            {
                OnTitleAnimationComplete();
            }
        }

        private void OnTitleAnimationComplete()
        {
            if (GameplayHudRoot != null)
            {
                GameplayHudRoot.SetActive(true);
                if (SceneFader != null)
                {
                    SceneFader.AnimatePanelOpacityIn(GameplayHudRoot, 0.70f);
                }
            }

            // RF-03: Mostrar ayuda inicial en la primera apertura
            if (InitialHelp != null)
            {
                InitialHelp.ShowIfFirstTime();
            }

            // RF-13: Aviso en el HUD durante 2 s cuando la gravedad cambia
            if (Mathf.Abs(CurrentLevel.Gravity - _previousGravity) > 0.01f && GravityNotification != null)
            {
                GravityNotification.ShowGravityNotice(CurrentLevel.Gravity);
            }
            _previousGravity = CurrentLevel.Gravity;

            EnterEditMode();
        }

        public void PositionLevelEntities(LevelData level)
        {
            if (level == null) return;

            if (StartMarkerA != null)
            {
                StartMarkerA.SetActive(true);
                StartMarkerA.transform.position = MathCoords.ToUnity(level.StartPointA);
                StartMarkerA.transform.localScale = Vector3.one * (level.BallRadius * 0.7f);
            }

            for (int i = 0; i < 3; i++)
            {
                if (StarObjects[i] != null && i < level.StarPositions.Length)
                {
                    StarObjects[i].SetActive(true);
                    StarObjects[i].transform.position = MathCoords.ToUnity(level.StarPositions[i]);
                    StarObjects[i].transform.localScale = Vector3.one * (level.StarRadius * 2.0f);
                }
            }
        }

        public void EnterEditMode()
        {
            Phase = LevelPhase.EditingFormula;
            if (ResultPanelRoot != null)
                ResultPanelRoot.SetActive(false);

            if (Ball != null)
            {
                Ball.ResetToStartPoint(SurfaceGenerator != null ? SurfaceGenerator.CurrentFunction : null);
            }

            if (FormulaInput != null)
            {
                FormulaInput.SetInteractable(true);
            }

            UpdateHudCounters(0, 0f);
        }

        private void HandleFormulaConfirmed(MathSurfaceFunction function)
        {
            if (CurrentLevel == null || SurfaceGenerator == null)
                return;

            Phase = LevelPhase.AnimatingSurface;
            if (FormulaInput != null)
                FormulaInput.SetInteractable(false);

            if (HintsPanel != null)
                HintsPanel.SetContext(CurrentLevel, function);

            SurfaceGenerator.GenerateSurface(
                function,
                CurrentLevel.MinX, CurrentLevel.MaxX,
                CurrentLevel.MinY, CurrentLevel.MaxY,
                animateAppearance: true);
        }

        private void HandleSurfaceAppearFinished()
        {
            Phase = LevelPhase.EditingFormula;
            if (FormulaInput != null)
                FormulaInput.SetInteractable(true);

            if (Ball != null && SurfaceGenerator != null)
            {
                Ball.ResetToStartPoint(SurfaceGenerator.CurrentFunction);
            }
        }

        private void HandleLaunchRequested()
        {
            if (SurfaceGenerator == null || SurfaceGenerator.CurrentFunction == null || Ball == null)
                return;

            Phase = LevelPhase.Simulating;
            if (ResultPanelRoot != null)
                ResultPanelRoot.SetActive(false);

            if (FormulaInput != null)
                FormulaInput.SetInteractable(false);

            UpdateHudCounters(0, 0f);
            Ball.LaunchSimulation(SurfaceGenerator.CurrentFunction);
        }

        public void RetrySimulation()
        {
            if (SurfaceGenerator != null && SurfaceGenerator.CurrentFunction != null)
            {
                HandleLaunchRequested();
            }
            else
            {
                EnterEditMode();
            }
        }

        private void HandleStarCollected(int count)
        {
            UpdateHudCounters(count, (float)Ball.CurrentState.ElapsedTime);
        }

        private void HandleVictory()
        {
            Phase = LevelPhase.Victory;
            SaveSystem.MarkLevelCompleted(CurrentLevel.LevelIndex);

            if (ResultPanelRoot != null)
                ResultPanelRoot.SetActive(true);

            if (ResultTitleText != null)
            {
                ResultTitleText.text = "¡NIVEL COMPLETADO!";
                ResultTitleText.color = new Color(0.45f, 0.95f, 0.55f, 1.0f);
            }

            if (ResultDetailsText != null)
            {
                ResultDetailsText.text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Has recolectado las 3 estrellas.\nTiempo: {0:F2} s",
                    Ball.CurrentState.ElapsedTime);
            }

            if (NextLevelButton != null)
            {
                NextLevelButton.gameObject.SetActive(CurrentLevel.LevelIndex + 1 < ConfiguredLevels.Length);
            }
        }

        private void HandleDefeat(SimulationStatus reason)
        {
            Phase = LevelPhase.Defeat;

            if (ResultPanelRoot != null)
                ResultPanelRoot.SetActive(true);

            if (ResultTitleText != null)
            {
                ResultTitleText.text = "SIMULACIÓN FALLIDA";
                ResultTitleText.color = new Color(0.95f, 0.35f, 0.35f, 1.0f);
            }

            string reasonStr;
            switch (reason)
            {
                case SimulationStatus.DefeatOutOfBounds:
                    reasonStr = "La pelota salió de los límites del dominio.";
                    break;
                case SimulationStatus.DefeatFellIntoVoid:
                    reasonStr = "La pelota cayó al vacío o a una región matemática no definida.";
                    break;
                case SimulationStatus.DefeatTimeOut:
                    reasonStr = "Se agotó el tiempo máximo del nivel.";
                    break;
                default:
                    reasonStr = "No se cumplieron las condiciones del nivel.";
                    break;
            }

            if (ResultDetailsText != null)
            {
                ResultDetailsText.text = $"{reasonStr}\nEstrellas recogidas: {Ball.CollectedStarsCount} / 3";
            }

            if (NextLevelButton != null)
            {
                NextLevelButton.gameObject.SetActive(false);
            }
        }

        public void AdvanceToNextLevel()
        {
            if (CurrentLevel == null) return;
            int nextIndex = CurrentLevel.LevelIndex + 1;
            if (nextIndex < ConfiguredLevels.Length)
            {
                StartLevel(nextIndex);
            }
        }

        public void ReturnToMainMenu()
        {
            MainMenuController localMenu = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (localMenu != null)
            {
                localMenu.ShowMainMenu();
            }
            else if (SceneFader != null)
            {
                SceneFader.TransitionToScene(GameSessionContext.MainMenuSceneName);
            }
            else
            {
                SceneManager.LoadScene(GameSessionContext.MainMenuSceneName);
            }
        }

        private void Update()
        {
            if (Phase == LevelPhase.Simulating && Ball != null)
            {
                UpdateHudCounters(Ball.CollectedStarsCount, (float)Ball.CurrentState.ElapsedTime);
            }

            bool isTypingFormula = FormulaInput != null &&
                                   FormulaInput.FormulaInputField != null &&
                                   FormulaInput.FormulaInputField.isFocused;

            if (!isTypingFormula && (Phase == LevelPhase.Simulating || Phase == LevelPhase.Defeat) && Input.GetKeyDown(KeyCode.R))
            {
                EnterEditMode();
            }
        }

        private void UpdateHudCounters(int stars, float timeSeconds)
        {
            if (StarsCounterText != null)
            {
                StarsCounterText.text = $"Estrellas: {stars} / 3";
            }

            if (TimerText != null && CurrentLevel != null)
            {
                TimerText.text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Tiempo: {0:F1} / {1:F0} s",
                    timeSeconds, CurrentLevel.MaxTimeSeconds);
            }
        }
    }
}
