using System;
using EulersFuneral.Core.MathParser;

namespace EulersFuneral.Core.Physics
{
    public struct StepResult
    {
        public BallState State;
        public bool DidBounce;
        public double BounceImpactSpeed;
        public bool FellIntoVoid;
        public bool OutOfDomain;
    }

    public sealed class RK4Integrator
    {
        public double Gravity { get; set; } = LagrangianPhysics.DefaultGravity;
        public double Restitution { get; set; } = 0.0;
        public double BallRadius { get; set; } = 0.25;

        // Domain bounds
        public double MinX { get; set; } = -5.0;
        public double MaxX { get; set; } = 5.0;
        public double MinY { get; set; } = -5.0;
        public double MaxY { get; set; } = 5.0;
        public double MinZVoid { get; set; } = -20.0;

        // Ceiling configuration (RF-14)
        public bool HasCeiling { get; set; } = false;
        public double CeilingHeight { get; set; } = 5.0;
        public MathSurfaceFunction CeilingFunction { get; set; } = null;

        public StepResult Step(
            in BallState current,
            MathSurfaceFunction surface,
            double dt = LagrangianPhysics.FixedDeltaTime)
        {
            var result = new StepResult
            {
                State = current,
                DidBounce = false,
                BounceImpactSpeed = 0.0,
                FellIntoVoid = false,
                OutOfDomain = false
            };

            if (surface == null)
            {
                result.FellIntoVoid = true;
                return result;
            }

            bool detached = false;
            bool hitUndefined = false;
            BallState next = current.IsAirborne
                ? StepAirborneRK4(in current, dt)
                : StepSurfaceRK4(in current, surface, dt, out detached, out hitUndefined);

            if (!current.IsAirborne)
            {
                if (hitUndefined)
                {
                    result.State = next;
                    result.FellIntoVoid = true;
                    return result;
                }

                // Check step discontinuity (floor / ceil jumps) using second difference across the step midpoint
                // so steep smooth slopes don't falsely trigger a jump while real step edges always do.
                double zOld = current.Z;
                double zNew = surface.Evaluate(next.X, next.Y);

                if (double.IsNaN(zNew))
                {
                    result.State = next;
                    result.FellIntoVoid = true;
                    return result;
                }

                double dzStep = zNew - zOld;
                bool crossedStepJump = false;
                if (surface.HasDiscontinuousNodes && Math.Abs(dzStep) > MathSurfaceFunction.StepDiscontinuityThreshold)
                {
                    double zMid = surface.Evaluate(0.5 * (current.X + next.X), 0.5 * (current.Y + next.Y));
                    crossedStepJump = !double.IsNaN(zMid) &&
                        Math.Abs(zNew - 2.0 * zMid + zOld) > MathSurfaceFunction.StepDiscontinuityThreshold;
                }

                if (crossedStepJump)
                {
                    if (dzStep > 0.0)
                    {
                        // Rising step wall: ball hits the vertical riser of a floor/ceil step
                        double impactSpeed = Math.Sqrt(current.Vx * current.Vx + current.Vy * current.Vy);
                        double e = Math.Max(0.2, Restitution);
                        next.X = current.X;
                        next.Y = current.Y;
                        next.Z = zOld;
                        next.Vx = -current.Vx * e;
                        next.Vy = -current.Vy * e;
                        next.Vz = 0.0;
                        result.DidBounce = impactSpeed > 0.05;
                        result.BounceImpactSpeed = impactSpeed;
                    }
                    else
                    {
                        // Dropping off a step ledge: enter ballistic flight from the upper terrace
                        next.Z = zOld;
                        next.Vz = Math.Min(0.0, current.Vz);
                        next.IsAirborne = true;
                    }
                }
                else if (detached && Restitution > 0.0)
                {
                    next.IsAirborne = true;
                }
            }
            else
            {
                // Airborne: check landing on surface z = f(x, y)
                double surfaceZ = surface.Evaluate(next.X, next.Y);
                if (double.IsNaN(surfaceZ))
                {
                    if (next.Z < MinZVoid)
                    {
                        result.State = next;
                        result.FellIntoVoid = true;
                        return result;
                    }
                }
                else if (next.Z <= surfaceZ)
                {
                    SurfaceDerivatives d = surface.EvaluateWithDerivatives(next.X, next.Y);
                    next.Z = surfaceZ;

                    // Compute surface unit normal n = (-fx, -fy, 1) / sqrt(1 + fx^2 + fy^2)
                    double norm = Math.Sqrt(1.0 + d.Fx * d.Fx + d.Fy * d.Fy);
                    double nx = -d.Fx / norm;
                    double ny = -d.Fy / norm;
                    double nz = 1.0 / norm;

                    double vn = next.Vx * nx + next.Vy * ny + next.Vz * nz;
                    if (vn < -0.15 && Restitution > 0.01)
                    {
                        // Bounce off surface with restitution
                        double factor = (1.0 + Restitution) * vn;
                        next.Vx -= factor * nx;
                        next.Vy -= factor * ny;
                        next.Vz -= factor * nz;
                        next.IsAirborne = true;
                        result.DidBounce = true;
                        result.BounceImpactSpeed = -vn;
                    }
                    else
                    {
                        // Re-attach smoothly to surface
                        double vtX = next.Vx - vn * nx;
                        double vtY = next.Vy - vn * ny;
                        next.Vx = vtX;
                        next.Vy = vtY;
                        next.Vz = d.Fx * next.Vx + d.Fy * next.Vy;
                        next.IsAirborne = false;
                        if (vn < -0.08 && Restitution > 0.0)
                        {
                            result.DidBounce = true;
                            result.BounceImpactSpeed = -vn;
                        }
                    }
                }
            }

            // Check Ceiling collision (RF-14)
            if (HasCeiling)
            {
                double ceilZ = CeilingFunction != null
                    ? CeilingFunction.Evaluate(next.X, next.Y)
                    : CeilingHeight;

                if (!double.IsNaN(ceilZ) && next.Z + BallRadius >= ceilZ)
                {
                    double e = Math.Max(0.4, Restitution);
                    double impactSpeed = Math.Abs(next.Vz) + 0.5 * Math.Sqrt(next.Vx * next.Vx + next.Vy * next.Vy);
                    next.Z = ceilZ - BallRadius;

                    if (next.IsAirborne)
                    {
                        if (next.Vz > 0)
                            next.Vz = -next.Vz * e;
                    }
                    else
                    {
                        // Confined between surface and ceiling: reflect planar velocity only if entering the wedge
                        // from a valid region; if already spawned above the ceiling, let it slide down out of the wedge.
                        SurfaceDerivatives d = surface.EvaluateWithDerivatives(next.X, next.Y);
                        double oldCeilZ = CeilingFunction != null
                            ? CeilingFunction.Evaluate(current.X, current.Y)
                            : CeilingHeight;

                        if (d.Z + BallRadius >= ceilZ && (double.IsNaN(oldCeilZ) || current.Z + BallRadius < oldCeilZ))
                        {
                            next.X = current.X;
                            next.Y = current.Y;
                            next.Z = current.Z;
                            next.Vx = -current.Vx * e;
                            next.Vy = -current.Vy * e;
                            next.Vz = d.Fx * next.Vx + d.Fy * next.Vy;
                        }
                    }

                    result.DidBounce = impactSpeed > 0.05;
                    result.BounceImpactSpeed = impactSpeed;
                }
            }

            // Domain bounds check (RF-11)
            if (next.X < MinX || next.X > MaxX || next.Y < MinY || next.Y > MaxY)
            {
                result.State = next;
                result.OutOfDomain = true;
                return result;
            }

            if (next.Z < MinZVoid || double.IsNaN(next.Z))
            {
                result.State = next;
                result.FellIntoVoid = true;
                return result;
            }

            result.State = next;
            return result;
        }

        private BallState StepSurfaceRK4(
            in BallState s,
            MathSurfaceFunction surface,
            double dt,
            out bool detached,
            out bool hitUndefined)
        {
            detached = false;
            hitUndefined = false;

            // k1
            SurfaceDerivatives d1 = surface.EvaluateWithDerivatives(s.X, s.Y);
            if (!d1.IsDefined)
            {
                hitUndefined = true;
                return s;
            }
            LagrangianPhysics.ComputeSurfaceAcceleration(in d1, s.Vx, s.Vy, Gravity, out double ax1, out double ay1, out _, out double n1);
            if (n1 < -0.5) detached = true;

            // k2
            double x2 = s.X + 0.5 * dt * s.Vx;
            double y2 = s.Y + 0.5 * dt * s.Vy;
            double vx2 = s.Vx + 0.5 * dt * ax1;
            double vy2 = s.Vy + 0.5 * dt * ay1;
            SurfaceDerivatives d2 = surface.EvaluateWithDerivatives(x2, y2);
            if (!d2.IsDefined)
            {
                hitUndefined = true;
                return s;
            }
            LagrangianPhysics.ComputeSurfaceAcceleration(in d2, vx2, vy2, Gravity, out double ax2, out double ay2, out _, out _);

            // k3
            double x3 = s.X + 0.5 * dt * vx2;
            double y3 = s.Y + 0.5 * dt * vy2;
            double vx3 = s.Vx + 0.5 * dt * ax2;
            double vy3 = s.Vy + 0.5 * dt * ay2;
            SurfaceDerivatives d3 = surface.EvaluateWithDerivatives(x3, y3);
            if (!d3.IsDefined)
            {
                hitUndefined = true;
                return s;
            }
            LagrangianPhysics.ComputeSurfaceAcceleration(in d3, vx3, vy3, Gravity, out double ax3, out double ay3, out _, out _);

            // k4
            double x4 = s.X + dt * vx3;
            double y4 = s.Y + dt * vy3;
            double vx4 = s.Vx + dt * ax3;
            double vy4 = s.Vy + dt * ay3;
            SurfaceDerivatives d4 = surface.EvaluateWithDerivatives(x4, y4);
            if (!d4.IsDefined)
            {
                hitUndefined = true;
                return s;
            }
            LagrangianPhysics.ComputeSurfaceAcceleration(in d4, vx4, vy4, Gravity, out double ax4, out double ay4, out _, out _);

            double nextX = s.X + (dt / 6.0) * (s.Vx + 2.0 * vx2 + 2.0 * vx3 + vx4);
            double nextY = s.Y + (dt / 6.0) * (s.Vy + 2.0 * vy2 + 2.0 * vy3 + vy4);
            double nextVx = s.Vx + (dt / 6.0) * (ax1 + 2.0 * ax2 + 2.0 * ax3 + ax4);
            double nextVy = s.Vy + (dt / 6.0) * (ay1 + 2.0 * ay2 + 2.0 * ay3 + ay4);

            SurfaceDerivatives dFinal = surface.EvaluateWithDerivatives(nextX, nextY);
            if (!dFinal.IsDefined)
            {
                hitUndefined = true;
                return s;
            }

            double nextZ = dFinal.Z;
            double nextVz = dFinal.Fx * nextVx + dFinal.Fy * nextVy;

            return new BallState
            {
                X = nextX,
                Y = nextY,
                Z = nextZ,
                Vx = nextVx,
                Vy = nextVy,
                Vz = nextVz,
                IsAirborne = false,
                ElapsedTime = s.ElapsedTime + dt
            };
        }

        private BallState StepAirborneRK4(in BallState s, double dt)
        {
            // Exact ballistic integration under constant gravity (0, 0, -Gravity),
            // which coincides identically with RK4 for constant acceleration.
            double nextX = s.X + s.Vx * dt;
            double nextY = s.Y + s.Vy * dt;
            double nextZ = s.Z + s.Vz * dt - 0.5 * Gravity * dt * dt;
            double nextVz = s.Vz - Gravity * dt;

            return new BallState
            {
                X = nextX,
                Y = nextY,
                Z = nextZ,
                Vx = s.Vx,
                Vy = s.Vy,
                Vz = nextVz,
                IsAirborne = true,
                ElapsedTime = s.ElapsedTime + dt
            };
        }
    }
}
