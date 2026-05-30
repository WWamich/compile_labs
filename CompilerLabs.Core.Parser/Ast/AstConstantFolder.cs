using CompilerLabs.Core.Lexer;
using System.Collections.Generic;
using System.Linq;

namespace CompilerLabs.Core.Parser.Ast
{
    public static class AstConstantFolder
    {
        public static List<Statement> FoldConstants(IEnumerable<Statement> statements)
        {
            return statements.Select(OptimizeStatement).ToList();
        }

        private static Statement OptimizeStatement(Statement statement)
        {
            switch (statement)
            {
                case VarStatement v:
                    return new VarStatement(v.Name, v.Initializer == null ? null : OptimizeExpression(v.Initializer), v.Line, v.Column);
                case PrintStatement p:
                    return new PrintStatement(OptimizeExpression(p.Expression), p.Line, p.Column);
                case ExpressionStatement e:
                    return new ExpressionStatement(OptimizeExpression(e.Expression), e.Line, e.Column);
                case BlockStatement b:
                    return OptimizeBlock(b);
                case IfStatement i:
                    return new IfStatement(
                        OptimizeExpression(i.Condition),
                        OptimizeStatement(i.ThenBranch),
                        i.ElseBranch == null ? null : OptimizeStatement(i.ElseBranch),
                        i.Line,
                        i.Column);
                case WhileStatement w:
                    return new WhileStatement(
                        OptimizeExpression(w.Condition),
                        OptimizeStatement(w.Body),
                        w.Line,
                        w.Column);
                case FunctionStatement f:
                    return new FunctionStatement(
                        f.Name,
                        new List<string>(f.Parameters),
                        OptimizeBlock(f.Body),
                        f.Line,
                        f.Column);
                case CallStatement c:
                    return new CallStatement((CallExpression)OptimizeExpression(c.Call), c.Line, c.Column);
                case ReturnStatement r:
                    return new ReturnStatement(r.Value == null ? null : OptimizeExpression(r.Value), r.Line, r.Column);
                default:
                    return statement;
            }
        }

        private static BlockStatement OptimizeBlock(BlockStatement block)
        {
            return new BlockStatement(block.Statements.Select(OptimizeStatement).ToList(), block.Line, block.Column);
        }

        private static Expression OptimizeExpression(Expression expression)
        {
            switch (expression)
            {
                case NumberExpression:
                case StringExpression:
                case BooleanExpression:
                case VariableExpression:
                    return expression;
                case AssignExpression a:
                    return new AssignExpression(a.Name, OptimizeExpression(a.Value), a.Line, a.Column);
                case UnaryExpression u:
                    {
                        var right = OptimizeExpression(u.Right);
                        if (TryFoldUnary(u.Operator, right, u.Line, u.Column, out var folded))
                        {
                            return folded;
                        }
                        return new UnaryExpression(u.Operator, right, u.Line, u.Column);
                    }
                case BinaryExpression b:
                    {
                        var left = OptimizeExpression(b.Left);
                        var right = OptimizeExpression(b.Right);
                        if (TryFoldBinary(b.Operator, left, right, b.Line, b.Column, out var folded))
                        {
                            return folded;
                        }
                        return new BinaryExpression(left, b.Operator, right, b.Line, b.Column);
                    }
                case CallExpression c:
                    return new CallExpression(c.Callee, c.Arguments.Select(OptimizeExpression).ToList(), c.Line, c.Column);
                case ArrayExpression a:
                    return new ArrayExpression(a.Elements.Select(OptimizeExpression).ToList(), a.Line, a.Column);
                case IndexExpression i:
                    return new IndexExpression(OptimizeExpression(i.Target), OptimizeExpression(i.Index), i.Line, i.Column);
                case IndexAssignExpression ia:
                    return new IndexAssignExpression(
                        OptimizeExpression(ia.Target),
                        OptimizeExpression(ia.Index),
                        OptimizeExpression(ia.Value),
                        ia.Line,
                        ia.Column);
                default:
                    return expression;
            }
        }

        private static bool TryFoldUnary(TokenType op, Expression right, int line, int column, out Expression folded)
        {
            if (right is NumberExpression number && op == TokenType.MINUS)
            {
                folded = new NumberExpression(-number.Value, line, column);
                return true;
            }

            if (right is BooleanExpression boolean && op == TokenType.EXCL)
            {
                folded = new BooleanExpression(!boolean.Value, line, column);
                return true;
            }

            folded = null!;
            return false;
        }

        private static bool TryFoldBinary(TokenType op, Expression left, Expression right, int line, int column, out Expression folded)
        {
            if (left is NumberExpression leftNumber && right is NumberExpression rightNumber)
            {
                switch (op)
                {
                    case TokenType.PLUS:
                        folded = new NumberExpression(leftNumber.Value + rightNumber.Value, line, column);
                        return true;
                    case TokenType.MINUS:
                        folded = new NumberExpression(leftNumber.Value - rightNumber.Value, line, column);
                        return true;
                    case TokenType.STAR:
                        folded = new NumberExpression(leftNumber.Value * rightNumber.Value, line, column);
                        return true;
                    case TokenType.SLASH:
                        folded = new NumberExpression(leftNumber.Value / rightNumber.Value, line, column);
                        return true;
                    case TokenType.LT:
                        folded = new BooleanExpression(leftNumber.Value < rightNumber.Value, line, column);
                        return true;
                    case TokenType.LTEQ:
                        folded = new BooleanExpression(leftNumber.Value <= rightNumber.Value, line, column);
                        return true;
                    case TokenType.GT:
                        folded = new BooleanExpression(leftNumber.Value > rightNumber.Value, line, column);
                        return true;
                    case TokenType.GTEQ:
                        folded = new BooleanExpression(leftNumber.Value >= rightNumber.Value, line, column);
                        return true;
                    case TokenType.EQEQ:
                        folded = new BooleanExpression(leftNumber.Value == rightNumber.Value, line, column);
                        return true;
                    case TokenType.NEQ:
                        folded = new BooleanExpression(leftNumber.Value != rightNumber.Value, line, column);
                        return true;
                }
            }

            if (left is StringExpression leftString && right is StringExpression rightString)
            {
                switch (op)
                {
                    case TokenType.PLUS:
                        folded = new StringExpression(leftString.Value + rightString.Value, line, column);
                        return true;
                    case TokenType.EQEQ:
                        folded = new BooleanExpression(leftString.Value == rightString.Value, line, column);
                        return true;
                    case TokenType.NEQ:
                        folded = new BooleanExpression(leftString.Value != rightString.Value, line, column);
                        return true;
                }
            }

            if (left is BooleanExpression leftBool && right is BooleanExpression rightBool)
            {
                switch (op)
                {
                    case TokenType.AND:
                        folded = new BooleanExpression(leftBool.Value && rightBool.Value, line, column);
                        return true;
                    case TokenType.OR:
                        folded = new BooleanExpression(leftBool.Value || rightBool.Value, line, column);
                        return true;
                    case TokenType.EQEQ:
                        folded = new BooleanExpression(leftBool.Value == rightBool.Value, line, column);
                        return true;
                    case TokenType.NEQ:
                        folded = new BooleanExpression(leftBool.Value != rightBool.Value, line, column);
                        return true;
                }
            }

            folded = null!;
            return false;
        }
    }
}
