using System.Text;

namespace IronLox.GenerateAst;

class AstRepresentation(string rootInterfaceName, string visitorInterfaceName, string className, IEnumerable<AstParameter> parameters)
{
    public string ClassName => className;

    readonly string rootInterfaceName = rootInterfaceName;
    readonly string visitorInterfaceName = visitorInterfaceName;
    readonly string className = className;
    readonly IEnumerable<AstParameter> parameters = parameters;

    public override string ToString()
    {
        // Class declaration section
        var builder = new StringBuilder()
            .Append("public class ")
            .Append(className);

        // Primary constructor section
        builder.Append('(')
            .Append(parameters.First().ToString());
        foreach (var parameter in parameters.Skip(1))
        {
            builder.Append(", ").Append(parameter.ToString());
        }
        builder.Append(')');

        // Implementing interface
        builder.Append(" : ")
            .Append(rootInterfaceName)
            .AppendLine();

        // Class body sections
        builder.AppendLine("{");
        {
            // Properties section
            foreach (var param in parameters)
            {
                var underlyingMemberName = param.Name;
                var propertyName = underlyingMemberName.Capitalize();
                builder.Append("\tpublic ")
                    .Append(param.Type)
                    .Append(' ')
                    .Append(propertyName)
                    .Append(" { get; } = ")
                    .Append(underlyingMemberName)
                    .AppendLine(";");
            }
            builder.AppendLine();

            // Vistor pattern implementation section
            builder.Append("\tpublic T Accept<T>(")
                .Append(visitorInterfaceName)
                .AppendLine("<T> visitor) => visitor.Visit(this);");
        }
        builder.Append('}');

        return builder.ToString();
    }
}

static class CapitalizeExtension
{
    public static string Capitalize(this string value)
    {
        if (value.StartsWith('@'))
            return $"{value[1..].Capitalize()}";
        return $"{char.ToUpper(value[0])}{value[1..]}";
    }
}