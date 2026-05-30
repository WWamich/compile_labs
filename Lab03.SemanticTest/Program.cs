using System;
using System.Linq;
using CompilerLabs.Core.Lexer;
using CompilerLabs.Core.Parser;
using CompilerLabs.Core.Parser.Ast;
using CompilerLabs.Core.Semantic;


Console.WriteLine("Test 1 -Unused variable");
var code1 = "var x = 5; var y = 10; print y;";
Console.WriteLine($"Code: {code1}\n");
TestAnalysis(code1);

Console.WriteLine("Test 2 - All variables used");
var code2 = "var x = 5; var y = 10; print x + y;";
Console.WriteLine($"Code: {code2}\n");
TestAnalysis(code2);

Console.WriteLine("\nTest 3 -Multiple unused variables");
var code3 = "var a = 1; var b = 2; var c = 3; print a;";
Console.WriteLine($"Code: {code3}\n");
TestAnalysis(code3);

Console.WriteLine("\nTest 4 - Variable used in condition");
var code4 = "var x = 5; var y = 10; if (x > 3) { print y; }";
Console.WriteLine($"Code: {code4}\n");
TestAnalysis(code4);

Console.WriteLine("\nTest 5 - Variable used in assignment");
var code5 = "var x = 5; var y = 0; y = x + 10; print y;";
Console.WriteLine($"Code: {code5}\n");
TestAnalysis(code5);

Console.WriteLine("\nTest 6 - Variable assigned but not read");
var code6 = "var x = 5; x = 10; print 42;";
Console.WriteLine($"Code: {code6}\n");
TestAnalysis(code6);


Console.WriteLine("Test N1 - Variable without initializer");
var codeN1 = "var x;";
Console.WriteLine($"Code: {codeN1}\n");
TestAnalysis(codeN1);

Console.WriteLine("\nTest N2 - Variable initialized at declaration");
var codeN2 = "var x = 5; print x;";
Console.WriteLine($"Code: {codeN2}\n");
TestAnalysis(codeN2);

Console.WriteLine("\nTest N3 - Variable reinitialized");
var codeN3 = "var x = 5; x = 10; print x;";
Console.WriteLine($"Code: {codeN3}\n");
TestAnalysis(codeN3);

Console.WriteLine("\nTest N4 - Variable initialized before if block");
var codeN4 = "var x = 5; var cond = 1; if (cond > 0) { x = 10; } print x;";
Console.WriteLine($"Code: {codeN4}\n");
TestAnalysis(codeN4);

Console.WriteLine("\nTest N5 - Variable not initialized before if block");
var codeN5 = "var x; var cond = 1; if (cond > 0) { x = 10; } print x;";
Console.WriteLine($"Code: {codeN5}\n");
TestAnalysis(codeN5);

Console.WriteLine("\nTest N6 - Variable initialized in both if/else branches");
var codeN6 = "var x = 1; var cond = 1; if (cond > 0) { x = 2; } else { x = 3; } print x;";
Console.WriteLine($"Code: {codeN6}\n");
TestAnalysis(codeN6);

Console.WriteLine("\nTest N7 - Variable initialized only in if branch (no else)");
var codeN7 = "var cond = 1; var x; if (cond > 0) { x = 2; } print x;";
Console.WriteLine($"Code: {codeN7}\n");
TestAnalysis(codeN7);

Console.WriteLine("\nTest N8 - Uninitialized variable in expression");
var codeN8 = "var x; print x + 5;";
Console.WriteLine($"Code: {codeN8}\n");
TestAnalysis(codeN8);

Console.WriteLine("\n=== Type Tests ===");

Console.WriteLine("\nType T1 - Number arithmetic");
var codeT1 = "var x = 1 + 2 * 3; print x;";
Console.WriteLine($"Code: {codeT1}\n");
TestAnalysis(codeT1);

Console.WriteLine("\nType T2 - String concatenation");
var codeT2 = "var s = \"hello\" + \" world\"; print s;";
Console.WriteLine($"Code: {codeT2}\n");
TestAnalysis(codeT2);

Console.WriteLine("\nType T3 - Boolean logic and if condition");
var codeT3 = "var b = true && !false; if (b) { print 1; }";
Console.WriteLine($"Code: {codeT3}\n");
TestAnalysis(codeT3);

Console.WriteLine("\nType T4 - Number comparison returns boolean");
var codeT4 = "var ok = 10 >= 3; if (ok) { print 1; }";
Console.WriteLine($"Code: {codeT4}\n");
TestAnalysis(codeT4);

Console.WriteLine("\nType E1 - number + string is invalid");
var codeE1 = "var bad = 1 + \"x\"; print bad;";
Console.WriteLine($"Code: {codeE1}\n");
TestAnalysis(codeE1);

Console.WriteLine("\nType E2 - string - string is invalid");
var codeE2 = "var bad = \"a\" - \"b\"; print bad;";
Console.WriteLine($"Code: {codeE2}\n");
TestAnalysis(codeE2);

Console.WriteLine("\nType E3 - boolean + boolean is invalid");
var codeE3 = "var bad = true + false; print bad;";
Console.WriteLine($"Code: {codeE3}\n");
TestAnalysis(codeE3);

Console.WriteLine("\nType E4 - if condition must be boolean");
var codeE4 = "if (1) { print 1; }";
Console.WriteLine($"Code: {codeE4}\n");
TestAnalysis(codeE4);

Console.WriteLine("\nType E5 - while condition must be boolean");
var codeE5 = "while (\"loop\") { print 1; }";
Console.WriteLine($"Code: {codeE5}\n");
TestAnalysis(codeE5);

Console.WriteLine("\nType E6 - reassignment with different type is invalid");
var codeE6 = "var x = 1; x = \"new\"; print x;";
Console.WriteLine($"Code: {codeE6}\n");
TestAnalysis(codeE6);

Console.WriteLine("\nType E7 - unary ! supports only boolean");
var codeE7 = "print !5;";
Console.WriteLine($"Code: {codeE7}\n");
TestAnalysis(codeE7);

Console.WriteLine("\nType E8 - && supports only boolean");
var codeE8 = "print \"a\" && \"b\";";
Console.WriteLine($"Code: {codeE8}\n");
TestAnalysis(codeE8);

Console.WriteLine("\n=== Function Tests (Semantic) ===");

Console.WriteLine("\nFunc S1 - Simple function declaration and call");
var funcS1 = "func add(a, b) { return a + b; } var x = add(2, 3); print x;";
Console.WriteLine($"Code: {funcS1}\n");
TestAnalysis(funcS1);

Console.WriteLine("\nFunc S2 - Wrong argument count");
var funcS2 = "func inc(a) { return a + 1; } print inc(1, 2);";
Console.WriteLine($"Code: {funcS2}\n");
TestAnalysis(funcS2);

Console.WriteLine("\nFunc S3 - Return outside function");
var funcS3 = "return 1;";
Console.WriteLine($"Code: {funcS3}\n");
TestAnalysis(funcS3);

Console.WriteLine("\nFunc S4 - Call undeclared function");
var funcS4 = "print missing(1);";
Console.WriteLine($"Code: {funcS4}\n");
TestAnalysis(funcS4);

Console.WriteLine("\n=== Interpreter Tests ===");

Console.WriteLine("\nInterp I1 - Basic arithmetic and print");
var interp1 = "var x = 10; var y = 5; print x + y * 2;";
Console.WriteLine($"Code: {interp1}\n");
RunInterpreter(interp1);

Console.WriteLine("\nInterp I2 - String concatenation");
var interp2 = "var a = \"Hello\"; var b = \"World\"; print a + b;";
Console.WriteLine($"Code: {interp2}\n");
RunInterpreter(interp2);

Console.WriteLine("\nInterp I3 - if/while execution");
var interp3 = "var i = 0; while (i < 3) { print i; i = i + 1; } if (true) { print \"done\"; }";
Console.WriteLine($"Code: {interp3}\n");
RunInterpreter(interp3);

Console.WriteLine("\nInterp E1 - Runtime error on invalid '+' operands");
var interpE1 = "print 1 + \"x\";";
Console.WriteLine($"Code: {interpE1}\n");
RunInterpreter(interpE1);

Console.WriteLine("\nInterp I4 - Function call with return");
var interp4 = "func mul(a, b) { return a * b; } print mul(6, 7);";
Console.WriteLine($"Code: {interp4}\n");
RunInterpreter(interp4);

Console.WriteLine("\nInterp E2 - Runtime error on unknown function");
var interpE2 = "print nope(1);";
Console.WriteLine($"Code: {interpE2}\n");
RunInterpreter(interpE2);

Console.WriteLine("\n=== Array Tests ===");

Console.WriteLine("\nArray A1 - Array literal and index access");
var array1 = "var arr = [1, 2, 3]; print arr[1];";
Console.WriteLine($"Code: {array1}\n");
TestAnalysis(array1);
RunInterpreter(array1);

Console.WriteLine("\nArray A2 - Index assignment");
var array2 = "var arr = [\"a\", \"b\"]; arr[0] = \"z\"; print arr[0];";
Console.WriteLine($"Code: {array2}\n");
TestAnalysis(array2);
RunInterpreter(array2);

Console.WriteLine("\nArray E1 - Mixed element types are invalid");
var arrayE1 = "var bad = [1, \"x\"];";
Console.WriteLine($"Code: {arrayE1}\n");
TestAnalysis(arrayE1);

Console.WriteLine("\nArray E2 - Out of bounds index at runtime");
var arrayE2 = "var arr = [1]; print arr[1];";
Console.WriteLine($"Code: {arrayE2}\n");
RunInterpreter(arrayE2);

void TestAnalysis(string code)
{
    var lexer = new Lexer(code);
    var tokens = lexer.Tokenize();
    var parser = new Parser(tokens);
    var ast = AstConstantFolder.FoldConstants(parser.Parse());

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
        Console.WriteLine("No errors");
    }
}

void RunInterpreter(string code)
{
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

        var interpreter = new Interpreter(s => Console.WriteLine($"  [print] {s}"));
        interpreter.Execute(ast);
        Console.WriteLine("Interpreter finished without runtime errors");
    }
    catch (RuntimeException ex)
    {
        Console.WriteLine(ex.Message);
    }
}
