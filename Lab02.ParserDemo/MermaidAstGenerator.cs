using CompilerLabs.Core.Parser.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab02.ParserDemo
{
    public class MermaidAstGenerator
    {
        private int _nodeCounter = 0;
        private readonly StringBuilder _sb = new StringBuilder();

        public string Generate(List<Statement> statements)
        {
            _sb.Clear();
            _nodeCounter = 0;

            _sb.AppendLine("graph TD");

            string rootId = GetNextId();
            _sb.AppendLine($"    {rootId}[\"Root (Program)\"]");

            foreach (var stmt in statements)
            {
                string childId = VisitNode(stmt);
                if (childId != null)
                {
                    _sb.AppendLine($"    {rootId} --> {childId}");
                }
            }

            return _sb.ToString();
        }

        private string GetNextId()
        {
            return $"node{_nodeCounter++}";
        }

        private string VisitNode(object node)
        {
            if (node == null) return null;

            string id = GetNextId();

            void AddNode(string label, string shapeLeft = "[", string shapeRight = "]")
            {
                string safeLabel = label.Replace("\"", "\\\"");
                _sb.AppendLine($"    {id}{shapeLeft}\"{safeLabel}\"{shapeRight}");
            }

            void LinkToChild(object childNode, string edgeLabel = null)
            {
                if (childNode == null) return;
                string childId = VisitNode(childNode);
                if (childId != null)
                {
                    if (string.IsNullOrEmpty(edgeLabel))
                        _sb.AppendLine($"    {id} --> {childId}");
                    else
                        _sb.AppendLine($"    {id} -- \"{edgeLabel}\" --> {childId}");
                }
            }

            switch (node)
            {
                case VarStatement v:
                    AddNode($"Var: {v.Name}");
                    LinkToChild(v.Initializer, "init");
                    break;

                case PrintStatement p:
                    AddNode("Print");
                    LinkToChild(p.Expression);
                    break;

                case IfStatement i:
                    AddNode("If", "{", "}");
                    LinkToChild(i.Condition, "condition");
                    LinkToChild(i.ThenBranch, "then");
                    LinkToChild(i.ElseBranch, "else");
                    break;

                case WhileStatement w:
                    AddNode("While", "{", "}");
                    LinkToChild(w.Condition, "condition");
                    LinkToChild(w.Body, "body");
                    break;

                case BlockStatement b:
                    AddNode("Block");
                    foreach (var stmt in b.Statements)
                    {
                        LinkToChild(stmt);
                    }
                    break;

                case ExpressionStatement e:
                    AddNode("ExprStmt");
                    LinkToChild(e.Expression);
                    break;

                case BinaryExpression bin:
                    AddNode($"Binary: {bin.Operator}", "(", ")");
                    LinkToChild(bin.Left, "left");
                    LinkToChild(bin.Right, "right");
                    break;

                case UnaryExpression un:
                    AddNode($"Unary: {un.Operator}", "(", ")");
                    LinkToChild(un.Right);
                    break;

                case AssignExpression assign:
                    AddNode($"Assign: {assign.Name} =", "(", ")");
                    LinkToChild(assign.Value, "value");
                    break;
                
                case IndexAssignExpression indexAssign:
                    AddNode("IndexAssign", "(", ")");
                    LinkToChild(indexAssign.Target, "target");
                    LinkToChild(indexAssign.Index, "index");
                    LinkToChild(indexAssign.Value, "value");
                    break;

                case NumberExpression num:
                    AddNode($"{num.Value}", "((", "))");
                    break;

                case StringExpression str:
                    AddNode($"\"{str.Value}\"", "((", "))");
                    break;
                
                case BooleanExpression boolean:
                    AddNode(boolean.Value ? "true" : "false", "((", "))");
                    break;

                case ArrayExpression array:
                    AddNode("Array", "(", ")");
                    foreach (var element in array.Elements)
                    {
                        LinkToChild(element);
                    }
                    break;

                case IndexExpression index:
                    AddNode("Index", "(", ")");
                    LinkToChild(index.Target, "target");
                    LinkToChild(index.Index, "index");
                    break;

                case VariableExpression varExpr:
                    AddNode($"var {varExpr.Name}", "((", "))");
                    break;

                default:
                    AddNode($"Unknown: {node.GetType().Name}");
                    break;
            }

            return id;
        }
    }
}
