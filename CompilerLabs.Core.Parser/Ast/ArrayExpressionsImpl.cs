using System.Collections.Generic;

namespace CompilerLabs.Core.Parser.Ast
{
    public class ArrayExpression : Expression
    {
        public List<Expression> Elements { get; }

        public ArrayExpression(List<Expression> elements, int line, int column) : base(line, column)
        {
            Elements = elements;
        }
    }

    public class IndexExpression : Expression
    {
        public Expression Target { get; }
        public Expression Index { get; }

        public IndexExpression(Expression target, Expression index, int line, int column) : base(line, column)
        {
            Target = target;
            Index = index;
        }
    }

    public class IndexAssignExpression : Expression
    {
        public Expression Target { get; }
        public Expression Index { get; }
        public Expression Value { get; }

        public IndexAssignExpression(Expression target, Expression index, Expression value, int line, int column) : base(line, column)
        {
            Target = target;
            Index = index;
            Value = value;
        }
    }
}
