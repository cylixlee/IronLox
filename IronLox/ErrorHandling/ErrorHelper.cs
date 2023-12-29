using IronLox.Runtime;
using IronLox.Scanning;

namespace IronLox.ErrorHandling;

/// <summary>
/// A helper class for reporting errors and recording whether any error has occurred.
/// </summary>
public static class ErrorHelper
{
    /// <summary>
    /// Representing whether there's any compile-time error occurred.
    /// </summary>
    public static bool CompileTimeErrorOccurred { get; set; }
    /// <summary>
    /// Representing whether there's any runtime error occurred.
    /// </summary>
    public static bool RuntimeErrorOccurred { get; set; }

    public static void ReportRuntime(RuntimeException exception)
    {
        Console.WriteLine($"<line {exception.Token.Line}> {exception.Message}");
        RuntimeErrorOccurred = true;
    }

    // A shortcut in parsing stage, prints more information about Token.
    public static void ReportCompileTime(Token token, string message)
    {
        if (token.Type is TokenType.EndOfFile)
            Report(token.Line, "end of file", message);
        else Report(token.Line, $"'{token.Lexeme}'", message);
    }

    // Since we haven't introduce the filesystem, the [`place`] is left empty.
    public static void ReportCompileTime(int lineNumber, string message)
        => Report(lineNumber, "", message);

    // The real report function. Outputs the error message along with its line number and place.
    static void Report(int lineNumber, string place, string message)
    {
        Console.Error.WriteLine($"[line {lineNumber}] Error at {place}: {message}");
        CompileTimeErrorOccurred = true;
    }
}
