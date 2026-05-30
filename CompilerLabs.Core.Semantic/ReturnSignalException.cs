using System;

namespace CompilerLabs.Core.Semantic
{
    internal class ReturnSignalException : Exception
    {
        public RuntimeValue Value { get; }

        public ReturnSignalException(RuntimeValue value)
        {
            Value = value;
        }
    }
}
