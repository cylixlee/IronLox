using IronLox.SyntaxTree.Expressions;
using System.Text;

namespace IronLox.SyntaxTree.Misc;

/// <summary>
/// A class of different pretty-print implementations for expressions, the Visitor in Visitor Pattern.
/// </summary>
public class PrettyPrintVisitor : IExpressionVisitor<string>
{
    // Basically put operators before other tokens.
    public string Visit(Binary element) => Parenthesize(element.Operator.Lexeme, element.Left, element.Right);
    public string Visit(Grouping element) => Parenthesize("group", element.Expression);
    public string Visit(Literal element) => element.Value is null ? "nil" : element.Value.ToString()!;
    public string Visit(Unary element) => Parenthesize(element.Operator.Lexeme, element.Right);

    // Add parentheses to claim an expression in postorder traversal of AST.
    // ... for example, (+ 1 2) means 1 + 2.
    string Parenthesize(string name, params IExpression[] expressions)
    {
        var builder = new StringBuilder("(")
            .Append(name);
        foreach (var expression in expressions)
        {
            builder.Append(' ')
                .Append(expression.Accept(this));
        }
        builder.Append(')');

        return builder.ToString();
    }
}
