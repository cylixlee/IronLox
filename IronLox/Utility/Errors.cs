using IronLox.Runtime;
using IronLox.Scanning;

namespace IronLox.Utility;

/// <summary>
/// A helper enum representing the reason to exit abnormally.
/// </summary>
public enum ExitCodes
{
    MalformedCommandArguments = 64,
    CompileTimeError,
    RuntimeError = 70
}

/// <summary>
/// An extension for easy and elegant exiting. <br />
/// For example, you can call <c>ExitCodes.CompileTimeError.Perform()</c>
///     instead of a super long method call with  and type conversion in it.
/// </summary>
public static class ExitCodesExtension
{
    /// <summary>
    /// Automatically calls <c>Environment.Exit</c> with type conversion.
    /// </summary>
    public static void Perform(this ExitCodes exitCode)
    {
        Environment.Exit((int)exitCode);
    }
}

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
        WriteErrorLine($"RuntimeError <line {exception.Token.Line}>", exception.Message);
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
        WriteErrorLine($"CompileTimeError [line {lineNumber}]", $"at {place}: {message}");
        CompileTimeErrorOccurred = true;
    }

    #region Colorize
    static void WriteErrorLine(string tag, string message)
    {
        WriteColored(ConsoleColor.Red, tag, Console.Error);
        Console.Error.Write(' ');
        Console.Error.WriteLine(message);
    }

    static void WriteColored(ConsoleColor color, string message, TextWriter? target = null)
    {
        var previousColor = Console.ForegroundColor;
        {
            Console.ForegroundColor = color;
            (target ?? Console.Out).Write(message);
        }
        Console.ForegroundColor = previousColor;
    }

    static void WriteColoredLine(ConsoleColor color, string message, TextWriter? target = null)
    {
        WriteColored(color, message, target);
        Console.WriteLine();
    }
    #endregion
}
