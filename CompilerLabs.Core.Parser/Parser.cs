using CompilerLabs.Core.Lexer;
using CompilerLabs.Core.Parser.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompilerLabs.Core.Parser
{   
    public class ParseException : Exception { }

    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _position;

        public List<string> Errors { get; } = new List<string>();

        public Parser(IEnumerable<Token> tokens)
        {
            _tokens = tokens.ToList();
            _position = 0;
        }

        public List<Statement> Parse()
        {
            var statements = new List<Statement>();
            while (!IsAtEnd())
            {
                try
                {
                    statements.Add(ParseDeclaration());
                }
                catch (ParseException)
                {
                    Synchronize();
                }
            }
            return statements;
        }

        private Statement ParseDeclaration()
        {
            if (Match(TokenType.FUNC)) return ParseFunctionDeclaration();
            if (Match(TokenType.VAR)) return ParseVarDeclaration();
            return ParseStatement();
        }

        private Statement ParseStatement()
        {
            if (Match(TokenType.IF)) return ParseIfStatement();
            if (Match(TokenType.WHILE)) return ParseWhileStatement();
            if (Match(TokenType.PRINT)) return ParsePrintStatement();
            if (Match(TokenType.RETURN)) return ParseReturnStatement();

            if (Match(TokenType.LBRACE))
            {
                var line = Previous().Line;
                var col = Previous().Column;
                return new BlockStatement(ParseBlock(), line, col);
            }

            return ParseExpressionStatement();
        }

        private Statement ParseFunctionDeclaration()
        {
            Token keyword = Previous();
            Token name = Consume(TokenType.ID, "Ожидается имя функции.");
            Consume(TokenType.LPAREN, "Ожидается '(' после имени функции.");

            var parameters = new List<string>();
            if (!Check(TokenType.RPAREN))
            {
                do
                {
                    var parameter = Consume(TokenType.ID, "Ожидается имя параметра функции.");
                    parameters.Add(parameter.Value);
                }
                while (Match(TokenType.COMMA));
            }

            Consume(TokenType.RPAREN, "Ожидается ')' после списка параметров.");
            Consume(TokenType.LBRACE, "Ожидается '{' перед телом функции.");

            var body = new BlockStatement(ParseBlock(), keyword.Line, keyword.Column);
            return new FunctionStatement(name.Value, parameters, body, keyword.Line, keyword.Column);
        }

        private Statement ParseVarDeclaration()
        {
            Token keyword = Previous();
            Token name = Consume(TokenType.ID, "Ожидается имя переменной.");
            Expression initializer = null;

            if (Match(TokenType.EQ))
            {
                initializer = ParseExpression();
            }

            Consume(TokenType.SEMICOLON, "Ожидается ';' после объявления переменной.");
            return new VarStatement(name.Value, initializer, keyword.Line, keyword.Column);
        }

        private Statement ParseIfStatement()
        {
            Token keyword = Previous();
            Consume(TokenType.LPAREN, "Ожидается '(' после 'if'.");
            Expression condition = ParseExpression();
            Consume(TokenType.RPAREN, "Ожидается ')' после условия 'if'.");

            Statement thenBranch = ParseStatement();
            Statement elseBranch = null;

            if (Match(TokenType.ELSE))
            {
                elseBranch = ParseStatement();
            }

            return new IfStatement(condition, thenBranch, elseBranch, keyword.Line, keyword.Column);
        }

        private Statement ParseWhileStatement()
        {
            Token keyword = Previous();
            Consume(TokenType.LPAREN, "Ожидается '(' после 'while'.");
            Expression condition = ParseExpression();
            Consume(TokenType.RPAREN, "Ожидается ')' после условия 'while'.");

            Statement body = ParseStatement();
            return new WhileStatement(condition, body, keyword.Line, keyword.Column);
        }

        private Statement ParsePrintStatement()
        {
            Token keyword = Previous();
            Expression value = ParseExpression();
            Consume(TokenType.SEMICOLON, "Ожидается ';' после значения.");
            return new PrintStatement(value, keyword.Line, keyword.Column);
        }

        private Statement ParseReturnStatement()
        {
            Token keyword = Previous();
            Expression? value = null;

            if (!Check(TokenType.SEMICOLON))
            {
                value = ParseExpression();
            }

            Consume(TokenType.SEMICOLON, "Ожидается ';' после return.");
            return new ReturnStatement(value, keyword.Line, keyword.Column);
        }

        private Statement ParseExpressionStatement()
        {
            Expression expr = ParseExpression();
            Token prev = Previous();
            Consume(TokenType.SEMICOLON, "Ожидается ';' после выражения.");

            if (expr is CallExpression call)
            {
                return new CallStatement(call, prev.Line, prev.Column);
            }

            return new ExpressionStatement(expr, prev.Line, prev.Column);
        }

        private List<Statement> ParseBlock()
        {
            var statements = new List<Statement>();

            while (!Check(TokenType.RBRACE) && !IsAtEnd())
            {
                statements.Add(ParseDeclaration());
            }

            Consume(TokenType.RBRACE, "Ожидается '}' после блока.");
            return statements;
        }

        private Expression ParseExpression()
        {
            return ParseAssignment();
        }

        private Expression ParseAssignment()
        {
            Expression expr = ParseLogicalOr();

            if (Match(TokenType.EQ))
            {
                Token equals = Previous();
                Expression value = ParseAssignment();

                if (expr is VariableExpression varExpr)
                {
                    return new AssignExpression(varExpr.Name, value, equals.Line, equals.Column);
                }

                if (expr is IndexExpression indexExpr)
                {
                    return new IndexAssignExpression(indexExpr.Target, indexExpr.Index, value, equals.Line, equals.Column);
                }

                Error(equals, "Недопустимая цель для присваивания.");
                throw new ParseException();
            }

            return expr;
        }

        private Expression ParseLogicalOr()
        {
            Expression expr = ParseLogicalAnd();
            while (Match(TokenType.OR))
            {
                Token op = Previous();
                Expression right = ParseLogicalAnd();
                expr = new BinaryExpression(expr, op.Type, right, op.Line, op.Column);
            }
            return expr;
        }

        private Expression ParseLogicalAnd()
        {
            Expression expr = ParseEquality();
            while (Match(TokenType.AND))
            {
                Token op = Previous();
                Expression right = ParseEquality();
                expr = new BinaryExpression(expr, op.Type, right, op.Line, op.Column);
            }
            return expr;
        }

        private Expression ParseEquality()
        {
            Expression expr = ParseComparison();
            while (Match(TokenType.EQEQ, TokenType.NEQ))
            {
                Token op = Previous();
                Expression right = ParseComparison();
                expr = new BinaryExpression(expr, op.Type, right, op.Line, op.Column);
            }
            return expr;
        }

        private Expression ParseComparison()
        {
            Expression expr = ParseTerm();
            while (Match(TokenType.LT, TokenType.LTEQ, TokenType.GT, TokenType.GTEQ))
            {
                Token op = Previous();
                Expression right = ParseTerm();
                expr = new BinaryExpression(expr, op.Type, right, op.Line, op.Column);
            }
            return expr;
        }

        private Expression ParseTerm()
        {
            Expression expr = ParseFactor();
            while (Match(TokenType.PLUS, TokenType.MINUS))
            {
                Token op = Previous();
                Expression right = ParseFactor();
                expr = new BinaryExpression(expr, op.Type, right, op.Line, op.Column);
            }
            return expr;
        }

        private Expression ParseFactor()
        {
            Expression expr = ParseUnary();
            while (Match(TokenType.STAR, TokenType.SLASH))
            {
                Token op = Previous();
                Expression right = ParseUnary();
                expr = new BinaryExpression(expr, op.Type, right, op.Line, op.Column);
            }
            return expr;
        }

        private Expression ParseUnary()
        {
            if (Match(TokenType.EXCL, TokenType.MINUS))
            {
                Token op = Previous();
                Expression right = ParseUnary();
                return new UnaryExpression(op.Type, right, op.Line, op.Column);
            }

            return ParseCall();
        }

        private Expression ParseCall()
        {
            Expression expr = ParsePrimary();

            while (true)
            {
                if (Match(TokenType.LPAREN))
                {
                    Token paren = Previous();
                    var arguments = new List<Expression>();

                    if (!Check(TokenType.RPAREN))
                    {
                        do
                        {
                            arguments.Add(ParseExpression());
                        }
                        while (Match(TokenType.COMMA));
                    }

                    Consume(TokenType.RPAREN, "Ожидается ')' после аргументов вызова.");

                    if (expr is VariableExpression callee)
                    {
                        expr = new CallExpression(callee.Name, arguments, paren.Line, paren.Column);
                        continue;
                    }

                    Error(paren, "Вызов функции возможен только по имени.");
                    throw new ParseException();
                }

                if (Match(TokenType.LBRACKET))
                {
                    Token bracket = Previous();
                    var index = ParseExpression();
                    Consume(TokenType.RBRACKET, "Ожидается ']' после индекса.");
                    expr = new IndexExpression(expr, index, bracket.Line, bracket.Column);
                    continue;
                }

                break;
            }

            return expr;
        }

        private Expression ParsePrimary()
        {
            if (Match(TokenType.NUMBER))
            {
                Token current = Previous();
                double value = double.Parse(current.Value, System.Globalization.CultureInfo.InvariantCulture);
                return new NumberExpression(value, current.Line, current.Column);
            }

            if (Match(TokenType.STRING))
            {
                Token current = Previous();
                return new StringExpression(current.Value, current.Line, current.Column);
            }

            if (Match(TokenType.TRUE))
            {
                Token current = Previous();
                return new BooleanExpression(true, current.Line, current.Column);
            }

            if (Match(TokenType.FALSE))
            {
                Token current = Previous();
                return new BooleanExpression(false, current.Line, current.Column);
            }

            if (Match(TokenType.ID))
            {
                Token current = Previous();
                return new VariableExpression(current.Value, current.Line, current.Column);
            }

            if (Match(TokenType.LPAREN))
            {
                Expression expr = ParseExpression();
                Consume(TokenType.RPAREN, "Ожидается ')' после выражения.");
                return expr;
            }

            if (Match(TokenType.LBRACKET))
            {
                Token start = Previous();
                var elements = new List<Expression>();

                if (!Check(TokenType.RBRACKET))
                {
                    do
                    {
                        elements.Add(ParseExpression());
                    }
                    while (Match(TokenType.COMMA));
                }

                Consume(TokenType.RBRACKET, "Ожидается ']' после литерала массива.");
                return new ArrayExpression(elements, start.Line, start.Column);
            }

            Error(Peek(), "Ожидается выражение.");
            throw new ParseException();
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) _position++;
            return Previous();
        }

        private bool IsAtEnd() => Peek().Type == TokenType.EOF;
        private Token Peek() => _tokens[_position];
        private Token Previous() => _tokens[_position - 1];

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            Error(Peek(), message);
            throw new ParseException();
        }

        private void Error(Token token, string message)
        {
            Errors.Add($"[Parser Error] Line {token.Line}, Col {token.Column}: {message}");
        }
        private void Synchronize()
        {
            Advance();
            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.SEMICOLON) return;

                switch (Peek().Type)
                {
                    case TokenType.VAR:
                    case TokenType.PRINT:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.FUNC:
                    case TokenType.RETURN:
                        return;
                }
                Advance();
            }
        }
    }
}