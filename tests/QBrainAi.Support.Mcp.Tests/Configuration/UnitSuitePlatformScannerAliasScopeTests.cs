using System.Globalization;

namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008): the unit-suite platform scanner resolves a using alias
/// or a <c>using static</c> import in the lexical scope of the code that uses it (the enclosing namespace, then the
/// file), in the compiler's scope order, so an alias or import declared in one namespace neither hides nor invents a
/// platform check in another namespace of the same file. An alias whose target is another alias and a
/// <c>using static</c> that names an alias are not resolved, a declared limit (hostile validation round 4 finding
/// R4-F1; operator 2026-10-21 "Declare the limit, no fix"; TODO IMPL-SCANNERALIAS-001), and no case here uses either form.
/// </summary>
/// <remarks>
/// Found by hostile validation round 3 (finding R3-F2: the scanner collected every alias of a file into one dictionary,
/// so a sibling namespace's <c>using OS = System.String</c> hid a real <c>OS.IsWindows()</c>); fix chosen by the
/// operator on 2026-10-20 ("Structural fix, round 4 (Recommended)"). Test data: scripted C# sources with the findings
/// the rule requires (the reviewer's counterexample, the same namespaces in reverse order, a nested namespace that
/// overrides a file-level alias, a namespace-scoped static import, a file-scoped namespace alias, and an alias whose
/// name is OperatingSystem but whose target is String). Negative controls are the outputs the file-wide resolution
/// gave, each rejected by the finding assertion inside a passing harness.
/// </remarks>
public sealed class UnitSuitePlatformScannerAliasScopeTests
{
    private static readonly Dictionary<string, (string Source, string[] Expected)> Cases = new(StringComparer.Ordinal)
    {
        ["sibling-namespace-alias"] = ("""
            namespace A
            {
                using OS = System.OperatingSystem;
                public class AliasTests
                {
                    [Fact]
                    public void A() { _ = OS.IsWindows(); }
                }
            }
            namespace B
            {
                using OS = System.String;
                public class Helper
                {
                    public OS Name { get; set; }
                }
            }
            """, ["7 os-check AliasTests"]),
        ["reverse-order-alias"] = ("""
            namespace B
            {
                using OS = System.String;
                public class StringTests
                {
                    [Fact]
                    public void B() { _ = OS.IsNullOrEmpty("x"); }
                }
            }
            namespace A
            {
                using OS = System.OperatingSystem;
                public class AliasTests
                {
                    [Fact]
                    public void A() { _ = OS.IsWindows(); }
                }
            }
            """, ["16 os-check AliasTests"]),
        ["nested-namespace-override"] = ("""
            using OS = System.OperatingSystem;
            namespace Outer
            {
                namespace Inner
                {
                    using OS = System.String;
                    public class InnerTests
                    {
                        [Fact]
                        public void A() { _ = OS.IsNullOrEmpty("x"); }
                    }
                }
                public class OuterTests
                {
                    [Fact]
                    public void B() { _ = OS.IsWindows(); }
                }
            }
            """, ["16 os-check OuterTests"]),
        ["namespace-static-import"] = ("""
            namespace A
            {
                using static System.OperatingSystem;
                public class StaticTests
                {
                    [Fact]
                    public void A() { _ = IsWindows(); }
                }
            }
            namespace B
            {
                public class OtherTests
                {
                    [Fact]
                    public void B() { _ = IsWindows(); }
                    private static bool IsWindows() => true;
                }
            }
            """, ["7 os-check StaticTests"]),
        ["file-scoped-namespace-alias"] = ("""
            namespace N;
            using OS = System.OperatingSystem;
            public sealed class FileScopedTests
            {
                [Fact]
                public void A() { _ = OS.IsLinux(); }
            }
            """, ["6 os-check FileScopedTests"]),
        ["non-platform-alias-named-like-os"] = ("""
            namespace N
            {
                using OperatingSystem = System.String;
                public class StringTests
                {
                    [Fact]
                    public void A() { _ = OperatingSystem.IsNullOrEmpty("x"); }
                }
            }
            """, []),
    };

    /// <summary>The scripted case names, for the theories.</summary>
    public static TheoryData<string> CaseNames =>
        new()
        {
            "sibling-namespace-alias",
            "reverse-order-alias",
            "nested-namespace-override",
            "namespace-static-import",
            "file-scoped-namespace-alias",
            "non-platform-alias-named-like-os",
        };

    /// <summary>Builds scanner findings from "line kind class" strings, as the scanner would report them.</summary>
    private static UnitSuitePlatformFinding[] Findings(string path, IEnumerable<string> lines) =>
        lines.Select(line => line.Split(' '))
            .Select(parts => new UnitSuitePlatformFinding(path, int.Parse(parts[0], CultureInfo.InvariantCulture), parts[1], parts[2]))
            .ToArray();

    /// <summary>Mock gate positive control: each case's required findings pass the finding assertion.</summary>
    /// <param name="name">Case name.</param>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ScriptedCase_RequiredFindings_PassTheFindingAssertion(string name)
    {
        var expected = Cases[name].Expected;
        UnitSuitePlatformScannerMockGateTests.AssertFindings(expected, Findings(name + ".cs", expected));
    }

    /// <summary>
    /// Production binding: the scanner gives each case exactly its required findings, so an alias or static import
    /// declared in one namespace neither hides nor invents a finding in another.
    /// </summary>
    /// <param name="name">Case name.</param>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ScriptedCase_Scanner_ReportsTheRequiredFindings(string name)
    {
        var (source, expected) = Cases[name];
        UnitSuitePlatformScannerMockGateTests.AssertFindings(expected, UnitSuitePlatformScanner.Scan(name + ".cs", source));
    }

    /// <summary>The theories cover every scripted case.</summary>
    [Fact]
    public void ScriptedTheory_CoversEveryCase() => Assert.Equal(6, Cases.Count);

    /// <summary>
    /// Mock gate negative controls: the outputs the file-wide alias and import resolution gave are each rejected by
    /// the finding assertion: no finding for the reviewer's counterexample (the sibling alias won), a false finding
    /// on String.IsNullOrEmpty in reverse order (the later OperatingSystem alias won), no finding for the outer test
    /// when an inner namespace overrides the alias, and a false finding on another namespace's own IsWindows method
    /// when a static import was applied file-wide.
    /// </summary>
    [Fact]
    public void FileWideResolutionOutputs_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => UnitSuitePlatformScannerMockGateTests.AssertFindings(
            Cases["sibling-namespace-alias"].Expected, Findings("sibling.cs", []))));
        Assert.NotNull(Record.Exception(() => UnitSuitePlatformScannerMockGateTests.AssertFindings(
            Cases["reverse-order-alias"].Expected, Findings("reverse.cs", ["7 os-check StringTests", "16 os-check AliasTests"]))));
        Assert.NotNull(Record.Exception(() => UnitSuitePlatformScannerMockGateTests.AssertFindings(
            Cases["nested-namespace-override"].Expected, Findings("nested.cs", []))));
        Assert.NotNull(Record.Exception(() => UnitSuitePlatformScannerMockGateTests.AssertFindings(
            Cases["namespace-static-import"].Expected, Findings("static.cs", ["7 os-check StaticTests", "15 os-check OtherTests"]))));
        Assert.NotNull(Record.Exception(() => UnitSuitePlatformScannerMockGateTests.AssertFindings(
            Cases["non-platform-alias-named-like-os"].Expected, Findings("named.cs", ["7 os-check StringTests"]))));
    }
}
