using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008): finds platform dependence in unit tests by reading
/// the C# syntax tree. A test that depends on the platform is an integration test (operator 2026-10-19: "If it
/// depends on the environment, it's an integration test."), so an operating-system check, an OS-platform
/// attribute, an xUnit skip, or a binding to the real process host is a finding unless the test method, its
/// test class, or an enclosing class carries <c>[Trait("Category", "Integration")]</c>. The compiler's parser
/// separates code from strings, comments and documentation and keeps the code inside interpolation holes, so a
/// member access split across lines, a qualified, global-qualified or aliased name, a <c>using static</c>
/// import, a method group, and an attribute anywhere in an attribute list are read like a plain call or
/// attribute; a name inside <c>nameof</c> is not a use. Every conditional-compilation branch is read: the file
/// is parsed once for each combination of the symbols its <c>#if</c> and <c>#elif</c> directives name, and a
/// file that names more than <see cref="MaxConditionalSymbols"/> symbols fails the scan instead of leaving a
/// branch unread. Test classes are types whose name ends in <c>Tests</c> or that declare a method with an
/// attribute whose name ends in <c>Fact</c> or <c>Theory</c>; other helper types (for example the host-neutral
/// <c>UnitTestPaths</c> fixture) are not test classes. An OS-platform attribute on the assembly or module is a
/// finding for the whole project. Limits: the scan reads one file's syntax, so dependence reached through a
/// helper type defined in another file, a global using or alias declared in another file, reflection, or
/// operating-system behavior of a base-library call such as <c>Path.GetFullPath</c> is outside it. Repository
/// reads are covered by construction instead: <see cref="FindRepositoryLocators"/> keeps the shared
/// <c>TestRepositoryRoot</c> the only repository-root locator (DRY), and that locator refuses to resolve outside
/// an integration test, so a unit test that reads the checkout through any helper fails the unit run.
/// </summary>
internal static class UnitSuitePlatformScanner
{
    /// <summary>Most distinct conditional-compilation symbols one file may name; every combination is parsed.</summary>
    internal const int MaxConditionalSymbols = 8;

    private const string AssemblyOwner = "<assembly>";

    private const string AttributeSuffix = "Attribute";

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview, DocumentationMode.None);

    private static readonly string[] KindOrder = ["os-check", "os-attribute", "skip", "process-host"];

    /// <summary>Calls, property reads and method groups on a type, matched by the type's rightmost name.</summary>
    private static readonly MemberRule[] MemberRules =
    [
        new("os-check", "OperatingSystem", static name => name.Length > 2 && name.StartsWith("Is", StringComparison.Ordinal) && name[2] is >= 'A' and <= 'Z'),
        new("os-check", "RuntimeInformation", static name => name == "IsOSPlatform"),
        new("skip", "Assert", static name => name is "Skip" or "SkipWhen" or "SkipUnless"),
        new("skip", "Skip", static name => name is "If" or "IfNot"),
        new("process-host", "SystemWorkspaceHostEnvironment", static name => name == "Instance"),
        new("process-host", "WorkspacePathNormalizer", static name => name == "Process"),
    ];

    /// <summary>
    /// TR-MCP-USECASE-018 (AC-006): the one definition of an xUnit skip shared by this scanner and the skip guard.
    /// Returns the 1-based lines, in any conditional-compilation branch and in any type or none, that hold a skip
    /// call (<c>Assert.Skip</c>, <c>Assert.SkipWhen</c>, <c>Assert.SkipUnless</c>, <c>Skip.If</c>,
    /// <c>Skip.IfNot</c>), a use of <c>SkipException</c>, a <c>SkippableFact</c> or <c>SkippableTheory</c>
    /// attribute, or an attribute argument that sets a property whose name starts with <c>Skip</c> (for example
    /// <c>Skip</c>, <c>SkipUnless</c>, <c>SkipWhen</c> or <c>SkipExceptions</c> on a test or data attribute); an
    /// attribute is reported on the line of its name.
    /// </summary>
    /// <param name="source">C# source text.</param>
    internal static IReadOnlyList<int> FindSkipLines(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ParseBranches(source)
            .SelectMany(FindHits)
            .Where(hit => hit.Kind == "skip")
            .Select(hit => LineOf(hit.Node))
            .Distinct()
            .Order()
            .ToArray();
    }

    /// <summary>
    /// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008), DRY guard for repository reads: returns the 1-based
    /// lines, in any type and any conditional-compilation branch, of a repository-root locator other than the shared
    /// <c>TestRepositoryRoot</c>: a walk up (<c>.Parent</c>, <c>?.Parent</c>, <c>Directory.GetParent</c>) in a member
    /// that starts from <c>AppContext.BaseDirectory</c> or a <c>[CallerFilePath]</c> parameter (reported at the first
    /// step), a <c>Path.Combine</c> or <c>Path.Join</c> of <c>AppContext.BaseDirectory</c> with a <c>..</c> segment,
    /// or a read of the <c>McpRepositoryRoot</c> build metadata (the key and <c>AssemblyMetadataAttribute</c> in one
    /// member; the key alone, as in a comparison, is not a read).
    /// </summary>
    /// <param name="source">C# source text.</param>
    internal static IReadOnlyList<int> FindRepositoryLocators(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ParseBranches(source)
            .SelectMany(RepositoryLocatorNodes)
            .Select(LineOf)
            .Distinct()
            .Order()
            .ToArray();
    }

    /// <summary>Scans one C# source file and returns its findings in line order, then rule order.</summary>
    /// <param name="relativePath">Path reported with each finding.</param>
    /// <param name="source">File text.</param>
    internal static IReadOnlyList<UnitSuitePlatformFinding> Scan(string relativePath, string source)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        ArgumentNullException.ThrowIfNull(source);
        var findings = new HashSet<(int Line, string Kind, string TestClass)>();
        foreach (var root in ParseBranches(source))
        {
            foreach (var (node, kind) in FindHits(root))
            {
                var testClass = FindingOwner(node);
                if (testClass is not null)
                    findings.Add((LineOf(node), kind, testClass));
            }
        }

        return findings
            .OrderBy(finding => finding.Line)
            .ThenBy(finding => Array.IndexOf(KindOrder, finding.Kind))
            .ThenBy(finding => finding.TestClass, StringComparer.Ordinal)
            .Select(finding => new UnitSuitePlatformFinding(relativePath, finding.Line, finding.Kind, finding.TestClass))
            .ToArray();
    }

    /// <summary>
    /// Parses the source once for every combination of the conditional-compilation symbols it names, so code in
    /// every <c>#if</c>, <c>#elif</c> and <c>#else</c> branch reachable by some combination is in a tree.
    /// </summary>
    private static IReadOnlyList<CompilationUnitSyntax> ParseBranches(string source)
    {
        var none = Parse(source, []);
        var symbols = new SortedSet<string>(SymbolsOf(none), StringComparer.Ordinal);
        var known = 0;
        while (symbols.Count != known && symbols.Count <= MaxConditionalSymbols)
        {
            // A directive nested in a branch that is inactive with no symbols defined names its symbols here.
            known = symbols.Count;
            symbols.UnionWith(SymbolsOf(Parse(source, symbols)).ToArray());
        }

        if (symbols.Count > MaxConditionalSymbols)
        {
            throw new InvalidOperationException(
                $"The source names {symbols.Count} conditional-compilation symbols; the platform scan reads every branch and supports at most {MaxConditionalSymbols}.");
        }

        var names = symbols.ToArray();
        var roots = new List<CompilationUnitSyntax> { none };
        for (var mask = 1; mask < 1 << names.Length; mask++)
            roots.Add(Parse(source, names.Where((_, index) => (mask & (1 << index)) != 0)));

        return roots;
    }

    private static CompilationUnitSyntax Parse(string source, IEnumerable<string> symbols) =>
        CSharpSyntaxTree.ParseText(source, ParseOptions.WithPreprocessorSymbols(symbols)).GetCompilationUnitRoot();

    private static IEnumerable<string> SymbolsOf(CompilationUnitSyntax root) =>
        root.DescendantNodes(descendIntoTrivia: true)
            .OfType<ConditionalDirectiveTriviaSyntax>()
            .SelectMany(directive => directive.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            .Select(identifier => identifier.Identifier.ValueText);

    /// <summary>Every rule hit in one tree, with the node it is reported on.</summary>
    private static IEnumerable<(SyntaxNode Node, string Kind)> FindHits(CompilationUnitSyntax root)
    {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        var staticTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            var target = RightmostName(directive.NamespaceOrType);
            if (target is null)
                continue;
            if (directive.Alias is not null)
                aliases[directive.Alias.Name.Identifier.ValueText] = target;
            else if (directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                staticTypes.Add(target);
        }

        foreach (var node in root.DescendantNodes(child => child is not UsingDirectiveSyntax))
        {
            IReadOnlyList<string> kinds = node switch
            {
                AttributeSyntax attribute => AttributeKinds(attribute, aliases),
                MemberAccessExpressionSyntax access => MemberKinds(TypeName(access.Expression, aliases), access.Name.Identifier.ValueText),
                IdentifierNameSyntax { Identifier.ValueText: "SkipException" } => ["skip"],
                IdentifierNameSyntax identifier when IsUnqualified(identifier) => StaticImportKinds(identifier.Identifier.ValueText, staticTypes),
                _ => [],
            };
            if (kinds.Count > 0 && (node is AttributeSyntax || !InNameOf(node)))
            {
                foreach (var kind in kinds)
                    yield return (node, kind);
            }
        }
    }

    private static IEnumerable<SyntaxNode> RepositoryLocatorNodes(CompilationUnitSyntax root)
    {
        foreach (var member in root.DescendantNodes().OfType<MemberDeclarationSyntax>()
                     .Where(member => member is not BaseTypeDeclarationSyntax and not BaseNamespaceDeclarationSyntax and not DelegateDeclarationSyntax))
        {
            var nodes = member.DescendantNodes().ToArray();
            if (nodes.Any(IsBaseDirectory) || nodes.OfType<ParameterSyntax>().Any(HasCallerFilePath))
            {
                var step = nodes.FirstOrDefault(IsParentStep);
                if (step is not null)
                    yield return step;
            }

            foreach (var walk in nodes.OfType<InvocationExpressionSyntax>().Where(IsRelativeWalkUp))
                yield return walk;

            // A read of the build metadata names the attribute type and the key in the same member.
            if (nodes.OfType<SimpleNameSyntax>().Any(name => WithoutAttributeSuffix(name.Identifier.ValueText) == "AssemblyMetadata"))
            {
                foreach (var literal in nodes.OfType<LiteralExpressionSyntax>().Where(literal => IsStringLiteral(literal, "McpRepositoryRoot")))
                    yield return literal;
            }
        }
    }

    private static bool IsBaseDirectory(SyntaxNode node) =>
        node is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "BaseDirectory" } access
        && RightmostName(access.Expression) == "AppContext";

    private static bool HasCallerFilePath(ParameterSyntax parameter) =>
        parameter.AttributeLists.SelectMany(list => list.Attributes)
            .Any(attribute => WithoutAttributeSuffix(RightmostName(attribute.Name)) == "CallerFilePath");

    private static bool IsParentStep(SyntaxNode node) => node switch
    {
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Parent" } => true,
        MemberBindingExpressionSyntax { Name.Identifier.ValueText: "Parent" } => true,
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetParent" } } => true,
        _ => false,
    };

    private static bool IsRelativeWalkUp(InvocationExpressionSyntax invocation) =>
        RightmostName(invocation.Expression) is "Combine" or "Join"
        && invocation.ArgumentList.Arguments.Any(argument => IsBaseDirectory(argument.Expression))
        && invocation.ArgumentList.Arguments.Any(argument =>
            argument.Expression is LiteralExpressionSyntax literal
            && literal.IsKind(SyntaxKind.StringLiteralExpression)
            && literal.Token.ValueText.StartsWith("..", StringComparison.Ordinal));

    private static IReadOnlyList<string> AttributeKinds(AttributeSyntax attribute, IReadOnlyDictionary<string, string> aliases)
    {
        var name = WithoutAttributeSuffix(TypeName(attribute.Name, aliases));
        var kinds = new List<string>();
        if (name is "SupportedOSPlatform" or "UnsupportedOSPlatform")
            kinds.Add("os-attribute");
        if (name is "SkippableFact" or "SkippableTheory" || SetsSkipProperty(attribute))
            kinds.Add("skip");
        return kinds;
    }

    private static IReadOnlyList<string> MemberKinds(string? typeName, string member) =>
        MemberRules.Where(rule => rule.Type == typeName && rule.Matches(member)).Select(rule => rule.Kind).ToArray();

    private static IReadOnlyList<string> StaticImportKinds(string member, IReadOnlySet<string> staticTypes) =>
        MemberRules.Where(rule => staticTypes.Contains(rule.Type) && rule.Matches(member)).Select(rule => rule.Kind).ToArray();

    private static bool SetsSkipProperty(AttributeSyntax attribute) =>
        attribute.ArgumentList is { } arguments
        && arguments.Arguments.Any(argument =>
            argument.NameEquals is { } nameEquals && nameEquals.Name.Identifier.ValueText.StartsWith("Skip", StringComparison.Ordinal));

    /// <summary>The unit test class a hit counts against, or null when it is in no test class or in an integration test.</summary>
    private static string? FindingOwner(SyntaxNode node)
    {
        if (node is AttributeSyntax { Parent: AttributeListSyntax { Target.Identifier.ValueText: "assembly" or "module" } })
            return AssemblyOwner;

        var testClass = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault(IsTestClass);
        if (testClass is null
            || testClass.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Any(type => HasIntegrationTrait(type.AttributeLists)))
        {
            return null;
        }

        var method = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method is not null && IsTestMethod(method) && HasIntegrationTrait(method.AttributeLists))
            return null;

        return testClass.Identifier.ValueText;
    }

    private static bool IsTestClass(TypeDeclarationSyntax type) =>
        type.Identifier.ValueText.EndsWith("Tests", StringComparison.Ordinal)
        || type.Members.OfType<MethodDeclarationSyntax>().Any(IsTestMethod);

    private static bool IsTestMethod(MethodDeclarationSyntax method) =>
        method.AttributeLists.SelectMany(list => list.Attributes).Any(attribute =>
            WithoutAttributeSuffix(RightmostName(attribute.Name)) is { } name
            && (name.EndsWith("Fact", StringComparison.Ordinal) || name.EndsWith("Theory", StringComparison.Ordinal)));

    private static bool HasIntegrationTrait(SyntaxList<AttributeListSyntax> lists) =>
        lists.SelectMany(list => list.Attributes).Any(attribute =>
            WithoutAttributeSuffix(RightmostName(attribute.Name)) == "Trait"
            && attribute.ArgumentList is { Arguments.Count: >= 2 } arguments
            && IsStringLiteral(arguments.Arguments[0].Expression, "Category")
            && IsStringLiteral(arguments.Arguments[1].Expression, "Integration"));

    private static bool IsStringLiteral(ExpressionSyntax expression, string value) =>
        expression is LiteralExpressionSyntax literal
        && literal.IsKind(SyntaxKind.StringLiteralExpression)
        && literal.Token.ValueText == value;

    /// <summary>True for a name that is not the member or right-hand part of a qualified name or access.</summary>
    private static bool IsUnqualified(IdentifierNameSyntax identifier) => identifier.Parent switch
    {
        MemberAccessExpressionSyntax access => access.Name != identifier,
        QualifiedNameSyntax qualified => qualified.Right != identifier,
        AliasQualifiedNameSyntax => false,
        MemberBindingExpressionSyntax => false,
        NameEqualsSyntax or NameColonSyntax => false,
        _ => true,
    };

    private static bool InNameOf(SyntaxNode node) =>
        node.Ancestors().Any(ancestor =>
            ancestor is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } });

    /// <summary>The rightmost name of a type or receiver expression, resolving a file-level using alias.</summary>
    private static string? TypeName(SyntaxNode node, IReadOnlyDictionary<string, string> aliases) =>
        node is IdentifierNameSyntax identifier && aliases.TryGetValue(identifier.Identifier.ValueText, out var target)
            ? target
            : RightmostName(node);

    private static string? RightmostName(SyntaxNode? node) => node switch
    {
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name.Identifier.ValueText,
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        _ => null,
    };

    private static string? WithoutAttributeSuffix(string? name) =>
        name is not null && name.Length > AttributeSuffix.Length && name.EndsWith(AttributeSuffix, StringComparison.Ordinal)
            ? name[..^AttributeSuffix.Length]
            : name;

    private static int LineOf(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private sealed record MemberRule(string Kind, string Type, Func<string, bool> Matches);
}

/// <summary>TR-MCP-USECASE-018 (AC-006): one platform-dependent line in a unit test class.</summary>
/// <param name="Path">Repository-relative source path.</param>
/// <param name="Line">1-based line number.</param>
/// <param name="Kind">os-check, os-attribute, skip, or process-host.</param>
/// <param name="TestClass">Unit test class that holds the line, or <c>&lt;assembly&gt;</c> for an assembly- or module-level attribute.</param>
internal sealed record UnitSuitePlatformFinding(string Path, int Line, string Kind, string TestClass)
{
    /// <inheritdoc />
    public override string ToString() => $"{Path}:{Line} {Kind} in {TestClass}";
}
