using System;

namespace EulersFuneral.Core.Physics
{
    public enum SimulationStatus
    {
        Ready,
        Running,
        Victory,
        DefeatOutOfBounds,
        DefeatFellIntoVoid,
        DefeatTimeOut
    }

    [Serializable]
    public struct BallState
    {
        public double X;
        public double Y;
        public double Z;
        public double Vx;
        public double Vy;
        public double Vz;
        public bool IsAirborne;
        public double ElapsedTime;

        public double SpeedSquared => Vx * Vx + Vy * Vy + Vz * Vz;
        public double Speed => Math.Sqrt(SpeedSquared);

        public static BallState CreateOnSurface(double x, double y, double z)
        {
            return new BallState
            {
                X = x,
                Y = y,
                Z = z,
                Vx = 0.0,
                Vy = 0.0,
                Vz = 0.0,
                IsAirborne = false,
                ElapsedTime = 0.0
            };
        }

        public static BallState Lerp(in BallState a, in BallState b, double alpha)
        {
            alpha = Math.Clamp(alpha, 0.0, 1.0);
            return new BallState
            {
                X = a.X + (b.X - a.X) * alpha,
                Y = a.Y + (b.Y - a.Y) * alpha,
                Z = a.Z + (b.Z - a.Z) * alpha,
                Vx = a.Vx + (b.Vx - a.Vx) * alpha,
                Vy = a.Vy + (b.Vy - a.Vy) * alpha,
                Vz = a.Vz + (b.Vz - a.Vz) * alpha,
                IsAirborne = b.IsAirborne,
                ElapsedTime = a.ElapsedTime + (b.ElapsedTime - a.ElapsedTime) * alpha
            };
        }
    }

    public readonly struct InducedMetric2D
    {
        public readonly double G11;
        public readonly double G12;
        public readonly double G22;
        public readonly double Determinant;

        public InducedMetric2D(double fx, double fy)
        {
            // g_ij = delta_ij + f_i * f_j
            G11 = 1.0 + fx * fx;
            G12 = fx * fy;
            G22 = 1.0 + fy * fy;
            Determinant = 1.0 + fx * fx + fy * fy;
        }
    }
}
