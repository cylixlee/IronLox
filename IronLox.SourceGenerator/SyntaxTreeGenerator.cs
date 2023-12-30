using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace IronLox.SourceGenerator;

[Generator]
public class SyntaxTreeGenerator : IIncrementalGenerator
{
    const string ExpectedToAffectOn = "IronLox.SyntaxTree.SyntaxTreeAttribute";

    struct SyntaxTreeContext
    {
        // Generation target context
        public string Namespace { get; set; }
        public string Class { get; set; }
        // Attribute values
        public string Interface { get; set; }
        public string VisitorInterface { get; set; }
        public string[] Patterns { get; set; }
        public string[]? Usings { get; set; }
    }

    struct SyntaxTree
    {
        public string Class { get; set; }
        public IEnumerable<(string Type, string Name)> Parameters { get; set; }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxTreeContexts = context.SyntaxProvider.CreateSyntaxProvider(
            FetchClassDeclarations,
            FilterClassDeclarations
        ).Where(x => x is not null).Select((x, _) => (SyntaxTreeContext)x!);

        context.RegisterSourceOutput(syntaxTreeContexts, (sourceProductionContext, syntaxTreeContext) =>
        {
            sourceProductionContext.AddSource(
                $"{FullName(syntaxTreeContext.Namespace, syntaxTreeContext.Class)}.g.cs",
                GenerateSource(syntaxTreeContext)
            );
        });
    }

    #region SourceGeneration
    static string GenerateSource(SyntaxTreeContext context)
    {
        var builder = new StringBuilder("#nullable enable").AppendLine();
        if (context.Usings is not null)
        {
            foreach (var usingItem in context.Usings)
            {
                builder.AppendLine($"using {usingItem};");
            }
        }

        builder.AppendLine()
            .AppendLine($"namespace {context.Namespace};")
            .AppendLine()
            .AppendLine($"public static partial class {context.Class}")
            .AppendLine("{");
        var syntaxTrees = new List<SyntaxTree>();
        foreach (var pattern in context.Patterns)
        {
            var syntaxTree = ParsePattern(pattern);
            syntaxTrees.Add(syntaxTree);
            builder.AppendLine(GenerateSingleClassSource(context, syntaxTree));
        }

        // Generate Interface and VisitorInterface
        builder.AppendLine(GenerateInterfaceSource(context))
            .Append(GenerateVisitorInterfaceSource(context, syntaxTrees))
            .AppendLine("}");

        return builder.ToString();
    }

    static string GenerateInterfaceSource(SyntaxTreeContext context)
    {
        var builder = new StringBuilder()
            .AppendLine($"\tpublic partial interface {context.Interface}")
            .AppendLine("\t{")
            .AppendLine($"\t\tT Accept<T>({context.VisitorInterface}<T> visitor);")
            .AppendLine("\t}");
        return builder.ToString();
    }

    static string GenerateVisitorInterfaceSource(SyntaxTreeContext context, IEnumerable<SyntaxTree> syntaxTrees)
    {
        var builder = new StringBuilder()
            .AppendLine($"\tpublic partial interface {context.VisitorInterface}<T>")
            .AppendLine("\t{");
        foreach (var syntaxTree in syntaxTrees)
        {
            builder.AppendLine($"\t\tT Visit({syntaxTree.Class} element);");
        }
        builder.AppendLine("\t}");
        return builder.ToString();
    }

    static string GenerateSingleClassSource(SyntaxTreeContext context, SyntaxTree tree)
    {
        var builder = new StringBuilder()
            .Append($"\tpublic partial class {tree.Class}(");
        var argumentList = new List<string>();
        foreach (var parameter in tree.Parameters)
        {
            argumentList.Add($"{parameter.Type} {parameter.Name}");
        }
        builder.Append(string.Join(", ", argumentList));
        builder.AppendLine($") : {context.Interface}")
            .AppendLine("\t{");
        foreach (var parameter in tree.Parameters)
        {
            builder.AppendLine($"\t\tpublic {parameter.Type} {Capitalize(parameter.Name)} {{ get; }} = {parameter.Name};");
        }
        builder.AppendLine()
            .AppendLine($"\t\tpublic T Accept<T>({context.VisitorInterface}<T> visitor) => visitor.Visit(this);")
            .AppendLine("\t}");
        return builder.ToString();
    }

    static SyntaxTree ParsePattern(string pattern)
    {
        var firstSplit = pattern.Split(':');
        var parameters = new List<(string, string)>();
        var syntaxTree = new SyntaxTree()
        {
            Class = firstSplit[0].Trim(),
            Parameters = parameters
        };
        var secondSplit = firstSplit[1].Trim().Split(',');
        foreach (var propertyPattern in secondSplit)
        {
            var thirdSplit = propertyPattern.Trim().Split();
            parameters.Add((thirdSplit[0], thirdSplit[1]));
        }
        return syntaxTree;
    }
    #endregion

    #region FetchAndFilter
    static bool FetchClassDeclarations(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    static SyntaxTreeContext? FilterClassDeclarations(GeneratorSyntaxContext context, CancellationToken _)
    {
        var symbol = (context.SemanticModel.GetDeclaredSymbol(context.Node) as ITypeSymbol)!;
        foreach (var attributeData in symbol.GetAttributes())
        {
            var attributeSymbol = attributeData.AttributeClass!;
            if (MatchesAttribute(attributeSymbol))
            {
                var syntaxTreeNodeContext = new SyntaxTreeContext()
                {
                    Namespace = symbol.ContainingNamespace.ToDisplayString(),
                    Class = symbol.Name,
                };
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    switch (namedArgument.Key)
                    {
                        case nameof(SyntaxTreeContext.Interface):
                            syntaxTreeNodeContext.Interface = (namedArgument.Value.Value as string)!;
                            break;
                        case nameof(SyntaxTreeContext.VisitorInterface):
                            syntaxTreeNodeContext.VisitorInterface = (namedArgument.Value.Value as string)!;
                            break;
                        case nameof(SyntaxTreeContext.Patterns):
                            var patternArray = namedArgument.Value.Values;
                            syntaxTreeNodeContext.Patterns = new string[patternArray.Length];
                            for (var i = 0; i < patternArray.Length; i++)
                            {
                                syntaxTreeNodeContext.Patterns[i] = (patternArray[i].Value as string)!;
                            }
                            break;
                        case nameof(SyntaxTreeContext.Usings):
                            var usingArray = namedArgument.Value.Values;
                            syntaxTreeNodeContext.Usings = new string[usingArray.Length];
                            for (var i = 0; i < usingArray.Length; i++)
                            {
                                syntaxTreeNodeContext.Usings[i] = (usingArray[i].Value as string)!;
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
