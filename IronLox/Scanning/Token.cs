using System.Text;

namespace IronLox.Scanning;

/// <summary>
/// An enum representing all possible token types. Often operators, literals and keywords.
/// </summary>
public enum TokenType
{
    // Single-character tokens
    LeftParenthesis, RightParenthesis,
    LeftBracket, RightBracket,
    Comma, Dot, Minus, Plus, Semicolon, Slash, Star,

    // One character itself or appears in two characters
    Bang, BangEqual,
    Equal, EqualEqual,
    Greater, GreaterEqual,
    Less, LessEqual,

    // Identifiers
    Identifier,

    // Literals
    String, Number,

    // Keywords
    And, Class, Else, False, Fun, For, If, Nil,
    Or, Print, Return, Super, This, True, Var, While,

    // Special
    EndOfFile
}

/// <summary>
/// Representing a token in scanning (or called lexical analysis). It's a record type due to invariant fields.
/// </summary>
/// <param name="Type">Type of the token, <c>TokenType</c>.</param>
/// <param name="Lexeme">The token's actual text content in source file.</param>
/// <param name="Literal">In this language may be strings or numbers. Those whose values are available during compiling.</param>
/// <param name="Line">The line number of the token. For possible error reporting.</param>
public record Token(TokenType Type, string Lexeme, object? Literal, int Line)
{
    public override string ToString()
    {
        var builder = new StringBuilder("[Type ").Append(Type);
        if (Literal is not null)
        {
            builder.Append(", LiteralExpression ");
            builder.Append(Literal);
        }
        else if (!string.IsNullOrWhiteSpace(Lexeme))
        {
            builder.Append(", Lexeme ");
            builder.Append(Lexeme);
        }
        return builder.Append(']').ToString();
    }
}

/// <summary>
/// A helper extension for quicker and shorter token initialization.
/// You can call <c>New</c> method on a certain TokenType.
/// </summary>
public static class TokenTypeExtension
{
    public static Token New(this TokenType type, string lexeme, object? literal, int line)
        => new(type, lexeme, literal, line);
}