using System;
using System.Collections.Generic;
using System.Globalization;

namespace EulersFuneral.Core.MathParser
{
    public class MathParseException : Exception
    {
        public int ErrorPosition { get; }

        public MathParseException(string message, int position) : base(message)
        {
            ErrorPosition = position;
        }
    }

    public static class MathTokenizer
    {
        private static readonly HashSet<string> ValidFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "sen", "cos", "tan", "exp", "ln", "sqrt", "abs", "floor", "ceil"
        };

        public static List<Token> Tokenize(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new MathParseException("La fórmula no puede estar vacía.", 0);
            }

            var rawTokens = new List<Token>();
            int i = 0;
            int n = expression.Length;

            while (i < n)
            {
                char c = expression[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                // Numbers
                if (char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    bool hasDot = (c == '.');
                    i++;
                    while (i < n && (char.IsDigit(expression[i]) || expression[i] == '.'))
                    {
                        if (expression[i] == '.')
                        {
                            if (hasDot)
                            {
                                throw new MathParseException($"Número con múltiples puntos decimales en la posición {i}.", i);
                            }
                            hasDot = true;
                        }
                        i++;
                    }

                    string numStr = expression.Substring(start, i - start);
                    if (numStr == ".")
                    {
                        throw new MathParseException($"Punto decimal aislado en la posición {start}.", start);
                    }

                    if (!double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                    {
                        throw new MathParseException($"Número inválido '{numStr}' en la posición {start}.", start);
                    }

                    rawTokens.Add(new Token(TokenType.Number, numStr, start, val));
                    continue;
                }

                // Identifiers (Variables, Constants, Functions)
                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < n && char.IsLetter(expression[i]))
                    {
                        i++;
                    }

                    string ident = expression.Substring(start, i - start).ToLowerInvariant();
                    TokenizeIdentifier(ident, start, rawTokens);
                    continue;
                }

                // Special floor / ceil Unicode symbols if pasted
                if (c == '⌊')
                {
                    rawTokens.Add(new Token(TokenType.FunctionName, "floor", i));
                    rawTokens.Add(new Token(TokenType.OpenParen, "(", i));
                    i++;
                    continue;
                }
                if (c == '⌋' || c == '⌉')
                {
                    rawTokens.Add(new Token(TokenType.CloseParen, ")", i));
                    i++;
                    continue;
                }
                if (c == '⌈')
                {
                    rawTokens.Add(new Token(TokenType.FunctionName, "ceil", i));
                    rawTokens.Add(new Token(TokenType.OpenParen, "(", i));
                    i++;
                    continue;
                }

                // Operators and Parentheses
                switch (c)
                {
                    case '+':
                        rawTokens.Add(new Token(TokenType.Plus, "+", i));
                        break;
                    case '-':
                    case '−': // Unicode minus
                        rawTokens.Add(new Token(TokenType.Minus, "-", i));
                        break;
                    case '*':
                    case '·':
                        rawTokens.Add(new Token(TokenType.Multiply, "*", i));
                        break;
                    case '/':
                        rawTokens.Add(new Token(TokenType.Divide, "/", i));
                        break;
                    case '^':
                        rawTokens.Add(new Token(TokenType.Power, "^", i));
                        break;
                    case '(':
                        rawTokens.Add(new Token(TokenType.OpenParen, "(", i));
                        break;
                    case ')':
                        rawTokens.Add(new Token(TokenType.CloseParen, ")", i));
                        break;
                    case ',':
                        rawTokens.Add(new Token(TokenType.Comma, ",", i));
                        break;
                    default:
                        throw new MathParseException($"Símbolo no reconocido '{c}' en la posición {i}.", i);
                }

                i++;
            }

            var tokensWithImplicitMult = InsertImplicitMultiplications(rawTokens);
            tokensWithImplicitMult.Add(new Token(TokenType.EndOfFile, "", n));
            return tokensWithImplicitMult;
        }

        private static void TokenizeIdentifier(string ident, int startPos, List<Token> output)
        {
            // Check exact match first
            if (ident == "x")
            {
                output.Add(new Token(TokenType.VariableX, "x", startPos));
                return;
            }
            if (ident == "y")
            {
                output.Add(new Token(TokenType.VariableY, "y", startPos));
                return;
            }
            if (ident == "e")
            {
                output.Add(new Token(TokenType.ConstantE, "e", startPos, Math.E));
                return;
            }
            if (ident == "pi")
            {
                output.Add(new Token(TokenType.ConstantPi, "pi", startPos, Math.PI));
                return;
            }
            if (ValidFunctions.Contains(ident))
            {
                output.Add(new Token(TokenType.FunctionName, ident, startPos));
                return;
            }

            if (ident == "sin")
            {
                throw new MathParseException($"Usa la sintaxis en español 'sen' en lugar de 'sin' (posición {startPos}).", startPos);
            }
            if (ident == "log")
            {
                throw new MathParseException($"Usa 'ln' para el logaritmo natural en lugar de 'log' (posición {startPos}).", startPos);
            }

            // Support implicit multiplication between adjacent single-letter variables/constants like "xy", "ex", "pix"
            int idx = 0;
            while (idx < ident.Length)
            {
                string remaining = ident.Substring(idx);

                if (ValidFunctions.Contains(remaining))
                {
                    output.Add(new Token(TokenType.FunctionName, remaining, startPos + idx));
                    return;
                }
                if (remaining.StartsWith("pi", StringComparison.Ordinal))
                {
                    output.Add(new Token(TokenType.ConstantPi, "pi", startPos + idx, Math.PI));
                    idx += 2;
                    continue;
                }

                char ch = ident[idx];
                if (ch == 'x')
                {
                    output.Add(new Token(TokenType.VariableX, "x", startPos + idx));
                    idx++;
                }
                else if (ch == 'y')
                {
                    output.Add(new Token(TokenType.VariableY, "y", startPos + idx));
                    idx++;
                }
                else if (ch == 'e')
                {
                    output.Add(new Token(TokenType.ConstantE, "e", startPos + idx, Math.E));
                    idx++;
                }
                else
                {
                    throw new MathParseException(
                        $"Identificador o función desconocida '{ident}' en la posición {startPos}. " +
                        "Variables válidas: x, y. Constantes: e, pi. Funciones: sen, cos, tan, exp, ln, sqrt, abs, floor, ceil.",
                        startPos);
                }
            }
        }

        private static List<Token> InsertImplicitMultiplications(List<Token> tokens)
        {
            var result = new List<Token>(tokens.Count * 2);
            for (int i = 0; i < tokens.Count; i++)
            {
                Token current = tokens[i];
                result.Add(current);

                if (i + 1 < tokens.Count)
                {
                    Token next = tokens[i + 1];
                    if (CanEndFactor(current.Type) && CanStartFactor(next.Type))
                    {
                        result.Add(new Token(TokenType.Multiply, "*", next.Position));
                    }
                }
            }
            return result;
        }

        private static bool CanEndFactor(TokenType type)
        {
            return type == TokenType.Number ||
                   type == TokenType.VariableX ||
                   type == TokenType.VariableY ||
                   type == TokenType.ConstantE ||
                   type == TokenType.ConstantPi ||
                   type == TokenType.CloseParen;
        }

        private static bool CanStartFactor(TokenType type)
        {
            return type == TokenType.Number ||
                   type == TokenType.VariableX ||
                   type == TokenType.VariableY ||
                   type == TokenType.ConstantE ||
                   type == TokenType.ConstantPi ||
                   type == TokenType.FunctionName ||
                   type == TokenType.OpenParen;
        }
    }
}
