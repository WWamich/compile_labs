using System;
using System.Linq;
using CompilerLabs.Core.Lexer;
using CompilerLabs.Core.Parser;
using CompilerLabs.Core.Semantic;


Console.WriteLine("Test 1: Unused variable");
var code1 = "var x = 5; var y = 10; print y;";
Console.WriteLine($"Code: {code1}\n");
TestAnalysis(code1);

Console.WriteLine("\nTest 2: All variables used");
var code2 = "var x = 5; var y = 10; print x + y;";
Console.WriteLine($"Code: {code2}\n");
TestAnalysis(code2);

Console.WriteLine("\nTest 3: Multiple unused variables");
var code3 = "var a = 1; var b = 2; var c = 3; print a;";
Console.WriteLine($"Code: {code3}\n");
TestAnalysis(code3);

Console.WriteLine("\nTest 4: Variable used in condition");
var code4 = "var x = 5; var y = 10; if (x > 3) { print y; }";
Console.WriteLine($"Code: {code4}\n");
TestAnalysis(code4);

Console.WriteLine("\nTest 5: Variable used in assignment");
var code5 = "var x = 5; var y = 0; y = x + 10; print y;";
Console.WriteLine($"Code: {code5}\n");
TestAnalysis(code5);

Console.WriteLine("\nTest 6: Variable assigned but not read");
var code6 = "var x = 5; x = 10; print 42;";
Console.WriteLine($"Code: {code6}\n");
TestAnalysis(code6);

Console.WriteLine("\n=== UNINITIALIZED VARIABLE TESTS ===\n");

Console.WriteLine("Test N1: Variable without initializer");
var codeN1 = "var x;";
Console.WriteLine($"Code: {codeN1}\n");
TestAnalysis(codeN1);

Console.WriteLine("\nTest N2: Variable initialized at declaration");
var codeN2 = "var x = 5; print x;";
Console.WriteLine($"Code: {codeN2}\n");
TestAnalysis(codeN2);

Console.WriteLine("\nTest N3: Variable reinitialized");
var codeN3 = "var x = 5; x = 10; print x;";
Console.WriteLine($"Code: {codeN3}\n");
TestAnalysis(codeN3);

Console.WriteLine("\nTest N4: Variable initialized before if block");
var codeN4 = "var x = 5; var cond = 1; if (cond > 0) { x = 10; } print x;";
Console.WriteLine($"Code: {codeN4}\n");
TestAnalysis(codeN4);

Console.WriteLine("\nTest N5: Variable not initialized before if block");
var codeN5 = "var x; var cond = 1; if (cond > 0) { x = 10; } print x;";
Console.WriteLine($"Code: {codeN5}\n");
TestAnalysis(codeN5);

Console.WriteLine("\nTest N6: Variable initialized in both if/else branches");
var codeN6 = "var x = 1; var cond = 1; if (cond > 0) { x = 2; } else { x = 3; } print x;";
Console.WriteLine($"Code: {codeN6}\n");
TestAnalysis(codeN6);

Console.WriteLine("\nTest N7: Variable initialized only in if branch (no else)");
var codeN7 = "var cond = 1; var x; if (cond > 0) { x = 2; } print x;";
Console.WriteLine($"Code: {codeN7}\n");
TestAnalysis(codeN7);

Console.WriteLine("\nTest N8: Uninitialized variable in expression");
var codeN8 = "var x; print x + 5;";
Console.WriteLine($"Code: {codeN8}\n");
TestAnalysis(codeN8);

void TestAnalysis(string code)
{
    var lexer = new Lexer(code);
    var tokens = lexer.Tokenize();
    var parser = new Parser(tokens);
    var ast = parser.Parse();

    var analyzer = new SemanticAnalyzer();
    analyzer.Analyze(ast);

    if (analyzer.Errors.Any())
    {
        Console.WriteLine("Errors detected:");
        foreach (var err in analyzer.Errors)
            Console.WriteLine($"  - {err}");
    }
    else
    {
        Console.WriteLine("✓ No errors");
    }
}
