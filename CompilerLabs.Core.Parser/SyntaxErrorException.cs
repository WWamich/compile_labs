using System;

namespace CompilerLabs.Core.Parser
{
    public class SyntaxErrorException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public SyntaxErrorException(int line, int column, string message)
            : base($"[Syntax Error] Line {line}, Col {column}: {message}")
        {
            Line = line;
            Column = column;
        }
    }
}
