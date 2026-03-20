using System;
using System.Collections.Generic;
using System.Linq;

namespace McpServer.Cqrs.Search;

/// <summary>
/// Parses boolean text filters with <c>&amp;&amp;</c>, <c>||</c>, <c>!</c>, parentheses, and quoted terms.
/// When no explicit operators are present, space-separated terms are treated as an AND chain.
/// </summary>
public static class BooleanSearchParser
{
    /// <summary>
    /// Parses a boolean text filter into a predicate that evaluates a searchable string.
    /// </summary>
    /// <param name="query">Filter text using boolean operators and quoted terms.</param>
    /// <returns>A predicate that returns <see langword="true"/> when the text matches.</returns>
    public static Func<string, bool> Parse(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return static _ => true;

        var tokens = Tokenize(query);
        if (tokens.Count == 0)
            return static _ => true;

        if (!HasExplicitOperators(tokens))
        {
            var matchers = tokens
                .Where(static token => token.Type == TokenType.Term)
                .Select(static token => CreateTermMatcher(token.Value))
                .ToArray();

            return text =>
            {
                var searchable = text ?? string.Empty;
                return matchers.All(matcher => matcher(searchable));
            };
        }

        var position = 0;
        var matcher = ParseOr(tokens, ref position);
        if (matcher is null)
            return static _ => true;

        return text => matcher(text ?? string.Empty);
    }

    private static bool HasExplicitOperators(IReadOnlyCollection<Token> tokens)
        => tokens.Any(static token => token.Type is not TokenType.Term);

    private static Func<string, bool> CreateTermMatcher(string term)
        => string.IsNullOrWhiteSpace(term)
            ? static _ => true
            : text => text.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        var index = 0;

        while (index < input.Length)
        {
            if (char.IsWhiteSpace(input[index]))
            {
                index++;
                continue;
            }

            if (TryReadOperator(input, ref index, out var operatorToken))
            {
                tokens.Add(operatorToken);
                continue;
            }

            if (input[index] == '"')
            {
                tokens.Add(new Token(TokenType.Term, ReadQuotedTerm(input, ref index)));
                continue;
            }

            tokens.Add(new Token(TokenType.Term, ReadUnquotedTerm(input, ref index)));
        }

        return tokens;
    }

    private static bool TryReadOperator(string input, ref int index, out Token token)
    {
        token = default;

        if (index + 1 < input.Length && input[index] == '&' && input[index + 1] == '&')
        {
            index += 2;
            token = new Token(TokenType.And);
            return true;
        }

        if (index + 1 < input.Length && input[index] == '|' && input[index + 1] == '|')
        {
            index += 2;
            token = new Token(TokenType.Or);
            return true;
        }

        switch (input[index])
        {
            case '!':
                index++;
                token = new Token(TokenType.Not);
                return true;
            case '(':
                index++;
                token = new Token(TokenType.LParen);
                return true;
            case ')':
                index++;
                token = new Token(TokenType.RParen);
                return true;
            default:
                return false;
        }
    }

    private static string ReadQuotedTerm(string input, ref int index)
    {
        index++;
        var start = index;

        while (index < input.Length && input[index] != '"')
            index++;

        var value = input[start..index];
        if (index < input.Length && input[index] == '"')
            index++;

        return value;
    }

    private static string ReadUnquotedTerm(string input, ref int index)
    {
        var start = index;

        while (index < input.Length &&
               !char.IsWhiteSpace(input[index]) &&
               !(index + 1 < input.Length && input[index] == '&' && input[index + 1] == '&') &&
               !(index + 1 < input.Length && input[index] == '|' && input[index + 1] == '|') &&
               input[index] is not '(' and not ')' and not '!')
        {
            index++;
        }

        return input[start..index];
    }

    private static Func<string, bool>? ParseOr(IReadOnlyList<Token> tokens, ref int position)
    {
        var left = ParseAnd(tokens, ref position);
        if (left is null)
            return null;

        while (position < tokens.Count && tokens[position].Type == TokenType.Or)
        {
            position++;
            var right = ParseAnd(tokens, ref position);
            if (right is null)
                return left;

            var currentLeft = left;
            left = text => currentLeft(text) || right(text);
        }

        return left;
    }

    private static Func<string, bool>? ParseAnd(IReadOnlyList<Token> tokens, ref int position)
    {
        var left = ParseNot(tokens, ref position);
        if (left is null)
            return null;

        while (position < tokens.Count && tokens[position].Type == TokenType.And)
        {
            position++;
            var right = ParseNot(tokens, ref position);
            if (right is null)
                return left;

            var currentLeft = left;
            left = text => currentLeft(text) && right(text);
        }

        return left;
    }

    private static Func<string, bool>? ParseNot(IReadOnlyList<Token> tokens, ref int position)
    {
        if (position < tokens.Count && tokens[position].Type == TokenType.Not)
        {
            position++;
            var inner = ParseNot(tokens, ref position);
            return inner is null ? null : text => !inner(text);
        }

        return ParsePrimary(tokens, ref position);
    }

    private static Func<string, bool>? ParsePrimary(IReadOnlyList<Token> tokens, ref int position)
    {
        if (position >= tokens.Count)
            return null;

        var token = tokens[position];
        switch (token.Type)
        {
            case TokenType.LParen:
                position++;
                var inner = ParseOr(tokens, ref position);
                if (position < tokens.Count && tokens[position].Type == TokenType.RParen)
                    position++;

                return inner;

            case TokenType.Term:
                position++;
                return CreateTermMatcher(token.Value);

            default:
                position++;
                return null;
        }
    }

    private enum TokenType
    {
        Term,
        And,
        Or,
        Not,
        LParen,
        RParen,
    }

    private readonly record struct Token(TokenType Type, string Value = "");
}
