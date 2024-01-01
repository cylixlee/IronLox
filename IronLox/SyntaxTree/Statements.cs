namespace IronLox.SyntaxTree;

[SyntaxTree(
    Usings = [
        "IronLox.Scanning",
        "static IronLox.SyntaxTree.Expressions",
    ],
    Interface = "IStatement",
    VisitorInterface = "IStatementVisitor",
    Patterns = [
        "BlockStatement               : IEnumerable<IStatement> statements",
        "ExpressionStatement          : IExpression expression",
        "IfStatement                  : IExpression condition, IStatement thenBranch, IStatement? elseBranch",
        "PrintStatement               : IExpression expression",
        "VariableDeclarationStatement : Token name, IExpression? initializer",
    ]
)]
public static partial class Statements;
