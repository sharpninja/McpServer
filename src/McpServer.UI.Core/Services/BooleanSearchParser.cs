using System;
using System.Collections.Generic;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Parses boolean search expressions with || (OR), &amp;&amp; (AND), ! (NOT), and parentheses.
/// Returns a predicate that tests whether a given text matches the expression.
/// When no operators are present, behaves as a simple substring match.
/// </summary>
public static class BooleanSearchParser
{
    /// <summary>
    /// Parses a boolean search query into a predicate that evaluates input text.
    /// </summary>
    /// <param name="query">Query text using <c>||</c>, <c>&amp;&amp;</c>, <c>!</c>, and parentheses.</param>
    /// <returns>A predicate that returns true when the text matches the query.</returns>
    public static Func<string, bool> Parse(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _ => true;

        var tokens = Tokenize(query);
        int pos = 0;
        var expr = ParseOr(tokens, ref pos);
        return expr;
    }

    private enum TokenType { Term, And, Or, Not, LParen, RParen }

    private sealed class Token
    {
        public TokenType Type;
        public string Value = "";
    }

    private static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < input.Length)
        {
            if (char.IsWhiteSpace(input[i])) { i++; continue; }

            if (i + 1 < input.Length && input[i] == '&' && input[i + 1] == '&')
            {
                tokens.Add(new Token { Type = TokenType.And });
                i += 2; continue;
            }
            if (i + 1 < input.Length && input[i] == '|' && input[i + 1] == '|')
            {
                tokens.Add(new Token { Type = TokenType.Or });
                i += 2; continue;
            }
            if (input[i] == '!')
            {
                tokens.Add(new Token { Type = TokenType.Not });
                i++; continue;
            }
            if (input[i] == '(')
            {
                tokens.Add(new Token { Type = TokenType.LParen });
                i++; continue;
            }
            if (input[i] == ')')
            {
                tokens.Add(new Token { Type = TokenType.RParen });
                i++; continue;
            }

            // Quoted string
            if (input[i] == '"')
            {
                int start = ++i;
                while (i < input.Length && input[i] != '"') i++;
                tokens.Add(new Token { Type = TokenType.Term, Value = input.Substring(start, i - start) });
                if (i < input.Length) i++; // skip closing quote
                continue;
            }

            // Unquoted term: read until whitespace or operator
            {
                int start = i;
                while (i < input.Length && !char.IsWhiteSpace(input[i]) &&
                       !(i + 1 < input.Length && input[i] == '&' && input[i + 1] == '&') &&
                       !(i + 1 < input.Length && input[i] == '|' && input[i + 1] == '|') &&
                       input[i] != '(' && input[i] != ')' && input[i] != '!')
                    i++;
                tokens.Add(new Token { Type = TokenType.Term, Value = input.Substring(start, i - start) });
            }
        }
        return tokens;
    }

    // OR has lowest precedence
    private static Func<string, bool> ParseOr(List<Token> tokens, ref int pos)
    {
        var left = ParseAnd(tokens, ref pos);
        while (pos < tokens.Count && tokens[pos].Type == TokenType.Or)
        {
            pos++;
            var right = ParseAnd(tokens, ref pos);
            var l = left; var r = right;
            left = text => l(text) || r(text);
        }
        return left;
    }

    // AND has higher precedence than OR
    private static Func<string, bool> ParseAnd(List<Token> tokens, ref int pos)
    {
        var left = ParseNot(tokens, ref pos);
        while (pos < tokens.Count && tokens[pos].Type == TokenType.And)
        {
            pos++;
            var right = ParseNot(tokens, ref pos);
            var l = left; var r = right;
            left = text => l(text) && r(text);
        }
        return left;
    }

    // NOT is unary prefix
    private static Func<string, bool> ParseNot(List<Token> tokens, ref int pos)
    {
        if (pos < tokens.Count && tokens[pos].Type == TokenType.Not)
        {
            pos++;
            var inner = ParseNot(tokens, ref pos);
            return text => !inner(text);
        }
        return ParsePrimary(tokens, ref pos);
    }

    private static Func<string, bool> ParsePrimary(List<Token> tokens, ref int pos)
    {
        if (pos >= tokens.Count)
            return _ => true;

        if (tokens[pos].Type == TokenType.LParen)
        {
            pos++; // skip '('
            var expr = ParseOr(tokens, ref pos);
            if (pos < tokens.Count && tokens[pos].Type == TokenType.RParen)
                pos++; // skip ')'
            return expr;
        }

        if (tokens[pos].Type == TokenType.Term)
        {
            var term = tokens[pos].Value.ToLowerInvariant();
            pos++;
            return text => text.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        // Skip unexpected tokens
        pos++;
        return _ => true;
    }
}
