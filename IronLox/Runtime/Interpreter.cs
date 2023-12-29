using IronLox.Ast;
using IronLox.ErrorHandling;
using IronLox.Scanning;

namespace IronLox.Runtime;

public class Interpreter : IVisitor<object?>
{
    public void Interpret(IExpression expression)
    {
        try
        {
            var value = expression.Accept(this);
            Console.WriteLine(Stringify(value));
        }
        catch (RuntimeException exception)
        {
            ErrorHelper.ReportRuntime(exception);
        }
    }

    static string Stringify(object? value)
    {
        if (value is null) return "nil";
        if (value is double)
        {
            var text = value.ToString()!;
            if (text.EndsWith(".0"))
                text = text[0..^2];
            return text;
        }
        return value.ToString()!;
    }

    public object? Visit(Binary element)
    {
        var left = element.Left.Accept(this)!;
        var right = element.Right.Accept(this)!;

        InterpreterExtension.Operator = element.Operator;
        return element.Operator.Type switch
        {
            // Arithmetics
            TokenType.Plus when left is double leftDouble && right is double rightDouble => leftDouble + rightDouble,
            TokenType.Plus when left is string leftString && right is string rightString => leftString + rightString,
            TokenType.Minus => left.EnsureNumber() - right.EnsureNumber(),
            TokenType.Slash => left.EnsureNumber() / right.EnsureNumber(),
            TokenType.Star => left.EnsureNumber() * right.EnsureNumber(),

            // Comparison
            TokenType.Greater => left.EnsureNumber() > right.EnsureNumber(),
            TokenType.GreaterEqual => left.EnsureNumber() >= right.EnsureNumber(),
            TokenType.Less => left.EnsureNumber() < right.EnsureNumber(),
            TokenType.LessEqual => left.EnsureNumber() <= right.EnsureNumber(),

            // Equality
            TokenType.BangEqual => !IsEqual(left, right),
            TokenType.EqualEqual => IsEqual(left, right),

            // Plus with mismatched types
            TokenType.Plus => throw new RuntimeException(element.Operator, "invalid plus operation with mismatched type"),

            // Unreachable
            _ => throw new NotImplementedException(),
        };
    }

    public object? Visit(Grouping element) => element.Expression.Accept(this);
    public object? Visit(Literal element) => element.Value;

    public object? Visit(Unary element)
    {
        var right = element.Right.Accept(this);

        return element.Operator.Type switch
        {
            TokenType.Minus => -(double)right!,
            TokenType.Bang => IsConvertiblyTrue(right),
            // Unreachable
            _ => throw new NotImplementedException(),
        };
    }

    static bool IsConvertiblyTrue(object? value)
    {
        if (value is null) return false;
        if (value is bool boolean) return boolean;
        return true;
    }

    static bool IsEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return false;

        return left.Equals(right);
    }
}

static class InterpreterExtension
{
    public static Token? Operator { get; set; }

    public static double EnsureNumber(this object operand)
        => operand is double doubleOperand
        ? doubleOperand
        : throw new RuntimeException(Operator!, "operand must be number.");
}