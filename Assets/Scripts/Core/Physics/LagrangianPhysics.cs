using System;
using EulersFuneral.Core.MathParser;

namespace EulersFuneral.Core.Physics
{
    public static class LagrangianPhysics
    {
        public const double DefaultGravity = 9.8;
        public const double FixedDeltaTime = 1.0 / 120.0; // RF-08 & Section 4: dt = 1/120 s

        /// <summary>
        /// Calcula las aceleraciones planares (ddx, ddy) y la reacción normal (lambda / m)
        /// de una partícula sin fricción restringida a la superficie z = f(x, y) mediante
        /// las ecuaciones de Euler-Lagrange:
        /// L = 1/2 m (vx^2 + vy^2 + vz^2) - m g z, con vz = fx * vx + fy * vy.
        /// Métrica inducida: g_ij = delta_ij + f_i * f_j.
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

            // Contracción de velocidad con la segunda forma fundamental (curvatura convectiva):
            // kappa_v = f_xx * vx^2 + 2 * f_xy * vx * vy + f_yy * vy^2
            double convectiveCurvature = d.Fxx * vx * vx + 2.0 * d.Fxy * vx * vy + d.Fyy * vy * vy;

            // Multiplicador de Lagrange / m = (g + kappa_v) / (1 + fx^2 + fy^2)
            double lambdaCoeff = (gravity + convectiveCurvature) / metric.Determinant;

            ax = -lambdaCoeff * d.Fx;
            ay = -lambdaCoeff * d.Fy;
            az = d.Fx * ax + d.Fy * ay + convectiveCurvature;

            // Magnitud de la fuerza normal física N/m = (g + kappa_v) / sqrt(1 + fx^2 + fy^2)
            normalForceOverMass = (gravity + convectiveCurvature) / Math.Sqrt(metric.Determinant);
        }

        /// <summary>
        /// RF-09: Verifica si la pelota entra en contacto esférico con una estrella en (starX, starY, starZ).
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
