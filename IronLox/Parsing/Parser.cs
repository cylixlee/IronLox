using IronLox.ErrorHandling;
using IronLox.Scanning;
using IronLox.SyntaxTree;
using IronLox.SyntaxTree.Expressions;

namespace IronLox.Parsing;

/*
 * expression -> equality
 * equality -> comparison (("!=" | "==") comparison)*;
 * comparison -> term ((">" | ">=" | "<" | "<=") term)*;
 * term -> factor (("+" | "-") factor)*;
 * factor -> unary (("*" | "/") unary)*;
 * unary -> ("!" | "-") unary
 *         | primary;
 * primary -> NUMBER | STRING | "true" | "false" | "nil"
 *           | "(" expression ")";
 */

/// <summary>
/// Gradient descent parser, with syntax above.
/// </summary>
/// <param name="tokens">Scanned tokens.</param>
public class Parser(IList<Token> tokens)
{
    readonly IList<Token> tokens = tokens;
    int current = 0;

    public IExpression? Parse()
    {
        try
        {
            return ParseExpression();
        }
        catch (ParseException)
        {
            // Simply returns null here because recovery from panic mode hasn't been implemented.
            return null;
        }
    }

    // expression -> equality
    IExpression ParseExpression() => ParseEquality();

    // equality -> comparison (("!=" | "==") comparison)*;
    IExpression ParseEquality()
    {
        var expression = ParseComparison();
        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            var @operator = Previous();
            var right = ParseComparison();
            expression = new Binary(expression, @operator, right);
        }
        return expression;
    }

    // comparison -> term ((">" | ">=" | "<" | "<=") term)*;
    IExpression ParseComparison()
    {
        var expression = ParseTerm();
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var @operator = Previous();
            var right = ParseTerm();
            expression = new Binary(expression, @operator, right);
        }
        return expression;
    }

    // term -> factor (("+" | "-") factor)*;
    IExpression ParseTerm()
    {
        var expression = ParseFactor();
        while (Match(TokenType.Plus, TokenType.Minus))
        {
            var @operator = Previous();
            var right = ParseFactor();
            expression = new Binary(expression, @operator, right);
        }
        return expression;
    }

    // factor -> unary (("*" | "/") unary)*;
    IExpression ParseFactor()
    {
        var expression = ParseUnary();
        while (Match(TokenType.Star, TokenType.Slash))
        {
            var @operator = Previous();
            var right = ParseUnary();
            expression = new Binary(expression, @operator, right);
        }
        return expression;
    }

    // unary -> ("!" | "-") unary
    //          | primary;
    IExpression ParseUnary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            var @operator = Previous();
            var right = ParseUnary();
            return new Unary(@operator, right);
        }
        return ParsePrimary();
    }

    // primary -> NUMBER | STRING | "true" | "false" | "nil"
    //          | "(" expression ")";
    IExpression ParsePrimary()
    {
        if (Match(TokenType.True)) return new Literal(true);
        if (Match(TokenType.False)) return new Literal(false);
        if (Match(TokenType.Nil)) return new Literal(null);

        if (Match(TokenType.Number, TokenType.String))
        {
            return new Literal(Previous().Literal);
        }

        if (Match(TokenType.LeftParenthesis))
        {
            var expression = ParseExpression();
            TryConsume(TokenType.RightParenthesis, "expect a closing ')'.");
            return new Grouping(expression);
        }
        throw Panic(Peek(), "expression expected.");
    }

    // Checks whether the current token fulfills the type requirement.
    // ... `consume` (not the method, but literally) it if positive.
    bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    // The panic mode, which is activated when no rule is fulfilled.
    ParseException Panic(Token token, string message)
    {
        ErrorHelper.ReportCompileTime(token, message);
        return new ParseException();
    }

    // Recovery from panic mode.
    // Here just skip all tokens left in this expression, and recovers until another expression starts.
    void Synchronize()
    {
        Advance();

        while (!HasReachedEnd())
        {
            if (Previous().Type is TokenType.Semicolon) return;

            switch (Peek().Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }

    // Consumes the current token if type matches, otherwise enter panic mode.
    Token? TryConsume(TokenType type, string errorMessage)
        => Check(type) ? Advance() : throw Panic(Peek(), errorMessage);

    // Check the current token's type.
    bool Check(TokenType type) => !HasReachedEnd() && Peek().Type == type;

    // Literally.
    bool HasReachedEnd() => current >= tokens.Count;
    Token? Advance() => HasReachedEnd() ? null : tokens[current++];
    Token Peek() => tokens[current];
    Token Previous() => tokens[current - 1];
}
