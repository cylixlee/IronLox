using IronLox.Utility;

namespace IronLox.Scanning;

/// <summary>
/// The scanner class, or called lexer or lexical analyzer.
/// <br/>
/// The key is to read a character and advance, which will break a possible dead-loop. Peeking is often necessary too.
/// </summary>
/// <param name="source">The source in a string form.</param>
public class Scanner(string source)
{
    // Keywords list, for converting a literal's TokenType from Identifier to a certain keyword type.
    static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { "and", TokenType.And },
        { "class", TokenType.Class },
        { "else", TokenType.Else },
        { "false", TokenType.False },
        { "for", TokenType.For },
        { "fun", TokenType.Fun },
        { "if", TokenType.If },
        { "nil", TokenType.Nil },
        { "or", TokenType.Or },
        { "print", TokenType.Print },
        { "return", TokenType.Return },
        { "super", TokenType.Super },
        { "this", TokenType.This },
        { "true", TokenType.True },
        { "var", TokenType.Var },
        { "while", TokenType.While },
    };

    readonly string source = source;
    readonly List<Token> tokens = new();
    int start;
    int current;
    int line = 1;

    /// <summary>
    /// Scan and returns the tokens, or just return the tokens if scanned previously.
    /// </summary>
    /// <returns>A list of tokens scanned, with an extra EndOfFile token appended.</returns>
    public IList<Token> Scan()
    {
        if (tokens.Count != 0) return tokens;

        // Calls ScanToken over and over again until reaches the end.
        while (!HasReachedEnd())
        {
            start = current;
            ScanToken();
        }

        tokens.Add(TokenType.EndOfFile.New("", null, line));
        return tokens;
    }

    // Scans forward until a complete token is recognized and returned.
    void ScanToken()
    {
        var character = Advance();
        switch (character)
        {
            // Single tokens with no need for peeking.
            case '(': AddToken(TokenType.LeftParenthesis); break;
            case ')': AddToken(TokenType.RightParenthesis); break;
            case '{': AddToken(TokenType.LeftBracket); break;
            case '}': AddToken(TokenType.RightBracket); break;
            case ',': AddToken(TokenType.Comma); break;
            case '.': AddToken(TokenType.Dot); break;
            case '-': AddToken(TokenType.Minus); break;
            case '+': AddToken(TokenType.Plus); break;
            case ';': AddToken(TokenType.Semicolon); break;
            case '*': AddToken(TokenType.Star); break;

            // 1 or 2 character tokens, with additional peeking.
            case '!': AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang); break;
            case '=': AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal); break;
            case '<': AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less); break;
            case '>': AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater); break;

            // Comment handling because / can be slash operator or half the comment sign.
            case '/':
                if (Match('/'))
                    while (Peek() is not '\n' && !HasReachedEnd()) Advance();
                else AddToken(TokenType.Slash);
                break;

            // Whitespace handling (just drop them.)
            case ' ':
            case '\r':
            case '\t': break;

            // Newline handling
            case '\n': line++; break;

            // String handling - in a method.
            case '"': HandleString(); break;

            default:
                // Number handling, cuz writing '0' to '9' cases is boring.
                if (char.IsAsciiDigit(character))
                    // Real handling exists within a method.
                    HandleNumber();
                // Identifier handling. First-letter principal is represented here.
                else if (char.IsAsciiLetter(character))
                    // Real handling exists within a method, too.
                    HandleIdentifier();
                else ErrorHelper.ReportCompileTime(line, $"unexpected character {character}");
                break;
        }
    }

    // Helper function for checking whether scanner has reached the end of source.
    bool HasReachedEnd() => current >= source.Length;

    // Helper function which returns the current character and advances 1 position forward.
    char Advance() => source[current++];

    // Shortcut of AddToken but with no literal attached.
    void AddToken(TokenType type) => AddToken(type, null);

    // Shortcut of adding a token to the [`tokens`] list.
    void AddToken(TokenType type, object? literal)
        => tokens.Add(type.New(source[start..current], literal, line));

    // Helper function for the 2 character operators.
    // Advances if the current character is as expected, or just do nothing.
    bool Match(char expected)
    {
        if (HasReachedEnd()) return false;
        if (source[current] != expected) return false;

        current++;
        return true;
    }

    // Returns the current character.
    char Peek() => HasReachedEnd() ? '\0' : source[current];

    // Returns the next character without advancing.
    char PeekNext() => current + 1 > source.Length ? '\0' : source[current + 1];

    // The real method that handles string scanning.
    void HandleString()
    {
        // Consume all that which is not the ending quote.
        while (Peek() is not '"' && !HasReachedEnd())
        {
            if (Peek() is '\n') line++;
            Advance();
        }

        // Reports error if reached the end of source without terminating the string.
        if (HasReachedEnd())
        {
            ErrorHelper.ReportCompileTime(line, "unterminated string");
            return;
        }

        // Drop the closing quote sign.
        Advance();
        // Trim surrounding quotes.
        AddToken(TokenType.String, source[(start + 1)..(current - 1)]);
    }

    // The real method that handles (double) number scanning.
    void HandleNumber()
    {
        // Consumes all digits before meeting the dot.
        while (char.IsAsciiDigit(Peek())) Advance();
        if (Peek() is '.' && char.IsAsciiDigit(PeekNext()))
        {
            // Consume the dot only once.
            Advance();
            // When another dot is encountered, this while-loop is ended, meaning that is not a part of the number.
            while (char.IsAsciiDigit(Peek())) Advance();
        }
        AddToken(TokenType.Number, double.Parse(source[start..current]));
    }

    // The real method that handles identifier scanning.
    void HandleIdentifier()
    {
        // There's no first-letter checking because if the first letter is digit, it will goes to the HandleNumber method.
        while (char.IsAsciiLetterOrDigit(Peek())) Advance();

        var text = source[start..current];
        // If it matches a keyword, then add it as a keyword.
        if (Keywords.TryGetValue(text, out var value))
            tokens.Add(value.New(text, null, line));
        // Otherwise, add it as a normal identifier.
        else tokens.Add(TokenType.Identifier.New(text, null, line));
    }
}
