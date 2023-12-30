namespace IronLox.SyntaxTree;

[SyntaxTree(
    Interface = "IStatement",
    VisitorInterface = "IStatementVisitor",
    Patterns = [
        "ExpressionStatement : IExpression expression",
        "PrintStatement : IExpression expression",
    ],
    Usings = ["static IronLox.SyntaxTree.Expressions"]
)]
public static partial class Statements;
