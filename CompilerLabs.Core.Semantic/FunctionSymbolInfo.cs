namespace CompilerLabs.Core.Semantic
{
    public class FunctionSymbolInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Arity { get; set; }
        public SemanticValueType ReturnType { get; set; } = SemanticValueType.Unknown;
    }
}
