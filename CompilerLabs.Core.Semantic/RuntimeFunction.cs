using CompilerLabs.Core.Parser.Ast;

namespace CompilerLabs.Core.Semantic
{
    public class RuntimeFunction
    {
        public FunctionStatement Declaration { get; }
        public RuntimeEnvironment Closure { get; }

        public RuntimeFunction(FunctionStatement declaration, RuntimeEnvironment closure)
        {
            Declaration = declaration;
            Closure = closure;
        }
    }
}
