using IronLox.Scanning;
using IronLox.Utility;
using static IronLox.SyntaxTree.Expressions;
using static IronLox.SyntaxTree.Statements;

namespace IronLox.Parsing;

/*
 * program -> declaration* EOF;
 * declaration -> varDecl
 *              | statement;
 * statement -> exprStmt
 *              | forStmt
 *              | ifStmt
 *              | printStmt
 *              | whileStmt
 *              | block;
 * forStmt -> "for" "(" ( varDecl | exprStmt | ";" )
 *            expression? ";"
 *            expression? ")" statement;
 * whileStmt -> "while" "(" expression ")" statement;
 * ifStmt -> "if" "(" expression ")" statement ( "else" statement )?;
 * block -> "{" declaration* "}";
 * exprStmt -> expression ";";
 * printStmt -> "print" expression ";";
 * varDecl -> "var" IDENTIFIER ( "=" expression )? ";";
 * 
 * 
 * expression -> assignment;
 * assignment -> IDENTIFIER "=" assignment
 *              | logic_or;
 * logic_or -> logic_and ( "or" logic_and )* ;
 * logic_and -> equality ( "and" equality )* ;
 * equality -> comparison (("!=" | "==") comparison)*;
 * comparison -> term ((">" | ">=" | "<" | "<=") term)*;
 * term -> factor (("+" | "-") factor)*;
 * factor -> unary (("*" | "/") unary)*;
 * unary -> ("!" | "-") unary
 *         | primary;
 * primary -> NUMBER | STRING | "true" | "false" | "nil"
 *           | "(" expression ")"
 *           | IDENTIFIER;
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
            if (ParseDeclaration() is IStatement statement)
            {
                statements.Add(statement);
            }
        }
        return statements;
    }

    IStatement? ParseDeclaration()
    {
        try
        {
            if (Match(TokenType.Var)) return ParseVariableDeclaration();
            return ParseStatement();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    VariableDeclarationStatement ParseVariableDeclaration()
    {
        var name = TryConsume(TokenType.Identifier, "expected variable name");

        IExpression? initializer = null;
        if (Match(TokenType.Equal))
        {
            initializer = ParseExpression();
        }

        TryConsume(TokenType.Semicolon, "expected ';' after variable declaration.");
        return new VariableDeclarationStatement(name, initializer);
    }

    WhileStatement ParseWhileStatement()
    {
        TryConsume(TokenType.LeftParenthesis, "expect '(' after 'while'.");
        var condition = ParseExpression();
        TryConsume(TokenType.RightParenthesis, "expect ')' after 'while' condition.");

        var body = ParseStatement();
        return new WhileStatement(condition, body);
    }

    IStatement ParseStatement()
    {
        if (Match(TokenType.For)) return ParseForStatement();
        if (Match(TokenType.If)) return ParseIfStatement();
        if (Match(TokenType.Print)) return ParsePrintStatement();
        if (Match(TokenType.While)) return ParseWhileStatement();
        if (Match(TokenType.LeftBracket)) return new BlockStatement(ParseBlock());
        return ParseExpressionStatement();
    }

    IStatement ParseForStatement()
    {
        TryConsume(TokenType.LeftParenthesis, "expect '(' after keyword 'for'.");

        IStatement? initializer;
        if (Match(TokenType.Semicolon)) initializer = null;
        else if (Match(TokenType.Var)) initializer = ParseVariableDeclaration();
        else initializer = ParseExpressionStatement();

        var condition = Match(TokenType.Semicolon) ? null : ParseExpression();
        TryConsume(TokenType.Semicolon, "expect ';' after 'for' condition.");

        var increment = Check(TokenType.RightParenthesis) ? null : ParseExpression();
        TryConsume(TokenType.RightParenthesis, "expect ')' after 'for' clauses.");

        var body = ParseStatement();
        if (increment is not null)
        {
            body = new BlockStatement([body, new ExpressionStatement(increment)]);
        }
        condition = condition is null ? new LiteralExpression(true) : condition;
        body = new WhileStatement(condition, body);
        if (initializer is not null)
        {
            body = new BlockStatement([initializer, body]);
        }
        return body;
    }

    IfStatement ParseIfStatement()
    {
        TryConsume(TokenType.LeftParenthesis, "expect '(' after keyword 'if'.");
        var condition = ParseExpression();
        TryConsume(TokenType.RightParenthesis, "expect '(' after 'if' condition.");

        var thenBranch = ParseStatement();
        var elseBranch = Match(TokenType.Else) ? ParseStatement() : null;

        return new IfStatement(condition, thenBranch, elseBranch);
    }

    PrintStatement ParsePrintStatement()
    {
        var expression = ParseExpression();
        TryConsume(TokenType.Semicolon, "expected ';' after a statement.");
        return new PrintStatement(expression);
    }

    ExpressionStatement ParseExpressionStatement()
    {
        var expression = ParseExpression();
        TryConsume(TokenType.Semicolon, "expected ';' after a statement.");
        return new ExpressionStatement(expression);
    }

    List<IStatement> ParseBlock()
    {
        var statements = new List<IStatement>();
        while (!Check(TokenType.RightBracket) && !HasReachedEnd())
        {
            if (ParseDeclaration() is IStatement statement)
            {
                statements.Add(statement);
            }
        }
        TryConsume(TokenType.RightBracket, "expect '}' at block end.");
        return statements;
    }
    #endregion

    #region Expressions
    IExpression ParseExpression() => ParseAssignment();

    IExpression ParseAssignment()
    {
        var expression = ParseLogicalOr();
        if (Match(TokenType.Equal))
        {
            var leftValue = Previous();
            var rightValue = ParseAssignment();
            if (expression is VariableExpression variableExpression)
            {
                var name = variableExpression.Name;
                return new AssignExpression(name, rightValue);
            }

            throw Panic(leftValue, "invalid assignment target.");
        }
        return expression;
    }

    IExpression ParseLogicalOr()
    {
        var expression = ParseLogicalAnd();
        while (Match(TokenType.Or))
        {
            var @operator = Previous();
            var right = ParseLogicalAnd();
            expression = new LogicalExpression(expression, @operator, right);
        }
        return expression;
    }

    IExpression ParseLogicalAnd()
    {
        var expression = ParseEquality();
        while (Match(TokenType.And))
        {
            var @operator = Previous();
            var right = ParseEquality();
            expression = new LogicalExpression(expression, @operator, right);
        }
        return expression;
    }

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

    IExpression ParsePrimary()
    {
        if (Match(TokenType.True)) return new LiteralExpression(true);
        if (Match(TokenType.False)) return new LiteralExpression(false);
        if (Match(TokenType.Nil)) return new LiteralExpression(null);

        if (Match(TokenType.Number, TokenType.String))
        {
            return new LiteralExpression(Previous().Literal);
        }

        if (Match(TokenType.Identifier)) return new VariableExpression(Previous());

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
    static ParseException Panic(Token token, string message)
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
    Token TryConsume(TokenType type, string errorMessage)
        => Check(type) ? Advance()! : throw Panic(Peek(), errorMessage);

    // Check the current token's type.
    bool Check(TokenType type) => !HasReachedEnd() && Peek().Type == type;

    // Literally.
    bool HasReachedEnd() => tokens[current].Type is TokenType.EndOfFile;
    Token? Advance() => HasReachedEnd() ? null : tokens[current++];
    Token Peek() => tokens[current];
    Token Previous() => tokens[current - 1];
    #endregion
}
