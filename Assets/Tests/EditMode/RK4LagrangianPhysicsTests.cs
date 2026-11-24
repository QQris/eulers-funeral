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
            double initialEnergy = 0.5 * (state.Vx * state.Vx + state.Vy * state.Vy + state.Vz * state.Vz) + integrator.Gravity * state.Z;

            for (int i = 0; i < 360; i++)
            {
                StepResult res = integrator.Step(in state, surface, LagrangianPhysics.FixedDeltaTime);
                Assert.IsFalse(res.FellIntoVoid);
                Assert.IsFalse(res.OutOfDomain);
                state = res.State;
            }

            double finalEnergy = 0.5 * (state.Vx * state.Vx + state.Vy * state.Vy + state.Vz * state.Vz) + integrator.Gravity * state.Z;
            Assert.AreEqual(initialEnergy, finalEnergy, 1e-4);
        }

        [Test]
        public void StarCollision_DetectsSphereContactCorrectly()
        {
            Assert.IsTrue(LagrangianPhysics.DoesBallTouchStar(
                1.0, 1.0, 0.5, 0.25,
                1.3, 1.0, 0.5, 0.20));

            Assert.IsFalse(LagrangianPhysics.DoesBallTouchStar(
                1.0, 1.0, 0.5, 0.25,
                2.0, 1.0, 0.5, 0.20));
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
    }
}
