using System;
using EulersFuneral.Core.MathParser;

namespace EulersFuneral.Core.Physics
{
    public static class LagrangianPhysics
    {
        public const double DefaultGravity = 9.8;
        public const double FixedDeltaTime = 1.0 / 120.0; // RF-08 & Section 4: dt = 1/120 s

        /// <summary>
        /// Computes the planar accelerations (ddx, ddy) and normal reaction parameter (lambda / m)
        /// for a frictionless particle constrained to z = f(x, y) via the Euler-Lagrange equations:
        /// L = 1/2 m (vx^2 + vy^2 + vz^2) - m g z, with vz = fx * vx + fy * vy.
        /// Induced metric: g_ij = delta_ij + f_i * f_j.
        /// </summary>
        public static void ComputeSurfaceAcceleration(
            in SurfaceDerivatives d,
            double vx,
            double vy,
            double gravity,
            out double ax,
            out double ay,
            out double az,
            out double normalForceOverMass)
        {
            if (!d.IsDefined)
            {
                ax = 0.0;
                ay = 0.0;
                az = -gravity;
                normalForceOverMass = 0.0;
                return;
            }

            InducedMetric2D metric = new InducedMetric2D(d.Fx, d.Fy);

            // Second fundamental form velocity contraction:
            // kappa_v = f_xx * vx^2 + 2 * f_xy * vx * vy + f_yy * vy^2
            double convectiveCurvature = d.Fxx * vx * vx + 2.0 * d.Fxy * vx * vy + d.Fyy * vy * vy;

            // Lagrange multiplier / m = (g + kappa_v) / (1 + fx^2 + fy^2)
            double lambdaCoeff = (gravity + convectiveCurvature) / metric.Determinant;

            ax = -lambdaCoeff * d.Fx;
            ay = -lambdaCoeff * d.Fy;
            az = d.Fx * ax + d.Fy * ay + convectiveCurvature;

            // Physical normal reaction magnitude N/m = (g + kappa_v) / sqrt(1 + fx^2 + fy^2)
            normalForceOverMass = (gravity + convectiveCurvature) / Math.Sqrt(metric.Determinant);
        }

        /// <summary>
        /// Computes the Lagrangian L = T - V per unit mass.
        /// </summary>
        public static double ComputeLagrangianPerUnitMass(
            in SurfaceDerivatives d,
            double vx,
            double vy,
            double gravity)
        {
            double vz = d.Fx * vx + d.Fy * vy;
            double kinetic = 0.5 * (vx * vx + vy * vy + vz * vz);
            double potential = gravity * d.Z;
            return kinetic - potential;
        }

        /// <summary>
        /// Computes total mechanical energy E = T + V per unit mass.
        /// Conserved on any frictionless static surface z = f(x, y) in absence of inelastic impacts.
        /// </summary>
        public static double ComputeTotalEnergyPerUnitMass(
            double z,
            double vx,
            double vy,
            double vz,
            double gravity)
        {
            double kinetic = 0.5 * (vx * vx + vy * vy + vz * vz);
            double potential = gravity * z;
            return kinetic + potential;
        }

        /// <summary>
        /// RF-10 Objective Containment Check:
        /// The objective B has radius r_t = 0.9 * r_bola.
        /// The ball completely contains the objective iff |p_bola - p_B| + r_t <= r_bola,
        /// i.e., |p_bola - p_B| <= r_bola - r_t = 0.1 * r_bola.
        /// </summary>
        public static bool DoesBallContainObjective(
            double ballX, double ballY, double ballZ, double ballRadius,
            double targetX, double targetY, double targetZ, double targetRadius)
        {
            double dx = ballX - targetX;
            double dy = ballY - targetY;
            double dz = ballZ - targetZ;
            double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            return dist + targetRadius <= ballRadius + 1e-6;
        }

        /// <summary>
        /// RF-09 Star Pickup Check:
        /// Checks if the ball touches a star at (starX, starY, starZ) with pickup radius.
        /// </summary>
        public static bool DoesBallTouchStar(
            double ballX, double ballY, double ballZ, double ballRadius,
            double starX, double starY, double starZ, double starRadius)
        {
            double dx = ballX - starX;
            double dy = ballY - starY;
            double dz = ballZ - starZ;
            double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            return dist <= (ballRadius + starRadius);
        }
    }
}
