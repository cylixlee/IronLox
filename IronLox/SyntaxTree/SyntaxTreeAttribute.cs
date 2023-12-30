namespace IronLox.SyntaxTree;

[AttributeUsage(AttributeTargets.Class)]
public class SyntaxTreeAttribute : Attribute
{
    public required string Interface { get; set; }
    public required string VisitorInterface { get; set; }
    public required string[] Patterns { get; set; }
    public string[]? Usings { get; set; }
}
