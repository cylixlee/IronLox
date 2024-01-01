using IronLox.Scanning;
using IronLox.Utility;
using static IronLox.SyntaxTree.Expressions;
using static IronLox.SyntaxTree.Statements;

namespace IronLox.Runtime;

public class Interpreter : IExpressionVisitor<object?>, IStatementVisitor<object?>
{
    InterpreterContext context = new();

    public void Interpret(IEnumerable<IStatement> statements)
    {
        try
        {
            foreach (var statement in statements)
            {
                statement.Accept(this);
            }
        }
        catch (RuntimeException exception)
        {
            ErrorHelper.ReportRuntime(exception);
        }
    }

    void ExecuteBlock(IEnumerable<IStatement> statements, InterpreterContext context)
    {
        var previousContext = this.context;
        try
        {
            this.context = context;
            foreach (var statement in statements)
            {
                statement.Accept(this);
            }
        }
        finally { this.context = previousContext; }
    }

    #region Expressions
    public object? Visit(AssignExpression element)
    {
        var value = element.Value.Accept(this);
        context.Assign(element.Name, value);
        return value;
    }

    public object? Visit(BinaryExpression element)
    {
        var left = element.Left.Accept(this)!;
        var right = element.Right.Accept(this)!;

        InterpreterExtension.Operator = element.Operator;
        return element.Operator.Type switch
        {
            // Arithmetics
            TokenType.Plus when left is double leftDouble && right is double rightDouble => leftDouble + rightDouble,
            TokenType.Plus when left is string leftString && right is string rightString => leftString + rightString,
            //TokenType.Plus when left is string leftString && right is double rightNumber => leftString + Stringify(rightNumber),
            //TokenType.Plus when left is double leftNumber && right is string rightString => Stringify(leftNumber) + rightString,
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

    public object? Visit(GroupingExpression element) => element.Expression.Accept(this);
    public object? Visit(LiteralExpression element) => element.Value;

    public object? Visit(LogicalExpression element)
    {
        var left = element.Left.Accept(this);

        if (element.Operator.Type is TokenType.Or)
        {
            if (IsConvertiblyTrue(left)) return left;
        }
        else
        {
            if (!IsConvertiblyTrue(left)) return left;
        }
        return element.Right.Accept(this);
    }

    public object? Visit(UnaryExpression element)
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

    public object? Visit(VariableExpression element) => context.RetrieveVariable(element.Name);
    #endregion

    #region Statements
    public object? Visit(BlockStatement element)
    {
        ExecuteBlock(element.Statements, new(context));
        return null;
    }

    public object? Visit(ExpressionStatement element) => element.Expression.Accept(this);

    public object? Visit(IfStatement element)
    {
        if (IsConvertiblyTrue(element.Condition))
            element.ThenBranch.Accept(this);
        else
            element.ElseBranch?.Accept(this);
        return null;
    }

    public object? Visit(PrintStatement element)
    {
        var previousColor = Console.ForegroundColor;
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(Stringify(element.Expression.Accept(this)));
        }
        Console.ForegroundColor = previousColor;
        return null;
    }

    public object? Visit(VariableDeclarationStatement element)
    {
        object? value = null;
        if (element.Initializer is not null)
            value = element.Initializer.Accept(this);
        context.DefineVariable(element.Name.Lexeme, value);
        return value;
    }

    public object? Visit(WhileStatement element)
    {
        while (IsConvertiblyTrue(element.Condition.Accept(this)))
        {
            element.Body.Accept(this);
        }
        return null;
    }
    #endregion

    #region Helpers
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
    #endregion
}

static class InterpreterExtension
{
    public static Token? Operator { get; set; }

    public static double EnsureNumber(this object operand)
        => operand is double doubleOperand
        ? doubleOperand
        : throw new RuntimeException(Operator!, "operand must be number.");
}