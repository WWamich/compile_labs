using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilerLabs.Core.Semantic
{
    public class SymbolInfo
    {
        public string Name { get; set; }
        public bool IsInitialized { get; set; }
        public bool IsUsed { get; set; }
        public SemanticValueType Type { get; set; } = SemanticValueType.Unknown;
        public SemanticValueType ElementType { get; set; } = SemanticValueType.Unknown;
    }
}
