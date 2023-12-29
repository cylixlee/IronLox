using IronLox.Scanning;

namespace IronLox.Runtime;

public class RuntimeException(Token token, string message) : ApplicationException(message)
{
    public Token Token { get; set; } = token;
}
