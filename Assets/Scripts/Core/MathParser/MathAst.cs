using System;

namespace EulersFuneral.Core.MathParser
{
    public enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power
    }

    public enum MathFunction
    {
        Sen,
        Cos,
        Tan,
        Exp,
        Ln,
        Sqrt,
        Abs,
        Floor,
        Ceil
    }

    public abstract class AstNode
    {
        public abstract double Evaluate(double x, double y);
        public abstract AstNode Differentiate(char variable);
        public abstract bool ContainsDiscontinuity();
    }

    public sealed class NumberNode : AstNode
    {
        public static readonly NumberNode Zero = new NumberNode(0.0);
        public static readonly NumberNode One = new NumberNode(1.0);

        public double Value { get; }

        public NumberNode(double value)
        {
            Value = value;
        }

        public override double Evaluate(double x, double y) => Value;

        public override AstNode Differentiate(char variable) => Zero;

        public override bool ContainsDiscontinuity() => false;

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public sealed class VariableNode : AstNode
    {
        public char Name { get; }

        public VariableNode(char name)
        {
            Name = char.ToLowerInvariant(name);
        }

        public override double Evaluate(double x, double y) => Name == 'x' ? x : y;

        public override AstNode Differentiate(char variable) =>
            Name == char.ToLowerInvariant(variable) ? NumberNode.One : NumberNode.Zero;

        public override bool ContainsDiscontinuity() => false;

        public override string ToString() => Name.ToString();
    }

    public sealed class UnaryMinusNode : AstNode
    {
        public AstNode Operand { get; }

        public UnaryMinusNode(AstNode operand)
        {
            Operand = operand;
        }

        public override double Evaluate(double x, double y) => -Operand.Evaluate(x, y);

        public override AstNode Differentiate(char variable) =>
            new UnaryMinusNode(Operand.Differentiate(variable));

        public override bool ContainsDiscontinuity() => Operand.ContainsDiscontinuity();

        public override string ToString() => $"-({Operand})";
    }

    public sealed class BinaryOpNode : AstNode
    {
        public BinaryOperator Operator { get; }
        public AstNode Left { get; }
        public AstNode Right { get; }

        public BinaryOpNode(BinaryOperator op, AstNode left, AstNode right)
        {
            Operator = op;
            Left = left;
            Right = right;
        }

        public override double Evaluate(double x, double y)
        {
            double l = Left.Evaluate(x, y);
            if (double.IsNaN(l) || double.IsInfinity(l)) return double.NaN;

            double r = Right.Evaluate(x, y);
            if (double.IsNaN(r) || double.IsInfinity(r)) return double.NaN;

            switch (Operator)
            {
                case BinaryOperator.Add:
                    return l + r;
                case BinaryOperator.Subtract:
                    return l - r;
                case BinaryOperator.Multiply:
                    return l * r;
                case BinaryOperator.Divide:
                    if (Math.Abs(r) < 1e-12) return double.NaN;
                    return l / r;
                case BinaryOperator.Power:
                    // Handle e^u directly with Math.Exp for maximum accuracy
                    if (Left is NumberNode numLeft && Math.Abs(numLeft.Value - Math.E) < 1e-12)
                    {
                        return Math.Exp(r);
                    }
                    if (l < 0.0 && Math.Abs(r - Math.Round(r)) > 1e-9)
                    {
                        return double.NaN; // Fractional power of negative number
                    }
                    if (Math.Abs(l) < 1e-12 && r < 0.0)
                    {
                        return double.NaN; // Division by zero in negative power
                    }
                    double res = Math.Pow(l, r);
                    return double.IsInfinity(res) ? double.NaN : res;
                default:
                    return double.NaN;
            }
        }

        public override AstNode Differentiate(char variable)
        {
            AstNode dl = Left.Differentiate(variable);
            AstNode dr = Right.Differentiate(variable);

            switch (Operator)
            {
                case BinaryOperator.Add:
                    return new BinaryOpNode(BinaryOperator.Add, dl, dr);
                case BinaryOperator.Subtract:
                    return new BinaryOpNode(BinaryOperator.Subtract, dl, dr);
                case BinaryOperator.Multiply:
                    // Product rule: u'v + uv'
                    return new BinaryOpNode(
                        BinaryOperator.Add,
                        new BinaryOpNode(BinaryOperator.Multiply, dl, Right),
                        new BinaryOpNode(BinaryOperator.Multiply, Left, dr)
                    );
                case BinaryOperator.Divide:
                    // Quotient rule: (u'v - uv') / v^2
                    return new BinaryOpNode(
                        BinaryOperator.Divide,
                        new BinaryOpNode(
                            BinaryOperator.Subtract,
                            new BinaryOpNode(BinaryOperator.Multiply, dl, Right),
                            new BinaryOpNode(BinaryOperator.Multiply, Left, dr)
                        ),
                        new BinaryOpNode(BinaryOperator.Power, Right, new NumberNode(2.0))
                    );
                case BinaryOperator.Power:
                    // Special case: e^u -> e^u * u'
                    if (Left is NumberNode nl && Math.Abs(nl.Value - Math.E) < 1e-12)
                    {
                        return new BinaryOpNode(
                            BinaryOperator.Multiply,
                            new FunctionCallNode(MathFunction.Exp, Right),
                            dr
                        );
                    }
                    // Constant exponent: u^c -> c * u^(c-1) * u'
                    if (Right is NumberNode nr)
                    {
                        return new BinaryOpNode(
                            BinaryOperator.Multiply,
                            new BinaryOpNode(
                                BinaryOperator.Multiply,
                                new NumberNode(nr.Value),
                                new BinaryOpNode(BinaryOperator.Power, Left, new NumberNode(nr.Value - 1.0))
                            ),
                            dl
                        );
                    }
                    // General power rule: d(u^v) = u^v * (v' * ln(u) + v * u'/u)
                    return new BinaryOpNode(
                        BinaryOperator.Multiply,
                        this,
                        new BinaryOpNode(
                            BinaryOperator.Add,
                            new BinaryOpNode(BinaryOperator.Multiply, dr, new FunctionCallNode(MathFunction.Ln, Left)),
                            new BinaryOpNode(
                                BinaryOperator.Multiply,
                                Right,
                                new BinaryOpNode(BinaryOperator.Divide, dl, Left)
                            )
                        )
                    );
                default:
                    return NumberNode.Zero;
            }
        }

        public override bool ContainsDiscontinuity() =>
            Left.ContainsDiscontinuity() || Right.ContainsDiscontinuity() || Operator == BinaryOperator.Divide;
    }

    public sealed class FunctionCallNode : AstNode
    {
        public MathFunction Function { get; }
        public AstNode Argument { get; }

        public FunctionCallNode(MathFunction function, AstNode argument)
        {
            Function = function;
            Argument = argument;
        }

        public override double Evaluate(double x, double y)
        {
            double arg = Argument.Evaluate(x, y);
            if (double.IsNaN(arg) || double.IsInfinity(arg)) return double.NaN;

            switch (Function)
            {
                case MathFunction.Sen:
                    return Math.Sin(arg);
                case MathFunction.Cos:
                    return Math.Cos(arg);
                case MathFunction.Tan:
                    double cos = Math.Cos(arg);
                    if (Math.Abs(cos) < 1e-9) return double.NaN;
                    return Math.Tan(arg);
                case MathFunction.Exp:
                    double exp = Math.Exp(arg);
                    return double.IsInfinity(exp) ? double.NaN : exp;
                case MathFunction.Ln:
                    if (arg <= 0.0) return double.NaN;
                    return Math.Log(arg);
                case MathFunction.Sqrt:
                    if (arg < 0.0) return double.NaN;
                    return Math.Sqrt(arg);
                case MathFunction.Abs:
                    return Math.Abs(arg);
                case MathFunction.Floor:
                    return Math.Floor(arg);
                case MathFunction.Ceil:
                    return Math.Ceiling(arg);
                default:
                    return double.NaN;
            }
        }

        public override AstNode Differentiate(char variable)
        {
            AstNode du = Argument.Differentiate(variable);

            switch (Function)
            {
                case MathFunction.Sen:
                    // d(sen(u)) = cos(u) * u'
                    return new BinaryOpNode(
                        BinaryOperator.Multiply,
                        new FunctionCallNode(MathFunction.Cos, Argument),
                        du
                    );
                case MathFunction.Cos:
                    // d(cos(u)) = -sen(u) * u'
                    return new UnaryMinusNode(
                        new BinaryOpNode(
                            BinaryOperator.Multiply,
                            new FunctionCallNode(MathFunction.Sen, Argument),
                            du
                        )
                    );
                case MathFunction.Tan:
                    // d(tan(u)) = (1 / cos(u)^2) * u'
                    return new BinaryOpNode(
                        BinaryOperator.Divide,
                        du,
                        new BinaryOpNode(
                            BinaryOperator.Power,
                            new FunctionCallNode(MathFunction.Cos, Argument),
                            new NumberNode(2.0)
                        )
                    );
                case MathFunction.Exp:
                    // d(exp(u)) = exp(u) * u'
                    return new BinaryOpNode(
                        BinaryOperator.Multiply,
                        new FunctionCallNode(MathFunction.Exp, Argument),
                        du
                    );
                case MathFunction.Ln:
                    // d(ln(u)) = u' / u
                    return new BinaryOpNode(BinaryOperator.Divide, du, Argument);
                case MathFunction.Sqrt:
                    // d(sqrt(u)) = u' / (2 * sqrt(u))
                    return new BinaryOpNode(
                        BinaryOperator.Divide,
                        du,
                        new BinaryOpNode(
                            BinaryOperator.Multiply,
                            new NumberNode(2.0),
                            new FunctionCallNode(MathFunction.Sqrt, Argument)
                        )
                    );
                case MathFunction.Abs:
                    // u / |u| * u'
                    return new BinaryOpNode(
                        BinaryOperator.Multiply,
                        new BinaryOpNode(BinaryOperator.Divide, Argument, new FunctionCallNode(MathFunction.Abs, Argument)),
                        du
                    );
                case MathFunction.Floor:
                case MathFunction.Ceil:
                    // Piecewise constant except at jump discontinuities
                    return NumberNode.Zero;
                default:
                    return NumberNode.Zero;
            }
        }

        public override bool ContainsDiscontinuity()
        {
            if (Function == MathFunction.Floor || Function == MathFunction.Ceil || Function == MathFunction.Tan)
                return true;
            return Argument.ContainsDiscontinuity();
        }
    }
}
