using System;

namespace CompilerLabs.Core.Semantic
{
    public class RuntimeException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public RuntimeException(int line, int column, string message)
            : base($"[Runtime Error] Line {line}, Col {column}: {message}")
        {
            Line = line;
            Column = column;
        }
    }
}