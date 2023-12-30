namespace IronLox.SyntaxTree;

[SyntaxTree(
    Interface = "IExpression",
    VisitorInterface = "IExpressionVisitor",
    Patterns = [
        "BinaryExpression : IExpression left, Token @operator, IExpression right",
        "GroupingExpression : IExpression expression",
        "LiteralExpression : object? value",
        "UnaryExpression : Token @operator, IExpression right",
    ],
    Usings = ["IronLox.Scanning"]
)]
public static partial class Expressions;