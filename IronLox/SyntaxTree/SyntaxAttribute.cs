namespace IronLox.SyntaxTree;

[AttributeUsage(AttributeTargets.Class)]
public class SyntaxAttribute : Attribute
{
    public required string Interface { get; set; }
    public required string VisitorInterface { get; set; }
    public required string Pattern { get; set; }
    public string[]? Usings { get; set; }
}
