/* Auto generated by IronLox.SourceGeneration */

namespace IronLox.SyntaxTree.Expressions;

public class Grouping(IExpression expression) : IExpression
{
	public IExpression Expression { get; } = expression;

	public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.Visit(this);
}