using System.Collections.Generic;

namespace CompilerLabs.Core.Semantic
{
    public sealed class RuntimeArray
    {
        public SemanticValueType ElementType { get; set; }
        public List<RuntimeValue> Elements { get; }

        public RuntimeArray(SemanticValueType elementType, List<RuntimeValue> elements)
        {
            ElementType = elementType;
            Elements = elements;
        }
    }
}
