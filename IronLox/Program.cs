using IronLox.Parsing;
using IronLox.Runtime;
using IronLox.Scanning;
using IronLox.Utility;

namespace IronLox;

// Now support print! Wow!
class Program
{
    static readonly Interpreter interpreter = new();

    // The entrypoint of this compiler, accepts only one argument or none.
    // When the script arguments is fulfilled, it reads all text in that file and passes it to the Run method;
    // ... Otherwise, it reads a single line from console and passes it to the Run method,
    // ... and do that over and over again.
    static void Main(string[] args)
    {
        if (args.Length > 1)
        {
            Console.Error.WriteLine("Usage: ironlox [script]");
            ExitCodes.MalformedCommandArguments.Perform();
        }

        if (args.Length == 1) RunWithFile(args[0]);
        else RunPrompt();
    }

    // Reads all text from the file and passes to the Run method.
    static void RunWithFile(string path)
    {
        Run(File.ReadAllText(path));
        if (ErrorHelper.CompileTimeErrorOccurred) ExitCodes.CompileTimeError.Perform();
        if (ErrorHelper.RuntimeErrorOccurred) ExitCodes.RuntimeError.Perform();
    }

    // Reads a single line from console and passes it to the Run method,
    // ... and do that over and over again.
    static void RunPrompt()
    {
        while (true)
        {
            Console.Write(">>> ");
            var line = Console.ReadLine();
            if (line == null) break;

            Run(line);
            if (ErrorHelper.CompileTimeErrorOccurred) ErrorHelper.CompileTimeErrorOccurred = false;
        }
    }

    // In this stage, the Run method just calls for scanning
    // ... and output those tokens.
    static void Run(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.Scan();
        if (ErrorHelper.CompileTimeErrorOccurred)
        {
            return;
        }

        var parser = new Parser(tokens);
        var statements = parser.Parse();

        if (ErrorHelper.CompileTimeErrorOccurred)
        {
            return;
        }

        interpreter.Interpret(statements);
    }
}
