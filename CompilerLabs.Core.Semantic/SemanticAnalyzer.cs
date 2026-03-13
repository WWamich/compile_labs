using CompilerLabs.Core.Parser.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilerLabs.Core.Semantic
{
    public class SemanticAnalyzer
    {
        private SemanticEnvironment _environment = new SemanticEnvironment();
        private readonly List<string> _errors = new List<string>();

        public void Analyze(IEnumerable<Statement> statements)
        {
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
                case VarStatement varStatement:
                    if (varStatement.Initializer != null)
                    {
                        VisitExpression(varStatement.Initializer);
                        if (!_environment.DefineVariable(varStatement.Name))
                        {
                            _errors.Add($"Variable '{varStatement.Name}' is already defined.");
                        }
                        else
                        {
                            _environment.MarkVariableAsInitialized(varStatement.Name);
                        }
                    }
                    else
                    {
                        _errors.Add($"Variable '{varStatement.Name}' must be initialized at declaration.");
                        if (!_environment.DefineVariable(varStatement.Name))
                        {
                            _errors.Add($"Variable '{varStatement.Name}' is already defined.");
                        }
                    }

                    break;
                case PrintStatement printStatement:
                    VisitExpression(printStatement.Expression);
                    break;
                case ExpressionStatement expressionStatement:
                    VisitExpression(expressionStatement.Expression);
                    break;
                case BlockStatement blockStatement:
                    var previousEnvironment = _environment;
                    var parentInitState = _environment.GetVariableInitializationState();

                    _environment = new SemanticEnvironment(previousEnvironment);

                    foreach (var innerStatement in blockStatement.Statements)
                    {
                        VisitStatement(innerStatement);
                    }

                    foreach (var unused in _environment.GetUnusedVariables())
                    {
                        _errors.Add($"Variable '{unused.Name}' is declared but never used.");
                    }

                    _environment = previousEnvironment;
                    _environment.RestoreInitializationState(parentInitState);

                    break;
                case IfStatement ifStatement:
                    VisitExpression(ifStatement.Condition);

                    var beforeThenState = _environment.GetVariableInitializationState();
                    VisitStatement(ifStatement.ThenBranch);
                    var afterThenState = _environment.GetVariableInitializationState();

                    if (ifStatement.ElseBranch != null)
                    {
                        _environment.RestoreInitializationState(beforeThenState);
                        VisitStatement(ifStatement.ElseBranch);
                        var afterElseState = _environment.GetVariableInitializationState();

                        // Find variables that are initialized in both branches
                        var thenInitVars = GetInitializedVars(afterThenState);
                        var elseInitVars = GetInitializedVars(afterElseState);
                        var commonInitVars = thenInitVars.Intersect(elseInitVars).ToList();

                        // Restore to before state and mark only commonly initialized variables
                        _environment.RestoreInitializationState(beforeThenState);
                        foreach (var varName in commonInitVars)
                        {
                            _environment.MarkVariableAsInitialized(varName);
                        }
                    }
                    else
                    {
                        // No else branch: initialization in then is not guaranteed
                        _environment.RestoreInitializationState(beforeThenState);
                    }
                    break;

                case WhileStatement whileStatement:
                    VisitExpression(whileStatement.Condition);
                    VisitStatement(whileStatement.Body);
                    break;

                default:
                    _errors.Add($"Unsupported statement type: {statement.GetType().Name}");
                    break;
            }
        }


        public void VisitExpression(Expression expression)
        {
            switch (expression)
            {
                case NumberExpression n:
                case StringExpression s:
                    break;

                case VariableExpression v:
                    if (!_environment.IsVariableDefined(v.Name))
                    {
                        _errors.Add($"Variable '{v.Name}' is not defined.");
                    }
                    else if (!_environment.IsVariableInitialized(v.Name))
                    {
                        _errors.Add($"Variable '{v.Name}' is used before initialization.");
                    }
                    else
                    {
                        _environment.MarkVariableAsUsed(v.Name);
                    }
                    break;
                case AssignExpression a:
                    VisitExpression(a.Value);
                    if (!_environment.IsVariableDefined(a.Name))
                    {
                        _errors.Add($"Variable '{a.Name}' is not defined.");
                    }
                    else
                    {
                        _environment.MarkVariableAsInitialized(a.Name);
                    }
                    break;
                case BinaryExpression b:
                    VisitExpression(b.Left);
                    VisitExpression(b.Right);
                    break;
                case UnaryExpression u:
                    VisitExpression(u.Right);
                    break;
                default:
                    _errors.Add($"Unsupported expression type: {expression.GetType().Name}");
                    break;
            }
        }

        public IEnumerable<string> Errors => _errors;

        private List<string> GetInitializedVars(Dictionary<string, bool> state)
        {
            return state.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        }
    }
}
