using CompilerLabs.Core.Parser.Ast;
using System.Collections.Generic;
using System.Linq;

namespace CompilerLabs.Core.Semantic
{
    public class SemanticAnalyzer
    {
        private SemanticEnvironment _environment = new SemanticEnvironment();
        private readonly List<string> _errors = new List<string>();
        private readonly Dictionary<string, FunctionSymbolInfo> _functions = new Dictionary<string, FunctionSymbolInfo>();
        private FunctionSymbolInfo? _currentFunction;
        public IEnumerable<string> Errors => _errors;

        public void Analyze(IEnumerable<Statement> statements)
        {
            foreach (var statement in statements)
            {
                if (statement is FunctionStatement functionStatement)
                {
                    if (_functions.ContainsKey(functionStatement.Name))
                    {
                        _errors.Add($"[{functionStatement.Line}:{functionStatement.Column}] Функция '{functionStatement.Name}' уже объявлена.");
                    }
                    else
                    {
                        _functions[functionStatement.Name] = new FunctionSymbolInfo
                        {
                            Name = functionStatement.Name,
                            Arity = functionStatement.Parameters.Count
                        };
                    }
                }
            }

            foreach (var statement in statements)
            {
                VisitStatement(statement);
            }

            foreach (var unused in _environment.GetUnusedVariables())
            {
                _errors.Add($"Variable '{unused.Name}' is declared but never used.");
            }
        }

        public void VisitStatement(Statement statement)
        {
            switch (statement)
            {
                case VarStatement v: AnalyzeVarStatement(v); break;
                case PrintStatement p: AnalyzePrintStatement(p); break;
                case ExpressionStatement e: AnalyzeExpressionStatement(e); break;
                case BlockStatement b: AnalyzeBlockStatement(b); break;
                case IfStatement i: AnalyzeIfStatement(i); break;
                case WhileStatement w: AnalyzeWhileStatement(w); break;
                case FunctionStatement f: AnalyzeFunctionStatement(f); break;
                case ReturnStatement r: AnalyzeReturnStatement(r); break;
                case CallStatement c: AnalyzeCallStatement(c); break;
                default:
                    _errors.Add($"[{statement.Line}:{statement.Column}] Неподдерживаемая инструкция: {statement.GetType().Name}");
                    break;
            }
        }

        public void VisitExpression(Expression expression)
        {
            _ = GetExpressionType(expression);
        }

        private void AnalyzeVarStatement(VarStatement stmt)
        {
            var initialType = SemanticValueType.Unknown;
            var isInitialized = false;

            if (stmt.Initializer != null)
            {
                initialType = GetExpressionType(stmt.Initializer);
                isInitialized = true;
            }

            if (!_environment.DefineVariable(stmt.Name, isInitialized, initialType))
            {
                _errors.Add($"[{stmt.Line}:{stmt.Column}] Переменная '{stmt.Name}' уже объявлена в этой области видимости.");
            }
        }

        private void AnalyzePrintStatement(PrintStatement stmt)
        {
            VisitExpression(stmt.Expression);
            CheckUnusedVariables();
        }

        private void AnalyzeExpressionStatement(ExpressionStatement stmt)
        {
            VisitExpression(stmt.Expression);
        }

        private void AnalyzeBlockStatement(BlockStatement stmt)
        {
            var previousEnvironment = _environment;
            _environment = new SemanticEnvironment(previousEnvironment);

            foreach (var innerStatement in stmt.Statements)
            {
                VisitStatement(innerStatement);
            }

            CheckUnusedVariables();
            _environment = previousEnvironment;
        }

        private void AnalyzeIfStatement(IfStatement stmt)
        {
            var conditionType = GetExpressionType(stmt.Condition);
            if (conditionType != SemanticValueType.Boolean
                && conditionType != SemanticValueType.Error
                && conditionType != SemanticValueType.Unknown)
            {
                _errors.Add($"[{stmt.Condition.Line}:{stmt.Condition.Column}] Условие if должно быть типа boolean.");
            }

            VisitStatement(stmt.ThenBranch);

            if (stmt.ElseBranch != null)
            {
                VisitStatement(stmt.ElseBranch);
            }
        }

        private void AnalyzeWhileStatement(WhileStatement stmt)
        {
            var conditionType = GetExpressionType(stmt.Condition);
            if (conditionType != SemanticValueType.Boolean
                && conditionType != SemanticValueType.Error
                && conditionType != SemanticValueType.Unknown)
            {
                _errors.Add($"[{stmt.Condition.Line}:{stmt.Condition.Column}] Условие while должно быть типа boolean.");
            }

            VisitStatement(stmt.Body);
        }

        private void AnalyzeFunctionStatement(FunctionStatement stmt)
        {
            if (!_functions.TryGetValue(stmt.Name, out var functionInfo))
            {
                functionInfo = new FunctionSymbolInfo
                {
                    Name = stmt.Name,
                    Arity = stmt.Parameters.Count
                };
                _functions[stmt.Name] = functionInfo;
            }

            var previousEnvironment = _environment;
            var previousFunction = _currentFunction;

            _environment = new SemanticEnvironment(previousEnvironment);
            _currentFunction = functionInfo;

            foreach (var parameter in stmt.Parameters)
            {
                if (!_environment.DefineVariable(parameter, true, SemanticValueType.Unknown))
                {
                    _errors.Add($"[{stmt.Line}:{stmt.Column}] Дублирующийся параметр '{parameter}' в функции '{stmt.Name}'.");
                }
            }

            foreach (var statement in stmt.Body.Statements)
            {
                VisitStatement(statement);
            }

            _environment = previousEnvironment;
            _currentFunction = previousFunction;
        }

        private void AnalyzeReturnStatement(ReturnStatement stmt)
        {
            if (_currentFunction == null)
            {
                _errors.Add($"[{stmt.Line}:{stmt.Column}] Оператор return допустим только внутри функции.");
                return;
            }

            var returnType = stmt.Value == null
                ? SemanticValueType.Unknown
                : GetExpressionType(stmt.Value);

            if (_currentFunction.ReturnType == SemanticValueType.Unknown)
            {
                _currentFunction.ReturnType = returnType;
                return;
            }

            if (returnType != SemanticValueType.Unknown
                && returnType != SemanticValueType.Error
                && _currentFunction.ReturnType != returnType)
            {
                _errors.Add($"[{stmt.Line}:{stmt.Column}] Несогласованный тип return в функции '{_currentFunction.Name}'.");
            }
        }

        private void AnalyzeCallStatement(CallStatement stmt)
        {
            _ = AnalyzeCallExpression(stmt.Call);
        }

        private void CheckUnusedVariables()
        {
            foreach (var symbol in _environment.GetLocalVariables())
            {
                if (!symbol.IsUsed)
                {
                    _errors.Add($"[Semantic Warning] Переменная '{symbol.Name}' объявлена, но ни разу не использовалась.");
                }
            }
        }

        private SemanticValueType AnalyzeVariableExpression(VariableExpression expr)
        {
            var symbol = _environment.GetVariable(expr.Name);
            if (symbol == null)
            {
                _errors.Add($"[{expr.Line}:{expr.Column}] Использование необъявленной переменной '{expr.Name}'.");
                return SemanticValueType.Error;
            }

            symbol.IsUsed = true;

            if (!symbol.IsInitialized)
            {
                _errors.Add($"[{expr.Line}:{expr.Column}] Использование неинициализированной переменной '{expr.Name}'.");
                return SemanticValueType.Error;
            }

            return symbol.Type;
        }

        private SemanticValueType AnalyzeAssignExpression(AssignExpression expr)
        {
            var valueType = GetExpressionType(expr.Value);

            var symbol = _environment.GetVariable(expr.Name);
            if (symbol == null)
            {
                _errors.Add($"[{expr.Line}:{expr.Column}] Попытка записи в необъявленную переменную '{expr.Name}'.");
                return SemanticValueType.Error;
            }

            if (symbol.Type == SemanticValueType.Unknown)
            {
                _environment.SetType(expr.Name, valueType);
            }
            else if (IsArrayType(symbol.Type) && IsArrayType(valueType))
            {
                if (symbol.Type == SemanticValueType.ArrayUnknown && valueType != SemanticValueType.Error)
                {
                    _environment.SetType(expr.Name, valueType);
                }
                else if (valueType != SemanticValueType.ArrayUnknown && valueType != SemanticValueType.Error && symbol.Type != valueType)
                {
                    _errors.Add($"[{expr.Line}:{expr.Column}] Нельзя присвоить значение типа {TypeName(valueType)} переменной '{expr.Name}' типа {TypeName(symbol.Type)}.");
                    return SemanticValueType.Error;
                }
            }
            else if (valueType != SemanticValueType.Error && symbol.Type != valueType)
            {
                _errors.Add($"[{expr.Line}:{expr.Column}] Нельзя присвоить значение типа {TypeName(valueType)} переменной '{expr.Name}' типа {TypeName(symbol.Type)}.");
                return SemanticValueType.Error;
            }

            _environment.SetInitialized(expr.Name);
            return symbol.Type == SemanticValueType.Unknown ? valueType : symbol.Type;
        }

        private SemanticValueType AnalyzeBinaryExpression(BinaryExpression expr)
        {
            var leftType = GetExpressionType(expr.Left);
            var rightType = GetExpressionType(expr.Right);

            if (leftType == SemanticValueType.Error || rightType == SemanticValueType.Error)
            {
                return SemanticValueType.Error;
            }

            if (leftType == SemanticValueType.Unknown || rightType == SemanticValueType.Unknown)
            {
                return SemanticValueType.Unknown;
            }

            switch (expr.Operator)
            {
                case Lexer.TokenType.PLUS:
                    if (leftType == SemanticValueType.Number && rightType == SemanticValueType.Number)
                    {
                        return SemanticValueType.Number;
                    }

                    if (leftType == SemanticValueType.String && rightType == SemanticValueType.String)
                    {
                        return SemanticValueType.String;
                    }

                    _errors.Add($"[{expr.Line}:{expr.Column}] Оператор '+' поддерживает только number+number или string+string.");
                    return SemanticValueType.Error;

                case Lexer.TokenType.MINUS:
                case Lexer.TokenType.STAR:
                case Lexer.TokenType.SLASH:
                    if (leftType == SemanticValueType.Number && rightType == SemanticValueType.Number)
                    {
                        return SemanticValueType.Number;
                    }

                    _errors.Add($"[{expr.Line}:{expr.Column}] Арифметические операторы поддерживаются только для number.");
                    return SemanticValueType.Error;

                case Lexer.TokenType.LT:
                case Lexer.TokenType.LTEQ:
                case Lexer.TokenType.GT:
                case Lexer.TokenType.GTEQ:
                    if (leftType == SemanticValueType.Number && rightType == SemanticValueType.Number)
                    {
                        return SemanticValueType.Boolean;
                    }

                    _errors.Add($"[{expr.Line}:{expr.Column}] Операторы сравнения '<', '<=', '>', '>=' поддерживаются только для number.");
                    return SemanticValueType.Error;

                case Lexer.TokenType.EQEQ:
                case Lexer.TokenType.NEQ:
                    if (leftType == rightType)
                    {
                        return SemanticValueType.Boolean;
                    }

                    _errors.Add($"[{expr.Line}:{expr.Column}] Операторы '==' и '!=' требуют одинаковые типы операндов.");
                    return SemanticValueType.Error;

                case Lexer.TokenType.AND:
                case Lexer.TokenType.OR:
                    if (leftType == SemanticValueType.Boolean && rightType == SemanticValueType.Boolean)
                    {
                        return SemanticValueType.Boolean;
                    }

                    _errors.Add($"[{expr.Line}:{expr.Column}] Логические операторы '&&' и '||' поддерживаются только для boolean.");
                    return SemanticValueType.Error;

                default:
                    _errors.Add($"[{expr.Line}:{expr.Column}] Неподдерживаемый бинарный оператор: {expr.Operator}.");
                    return SemanticValueType.Error;
            }
        }

        private SemanticValueType AnalyzeUnaryExpression(UnaryExpression expr)
        {
            var rightType = GetExpressionType(expr.Right);
            if (rightType == SemanticValueType.Error)
            {
                return SemanticValueType.Error;
            }

            if (rightType == SemanticValueType.Unknown)
            {
                return SemanticValueType.Unknown;
            }

            switch (expr.Operator)
            {
                case Lexer.TokenType.MINUS:
                    if (rightType == SemanticValueType.Number)
                    {
                        return SemanticValueType.Number;
                    }

                    _errors.Add($"[{expr.Line}:{expr.Column}] Унарный '-' поддерживается только для number.");
                    return SemanticValueType.Error;

                case Lexer.TokenType.EXCL:
                    if (rightType == SemanticValueType.Boolean)
                    {
                        return SemanticValueType.Boolean;
                    }

                    _errors.Add($"[{expr.Line}:{expr.Column}] Унарный '!' поддерживается только для boolean.");
                    return SemanticValueType.Error;

                default:
                    _errors.Add($"[{expr.Line}:{expr.Column}] Неподдерживаемый унарный оператор: {expr.Operator}.");
                    return SemanticValueType.Error;
            }
        }

        private SemanticValueType AnalyzeArrayExpression(ArrayExpression expr)
        {
            SemanticValueType elementType = SemanticValueType.Unknown;

            foreach (var element in expr.Elements)
            {
                var currentType = GetExpressionType(element);
                if (currentType == SemanticValueType.Error)
                {
                    return SemanticValueType.Error;
                }

                if (currentType == SemanticValueType.Unknown)
                {
                    continue;
                }

                if (elementType == SemanticValueType.Unknown)
                {
                    elementType = currentType;
                    continue;
                }

                if (elementType != currentType)
                {
                    _errors.Add($"[{expr.Line}:{expr.Column}] Элементы массива должны быть одного типа.");
                    return SemanticValueType.Error;
                }
            }

            return ToArrayType(elementType);
        }

        private SemanticValueType AnalyzeIndexExpression(IndexExpression expr)
        {
            var targetType = GetExpressionType(expr.Target);
            var indexType = GetExpressionType(expr.Index);

            if (indexType != SemanticValueType.Number && indexType != SemanticValueType.Unknown && indexType != SemanticValueType.Error)
            {
                _errors.Add($"[{expr.Index.Line}:{expr.Index.Column}] Индекс массива должен быть числом.");
                return SemanticValueType.Error;
            }

            if (targetType == SemanticValueType.Unknown)
            {
                return SemanticValueType.Unknown;
            }

            if (!IsArrayType(targetType))
            {
                _errors.Add($"[{expr.Target.Line}:{expr.Target.Column}] Обращение по индексу возможно только к массиву.");
                return SemanticValueType.Error;
            }

            return FromArrayType(targetType);
        }

        private SemanticValueType AnalyzeIndexAssignExpression(IndexAssignExpression expr)
        {
            var targetType = GetExpressionType(expr.Target);
            var indexType = GetExpressionType(expr.Index);
            var valueType = GetExpressionType(expr.Value);

            if (indexType != SemanticValueType.Number && indexType != SemanticValueType.Unknown && indexType != SemanticValueType.Error)
            {
                _errors.Add($"[{expr.Index.Line}:{expr.Index.Column}] Индекс массива должен быть числом.");
                return SemanticValueType.Error;
            }

            if (targetType == SemanticValueType.Unknown)
            {
                return valueType;
            }

            if (!IsArrayType(targetType))
            {
                _errors.Add($"[{expr.Target.Line}:{expr.Target.Column}] Присваивание по индексу возможно только для массива.");
                return SemanticValueType.Error;
            }

            var elementType = FromArrayType(targetType);
            if (elementType == SemanticValueType.Unknown)
            {
                if (expr.Target is VariableExpression variable && _environment.GetVariable(variable.Name) != null)
                {
                    var inferredArrayType = ToArrayType(valueType);
                    if (inferredArrayType != SemanticValueType.ArrayUnknown)
                    {
                        _environment.SetType(variable.Name, inferredArrayType);
                    }
                }

                return valueType;
            }

            if (valueType != SemanticValueType.Unknown && valueType != SemanticValueType.Error && valueType != elementType)
            {
                _errors.Add($"[{expr.Value.Line}:{expr.Value.Column}] Нельзя записать значение типа {TypeName(valueType)} в массив типа {TypeName(targetType)}.");
                return SemanticValueType.Error;
            }

            return elementType;
        }

        private SemanticValueType GetExpressionType(Expression expression)
        {
            switch (expression)
            {
                case NumberExpression:
                    return SemanticValueType.Number;
                case StringExpression:
                    return SemanticValueType.String;
                case BooleanExpression:
                    return SemanticValueType.Boolean;
                case VariableExpression v:
                    return AnalyzeVariableExpression(v);
                case AssignExpression a:
                    return AnalyzeAssignExpression(a);
                case BinaryExpression b:
                    return AnalyzeBinaryExpression(b);
                case UnaryExpression u:
                    return AnalyzeUnaryExpression(u);
                case ArrayExpression a:
                    return AnalyzeArrayExpression(a);
                case IndexExpression i:
                    return AnalyzeIndexExpression(i);
                case IndexAssignExpression ia:
                    return AnalyzeIndexAssignExpression(ia);
                case CallExpression c:
                    return AnalyzeCallExpression(c);
                default:
                    _errors.Add($"[{expression.Line}:{expression.Column}] Неподдерживаемое выражение: {expression.GetType().Name}");
                    return SemanticValueType.Error;
            }
        }

        private SemanticValueType AnalyzeCallExpression(CallExpression expr)
        {
            if (!_functions.TryGetValue(expr.Callee, out var functionInfo))
            {
                _errors.Add($"[{expr.Line}:{expr.Column}] Вызов необъявленной функции '{expr.Callee}'.");
                return SemanticValueType.Error;
            }

            foreach (var argument in expr.Arguments)
            {
                _ = GetExpressionType(argument);
            }

            if (expr.Arguments.Count != functionInfo.Arity)
            {
                _errors.Add($"[{expr.Line}:{expr.Column}] Функция '{expr.Callee}' ожидает {functionInfo.Arity} арг., передано {expr.Arguments.Count}.");
                return SemanticValueType.Error;
            }

            return functionInfo.ReturnType;
        }

        private static bool IsArrayType(SemanticValueType type)
        {
            return type == SemanticValueType.ArrayUnknown
                || type == SemanticValueType.ArrayNumber
                || type == SemanticValueType.ArrayString
                || type == SemanticValueType.ArrayBoolean;
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

        private static SemanticValueType FromArrayType(SemanticValueType arrayType)
        {
            return arrayType switch
            {
                SemanticValueType.ArrayNumber => SemanticValueType.Number,
                SemanticValueType.ArrayString => SemanticValueType.String,
                SemanticValueType.ArrayBoolean => SemanticValueType.Boolean,
                _ => SemanticValueType.Unknown
            };
        }

        private static string TypeName(SemanticValueType type)
        {
            return type switch
            {
                SemanticValueType.Number => "number",
                SemanticValueType.String => "string",
                SemanticValueType.Boolean => "boolean",
                SemanticValueType.ArrayUnknown => "array",
                SemanticValueType.ArrayNumber => "array<number>",
                SemanticValueType.ArrayString => "array<string>",
                SemanticValueType.ArrayBoolean => "array<boolean>",
                SemanticValueType.Unknown => "unknown",
                _ => "error"
            };
        }
    }
}