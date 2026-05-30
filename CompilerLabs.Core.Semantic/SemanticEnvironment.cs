using System.Collections.Generic;
using System.Linq;

namespace CompilerLabs.Core.Semantic
{
    public class VariableInfo
    {
        public string Name { get; set; }
        public bool IsUsed { get; set; } = false;
        public bool IsInitialized { get; set; } = false;
    }

    public class SemanticEnvironment
    {
        private readonly SemanticEnvironment? _parent;

        private readonly Dictionary<string, SymbolInfo> _variables;

        public SemanticEnvironment(SemanticEnvironment? parent = null)
        {
            _parent = parent;
            _variables = new Dictionary<string, SymbolInfo>();
        }

        public bool DefineVariable(string name, bool isInitialized, SemanticValueType type = SemanticValueType.Unknown)
        {
            if (_variables.ContainsKey(name))
            {
                return false;
            }

            _variables[name] = new SymbolInfo
            {
                Name = name,
                IsInitialized = isInitialized,
                Type = type
            };
            return true;
        }

        public bool IsVariableDefined(string name)
        {
            if (_variables.ContainsKey(name)) return true;
            return _parent?.IsVariableDefined(name) ?? false;
        }

        public SymbolInfo? GetVariable(string name)
        {
            if (_variables.TryGetValue(name, out var symbol)) return symbol;
            return _parent?.GetVariable(name);
        }

        public void SetInitialized(string name)
        {
            var symbol = GetVariable(name);
            if (symbol != null)
                symbol.IsInitialized = true;
        }

        public void SetType(string name, SemanticValueType type)
        {
            var symbol = GetVariable(name);
            if (symbol != null)
            {
                symbol.Type = type;
            }
        }

        public IEnumerable<SymbolInfo> GetLocalVariables()
        {
            return _variables.Values;
        }

        public IEnumerable<SymbolInfo> GetUnusedVariables()
        {
            return _variables.Values.Where(v => !v.IsUsed);
        }
    }
}