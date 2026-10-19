using System.Text;
using System.Text.RegularExpressions;

namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008): finds platform dependence in unit test classes.
/// A test that depends on the platform is an integration test (operator 2026-10-19: "If it depends on the
/// environment, it's an integration test."), so an operating-system check, an OS-platform attribute, an
/// xUnit skip, or a binding to the real process host is a finding unless its test class (or an enclosing
/// class) carries <c>[Trait("Category", "Integration")]</c>. Strings and comments are ignored. Test classes
/// are types whose name ends in <c>Tests</c> or that declare <c>[Fact]</c> or <c>[Theory]</c> methods; other
/// helper types (for example the host-neutral <c>UnitTestPaths</c> fixture) are not test classes.
/// </summary>
internal static class UnitSuitePlatformScanner
{
    private static readonly (string Kind, Regex Pattern)[] Rules =
    [
        ("os-check", new Regex(@"\bOperatingSystem\s*\.\s*Is[A-Z]\w*\s*\(", RegexOptions.CultureInvariant)),
        ("os-check", new Regex(@"\bRuntimeInformation\s*\.\s*IsOSPlatform\s*\(", RegexOptions.CultureInvariant)),
        ("os-attribute", new Regex(@"\[\s*(?:[\w.]+\.)?(?:Supported|Unsupported)OSPlatform(?:Attribute)?\s*\(", RegexOptions.CultureInvariant)),
        ("skip", new Regex(@"\bAssert\s*\.\s*Skip(?:When|Unless)?\s*\(|\bSkipException\b|\bSkippable(?:Fact|Theory)\b|\[\s*(?:Fact|Theory)\s*\([^\]]*\bSkip\s*=", RegexOptions.CultureInvariant)),
        ("process-host", new Regex(@"\bSystemWorkspaceHostEnvironment\s*\.\s*Instance\b|\bWorkspacePathNormalizer\s*\.\s*Process\b", RegexOptions.CultureInvariant)),
    ];

    private static readonly Regex TypeDeclaration = new(
        @"\b(?:class|record|struct|interface)\s+([A-Za-z_]\w*)",
        RegexOptions.CultureInvariant);

    private static readonly Regex IntegrationTrait = new(
        @"\[\s*(?:Xunit\.)?Trait(?:Attribute)?\s*\(\s*""Category""\s*,\s*""Integration""\s*\)",
        RegexOptions.CultureInvariant);

    private static readonly Regex TestAttribute = new(@"\[\s*(?:Xunit\.)?(?:Fact|Theory)\b", RegexOptions.CultureInvariant);

    /// <summary>Scans one C# source file and returns its findings in line order.</summary>
    /// <param name="relativePath">Path reported with each finding.</param>
    /// <param name="source">File text.</param>
    internal static IReadOnlyList<UnitSuitePlatformFinding> Scan(string relativePath, string source)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        ArgumentNullException.ThrowIfNull(source);
        var original = source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var code = StripCommentsAndStrings(source).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

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

            var lineHits = Rules.Where(rule => rule.Pattern.IsMatch(line)).Select(rule => rule.Kind).ToList();
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
    /// literals with spaces, keeping every newline so line numbers stay the same.
    /// </summary>
    /// <param name="source">C# source text.</param>
    internal static string StripCommentsAndStrings(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var result = new StringBuilder(source.Length);
        var i = 0;
        while (i < source.Length)
        {
            var c = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';
            if (c == '/' && next == '/')
            {
                while (i < source.Length && source[i] != '\n')
                    Blank(result, source[i++]);
            }
            else if (c == '/' && next == '*')
            {
                Blank(result, source[i++]);
                Blank(result, source[i++]);
                while (i < source.Length && !(source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/'))
                    Blank(result, source[i++]);
                if (i < source.Length)
                {
                    Blank(result, source[i++]);
                    Blank(result, source[i++]);
                }
            }
            else if (c == '"' || ((c == '@' || c == '$') && (next == '"' || next == '@' || next == '$')))
            {
                var verbatim = false;
                while (i < source.Length && source[i] != '"')
                {
                    verbatim |= source[i] == '@';
                    result.Append(source[i++]);
                }

                var quotes = 0;
                while (i + quotes < source.Length && source[i + quotes] == '"')
                    quotes++;
                if (quotes >= 3)
                {
                    for (var q = 0; q < quotes; q++)
                        result.Append(source[i++]);
                    while (i < source.Length && !IsQuoteRun(source, i, quotes))
                        Blank(result, source[i++]);
                    for (var q = 0; q < quotes && i < source.Length; q++)
                        result.Append(source[i++]);
                }
                else
                {
                    result.Append(source[i++]);
                    while (i < source.Length)
                    {
                        if (verbatim && source[i] == '"' && i + 1 < source.Length && source[i + 1] == '"')
                        {
                            Blank(result, source[i++]);
                            Blank(result, source[i++]);
                        }
                        else if (!verbatim && source[i] == '\\' && i + 1 < source.Length)
                        {
                            Blank(result, source[i++]);
                            Blank(result, source[i++]);
                        }
                        else if (source[i] == '"' || (!verbatim && source[i] == '\n'))
                        {
                            break;
                        }
                        else
                        {
                            Blank(result, source[i++]);
                        }
                    }

                    if (i < source.Length && source[i] == '"')
                        result.Append(source[i++]);
                }
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
                result.Append(source[i++]);
            }
        }

        return result.ToString();
    }

    private static void Blank(StringBuilder result, char c) => result.Append(c == '\n' ? '\n' : ' ');

    private static bool IsQuoteRun(string source, int index, int count)
    {
        for (var q = 0; q < count; q++)
        {
            if (index + q >= source.Length || source[index + q] != '"')
                return false;
        }

        return true;
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
