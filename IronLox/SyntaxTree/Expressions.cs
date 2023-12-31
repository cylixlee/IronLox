namespace IronLox.SyntaxTree;

[SyntaxTree(
    Usings = ["IronLox.Scanning"],
    Interface = "IExpression",
    VisitorInterface = "IExpressionVisitor",
    Patterns = [
        "AssignExpression   : Token name, IExpression value",
        "BinaryExpression   : IExpression left, Token @operator, IExpression right",
        "GroupingExpression : IExpression expression",
        "LiteralExpression  : object? value",
        "UnaryExpression    : Token @operator, IExpression right",
        "VariableExpression : Token name",
    ]
)]
public static partial class Expressions;