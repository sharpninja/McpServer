using System.Text;
using System.Text.RegularExpressions;

namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008): finds platform dependence in unit test classes.
/// A test that depends on the platform is an integration test (operator 2026-10-19: "If it depends on the
/// environment, it's an integration test."), so an operating-system check, an OS-platform attribute, an
/// xUnit skip, or a binding to the real process host is a finding unless its test class (or an enclosing
/// class) carries <c>[Trait("Category", "Integration")]</c>. Strings and comments are ignored, but the code
/// inside an interpolation hole is not. Test classes are types whose name ends in <c>Tests</c> or that
/// declare <c>[Fact]</c> or <c>[Theory]</c> methods; other helper types (for example the host-neutral
/// <c>UnitTestPaths</c> fixture) are not test classes. The scan reads source text only: dependence reached
/// through a helper type, or through operating-system behavior of a base-library call such as
/// <c>Path.GetFullPath</c>, is outside it.
/// </summary>
internal static class UnitSuitePlatformScanner
{
    /// <summary>Skip calls and skippable attributes, which never span lines (declared before <see cref="Rules"/>, which uses it).</summary>
    private static readonly Regex SkipCall = new(
        @"\bAssert\s*\.\s*Skip(?:When|Unless)?\b|\bSkipException\b|\bSkippable(?:Fact|Theory)\b",
        RegexOptions.CultureInvariant);

    private static readonly (string Kind, Regex Pattern)[] Rules =
    [
        ("os-check", new Regex(@"\bOperatingSystem\s*\.\s*Is[A-Z]\w*\s*\(", RegexOptions.CultureInvariant)),
        ("os-check", new Regex(@"\bRuntimeInformation\s*\.\s*IsOSPlatform\s*\(", RegexOptions.CultureInvariant)),
        ("os-attribute", new Regex(@"\[\s*(?:[\w.]+\.)?(?:Supported|Unsupported)OSPlatform(?:Attribute)?\s*\(", RegexOptions.CultureInvariant)),
        ("skip", SkipCall),
        ("process-host", new Regex(@"\bSystemWorkspaceHostEnvironment\s*\.\s*Instance\b|\bWorkspacePathNormalizer\s*\.\s*Process\b", RegexOptions.CultureInvariant)),
    ];

    private static readonly Regex TypeDeclaration = new(
        @"\b(?:class|record|struct|interface)\s+([A-Za-z_]\w*)",
        RegexOptions.CultureInvariant);

    private static readonly Regex IntegrationTrait = new(
        @"\[\s*(?:Xunit\.)?Trait(?:Attribute)?\s*\(\s*""Category""\s*,\s*""Integration""\s*\)",
        RegexOptions.CultureInvariant);

    private static readonly Regex TestAttribute = new(
        @"(?<=[\[,]\s*)(?:global\s*::\s*)?(?:[A-Za-z_]\w*\s*\.\s*)*(?:Fact|Theory)(?:Attribute)?\b",
        RegexOptions.CultureInvariant);

    /// <summary>A <c>[Fact(</c> or <c>[Theory(</c> argument list, plain, qualified, or later in an attribute list.</summary>
    private static readonly Regex TestAttributeCall = new(
        @"(?<=[\[,]\s*)(?:global\s*::\s*)?(?:[A-Za-z_]\w*\s*\.\s*)*(?:Fact|Theory)(?:Attribute)?\s*\(",
        RegexOptions.CultureInvariant);

    /// <summary>The xUnit skip properties: <c>Skip</c>, <c>SkipUnless</c>, <c>SkipWhen</c>, <c>SkipType</c>.</summary>
    private static readonly Regex SkipArgument = new(@"\bSkip\w*\s*=(?!=)", RegexOptions.CultureInvariant);

    /// <summary>
    /// TR-MCP-USECASE-018 (AC-006): the one definition of an xUnit skip shared by this scanner and the skip
    /// guard. Returns the 1-based lines of code (already stripped of strings and comments) that hold a skip
    /// call, <c>SkipException</c>, a skippable attribute, or a test attribute whose argument list (which may
    /// span lines) sets a skip property; an attribute is reported on the line of its name.
    /// </summary>
    /// <param name="strippedCode">Output of <see cref="StripCommentsAndStrings"/>.</param>
    internal static IReadOnlyList<int> FindSkipLines(string strippedCode)
    {
        ArgumentNullException.ThrowIfNull(strippedCode);
        var code = strippedCode.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = new SortedSet<int>();
        foreach (Match call in SkipCall.Matches(code))
            lines.Add(LineOf(code, call.Index));
        foreach (Match attribute in TestAttributeCall.Matches(code))
        {
            var open = attribute.Index + attribute.Length - 1;
            var close = MatchingParenthesis(code, open);
            var arguments = code.Substring(open + 1, (close < 0 ? code.Length : close) - open - 1);
            if (SkipArgument.IsMatch(arguments))
                lines.Add(LineOf(code, attribute.Index));
        }

        return lines.ToArray();
    }

    /// <summary>Scans one C# source file and returns its findings in line order.</summary>
    /// <param name="relativePath">Path reported with each finding.</param>
    /// <param name="source">File text.</param>
    internal static IReadOnlyList<UnitSuitePlatformFinding> Scan(string relativePath, string source)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        ArgumentNullException.ThrowIfNull(source);
        var original = source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var stripped = StripCommentsAndStrings(source).Replace("\r\n", "\n", StringComparison.Ordinal);
        var code = stripped.Split('\n');
        var skipLines = new HashSet<int>(FindSkipLines(stripped));

        var open = new Stack<TypeScope>();
        var hits = new List<(int Line, string Kind, TypeScope? Owner)>();
        var attributeHits = new List<(int Line, string Kind)>();
        TypeScope? pending = null;
        var depth = 0;

        for (var i = 0; i < code.Length; i++)
        {
            var line = code[i];
            var trimmed = line.Trim();
            var declaration = TypeDeclaration.Match(line);
            if (declaration.Success)
            {
                pending = new TypeScope(
                    declaration.Groups[1].Value,
                    HasIntegrationTrait(original, code, i),
                    open.Count > 0 ? open.Peek() : null,
                    depth);
            }

            if (open.Count > 0 && TestAttribute.IsMatch(line))
                open.Peek().HasTests = true;

            var lineNumber = i + 1;
            var lineHits = Rules
                .Where(rule => rule.Kind == "skip" ? skipLines.Contains(lineNumber) : rule.Pattern.IsMatch(line))
                .Select(rule => rule.Kind)
                .ToList();
            if (!declaration.Success && trimmed.StartsWith('['))
            {
                // Attribute lines belong to the type or member declared after them.
                attributeHits.AddRange(lineHits.Select(kind => (i + 1, kind)));
            }
            else
            {
                if (trimmed.Length > 0)
                {
                    var target = declaration.Success ? pending : (open.Count > 0 ? open.Peek() : null);
                    hits.AddRange(attributeHits.Select(hit => (hit.Line, hit.Kind, target)));
                    attributeHits.Clear();
                }

                hits.AddRange(lineHits.Select(kind => (i + 1, kind, pending ?? (open.Count > 0 ? open.Peek() : null))));
            }

            foreach (var ch in line)
            {
                if (ch == '{')
                {
                    depth++;
                    if (pending is not null)
                    {
                        pending.Depth = depth;
                        open.Push(pending);
                        pending = null;
                    }
                }
                else if (ch == '}')
                {
                    while (open.Count > 0 && open.Peek().Depth >= depth)
                        open.Pop();
                    depth--;
                }
                else if (ch == ';' && pending is not null && depth == pending.DeclaredDepth)
                {
                    // A positional record or a declaration without a body ends here.
                    pending = null;
                }
            }
        }

        var findings = new List<UnitSuitePlatformFinding>();
        foreach (var (line, kind, owner) in hits)
        {
            var testClass = owner;
            while (testClass is not null && !testClass.IsTestClass)
                testClass = testClass.Parent;
            if (testClass is null || testClass.IsIntegration)
                continue;
            findings.Add(new UnitSuitePlatformFinding(relativePath, line, kind, testClass.Name));
        }

        return findings;
    }

    /// <summary>
    /// Replaces the contents of comments, string literals (regular, verbatim, interpolated, raw) and character
    /// literals with spaces, keeping every newline so line numbers stay the same. The code inside an
    /// interpolation hole is kept (strings nested in it are blanked in turn); escaped braces and a hole's
    /// format clause are literal text and are blanked.
    /// </summary>
    /// <param name="source">C# source text.</param>
    internal static string StripCommentsAndStrings(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var result = new StringBuilder(source.Length);
        var i = 0;
        CopyCode(source, ref i, result, inHole: false);
        return result.ToString();
    }

    /// <summary>
    /// Copies code, blanking comments and literals. In an interpolation hole, copying stops (without consuming
    /// the character) at a <c>}</c> or a format-clause <c>:</c> that is not nested in brackets.
    /// </summary>
    private static void CopyCode(string source, ref int i, StringBuilder result, bool inHole)
    {
        var depth = 0;
        while (i < source.Length)
        {
            var c = source[i];
            var next = Peek(source, i + 1);
            if (inHole && depth == 0 && (c == '}' || (c == ':' && next != ':' && Peek(source, i - 1) != ':')))
                return;
            if (c == '/' && next == '/')
            {
                while (i < source.Length && source[i] != '\n')
                    Blank(result, source[i++]);
            }
            else if (c == '/' && next == '*')
            {
                Blank(result, source[i++]);
                Blank(result, source[i++]);
                while (i < source.Length && !(source[i] == '*' && Peek(source, i + 1) == '/'))
                    Blank(result, source[i++]);
                if (i < source.Length)
                {
                    Blank(result, source[i++]);
                    Blank(result, source[i++]);
                }
            }
            else if (c == '"' || ((c == '@' || c == '$') && (next == '"' || next == '@' || next == '$')))
            {
                CopyString(source, ref i, result);
            }
            else if (c == '\'')
            {
                result.Append(source[i++]);
                while (i < source.Length && source[i] != '\'' && source[i] != '\n')
                {
                    if (source[i] == '\\' && i + 1 < source.Length)
                        Blank(result, source[i++]);
                    Blank(result, source[i++]);
                }

                if (i < source.Length && source[i] == '\'')
                    result.Append(source[i++]);
            }
            else
            {
                if (inHole && c is '(' or '[' or '{')
                    depth++;
                else if (inHole && c is ')' or ']' or '}')
                    depth--;
                result.Append(source[i++]);
            }
        }
    }

    /// <summary>Copies one string literal starting at its prefix, blanking its text and keeping its holes.</summary>
    private static void CopyString(string source, ref int i, StringBuilder result)
    {
        var dollars = 0;
        var verbatim = false;
        while (i < source.Length && source[i] != '"')
        {
            dollars += source[i] == '$' ? 1 : 0;
            verbatim |= source[i] == '@';
            result.Append(source[i++]);
        }

        var quotes = 0;
        while (i + quotes < source.Length && source[i + quotes] == '"')
            quotes++;
        if (!verbatim && quotes >= 3)
        {
            // Raw string: n dollars mean a run of n braces opens a hole; shorter runs are text.
            for (var q = 0; q < quotes; q++)
                result.Append(source[i++]);
            while (i < source.Length && !IsRun(source, i, '"', quotes))
            {
                var braces = dollars > 0 ? CountRun(source, i, '{') : 0;
                if (braces >= dollars && braces > 0)
                {
                    for (var b = 0; b < braces - dollars; b++)
                        Blank(result, source[i++]);
                    for (var b = 0; b < dollars; b++)
                        result.Append(source[i++]);
                    CopyHole(source, ref i, result, dollars);
                }
                else if (braces > 0)
                {
                    for (var b = 0; b < braces; b++)
                        Blank(result, source[i++]);
                }
                else
                {
                    Blank(result, source[i++]);
                }
            }

            for (var q = 0; q < quotes && i < source.Length; q++)
                result.Append(source[i++]);
            return;
        }

        result.Append(source[i++]);
        while (i < source.Length)
        {
            var ch = source[i];
            var after = Peek(source, i + 1);
            if ((verbatim && ch == '"' && after == '"')
                || (!verbatim && ch == '\\' && i + 1 < source.Length)
                || (dollars > 0 && ch == '{' && after == '{')
                || (dollars > 0 && ch == '}' && after == '}'))
            {
                Blank(result, source[i++]);
                Blank(result, source[i++]);
            }
            else if (ch == '"' || (!verbatim && ch == '\n'))
            {
                break;
            }
            else if (dollars > 0 && ch == '{')
            {
                result.Append(source[i++]);
                CopyHole(source, ref i, result, 1);
            }
            else
            {
                Blank(result, source[i++]);
            }
        }

        if (i < source.Length && source[i] == '"')
            result.Append(source[i++]);
    }

    /// <summary>Copies one interpolation hole after its opening braces: code, then a blanked format clause, then the closing braces.</summary>
    private static void CopyHole(string source, ref int i, StringBuilder result, int braces)
    {
        CopyCode(source, ref i, result, inHole: true);
        if (i < source.Length && source[i] == ':')
        {
            while (i < source.Length && source[i] != '}')
                Blank(result, source[i++]);
        }

        for (var b = 0; b < braces && i < source.Length && source[i] == '}'; b++)
            result.Append(source[i++]);
    }

    private static char Peek(string source, int index) => index >= 0 && index < source.Length ? source[index] : '\0';

    private static void Blank(StringBuilder result, char c) => result.Append(c == '\n' ? '\n' : ' ');

    private static int CountRun(string source, int index, char c)
    {
        var count = 0;
        while (index + count < source.Length && source[index + count] == c)
            count++;
        return count;
    }

    private static bool IsRun(string source, int index, char c, int count) => CountRun(source, index, c) >= count;

    private static int LineOf(string code, int index)
    {
        var line = 1;
        for (var k = 0; k < index; k++)
        {
            if (code[k] == '\n')
                line++;
        }

        return line;
    }

    private static int MatchingParenthesis(string code, int open)
    {
        var depth = 0;
        for (var k = open; k < code.Length; k++)
        {
            if (code[k] == '(')
                depth++;
            else if (code[k] == ')' && --depth == 0)
                return k;
        }

        return -1;
    }

    private static bool HasIntegrationTrait(string[] original, string[] code, int declarationLine)
    {
        if (IntegrationTrait.IsMatch(original[declarationLine]))
            return true;
        for (var i = declarationLine - 1; i >= 0; i--)
        {
            var stripped = code[i].Trim();
            if (stripped.Length == 0)
                continue;
            if (!stripped.StartsWith('[') && !stripped.EndsWith(']') && !stripped.EndsWith(','))
                break;
            if (IntegrationTrait.IsMatch(original[i]))
                return true;
        }

        return false;
    }

    private sealed class TypeScope(string name, bool hasIntegrationTrait, TypeScope? parent, int declaredDepth)
    {
        internal string Name { get; } = name;

        internal TypeScope? Parent { get; } = parent;

        internal int DeclaredDepth { get; } = declaredDepth;

        internal int Depth { get; set; } = -1;

        internal bool HasTests { get; set; }

        internal bool IsTestClass => HasTests || Name.EndsWith("Tests", StringComparison.Ordinal);

        internal bool IsIntegration => hasIntegrationTrait || (Parent?.IsIntegration ?? false);
    }
}

/// <summary>TR-MCP-USECASE-018 (AC-006): one platform-dependent line in a unit test class.</summary>
/// <param name="Path">Repository-relative source path.</param>
/// <param name="Line">1-based line number.</param>
/// <param name="Kind">os-check, os-attribute, skip, or process-host.</param>
/// <param name="TestClass">Unit test class that holds the line.</param>
internal sealed record UnitSuitePlatformFinding(string Path, int Line, string Kind, string TestClass)
{
    /// <inheritdoc />
    public override string ToString() => $"{Path}:{Line} {Kind} in {TestClass}";
}
