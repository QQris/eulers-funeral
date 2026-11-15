using System;

namespace EulersFuneral.Core.MathParser
{
    public readonly struct SurfaceDerivatives
    {
        public readonly double Z;
        public readonly double Fx;
        public readonly double Fy;
        public readonly double Fxx;
        public readonly double Fxy;
        public readonly double Fyy;
        public readonly bool IsDefined;
        public readonly bool HasStepDiscontinuity;

        public SurfaceDerivatives(
            double z, double fx, double fy,
            double fxx, double fxy, double fyy,
            bool isDefined, bool hasStepDiscontinuity)
        {
            Z = z;
            Fx = fx;
            Fy = fy;
            Fxx = fxx;
            Fxy = fxy;
            Fyy = fyy;
            IsDefined = isDefined;
            HasStepDiscontinuity = hasStepDiscontinuity;
        }

        public double GradientMagnitude => Math.Sqrt(Fx * Fx + Fy * Fy);
    }

    public sealed class MathSurfaceFunction
    {
        public const double FiniteDifferenceStep = 1e-4;
        public const double StepDiscontinuityThreshold = 0.02;
        public const double MacroscopicStepProbe = 0.75;

        public string ExpressionText { get; }
        public AstNode Root { get; }
        public AstNode SymbolicFx { get; }
        public AstNode SymbolicFy { get; }
        public AstNode SymbolicFxx { get; }
        public AstNode SymbolicFxy { get; }
        public AstNode SymbolicFyy { get; }
        public bool HasDiscontinuousNodes { get; }
        public bool UseSymbolicDerivatives { get; set; }

        public MathSurfaceFunction(string expressionText, bool useSymbolicDerivatives = true)
        {
            ExpressionText = expressionText;
            Root = MathParser.Parse(expressionText);
            HasDiscontinuousNodes = Root.ContainsDiscontinuity();
            UseSymbolicDerivatives = useSymbolicDerivatives;

            SymbolicFx = MathParser.Simplify(Root.Differentiate('x'));
            SymbolicFy = MathParser.Simplify(Root.Differentiate('y'));
            SymbolicFxx = MathParser.Simplify(SymbolicFx.Differentiate('x'));
            SymbolicFxy = MathParser.Simplify(SymbolicFx.Differentiate('y'));
            SymbolicFyy = MathParser.Simplify(SymbolicFy.Differentiate('y'));
        }

        public double Evaluate(double x, double y)
        {
            double z = Root.Evaluate(x, y);
            return (double.IsNaN(z) || double.IsInfinity(z)) ? double.NaN : z;
        }

        public SurfaceDerivatives EvaluateWithDerivatives(double x, double y)
        {
            double z = Evaluate(x, y);
            if (double.IsNaN(z))
            {
                return new SurfaceDerivatives(double.NaN, 0, 0, 0, 0, 0, false, false);
            }

            double h = FiniteDifferenceStep;
            double zxPlus = Evaluate(x + h, y);
            double zxMinus = Evaluate(x - h, y);
            double zyPlus = Evaluate(x, y + h);
            double zyMinus = Evaluate(x, y - h);

            if (double.IsNaN(zxPlus) || double.IsNaN(zxMinus) ||
                double.IsNaN(zyPlus) || double.IsNaN(zyMinus))
            {
                return new SurfaceDerivatives(z, 0, 0, 0, 0, 0, false, false);
            }

            // Detect jump discontinuities (floor, ceil)
            bool stepX = Math.Abs(zxPlus - zxMinus) > StepDiscontinuityThreshold;
            bool stepY = Math.Abs(zyPlus - zyMinus) > StepDiscontinuityThreshold;
            bool hasStep = HasDiscontinuousNodes && (stepX || stepY);

            if (hasStep)
            {
                // On a step boundary of floor/ceil, use macroscopic slope across the step
                // so the ball rolls naturally off downward steps instead of freezing on a 1/2h spike.
                double hm = MacroscopicStepProbe;
                double zmXPlus = Evaluate(x + hm, y);
                double zmXMinus = Evaluate(x - hm, y);
                double zmYPlus = Evaluate(x, y + hm);
                double zmYMinus = Evaluate(x, y - hm);

                double macroFx = (!double.IsNaN(zmXPlus) && !double.IsNaN(zmXMinus))
                    ? (zmXPlus - zmXMinus) / (2.0 * hm)
                    : 0.0;
                double macroFy = (!double.IsNaN(zmYPlus) && !double.IsNaN(zmYMinus))
                    ? (zmYPlus - zmYMinus) / (2.0 * hm)
                    : 0.0;

                return new SurfaceDerivatives(z, macroFx, macroFy, 0, 0, 0, true, true);
            }

            if (UseSymbolicDerivatives && !HasDiscontinuousNodes)
            {
                double fx = SymbolicFx.Evaluate(x, y);
                double fy = SymbolicFy.Evaluate(x, y);
                double fxx = SymbolicFxx.Evaluate(x, y);
                double fxy = SymbolicFxy.Evaluate(x, y);
                double fyy = SymbolicFyy.Evaluate(x, y);

                if (!double.IsNaN(fx) && !double.IsNaN(fy) &&
                    !double.IsNaN(fxx) && !double.IsNaN(fxy) && !double.IsNaN(fyy))
                {
                    return new SurfaceDerivatives(z, fx, fy, fxx, fxy, fyy, true, false);
                }
            }

            // Central finite differences (h = 1e-4)
            double fdFx = (zxPlus - zxMinus) / (2.0 * h);
            double fdFy = (zyPlus - zyMinus) / (2.0 * h);
            double fdFxx = (zxPlus - 2.0 * z + zxMinus) / (h * h);
            double fdFyy = (zyPlus - 2.0 * z + zyMinus) / (h * h);

            double zpp = Evaluate(x + h, y + h);
            double zpm = Evaluate(x + h, y - h);
            double zmp = Evaluate(x - h, y + h);
            double zmm = Evaluate(x - h, y - h);

            double fdFxy = 0.0;
            if (!double.IsNaN(zpp) && !double.IsNaN(zpm) && !double.IsNaN(zmp) && !double.IsNaN(zmm))
            {
                fdFxy = (zpp - zpm - zmp + zmm) / (4.0 * h * h);
            }

            // Si la función contiene floor/ceil y el escalón local es completamente horizontal (gradiente ~ 0),
            // añadir una suave inclinación macroscópica en la dirección descendente de la escalera
            // para que la pelota no se quede estancada en una terraza plana.
            if (HasDiscontinuousNodes)
            {
                if (Math.Abs(fdFxx) > 200.0) fdFxx = 0.0;
                if (Math.Abs(fdFyy) > 200.0) fdFyy = 0.0;
                if (Math.Abs(fdFxy) > 200.0) fdFxy = 0.0;

                if (Math.Abs(fdFx) < 1e-4 || Math.Abs(fdFy) < 1e-4)
                {
                    double hm = MacroscopicStepProbe;
                    if (Math.Abs(fdFx) < 1e-4)
                    {
                        double zmXPlus = Evaluate(x + hm, y);
                        double zmXMinus = Evaluate(x - hm, y);
                        if (!double.IsNaN(zmXPlus) && !double.IsNaN(zmXMinus))
                        {
                            fdFx = 0.25 * (zmXPlus - zmXMinus) / (2.0 * hm);
                        }
                    }
                    if (Math.Abs(fdFy) < 1e-4)
                    {
                        double zmYPlus = Evaluate(x, y + hm);
                        double zmYMinus = Evaluate(x, y - hm);
                        if (!double.IsNaN(zmYPlus) && !double.IsNaN(zmYMinus))
                        {
                            fdFy = 0.25 * (zmYPlus - zmYMinus) / (2.0 * hm);
                        }
                    }
                }
            }

            return new SurfaceDerivatives(z, fdFx, fdFy, fdFxx, fdFxy, fdFyy, true, false);
        }
    }
}
