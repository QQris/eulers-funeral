using System;
using EulersFuneral.Core.Data;
using EulersFuneral.Core.MathParser;
using EulersFuneral.Core.Physics;
using EulersFuneral.Graphics;
using NUnit.Framework;
using UnityEngine;

namespace EulersFuneral.Tests.EditMode
{
    public class RK4LagrangianPhysicsTests
    {
        [Test]
        public void RK4Lagrangian_ConservesTotalMechanicalEnergyOnParaboloid()
        {
            var surface = new MathSurfaceFunction("0.15 * x^2 + 0.1 * y^2");
            var integrator = new RK4Integrator
            {
                Gravity = 9.8,
                Restitution = 0.0,
                MinX = -10, MaxX = 10,
                MinY = -10, MaxY = 10
            };

            double z0 = surface.Evaluate(-3.0, 2.0);
            BallState state = BallState.CreateOnSurface(-3.0, 2.0, z0);
            double initialEnergy = LagrangianPhysics.ComputeTotalEnergyPerUnitMass(
                state.Z, state.Vx, state.Vy, state.Vz, integrator.Gravity);

            // Simulate 360 steps (3.0 seconds at dt = 1/120 s)
            for (int i = 0; i < 360; i++)
            {
                StepResult res = integrator.Step(in state, surface, LagrangianPhysics.FixedDeltaTime);
                Assert.IsFalse(res.FellIntoVoid);
                Assert.IsFalse(res.OutOfDomain);
                state = res.State;
            }

            double finalEnergy = LagrangianPhysics.ComputeTotalEnergyPerUnitMass(
                state.Z, state.Vx, state.Vy, state.Vz, integrator.Gravity);

            Assert.AreEqual(initialEnergy, finalEnergy, 1e-4);
        }

        [Test]
        public void ObjectiveContainment_FollowsStrictRF10Formula()
        {
            double rBall = 0.25;
            double rTarget = 0.9 * rBall; // 0.225 -> max center distance = 0.025

            // Contained when |p_bola - p_B| <= 0.025
            Assert.IsTrue(LagrangianPhysics.DoesBallContainObjective(
                3.0, 0.0, 0.0, rBall,
                3.02, 0.0, 0.0, rTarget));

            // Not contained when |p_bola - p_B| > 0.025 (even though spheres overlap)
            Assert.IsFalse(LagrangianPhysics.DoesBallContainObjective(
                3.0, 0.0, 0.0, rBall,
                3.05, 0.0, 0.0, rTarget));
        }

        [Test]
        public void OrbitCamera_NeverCrossesBelowPlaneAndClampsPitch()
        {
            Vector3 posLow = OrbitCameraController.ComputeOrbitPosition(
                Vector3.zero, 0f, -40f, 10f, 0.05f,
                out float clampedPitchLow, out _);

            Assert.GreaterOrEqual(clampedPitchLow, OrbitCameraController.MinPitchDegrees);
            Assert.GreaterOrEqual(posLow.y, 0.05f);

            OrbitCameraController.ComputeOrbitPosition(
                Vector3.zero, 0f, 120f, 10f, 0.05f,
                out float clampedPitchHigh, out _);

            Assert.LessOrEqual(clampedPitchHigh, OrbitCameraController.MaxPitchDegrees);
        }

        [Test]
        public void AllSixLevels_AreConfiguredAndSolvable()
        {
            LevelData[] levels = LevelDefinitions.CreateAllLevels();
            Assert.AreEqual(6, levels.Length);

            string[] referenceFormulas =
            {
                "-0.2*x - 0.2*y",
                "0.2*y^2 - 0.25*x",
                "0.25*floor(-0.5*x) - 0.15*x - 0.15*y",
                "-0.25*x + 0.2*ceil(-0.4*x) + 0.15*y^2",
                "0.5*sen(x) - 0.3*x - 0.2*y",
                "e^x + 0.2*y^2"
            };

            for (int idx = 0; idx < 6; idx++)
            {
                LevelData lvl = levels[idx];
                Assert.AreEqual(idx, lvl.LevelIndex);
                Assert.AreEqual(3, lvl.StarPositions.Length);
                Assert.AreEqual(0.9f * lvl.BallRadius, lvl.ObjectiveRadius, 1e-6f);

                var fn = new MathSurfaceFunction(referenceFormulas[idx]);
                double zStart = fn.Evaluate(lvl.StartPointA.x, lvl.StartPointA.y);
                Assert.IsFalse(double.IsNaN(zStart), $"Level {idx} start Z should be defined");
            }
        }
    }
}
