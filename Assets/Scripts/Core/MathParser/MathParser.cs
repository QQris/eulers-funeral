using System;
using System.Collections.Generic;

namespace EulersFuneral.Core.MathParser
{
    public sealed class MathParser
    {
        private readonly List<Token> _tokens;
        private int _pos;

        private MathParser(List<Token> tokens)
        {
            _tokens = tokens;
            _pos = 0;
        }

        public static AstNode Parse(string expression)
        {
            List<Token> tokens = MathTokenizer.Tokenize(expression);
            var parser = new MathParser(tokens);
            AstNode node = parser.ParseExpression();

            if (parser.Current.Type != TokenType.EndOfFile)
            {
                throw new MathParseException(
                    $"Token inesperado '{parser.Current.Text}' en la posición {parser.Current.Position}.",
                    parser.Current.Position);
            }

            return Simplify(node);
        }

        public static bool TryParse(string expression, out AstNode ast, out string errorMessage)
        {
            try
            {
                ast = Parse(expression);
                errorMessage = string.Empty;
                return true;
            }
            catch (MathParseException ex)
            {
                ast = null;
                errorMessage = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                ast = null;
                errorMessage = $"Error de sintaxis: {ex.Message}";
                return false;
            }
        }

        private Token Current => _pos < _tokens.Count ? _tokens[_pos] : _tokens[_tokens.Count - 1];

        private Token Consume()
        {
            Token t = Current;
            if (_pos < _tokens.Count - 1)
                _pos++;
            return t;
        }

        // Expression -> Term (( '+' | '-' ) Term)*
        private AstNode ParseExpression()
        {
            AstNode left = ParseTerm();

            while (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
            {
                Token opToken = Consume();
                BinaryOperator op = opToken.Type == TokenType.Plus
                    ? BinaryOperator.Add
                    : BinaryOperator.Subtract;
                AstNode right = ParseTerm();
                left = new BinaryOpNode(op, left, right);
            }

            return left;
        }

        // Term -> Unary (( '*' | '/' ) Unary)*
        private AstNode ParseTerm()
        {
            AstNode left = ParseUnary();

            while (Current.Type == TokenType.Multiply || Current.Type == TokenType.Divide)
            {
                Token opToken = Consume();
                BinaryOperator op = opToken.Type == TokenType.Multiply
                    ? BinaryOperator.Multiply
                    : BinaryOperator.Divide;
                AstNode right = ParseUnary();
                left = new BinaryOpNode(op, left, right);
            }

            return left;
        }

        // Unary -> ('+' | '-') Unary | Power
        // Note: -x^2 is parsed as -(x^2) per standard mathematical convention!
        private AstNode ParseUnary()
        {
            if (Current.Type == TokenType.Plus)
            {
                Consume();
                return ParseUnary();
            }

            if (Current.Type == TokenType.Minus)
            {
                Consume();
                return new UnaryMinusNode(ParseUnary());
            }

            return ParsePower();
        }

        // Power -> Primary ('^' Unary)? (Right-associative)
        private AstNode ParsePower()
        {
            AstNode left = ParsePrimary();

            if (Current.Type == TokenType.Power)
            {
                Consume();
                AstNode right = ParseUnary();
                return new BinaryOpNode(BinaryOperator.Power, left, right);
            }

            return left;
        }

        private AstNode ParsePrimary()
        {
            Token t = Current;

            switch (t.Type)
            {
                case TokenType.Number:
                    Consume();
                    return new NumberNode(t.NumberValue);

                case TokenType.ConstantE:
                    Consume();
                    return new NumberNode(Math.E);

                case TokenType.ConstantPi:
                    Consume();
                    return new NumberNode(Math.PI);

                case TokenType.VariableX:
                    Consume();
                    return new VariableNode('x');

                case TokenType.VariableY:
                    Consume();
                    return new VariableNode('y');

                case TokenType.FunctionName:
                    return ParseFunctionCall();

                case TokenType.OpenParen:
                    int openPos = t.Position;
                    Consume();
                    if (Current.Type == TokenType.CloseParen)
                    {
                        throw new MathParseException(
                            $"Paréntesis vacíos '()' en la posición {openPos}.",
                            openPos);
                    }
                    AstNode inner = ParseExpression();
                    if (Current.Type != TokenType.CloseParen)
                    {
                        throw new MathParseException(
                            $"Falta cerrar paréntesis ')' para el '(' abierto en la posición {openPos}.",
                            Current.Position);
                    }
                    Consume();
                    return inner;

                case TokenType.EndOfFile:
                    throw new MathParseException(
                        "Fórmula incompleta: se esperaba un número, variable o expresión al final.",
                        t.Position);

                default:
                    throw new MathParseException(
                        $"Elemento inesperado '{t.Text}' en la posición {t.Position}.",
                        t.Position);
            }
        }

        private AstNode ParseFunctionCall()
        {
            Token fnToken = Consume();
            MathFunction fn = MapFunction(fnToken.Text, fnToken.Position);

            if (Current.Type != TokenType.OpenParen)
            {
                throw new MathParseException(
                    $"La función '{fnToken.Text}' requiere argumentos entre paréntesis, por ejemplo: {fnToken.Text}(x).",
                    Current.Position);
            }

            int openPos = Current.Position;
            Consume(); // '('

            if (Current.Type == TokenType.CloseParen)
            {
                throw new MathParseException(
                    $"La función '{fnToken.Text}' no puede tener paréntesis vacíos en la posición {openPos}.",
                    openPos);
            }

            AstNode arg = ParseExpression();

            if (Current.Type != TokenType.CloseParen)
            {
                throw new MathParseException(
                    $"Falta cerrar paréntesis ')' en la función '{fnToken.Text}' iniciada en la posición {fnToken.Position}.",
                    Current.Position);
            }

            Consume(); // ')'
            return new FunctionCallNode(fn, arg);
        }

        private static MathFunction MapFunction(string name, int pos)
        {
            switch (name.ToLowerInvariant())
            {
                case "sen": return MathFunction.Sen;
                case "cos": return MathFunction.Cos;
                case "tan": return MathFunction.Tan;
                case "exp": return MathFunction.Exp;
                case "ln": return MathFunction.Ln;
                case "sqrt": return MathFunction.Sqrt;
                case "abs": return MathFunction.Abs;
                case "floor": return MathFunction.Floor;
                case "ceil": return MathFunction.Ceil;
                default:
                    throw new MathParseException($"Función desconocida '{name}' en la posición {pos}.", pos);
            }
        }

        public static AstNode Simplify(AstNode node)
        {
            if (node is UnaryMinusNode u)
            {
                AstNode inner = Simplify(u.Operand);
                if (inner is NumberNode n)
                {
                    return new NumberNode(-n.Value);
                }
                if (inner is UnaryMinusNode u2)
                {
                    return u2.Operand;
                }
                return new UnaryMinusNode(inner);
            }

            if (node is BinaryOpNode b)
            {
                AstNode l = Simplify(b.Left);
                AstNode r = Simplify(b.Right);

                if (l is NumberNode nl && r is NumberNode nr)
                {
                    double evaluated = new BinaryOpNode(b.Operator, nl, nr).Evaluate(0, 0);
                    if (!double.IsNaN(evaluated))
                    {
                        return new NumberNode(evaluated);
                    }
                }

                if (b.Operator == BinaryOperator.Add)
                {
                    if (l is NumberNode nl0 && Math.Abs(nl0.Value) < 1e-15) return r;
                    if (r is NumberNode nr0 && Math.Abs(nr0.Value) < 1e-15) return l;
                }
                else if (b.Operator == BinaryOperator.Subtract)
                {
                    if (r is NumberNode nr0 && Math.Abs(nr0.Value) < 1e-15) return l;
                }
                else if (b.Operator == BinaryOperator.Multiply)
                {
                    if ((l is NumberNode nl0 && Math.Abs(nl0.Value) < 1e-15) ||
                        (r is NumberNode nr0 && Math.Abs(nr0.Value) < 1e-15))
                        return NumberNode.Zero;
                    if (l is NumberNode nl1 && Math.Abs(nl1.Value - 1.0) < 1e-15) return r;
                    if (r is NumberNode nr1 && Math.Abs(nr1.Value - 1.0) < 1e-15) return l;
                }
                else if (b.Operator == BinaryOperator.Power)
                {
                    if (r is NumberNode nr0 && Math.Abs(nr0.Value) < 1e-15) return NumberNode.One;
                    if (r is NumberNode nr1 && Math.Abs(nr1.Value - 1.0) < 1e-15) return l;
                }

                return new BinaryOpNode(b.Operator, l, r);
            }

            if (node is FunctionCallNode f)
            {
                AstNode arg = Simplify(f.Argument);
                return new FunctionCallNode(f.Function, arg);
            }

            return node;
        }
    }
}
