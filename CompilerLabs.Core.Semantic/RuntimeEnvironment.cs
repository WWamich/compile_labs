using System.Collections.Generic;

namespace CompilerLabs.Core.Semantic
{
    public class RuntimeEnvironment
    {
        private readonly RuntimeEnvironment? _parent;
        private readonly Dictionary<string, RuntimeValue> _variables;

        public RuntimeEnvironment(RuntimeEnvironment? parent = null)
        {
            _parent = parent;
            _variables = new Dictionary<string, RuntimeValue>();
        }

        public bool Define(string name, RuntimeValue value)
        {
            if (_variables.ContainsKey(name))
            {
                return false;
            }

            _variables[name] = value;
            return true;
        }

        public bool Assign(string name, RuntimeValue value)
        {
            if (_variables.ContainsKey(name))
            {
                _variables[name] = value;
                return true;
            }

            return _parent?.Assign(name, value) ?? false;
        }

        public bool TryGet(string name, out RuntimeValue value)
        {
            if (_variables.TryGetValue(name, out value!))
            {
                return true;
            }

            if (_parent != null)
            {
                return _parent.TryGet(name, out value!);
            }

            value = RuntimeValue.Uninitialized;
            return false;
        }
    }
}