using CompilerLabs.Core.Lexer;
using CompilerLabs.Core.Parser.Ast;
using System;
using System.Collections.Generic;

namespace CompilerLabs.Core.Semantic
{
    public class Interpreter
    {
        private RuntimeEnvironment _environment = new RuntimeEnvironment();
        private readonly Action<string> _print;
        private readonly Dictionary<string, RuntimeFunction> _functions = new Dictionary<string, RuntimeFunction>();
        private int _functionDepth;

        public Interpreter(Action<string>? print = null)
        {
            _print = print ?? Console.WriteLine;
        }

        public RuntimeValue Eval(Expression expression)
        {
            return expression switch
            {
                NumberExpression n => RuntimeValue.FromNumber(n.Value),
                StringExpression s => RuntimeValue.FromString(s.Value),
                BooleanExpression b => RuntimeValue.FromBoolean(b.Value),
                ArrayExpression a => EvalArray(a),
                VariableExpression v => EvalVariable(v),
                AssignExpression a => EvalAssign(a),
                UnaryExpression u => EvalUnary(u),
                BinaryExpression b => EvalBinary(b),
                IndexExpression i => EvalIndex(i),
                IndexAssignExpression ia => EvalIndexAssign(ia),
                CallExpression c => EvalCall(c),
                _ => throw new RuntimeException(expression.Line, expression.Column, $"Unsupported expression: {expression.GetType().Name}")
            };
        }

        public void Execute(Statement statement)
        {
            switch (statement)
            {
                case VarStatement v:
                    ExecuteVar(v);
                    return;

                case ExpressionStatement e:
                    _ = Eval(e.Expression);
                    return;

                case PrintStatement p:
                    var value = Eval(p.Expression);
                    _print(value.ToString());
                    return;

                case BlockStatement b:
                    ExecuteBlock(b.Statements, new RuntimeEnvironment(_environment));
                    return;

                case IfStatement i:
                    ExecuteIf(i);
                    return;

                case WhileStatement w:
                    ExecuteWhile(w);
                    return;

                case FunctionStatement f:
                    ExecuteFunctionDeclaration(f);
                    return;

                case CallStatement c:
                    _ = Eval(c.Call);
                    return;

                case ReturnStatement r:
                    ExecuteReturn(r);
                    return;

                default:
                    throw new RuntimeException(statement.Line, statement.Column, $"Unsupported statement: {statement.GetType().Name}");
            }
        }

        public void Execute(IEnumerable<Statement> statements)
        {
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }

        private void ExecuteVar(VarStatement statement)
        {
            var initial = statement.Initializer != null
                ? Eval(statement.Initializer)
                : RuntimeValue.Uninitialized;

            if (!_environment.Define(statement.Name, initial))
            {
                throw new RuntimeException(statement.Line, statement.Column, $"Variable '{statement.Name}' is already defined in this scope.");
            }
        }

        private void ExecuteIf(IfStatement statement)
        {
            var condition = Eval(statement.Condition);
            EnsureType(condition, SemanticValueType.Boolean, statement.Condition.Line, statement.Condition.Column, "if condition must be boolean");

            if (condition.AsBoolean())
            {
                Execute(statement.ThenBranch);
                return;
            }

            if (statement.ElseBranch != null)
            {
                Execute(statement.ElseBranch);
            }
        }

        private void ExecuteWhile(WhileStatement statement)
        {
            while (true)
            {
                var condition = Eval(statement.Condition);
                EnsureType(condition, SemanticValueType.Boolean, statement.Condition.Line, statement.Condition.Column, "while condition must be boolean");

                if (!condition.AsBoolean())
                {
                    break;
                }

                Execute(statement.Body);
            }
        }

        private void ExecuteBlock(IEnumerable<Statement> statements, RuntimeEnvironment nestedEnvironment)
        {
            var previous = _environment;
            try
            {
                _environment = nestedEnvironment;
                foreach (var statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                _environment = previous;
            }
        }

        private RuntimeValue EvalVariable(VariableExpression expression)
        {
            if (!_environment.TryGet(expression.Name, out var value))
            {
                throw new RuntimeException(expression.Line, expression.Column, $"Variable '{expression.Name}' is not defined.");
            }

            if (value.Type == SemanticValueType.Unknown)
            {
                throw new RuntimeException(expression.Line, expression.Column, $"Variable '{expression.Name}' is not initialized.");
            }

            return value;
        }

        private RuntimeValue EvalAssign(AssignExpression expression)
        {
            var value = Eval(expression.Value);
            if (!_environment.Assign(expression.Name, value))
            {
                throw new RuntimeException(expression.Line, expression.Column, $"Variable '{expression.Name}' is not defined.");
            }

            return value;
        }

        private RuntimeValue EvalArray(ArrayExpression expression)
        {
            var elements = new List<RuntimeValue>();
            SemanticValueType elementType = SemanticValueType.Unknown;

            foreach (var item in expression.Elements)
            {
                var value = Eval(item);
                if (value.Type == SemanticValueType.Error || value.Type == SemanticValueType.Unknown)
                {
                    throw new RuntimeException(item.Line, item.Column, "Array elements must be initialized values.");
                }

                if (elementType == SemanticValueType.Unknown)
                {
                    elementType = value.Type;
                }
                else if (elementType != value.Type)
                {
                    throw new RuntimeException(item.Line, item.Column, "Array elements must have the same type.");
                }

                elements.Add(value);
            }

            return RuntimeValue.FromArray(new RuntimeArray(elementType, elements));
        }

        private RuntimeValue EvalIndex(IndexExpression expression)
        {
            var target = Eval(expression.Target);
            var indexValue = Eval(expression.Index);

            EnsureType(indexValue, SemanticValueType.Number, expression.Index.Line, expression.Index.Column, "array index must be number");

            if (!IsArrayValue(target))
            {
                throw new RuntimeException(expression.Target.Line, expression.Target.Column, "indexing is possible only for arrays");
            }

            var array = target.AsArray();
            var index = (int)indexValue.AsNumber();
            EnsureArrayBounds(array, index, expression.Line, expression.Column);
            return array.Elements[index];
        }

        private RuntimeValue EvalIndexAssign(IndexAssignExpression expression)
        {
            if (expression.Target is not VariableExpression variable)
            {
                throw new RuntimeException(expression.Target.Line, expression.Target.Column, "assignment by index is supported only for array variables");
            }

            if (!_environment.TryGet(variable.Name, out var arrayValue))
            {
                throw new RuntimeException(variable.Line, variable.Column, $"Variable '{variable.Name}' is not defined.");
            }

            if (!IsArrayValue(arrayValue))
            {
                throw new RuntimeException(variable.Line, variable.Column, $"Variable '{variable.Name}' is not an array.");
            }

            var indexValue = Eval(expression.Index);
            EnsureType(indexValue, SemanticValueType.Number, expression.Index.Line, expression.Index.Column, "array index must be number");

            var newValue = Eval(expression.Value);
            var array = arrayValue.AsArray();
            var index = (int)indexValue.AsNumber();
            EnsureArrayBounds(array, index, expression.Line, expression.Column);

            if (array.ElementType != SemanticValueType.ArrayUnknown && array.ElementType != SemanticValueType.Unknown)
            {
                if (!MatchesArrayElementType(array.ElementType, newValue.Type))
                {
                    throw new RuntimeException(expression.Value.Line, expression.Value.Column, "value type does not match array element type");
                }
            }

            if (array.ElementType == SemanticValueType.ArrayUnknown || array.ElementType == SemanticValueType.Unknown)
            {
                array.ElementType = newValue.Type;
            }

            array.Elements[index] = newValue;
            return newValue;
        }

        private RuntimeValue EvalCall(CallExpression expression)
        {
            if (!_functions.TryGetValue(expression.Callee, out var function))
            {
                throw new RuntimeException(expression.Line, expression.Column, $"Function '{expression.Callee}' is not defined.");
            }

            if (expression.Arguments.Count != function.Declaration.Parameters.Count)
            {
                throw new RuntimeException(
                    expression.Line,
                    expression.Column,
                    $"Function '{expression.Callee}' expects {function.Declaration.Parameters.Count} args, got {expression.Arguments.Count}.");
            }

            var callEnvironment = new RuntimeEnvironment(function.Closure);
            for (var i = 0; i < function.Declaration.Parameters.Count; i++)
            {
                var argumentValue = Eval(expression.Arguments[i]);
                callEnvironment.Define(function.Declaration.Parameters[i], argumentValue);
            }

            _functionDepth++;
            try
            {
                ExecuteBlock(function.Declaration.Body.Statements, callEnvironment);
                return RuntimeValue.Uninitialized;
            }
            catch (ReturnSignalException returned)
            {
                return returned.Value;
            }
            finally
            {
                _functionDepth--;
            }
        }

        private void ExecuteFunctionDeclaration(FunctionStatement statement)
        {
            if (_functions.ContainsKey(statement.Name))
            {
                throw new RuntimeException(statement.Line, statement.Column, $"Function '{statement.Name}' is already defined.");
            }

            _functions[statement.Name] = new RuntimeFunction(statement, _environment);
        }

        private void ExecuteReturn(ReturnStatement statement)
        {
            if (_functionDepth == 0)
            {
                throw new RuntimeException(statement.Line, statement.Column, "return can be used only inside function.");
            }

            var value = statement.Value == null
                ? RuntimeValue.Uninitialized
                : Eval(statement.Value);

            throw new ReturnSignalException(value);
        }

        private RuntimeValue EvalUnary(UnaryExpression expression)
        {
            var right = Eval(expression.Right);
            return expression.Operator switch
            {
                TokenType.MINUS => EvalUnaryMinus(right, expression.Line, expression.Column),
                TokenType.EXCL => EvalUnaryNot(right, expression.Line, expression.Column),
                _ => throw new RuntimeException(expression.Line, expression.Column, $"Unsupported unary operator '{expression.Operator}'.")
            };
        }

        private RuntimeValue EvalBinary(BinaryExpression expression)
        {
            if (expression.Operator == TokenType.AND)
            {
                var leftBool = Eval(expression.Left);
                EnsureType(leftBool, SemanticValueType.Boolean, expression.Line, expression.Column, "left operand for '&&' must be boolean");
                if (!leftBool.AsBoolean())
                {
                    return RuntimeValue.FromBoolean(false);
                }

                var rightBool = Eval(expression.Right);
                EnsureType(rightBool, SemanticValueType.Boolean, expression.Line, expression.Column, "right operand for '&&' must be boolean");
                return RuntimeValue.FromBoolean(rightBool.AsBoolean());
            }

            if (expression.Operator == TokenType.OR)
            {
                var leftBool = Eval(expression.Left);
                EnsureType(leftBool, SemanticValueType.Boolean, expression.Line, expression.Column, "left operand for '||' must be boolean");
                if (leftBool.AsBoolean())
                {
                    return RuntimeValue.FromBoolean(true);
                }

                var rightBool = Eval(expression.Right);
                EnsureType(rightBool, SemanticValueType.Boolean, expression.Line, expression.Column, "right operand for '||' must be boolean");
                return RuntimeValue.FromBoolean(rightBool.AsBoolean());
            }

            var left = Eval(expression.Left);
            var right = Eval(expression.Right);

            return expression.Operator switch
            {
                TokenType.PLUS => EvalPlus(left, right, expression.Line, expression.Column),
                TokenType.MINUS => EvalMath(left, right, (a, b) => a - b, expression.Line, expression.Column),
                TokenType.STAR => EvalMath(left, right, (a, b) => a * b, expression.Line, expression.Column),
                TokenType.SLASH => EvalMath(left, right, (a, b) => a / b, expression.Line, expression.Column),
                TokenType.LT => EvalCompare(left, right, (a, b) => a < b, expression.Line, expression.Column),
                TokenType.LTEQ => EvalCompare(left, right, (a, b) => a <= b, expression.Line, expression.Column),
                TokenType.GT => EvalCompare(left, right, (a, b) => a > b, expression.Line, expression.Column),
                TokenType.GTEQ => EvalCompare(left, right, (a, b) => a >= b, expression.Line, expression.Column),
                TokenType.EQEQ => RuntimeValue.FromBoolean(AreEqual(left, right)),
                TokenType.NEQ => RuntimeValue.FromBoolean(!AreEqual(left, right)),
                _ => throw new RuntimeException(expression.Line, expression.Column, $"Unsupported binary operator '{expression.Operator}'.")
            };
        }

        private static RuntimeValue EvalUnaryMinus(RuntimeValue value, int line, int column)
        {
            EnsureType(value, SemanticValueType.Number, line, column, "unary '-' works only for number");
            return RuntimeValue.FromNumber(-value.AsNumber());
        }

        private static RuntimeValue EvalUnaryNot(RuntimeValue value, int line, int column)
        {
            EnsureType(value, SemanticValueType.Boolean, line, column, "unary '!' works only for boolean");
            return RuntimeValue.FromBoolean(!value.AsBoolean());
        }

        private static RuntimeValue EvalPlus(RuntimeValue left, RuntimeValue right, int line, int column)
        {
            if (left.Type == SemanticValueType.Number && right.Type == SemanticValueType.Number)
            {
                return RuntimeValue.FromNumber(left.AsNumber() + right.AsNumber());
            }

            if (left.Type == SemanticValueType.String && right.Type == SemanticValueType.String)
            {
                return RuntimeValue.FromString(left.AsString() + right.AsString());
            }

            throw new RuntimeException(line, column, "'+' supports only number+number or string+string");
        }

        private static RuntimeValue EvalMath(RuntimeValue left, RuntimeValue right, Func<double, double, double> operation, int line, int column)
        {
            EnsureType(left, SemanticValueType.Number, line, column, "arithmetic operators require number operands");
            EnsureType(right, SemanticValueType.Number, line, column, "arithmetic operators require number operands");
            return RuntimeValue.FromNumber(operation(left.AsNumber(), right.AsNumber()));
        }

        private static RuntimeValue EvalCompare(RuntimeValue left, RuntimeValue right, Func<double, double, bool> predicate, int line, int column)
        {
            EnsureType(left, SemanticValueType.Number, line, column, "comparison operators require number operands");
            EnsureType(right, SemanticValueType.Number, line, column, "comparison operators require number operands");
            return RuntimeValue.FromBoolean(predicate(left.AsNumber(), right.AsNumber()));
        }

        private static bool IsArrayValue(RuntimeValue value)
        {
            return value.Type == SemanticValueType.ArrayUnknown
                || value.Type == SemanticValueType.ArrayNumber
                || value.Type == SemanticValueType.ArrayString
                || value.Type == SemanticValueType.ArrayBoolean;
        }

        private static void EnsureArrayBounds(RuntimeArray array, int index, int line, int column)
        {
            if (index < 0 || index >= array.Elements.Count)
            {
                throw new RuntimeException(line, column, $"array index {index} is out of bounds");
            }
        }

        private static bool MatchesArrayElementType(SemanticValueType elementType, SemanticValueType valueType)
        {
            return elementType switch
            {
                SemanticValueType.Number => valueType == SemanticValueType.Number,
                SemanticValueType.String => valueType == SemanticValueType.String,
                SemanticValueType.Boolean => valueType == SemanticValueType.Boolean,
                SemanticValueType.ArrayUnknown or SemanticValueType.Unknown => true,
                _ => false
            };
        }

        private static bool AreEqual(RuntimeValue left, RuntimeValue right)
        {
            if (left.Type != right.Type)
            {
                return false;
            }

            return left.Type switch
            {
                SemanticValueType.Number => left.AsNumber() == right.AsNumber(),
                SemanticValueType.String => left.AsString() == right.AsString(),
                SemanticValueType.Boolean => left.AsBoolean() == right.AsBoolean(),
                _ => false
            };
        }

        private static void EnsureType(RuntimeValue value, SemanticValueType expectedType, int line, int column, string message)
        {
            if (value.Type != expectedType)
            {
                throw new RuntimeException(line, column, message);
            }
        }
    }
}