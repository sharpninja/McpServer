using System;
using System.Collections.Generic;
using System.Linq;

namespace McpServer.VsExtension.McpTodo;

/// <summary>
/// Boolean filter expression for text filter: terms, !, &amp;&amp;, ||, and parentheses.
/// Examples: "plan || impl", "!plan", "plan &amp;&amp; !impl", "(plan || impl) &amp;&amp; !trip"
/// </summary>
internal static class FilterExpr
{
    private const string OpAnd = "&&";
    private const string OpOr = "||";

    private enum TokenType { Term, Not, And, Or, LParen, RParen }

    private sealed class Token
    {
        internal TokenType Type { get; }
        internal string? Value { get; }
        internal Token(TokenType type, string? value = null) { Type = type; Value = value; }
    }

    public abstract class Expr { }

    public sealed class TermExpr : Expr
    {
        internal string Value { get; }
        internal TermExpr(string value) { Value = value ?? ""; }
    }

    public sealed class AndExpr : Expr
    {
        internal Expr Left { get; }
        internal Expr Right { get; }
        internal AndExpr(Expr left, Expr right) { Left = left; Right = right; }
    }

    public sealed class OrExpr : Expr
    {
        internal Expr Left { get; }
        internal Expr Right { get; }
        internal OrExpr(Expr left, Expr right) { Left = left; Right = right; }
    }

    public sealed class NotExpr : Expr
    {
        internal Expr Operand { get; }
        internal NotExpr(Expr operand) { Operand = operand; }
    }

    private static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int i = 0;
        int n = input.Length;

        while (i < n)
        {
            while (i < n && char.IsWhiteSpace(input[i])) i++;
            if (i >= n) break;

            if (i + 2 <= n && input.Substring(i, 2) == OpAnd)
            {
                tokens.Add(new Token(TokenType.And));
                i += 2;
                continue;
            }
            if (i + 2 <= n && input.Substring(i, 2) == OpOr)
            {
                tokens.Add(new Token(TokenType.Or));
                i += 2;
                continue;
            }
            if (input[i] == '(')
            {
                tokens.Add(new Token(TokenType.LParen));
                i += 1;
                continue;
            }
            if (input[i] == ')')
            {
                tokens.Add(new Token(TokenType.RParen));
                i += 1;
                continue;
            }
            if (input[i] == '!')
            {
                tokens.Add(new Token(TokenType.Not));
                i += 1;
                continue;
            }

            var term = "";
            while (i < n)
            {
                var rest = i + 2 <= n ? input.Substring(i, 2) : "";
                if (rest == OpAnd || rest == OpOr || input[i] == '(' || input[i] == ')' || input[i] == '!' || char.IsWhiteSpace(input[i]))
                    break;
                term += input[i];
                i += 1;
            }
            if (term.Length > 0)
                tokens.Add(new Token(TokenType.Term, term));
        }

        return tokens;
    }

    private static Expr? ParsePrimary(List<Token> tokens, ref int pos)
    {
        if (pos >= tokens.Count) return null;
        var t = tokens[pos];
        if (t.Type == TokenType.Not)
        {
            pos += 1;
            var operand = ParsePrimary(tokens, ref pos);
            if (operand == null) return null;
            return new NotExpr(operand);
        }
        if (t.Type == TokenType.Term)
        {
            pos += 1;
            return new TermExpr(t.Value ?? "");
        }
        if (t.Type == TokenType.LParen)
        {
            pos += 1;
            var inner = ParseOr(tokens, ref pos);
            if (pos < tokens.Count && tokens[pos].Type == TokenType.RParen)
                pos += 1;
            return inner;
        }
        return null;
    }

    private static Expr? ParseAnd(List<Token> tokens, ref int pos)
    {
        var left = ParsePrimary(tokens, ref pos);
        if (left == null) return null;
        while (pos < tokens.Count && tokens[pos].Type == TokenType.And)
        {
            pos += 1;
            var right = ParsePrimary(tokens, ref pos);
            if (right == null) return left;
            left = new AndExpr(left, right);
        }
        return left;
    }

    private static Expr? ParseOr(List<Token> tokens, ref int pos)
    {
        var left = ParseAnd(tokens, ref pos);
        if (left == null) return null;
        while (pos < tokens.Count && tokens[pos].Type == TokenType.Or)
        {
            pos += 1;
            var right = ParseAnd(tokens, ref pos);
            if (right == null) return left;
            left = new OrExpr(left, right);
        }
        return left;
    }

    private static Expr? Parse(List<Token> tokens)
    {
        if (tokens.Count == 0) return null;
        int pos = 0;
        return ParseOr(tokens, ref pos);
    }

    /// <summary>Returns true if searchable text matches the expression (case-insensitive).</summary>
    public static bool Evaluate(Expr? expr, string searchable)
    {
        if (expr == null) return true;
        return EvalNode(expr, searchable ?? "");
    }

    private static bool EvalNode(Expr e, string searchable)
    {
        if (e is TermExpr t)
            return searchable.IndexOf(t.Value ?? "", StringComparison.OrdinalIgnoreCase) >= 0;
        if (e is NotExpr n)
            return !EvalNode(n.Operand, searchable);
        if (e is AndExpr a)
            return EvalNode(a.Left, searchable) && EvalNode(a.Right, searchable);
        if (e is OrExpr o)
            return EvalNode(o.Left, searchable) || EvalNode(o.Right, searchable);
        return false;
    }

    /// <summary>
    /// Parse filter string into an expression. If no operators/parens, treats space-separated words as AND.
    /// </summary>
    public static Expr? ParseFilterText(string? input)
    {
        var trimmed = input?.Trim() ?? "";
        if (trimmed.Length == 0) return null;
        var tokens = Tokenize(trimmed);
        if (tokens.Count == 0) return null;
        var hasOperators = tokens.Any(t => t.Type == TokenType.Not || t.Type == TokenType.And || t.Type == TokenType.Or || t.Type == TokenType.LParen || t.Type == TokenType.RParen);
        if (!hasOperators)
        {
            var terms = tokens.Where(t => t.Type == TokenType.Term).Select(t => t.Value ?? "").ToList();
            if (terms.Count == 0) return null;
            if (terms.Count == 1) return new TermExpr(terms[0]);
            Expr expr = new TermExpr(terms[0]);
            for (int i = 1; i < terms.Count; i++)
                expr = new AndExpr(expr, new TermExpr(terms[i]));
            return expr;
        }
        return Parse(tokens);
    }
}
