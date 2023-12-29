namespace IronLox.ErrorHandling;

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
