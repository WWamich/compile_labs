using System.Linq;

namespace CompilerLabs.Core.Semantic
{
    public sealed class RuntimeValue
    {
        public static readonly RuntimeValue Uninitialized = new RuntimeValue(SemanticValueType.Unknown, null);

        public SemanticValueType Type { get; }
        public object? Value { get; }

        private RuntimeValue(SemanticValueType type, object? value)
        {
            Type = type;
            Value = value;
        }

        public static RuntimeValue FromNumber(double value) => new RuntimeValue(SemanticValueType.Number, value);
        public static RuntimeValue FromString(string value) => new RuntimeValue(SemanticValueType.String, value);
        public static RuntimeValue FromBoolean(bool value) => new RuntimeValue(SemanticValueType.Boolean, value);
        public static RuntimeValue FromArray(RuntimeArray value) => new RuntimeValue(ToArrayType(value.ElementType), value);

        public double AsNumber() => (double)Value!;
        public string AsString() => (string)Value!;
        public bool AsBoolean() => (bool)Value!;
        public RuntimeArray AsArray() => (RuntimeArray)Value!;

        public override string ToString()
        {
            return Type switch
            {
                SemanticValueType.Number => AsNumber().ToString(System.Globalization.CultureInfo.InvariantCulture),
                SemanticValueType.String => AsString(),
                SemanticValueType.Boolean => AsBoolean() ? "true" : "false",
                SemanticValueType.ArrayNumber or SemanticValueType.ArrayString or SemanticValueType.ArrayBoolean or SemanticValueType.ArrayUnknown => FormatArray(AsArray()),
                _ => "<uninitialized>"
            };
        }

        private static SemanticValueType ToArrayType(SemanticValueType elementType)
        {
            return elementType switch
            {
                SemanticValueType.Number => SemanticValueType.ArrayNumber,
                SemanticValueType.String => SemanticValueType.ArrayString,
                SemanticValueType.Boolean => SemanticValueType.ArrayBoolean,
                _ => SemanticValueType.ArrayUnknown
            };
        }

        private static string FormatArray(RuntimeArray array)
        {
            if (array.Elements.Count == 0)
            {
                return "[]";
            }

            var parts = array.Elements.Select(element => element.ToString());
            return $"[{string.Join(", ", parts)}]";
        }
    }
}
