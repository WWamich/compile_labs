using System.Text;
using CompilerLabs.Core.Lexer;
using CompilerLabs.Core.Parser;
using CompilerLabs.Core.Parser.Ast;
using CompilerLabs.Core.Semantic;

Console.WriteLine("Lab04 Interpreter Demo");
Console.WriteLine("Введите программу. Пустая строка запускает выполнение.");
Console.WriteLine("Команды: :exit - выход, :clear - очистить буфер.");

var buffer = new StringBuilder();

while (true)
{
    Console.Write(buffer.Length == 0 ? "> " : "| ");
    var line = Console.ReadLine();

    if (line == null)
    {
        break;
    }

    if (line.Equals(":exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (line.Equals(":clear", StringComparison.OrdinalIgnoreCase))
    {
        buffer.Clear();
        Console.WriteLine("Буфер очищен.");
        continue;
    }

    if (string.IsNullOrWhiteSpace(line))
    {
        Run(buffer.ToString());
        buffer.Clear();
        continue;
    }

    buffer.AppendLine(line);
}

static void Run(string code)
{
    if (string.IsNullOrWhiteSpace(code))
    {
        Console.WriteLine("Нет кода для выполнения.");
        return;
    }

    try
    {
        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();

        var parser = new Parser(tokens);
        var ast = AstConstantFolder.FoldConstants(parser.Parse());

        if (parser.Errors.Any())
        {
            Console.WriteLine("Parser errors:");
            foreach (var error in parser.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
            return;
        }

        var semantic = new SemanticAnalyzer();
        semantic.Analyze(ast);

        if (semantic.Errors.Any())
        {
            Console.WriteLine("Semantic errors:");
            foreach (var error in semantic.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
            return;
        }

        Console.WriteLine("Output:");
        var interpreter = new Interpreter(message => Console.WriteLine($"  {message}"));
        interpreter.Execute(ast);
        Console.WriteLine("Done.");
    }
    catch (RuntimeException ex)
    {
        Console.WriteLine(ex.Message);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error: {ex.Message}");
    }
}
