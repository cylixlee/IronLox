using IronLox.Scanning;
using IronLox.Utility;
using static IronLox.SyntaxTree.Expressions;
using static IronLox.SyntaxTree.Statements;

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

    #region Statements
    public IEnumerable<IStatement> Parse()
    {
        var statements = new List<IStatement>();
        while (!HasReachedEnd())
        {
            statements.Add(ParseStatement());
        }
        return statements;
    }

    public IStatement ParseStatement() => Match(TokenType.Print) ? ParsePrintStatement() : ParseExpressionStatement();

    public IStatement ParsePrintStatement()
    {
        var expression = ParseExpression();
        TryConsume(TokenType.Semicolon, "expected ';' after a statement.");
        return new PrintStatement(expression);
    }

    public IStatement ParseExpressionStatement()
    {
        var expression = ParseExpression();
        TryConsume(TokenType.Semicolon, "expected ';' after a statement.");
        return new ExpressionStatement(expression);
    }
    #endregion

    #region Expressions
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
            expression = new BinaryExpression(expression, @operator, right);
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
            expression = new BinaryExpression(expression, @operator, right);
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
            expression = new BinaryExpression(expression, @operator, right);
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
            expression = new BinaryExpression(expression, @operator, right);
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
            return new UnaryExpression(@operator, right);
        }
        return ParsePrimary();
    }

    // primary -> NUMBER | STRING | "true" | "false" | "nil"
    //          | "(" expression ")";
    IExpression ParsePrimary()
    {
        if (Match(TokenType.True)) return new LiteralExpression(true);
        if (Match(TokenType.False)) return new LiteralExpression(false);
        if (Match(TokenType.Nil)) return new LiteralExpression(null);

        if (Match(TokenType.Number, TokenType.String))
        {
            return new LiteralExpression(Previous().Literal);
        }

        if (Match(TokenType.LeftParenthesis))
        {
            var expression = ParseExpression();
            TryConsume(TokenType.RightParenthesis, "expect a closing ')'.");
            return new GroupingExpression(expression);
        }
        throw Panic(Peek(), "expression expected.");
    }
    #endregion

    #region Helpers
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
    bool HasReachedEnd() => tokens[current].Type is TokenType.EndOfFile;
    Token? Advance() => HasReachedEnd() ? null : tokens[current++];
    Token Peek() => tokens[current];
    Token Previous() => tokens[current - 1];
    #endregion
}
