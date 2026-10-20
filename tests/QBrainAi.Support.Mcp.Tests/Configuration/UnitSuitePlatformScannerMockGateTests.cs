using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008), BDPv4 mock gate for
/// <see cref="UnitSuitePlatformScanner"/>: scripted C# sources with findings derived from the rule (an OS
/// check, OS-platform attribute, xUnit skip, or real process-host binding in a test that is not tagged
/// <c>[Trait("Category", "Integration")]</c> on its method or class is a finding; strings, comments,
/// <c>nameof</c> and non-test helpers are not; every conditional-compilation branch is read), plus negative
/// controls proving the finding assertion rejects a missing, extra, or misattributed finding.
/// </summary>
public sealed class UnitSuitePlatformScannerMockGateTests
{
    private static readonly Dictionary<string, (string Source, string[] Expected)> Cases = new(StringComparer.Ordinal)
    {
        ["unit-os-check"] = ("""
            namespace N;
            public sealed class FooTests
            {
                [Fact]
                public void A()
                {
                    if (OperatingSystem.IsWindows()) return;
                }
            }
            """, ["7 os-check FooTests"]),
        ["integration-class"] = ("""
            namespace N;
            [Trait("Category", "Integration")]
            public sealed class FooTests
            {
                [Fact]
                public void A()
                {
                    if (OperatingSystem.IsWindows()) return;
                }
            }
            """, []),
        ["string-and-comment"] = ("""
            namespace N;
            public sealed class FooTests
            {
                [Fact]
                public void A()
                {
                    var s = "OperatingSystem.IsWindows()"; // RuntimeInformation.IsOSPlatform(x)
                    /* Assert.Skip("x"); [SupportedOSPlatform("linux")] */
                    Assert.Equal("WorkspacePathNormalizer.Process", s);
                }
            }
            """, []),
        ["os-attribute-before-class"] = ("""
            namespace N;
            [SupportedOSPlatform("linux")]
            public sealed class LinuxTests
            {
                [Fact]
                public void A() { }
            }
            """, ["2 os-attribute LinuxTests"]),
        ["skip-forms"] = ("""
            namespace N;
            public sealed class FooTests
            {
                [Fact(Skip = "later")]
                public void A() { }
                [Fact]
                public void B()
                {
                    Assert.SkipWhen(true, "w");
                    Assert.SkipUnless(false, "u");
                    Assert.Skip("s");
                }
            }
            """, ["4 skip FooTests", "9 skip FooTests", "10 skip FooTests", "11 skip FooTests"]),
        ["helper-not-test"] = ("""
            namespace N;
            internal static class UnitTestPaths
            {
                public static string Root { get; } = OperatingSystem.IsWindows() ? "a" : "b";
            }
            """, []),
        ["nested-in-integration"] = ("""
            namespace N;
            [Trait("Category", "Integration")]
            public sealed class OuterTests
            {
                [Fact]
                public void A() { }
                private sealed class Helper
                {
                    public bool On => OperatingSystem.IsLinux();
                }
            }
            """, []),
        ["second-class-unit"] = ("""
            namespace N;
            [Trait("Category", "Integration")]
            public sealed class FirstTests
            {
                [Fact]
                public void A() { _ = OperatingSystem.IsLinux(); }
            }
            public sealed class SecondTests
            {
                [Fact]
                public void B() { _ = OperatingSystem.IsLinux(); }
            }
            """, ["11 os-check SecondTests"]),
        ["process-host"] = ("""
            namespace N;
            public sealed class HostTests
            {
                [Fact]
                public void A()
                {
                    var host = SystemWorkspaceHostEnvironment.Instance;
                    var key = QBrainAi.Client.WorkspacePathNormalizer.Process.Normalize("x");
                }
            }
            """, ["7 process-host HostTests", "8 process-host HostTests"]),
        ["runtime-information"] = ("""
            namespace N;
            public sealed class RuntimeTests
            {
                [Fact]
                public void A() => Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            }
            """, ["5 os-check RuntimeTests"]),
        ["method-attribute-in-unit"] = ("""
            namespace N;
            public sealed class MethodTests
            {
                [Fact]
                [SupportedOSPlatform("windows")]
                public void A() { }
            }
            """, ["5 os-attribute MethodTests"]),
        ["verbatim-raw-and-char"] = ("""""
            namespace N;
            public sealed class LiteralTests
            {
                [Fact]
                public void A()
                {
                    var v = @"OperatingSystem.IsLinux() "" still inside";
                    var r = """OperatingSystem.IsMacOS()""";
                    var c = '"';
                    _ = OperatingSystem.IsLinux();
                }
            }
            """"", ["10 os-check LiteralTests"]),
        ["abstract-contract-with-skip"] = ("""
            namespace N;
            public abstract class ContractTests
            {
                [Fact]
                public void A()
                {
                    Assert.SkipUnless(OperatingSystem.IsWindows(), "w");
                }
            }
            """, ["7 os-check ContractTests", "7 skip ContractTests"]),
        ["integration-with-other-attributes"] = ("""
            namespace N;
            [Collection("x")]
            [Trait("Category", "Integration")]
            [SupportedOSPlatform("linux")]
            public sealed class LinuxTests
            {
                [Fact]
                public void A() => Assert.True(OperatingSystem.IsLinux());
            }
            """, []),
        ["multiline-skip-attribute"] = ("""
            namespace N;
            public sealed class FooTests
            {
                [Fact(
                    Skip = "later")]
                public void A() { }
                [Theory(DisplayName = nameof(B),
                    SkipUnless = nameof(Ready),
                    Skip = "not ready")]
                public void B(int x) { }
            }
            """, ["4 skip FooTests", "7 skip FooTests"]),
        ["qualified-skip-attribute"] = ("""
            namespace N;
            public sealed class FooTests
            {
                [Xunit.Fact(Skip = "later")]
                public void A() { }
                [global::Xunit.TheoryAttribute(Skip = "x")]
                public void B(int x) { }
                [Trait("k", "v"), Fact(Skip = "y")]
                public void C() { }
                [Fact(Timeout = 10)]
                public void D() { }
                [Fact(DisplayName = "Skip = later")]
                public void E() { }
            }
            """, ["4 skip FooTests", "6 skip FooTests", "8 skip FooTests"]),
        ["interpolation-holes"] = (""""
            namespace N;
            public sealed class FooTests
            {
                [Fact]
                public void A()
                {
                    var a = $"host {OperatingSystem.IsWindows()} end";
                    var b = $"text OperatingSystem.IsLinux() {{OperatingSystem.IsMacOS()}}";
                    var c = $@"{(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "l" : "w")}";
                    var d = $$"""{{OperatingSystem.IsFreeBSD()}} {OperatingSystem.IsAndroid()}""";
                    var e = $"{value:OperatingSystem.IsIOS()}";
                }
            }
            """", ["7 os-check FooTests", "9 os-check FooTests", "10 os-check FooTests"]),
        ["split-and-qualified-os-calls"] = ("""
            namespace N;
            public sealed class FooTests
            {
                [Fact]
                public void A()
                {
                    _ = OperatingSystem
                        .IsWindows();
                    _ = OperatingSystem.
                        IsLinux();
                    _ = global::System.OperatingSystem.IsMacOS();
                    var s = $@"{OperatingSystem.
                        IsFreeBSD()}";
                    _ = System.Runtime.InteropServices.RuntimeInformation
                        .IsOSPlatform(OSPlatform.Linux);
                }
            }
            """, ["7 os-check FooTests", "9 os-check FooTests", "11 os-check FooTests", "12 os-check FooTests", "14 os-check FooTests"]),
        ["qualified-and-listed-os-attributes"] = ("""
            namespace N;
            public sealed class FooTests
            {
                [global::System.Runtime.Versioning.SupportedOSPlatform("windows")]
                [Fact]
                public void A() { }
                [Fact, System.Runtime.Versioning.SupportedOSPlatform("windows")]
                public void B() { }
                [Fact]
                [UnsupportedOSPlatformAttribute("linux")]
                public void C() { }
                [Fact(DisplayName = "SupportedOSPlatform(\"windows\")")]
                public void D() { }
            }
            """, ["4 os-attribute FooTests", "7 os-attribute FooTests", "10 os-attribute FooTests"]),
        ["aliases-static-imports-and-method-groups"] = ("""
            using OS = System.OperatingSystem;
            using static System.Runtime.InteropServices.RuntimeInformation;
            using static Xunit.Assert;
            namespace N;
            public sealed class FooTests
            {
                [Fact]
                public void A()
                {
                    _ = OS.IsWindows();
                    _ = IsOSPlatform(OSPlatform.Linux);
                    Func<bool> check = OperatingSystem.IsLinux;
                    Skip("s");
                    _ = nameof(OperatingSystem.IsMacOS);
                    True(true);
                }
            }
            """, ["10 os-check FooTests", "11 os-check FooTests", "12 os-check FooTests", "13 skip FooTests"]),
        ["assembly-attribute"] = ("""
            [assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]
            namespace N;
            public sealed class FooTests
            {
                [Fact]
                public void A() { }
            }
            """, ["1 os-attribute <assembly>"]),
        ["method-level-integration"] = ("""
            namespace N;
            public sealed class FooTests
            {
                [Fact]
                [Trait("Category", "Integration")]
                [SupportedOSPlatform("windows")]
                public void A() { _ = OperatingSystem.IsWindows(); }
                [Fact]
                public void B() { _ = OperatingSystem.IsWindows(); }
            }
            """, ["9 os-check FooTests"]),
        ["skip-variants"] = ("""
            namespace N;
            public sealed class FooTests
            {
                [Theory]
                [InlineData(1, Skip = "row")]
                [InlineData(2)]
                public void A(int x) { }
                [SkippableFactAttribute]
                public void B() { Skip.If(true); Skip.IfNot(false); }
                [Fact(SkipExceptions = new[] { typeof(InvalidOperationException) })]
                public void C() => throw new Xunit.Sdk.SkipException("x");
            }
            """, ["5 skip FooTests", "8 skip FooTests", "9 skip FooTests", "10 skip FooTests", "11 skip FooTests"]),
        ["conditional-compilation-branches"] = ("""
            namespace N;
            public sealed class FooTests
            {
                [Fact]
                public void A()
                {
            #if PROCESS_MOCK
                    _ = OperatingSystem.IsWindows();
            #else
                    _ = OperatingSystem.IsLinux();
            #endif
            #if WINDOWS && !LINUX
                    Assert.Skip("w");
            #endif
                }
            }
            """, ["8 os-check FooTests", "10 os-check FooTests", "13 skip FooTests"]),
    };

    /// <summary>Every scripted source yields exactly the findings the rule requires.</summary>
    /// <param name="name">Case name.</param>
    [Theory]
    [InlineData("unit-os-check")]
    [InlineData("integration-class")]
    [InlineData("string-and-comment")]
    [InlineData("os-attribute-before-class")]
    [InlineData("skip-forms")]
    [InlineData("helper-not-test")]
    [InlineData("nested-in-integration")]
    [InlineData("second-class-unit")]
    [InlineData("process-host")]
    [InlineData("runtime-information")]
    [InlineData("method-attribute-in-unit")]
    [InlineData("verbatim-raw-and-char")]
    [InlineData("abstract-contract-with-skip")]
    [InlineData("integration-with-other-attributes")]
    [InlineData("multiline-skip-attribute")]
    [InlineData("qualified-skip-attribute")]
    [InlineData("interpolation-holes")]
    [InlineData("split-and-qualified-os-calls")]
    [InlineData("qualified-and-listed-os-attributes")]
    [InlineData("aliases-static-imports-and-method-groups")]
    [InlineData("assembly-attribute")]
    [InlineData("method-level-integration")]
    [InlineData("skip-variants")]
    [InlineData("conditional-compilation-branches")]
    public void ScriptedSource_YieldsRuleFindings(string name)
    {
        var (source, expected) = Cases[name];
        AssertFindings(expected, UnitSuitePlatformScanner.Scan(name + ".cs", source));
    }

    /// <summary>The theory above covers every scripted case.</summary>
    [Fact]
    public void ScriptedTheory_CoversEveryCase() => Assert.Equal(24, Cases.Count);

    /// <summary>
    /// Negative controls for the forms the syntax-tree scanner reads (structural fix after hostile validation
    /// round 2): each new case's findings are not empty; counting a name inside <c>nameof</c> is rejected;
    /// honoring the Integration trait only on classes (which also counts the method-level Integration test) is
    /// rejected; and the output of one parse with no conditional symbols defined (which sees only the
    /// <c>#else</c> branch) is rejected. A plain parse with no symbols has no <c>IsWindows</c> access at all,
    /// which is why every branch combination is parsed.
    /// </summary>
    [Fact]
    public void RoundThreeFormNegativeControls_AreRejectedInsidePassingHarness()
    {
        foreach (var name in new[] { "aliases-static-imports-and-method-groups", "assembly-attribute", "method-level-integration", "skip-variants", "conditional-compilation-branches" })
        {
            var expected = Cases[name].Expected;
            Assert.NotNull(Record.Exception(() => AssertFindings(expected, [])));
        }

        var aliases = Cases["aliases-static-imports-and-method-groups"];
        var aliasFindings = UnitSuitePlatformScanner.Scan("aliases.cs", aliases.Source);
        Assert.NotNull(Record.Exception(() => AssertFindings([.. aliases.Expected, "14 os-check FooTests"], aliasFindings)));
        Assert.Null(Record.Exception(() => AssertFindings(aliases.Expected, aliasFindings)));

        var methodLevel = Cases["method-level-integration"];
        var methodFindings = UnitSuitePlatformScanner.Scan("method-level.cs", methodLevel.Source);
        Assert.NotNull(Record.Exception(() => AssertFindings(
            ["6 os-attribute FooTests", "7 os-check FooTests", "9 os-check FooTests"], methodFindings)));
        Assert.Null(Record.Exception(() => AssertFindings(methodLevel.Expected, methodFindings)));

        var branches = Cases["conditional-compilation-branches"];
        var branchFindings = UnitSuitePlatformScanner.Scan("branches.cs", branches.Source);
        Assert.NotNull(Record.Exception(() => AssertFindings(["10 os-check FooTests"], branchFindings)));
        Assert.Null(Record.Exception(() => AssertFindings(branches.Expected, branchFindings)));
        var plainParse = CSharpSyntaxTree.ParseText(branches.Source, cancellationToken: TestContext.Current.CancellationToken)
            .GetRoot(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(
            plainParse.DescendantNodes().OfType<MemberAccessExpressionSyntax>(),
            access => access.Name.Identifier.ValueText == "IsWindows");
    }

    /// <summary>
    /// A file that names more conditional-compilation symbols than the scanner parses combinations for fails the
    /// scan and the skip definition instead of leaving a branch unread; a file at the limit is read.
    /// </summary>
    [Fact]
    public void ConditionalSymbolsBeyondTheLimit_FailClosed()
    {
        static string SourceWith(int symbolCount) =>
            "#if " + string.Join(" || ", Enumerable.Range(1, symbolCount).Select(index => "S" + index.ToString(CultureInfo.InvariantCulture)))
            + "\npublic sealed class FooTests\n{\n    [Fact]\n    public void A() => Assert.Skip(\"s\");\n}\n#endif\n";

        var over = SourceWith(UnitSuitePlatformScanner.MaxConditionalSymbols + 1);
        Assert.Throws<InvalidOperationException>(() => UnitSuitePlatformScanner.Scan("over.cs", over));
        Assert.Throws<InvalidOperationException>(() => UnitSuitePlatformScanner.FindSkipLines(over));

        var atLimit = SourceWith(UnitSuitePlatformScanner.MaxConditionalSymbols);
        AssertFindings(["5 skip FooTests"], UnitSuitePlatformScanner.Scan("limit.cs", atLimit));
        Assert.Equal(new[] { 5 }, UnitSuitePlatformScanner.FindSkipLines(atLimit));
    }

    /// <summary>
    /// Negative controls for the cases added after hostile validation round 2 (finding R2-F2): the outputs
    /// the line-based scanner gave for a member call split after the dot and for a qualified or listed OS
    /// attribute (no findings), an output that drops the global-qualified call, and an output that also
    /// counts an OS attribute name inside a string argument are each rejected by the finding assertion.
    /// </summary>
    [Fact]
    public void RoundTwoCounterexampleNegativeControls_AreRejectedInsidePassingHarness()
    {
        foreach (var name in new[] { "split-and-qualified-os-calls", "qualified-and-listed-os-attributes" })
        {
            var expected = Cases[name].Expected;
            Assert.NotNull(Record.Exception(() => AssertFindings(expected, [])));
        }

        var calls = UnitSuitePlatformScanner.Scan("split-and-qualified-os-calls.cs", Cases["split-and-qualified-os-calls"].Source);
        Assert.NotNull(Record.Exception(() => AssertFindings(
            ["7 os-check FooTests", "9 os-check FooTests", "12 os-check FooTests", "14 os-check FooTests"], calls)));
        Assert.Null(Record.Exception(() => AssertFindings(Cases["split-and-qualified-os-calls"].Expected, calls)));

        var attributes = UnitSuitePlatformScanner.Scan("qualified-and-listed-os-attributes.cs", Cases["qualified-and-listed-os-attributes"].Source);
        Assert.NotNull(Record.Exception(() => AssertFindings(
            ["4 os-attribute FooTests", "7 os-attribute FooTests", "10 os-attribute FooTests", "12 os-attribute FooTests"], attributes)));
        Assert.Null(Record.Exception(() => AssertFindings(Cases["qualified-and-listed-os-attributes"].Expected, attributes)));
    }

    /// <summary>
    /// Negative controls for the cases added after hostile validation round 1 (finding F3): the outputs a
    /// line-based skip match, an unqualified attribute match, and a literal-blanking interpolation strip gave
    /// (no findings), and an output that also counts the literal text outside a hole or an escaped-brace
    /// block, are each rejected by the finding assertion.
    /// </summary>
    [Fact]
    public void RoundOneCounterexampleNegativeControls_AreRejectedInsidePassingHarness()
    {
        foreach (var name in new[] { "multiline-skip-attribute", "qualified-skip-attribute", "interpolation-holes" })
        {
            var expected = Cases[name].Expected;
            Assert.NotNull(Record.Exception(() => AssertFindings(expected, [])));
        }

        var interpolation = UnitSuitePlatformScanner.Scan("interpolation-holes.cs", Cases["interpolation-holes"].Source);
        Assert.NotNull(Record.Exception(() => AssertFindings(
            ["7 os-check FooTests", "8 os-check FooTests", "9 os-check FooTests", "10 os-check FooTests"], interpolation)));
        Assert.NotNull(Record.Exception(() => AssertFindings(
            ["7 os-check FooTests", "9 os-check FooTests", "10 os-check FooTests", "11 os-check FooTests"], interpolation)));
        Assert.Null(Record.Exception(() => AssertFindings(Cases["interpolation-holes"].Expected, interpolation)));

        var qualified = UnitSuitePlatformScanner.Scan("qualified-skip-attribute.cs", Cases["qualified-skip-attribute"].Source);
        Assert.NotNull(Record.Exception(() => AssertFindings(
            ["4 skip FooTests", "6 skip FooTests", "8 skip FooTests", "10 skip FooTests"], qualified)));
        Assert.NotNull(Record.Exception(() => AssertFindings(
            ["4 skip FooTests", "6 skip FooTests", "8 skip FooTests", "12 skip FooTests"], qualified)));
    }

    /// <summary>
    /// Negative controls: the finding assertion rejects a missing finding, an extra finding, a finding on
    /// the wrong line, and a finding attributed to the wrong class.
    /// </summary>
    [Fact]
    public void FindingAssertionNegativeControls_AreRejectedInsidePassingHarness()
    {
        var actual = UnitSuitePlatformScanner.Scan("skip-forms.cs", Cases["skip-forms"].Source);
        Assert.NotNull(Record.Exception(() => AssertFindings(["4 skip FooTests", "9 skip FooTests", "10 skip FooTests"], actual)));
        Assert.NotNull(Record.Exception(() => AssertFindings(["4 skip FooTests", "9 skip FooTests", "10 skip FooTests", "11 skip FooTests", "12 skip FooTests"], actual)));
        Assert.NotNull(Record.Exception(() => AssertFindings(["4 skip FooTests", "9 skip FooTests", "10 skip FooTests", "12 skip FooTests"], actual)));
        Assert.NotNull(Record.Exception(() => AssertFindings(["4 skip FooTests", "9 skip FooTests", "10 skip FooTests", "11 skip BarTests"], actual)));
        Assert.Null(Record.Exception(() => AssertFindings(["4 skip FooTests", "9 skip FooTests", "10 skip FooTests", "11 skip FooTests"], actual)));
    }

    /// <summary>
    /// Literals and comments never yield a finding or a skip line: every rule's text placed only inside
    /// regular, verbatim, raw and interpolated string text, character literals, line comments, block comments
    /// and XML documentation is ignored, while the same text as code in the last line is found.
    /// </summary>
    [Fact]
    public void LiteralsAndComments_NeverYieldFindings()
    {
        const string source = """"
            namespace N;
            public sealed class FooTests
            {
                /// <summary>OperatingSystem.IsWindows() [SupportedOSPlatform("x")] Assert.Skip("y")</summary>
                [Fact]
                public void A()
                {
                    var a = "OperatingSystem.IsWindows() RuntimeInformation.IsOSPlatform(p)";
                    var b = @"[Fact(Skip = ""x"")] SkipException SystemWorkspaceHostEnvironment.Instance";
                    var c = """[SupportedOSPlatform("linux")] WorkspacePathNormalizer.Process""";
                    var d = $"text OperatingSystem.IsLinux() {{Assert.SkipWhen(true, w)}}";
                    var e = '[';
                    // OperatingSystem.IsMacOS(); Assert.SkipUnless(false, "u");
                    /* [SkippableFact] [UnsupportedOSPlatform("ios")] */
                    _ = OperatingSystem.IsLinux();
                }
            }
            """";
        AssertFindings(["15 os-check FooTests"], UnitSuitePlatformScanner.Scan("literals.cs", source));
        Assert.Empty(UnitSuitePlatformScanner.FindSkipLines(source));
    }

    /// <summary>Assertion shared by the scripted cases and the negative controls.</summary>
    /// <param name="expected">Findings as "line kind class".</param>
    /// <param name="actual">Scanner output.</param>
    internal static void AssertFindings(IReadOnlyList<string> expected, IReadOnlyList<UnitSuitePlatformFinding> actual) =>
        Assert.Equal(expected, actual.Select(finding => $"{finding.Line} {finding.Kind} {finding.TestClass}").ToArray());
}
