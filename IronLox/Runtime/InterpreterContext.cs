using IronLox.Scanning;

namespace IronLox.Runtime;

public class InterpreterContext(InterpreterContext? parent = null)
{
    public InterpreterContext? Parent { get; } = parent;

    readonly Dictionary<string, object?> values = [];

    public void DefineVariable(string name, object? value)
    {
        if (!values.TryAdd(name, value))
            values[name] = value;
    }

    public object? RetrieveVariable(Token name)
    {
        if (values.TryGetValue(name.Lexeme, out var value))
            return value;
        if (Parent is not null) return Parent.RetrieveVariable(name);
        throw new RuntimeException(name, $"undefined variable '{name.Lexeme}'.");
    }

    public void Assign(Token name, object? value)
    {
        if (values.ContainsKey(name.Lexeme))
        {
            values[name.Lexeme] = value;
            return;
        }
        if (Parent is not null)
        {
            Parent.Assign(name, value);
            return;
        }
        throw new RuntimeException(name, $"undefined variable '{name.Lexeme}'.");
    }
}
