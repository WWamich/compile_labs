using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private readonly Dictionary<string, VariableInfo> _variables;

        public SemanticEnvironment(SemanticEnvironment? parent = null)
        {
            _parent = parent;
            _variables = new Dictionary<string, VariableInfo>();
        }

        public bool DefineVariable(string name)
        {
            if (_variables.ContainsKey(name))
            {
                return false;
            }

            _variables[name] = new VariableInfo { Name = name };
            return true;
        }

        public bool IsVariableDefined(string name)
        {
            if (_variables.ContainsKey(name))
            {
                return true;
            }

            return _parent?.IsVariableDefined(name) ?? false;
        }

        public void MarkVariableAsUsed(string name)
        {
            if (_variables.ContainsKey(name))
            {
                _variables[name].IsUsed = true;
            }
            else
            {
                _parent?.MarkVariableAsUsed(name);
            }
        }

        public IEnumerable<VariableInfo> GetUnusedVariables()
        {
            return _variables.Values.Where(v => !v.IsUsed);
        }

        public void MarkVariableAsInitialized(string name)
        {
            if (_variables.ContainsKey(name))
            {
                _variables[name].IsInitialized = true;
            }
            else
            {
                _parent?.MarkVariableAsInitialized(name);
            }
        }

        public bool IsVariableInitialized(string name)
        {
            if (_variables.ContainsKey(name))
            {
                return _variables[name].IsInitialized;
            }

            return _parent?.IsVariableInitialized(name) ?? false;
        }

        public Dictionary<string, bool> GetVariableInitializationState()
        {
            var result = new Dictionary<string, bool>();
            foreach (var variable in _variables)
            {
                result[variable.Key] = variable.Value.IsInitialized;
            }
            return result;
        }

        public void RestoreInitializationState(Dictionary<string, bool> state)
        {
            foreach (var kvp in state)
            {
                if (_variables.ContainsKey(kvp.Key))
                {
                    _variables[kvp.Key].IsInitialized = kvp.Value;
                }
            }
        }
    }
}
