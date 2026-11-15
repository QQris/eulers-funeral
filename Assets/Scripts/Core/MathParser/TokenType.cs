namespace EulersFuneral.Core.MathParser
{
    public enum TokenType
    {
        Number,
        VariableX,
        VariableY,
        ConstantE,
        ConstantPi,
        FunctionName,
        Plus,
        Minus,
        Multiply,
        Divide,
        Power,
        OpenParen,
        CloseParen,
        Comma,
        EndOfFile
    }

    public readonly struct Token
    {
        public readonly TokenType Type;
        public readonly string Text;
        public readonly double NumberValue;
        public readonly int Position;

        public Token(TokenType type, string text, int position, double numberValue = 0)
        {
            Type = type;
            Text = text;
            Position = position;
            NumberValue = numberValue;
        }

        public override string ToString() => $"{Type}('{Text}' at {Position})";
    }
}
