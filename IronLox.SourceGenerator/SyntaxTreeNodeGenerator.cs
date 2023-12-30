using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace IronLox.SourceGenerator;

[Generator]
public class SyntaxTreeNodeGenerator : IIncrementalGenerator
{
    const string ExpectedToAffectOn = "IronLox.SyntaxTree.SyntaxAttribute";

    struct SyntaxTreeNodeContext
    {
        public string Namespace { get; set; }
        public string Class { get; set; }
        public string Interface { get; set; }
        public string VisitorInterface { get; set; }
        public string Pattern { get; set; }
        public string[]? Usings { get; set; }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxTreeNodeContexts = context.SyntaxProvider.CreateSyntaxProvider(
            FetchClassDeclarations,
            FilterClassDeclarations
        ).Where(x => x is not null).Select((x, _) => (SyntaxTreeNodeContext)x!);

        context.RegisterSourceOutput(syntaxTreeNodeContexts, Generate);
    }

    #region SourceGeneration
    static void Generate(SourceProductionContext context, SyntaxTreeNodeContext syntaxTreeNodeContext)
    {
        context.AddSource(
            FullName(syntaxTreeNodeContext.Namespace, syntaxTreeNodeContext.Class),
            GetSource(syntaxTreeNodeContext)
        );
    }

    static string GetSource(SyntaxTreeNodeContext context)
    {
        var builder = new StringBuilder();
        if (context.Usings is not null)
        {
            foreach (var usingItem in context.Usings)
            {
                builder.AppendLine($"using {usingItem};");
            }
        }

        builder.AppendLine($"namespace {context.Namespace};")
            .AppendLine()
            .Append($"public partial class {context.Class}(");
        var pattern = ParsePattern(context.Pattern);
        {
            var argumentList = new List<string>();
            foreach (var pair in pattern)
            {
                argumentList.Add($"{pair.Type} {pair.Name}");
            }
            builder.Append(string.Join(", ", argumentList));
        }
        builder.AppendLine($") : {context.Interface}")
            .AppendLine("{");
        foreach (var pair in pattern)
        {
            builder.AppendLine($"\tpublic {pair.Type} {Capitalize(pair.Name)} {{ get; set; }} = {pair.Name};");
        }
        builder.AppendLine()
            .AppendLine($"\tpublic T Accept<T>({context.VisitorInterface}<T> visitor) => visitor.Visit(this);")
            .AppendLine("}");
        return builder.ToString();
    }

    static IEnumerable<(string Type, string Name)> ParsePattern(string pattern)
    {
        var properties = new List<(string, string)>();
        var firstSplit = pattern.Split(',');
        foreach (var propertyPattern in firstSplit)
        {
            var secondSplit = propertyPattern.Trim().Split();
            properties.Add((secondSplit[0], secondSplit[1]));
        }
        return properties;
    }
    #endregion

    #region FetchAndFilter
    static bool FetchClassDeclarations(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    static SyntaxTreeNodeContext? FilterClassDeclarations(GeneratorSyntaxContext context, CancellationToken _)
    {
        var symbol = (context.SemanticModel.GetDeclaredSymbol(context.Node) as ITypeSymbol)!;
        foreach (var attributeData in symbol.GetAttributes())
        {
            var attributeSymbol = attributeData.AttributeClass!;
            if (MatchesAttribute(attributeSymbol))
            {
                var syntaxTreeNodeContext = new SyntaxTreeNodeContext()
                {
                    Namespace = symbol.ContainingNamespace.ToDisplayString(),
                    Class = symbol.Name,
                };
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    switch (namedArgument.Key)
                    {
                        case nameof(SyntaxTreeNodeContext.Interface):
                            syntaxTreeNodeContext.Interface = (namedArgument.Value.Value as string)!;
                            break;
                        case nameof(SyntaxTreeNodeContext.VisitorInterface):
                            syntaxTreeNodeContext.VisitorInterface = (namedArgument.Value.Value as string)!;
                            break;
                        case nameof(SyntaxTreeNodeContext.Pattern):
                            syntaxTreeNodeContext.Pattern = (namedArgument.Value.Value as string)!;
                            break;
                        case nameof(SyntaxTreeNodeContext.Usings):
                            var immutableArray = namedArgument.Value.Values;
                            syntaxTreeNodeContext.Usings = new string[immutableArray.Length];
                            for (var i = 0; i < immutableArray.Length; i++)
                            {
                                syntaxTreeNodeContext.Usings[i] = (immutableArray[i].Value as string)!;
                            }
                            break;
                        // Unreachable
                        default: throw new NotImplementedException();
                    }
                }
                return syntaxTreeNodeContext;
            }
        }
        return null;
    }
    #endregion

    #region Helper
    static string Capitalize(string str) => str[0] == '@'
        ? Capitalize(str.Substring(1))
        : char.ToUpper(str[0]) + str.Substring(1);

    static string FullName(ITypeSymbol symbol) => FullName(symbol.ContainingNamespace, symbol.Name);

    static string FullName(INamespaceSymbol namespaceSymbol, string className)
        => FullName(namespaceSymbol.ToDisplayString(), className);

    static string FullName(string namespaceName, string className) => $"{namespaceName}.{className}";

    static bool MatchesAttribute(ITypeSymbol attributeSymbol)
        => FullName(attributeSymbol) == ExpectedToAffectOn;
    #endregion
}