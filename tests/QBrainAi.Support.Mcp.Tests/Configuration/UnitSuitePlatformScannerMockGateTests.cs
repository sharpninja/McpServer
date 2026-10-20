namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008), BDPv4 mock gate for
/// <see cref="UnitSuitePlatformScanner"/>: scripted C# sources with findings derived from the rule (an OS
/// check, OS-platform attribute, xUnit skip, or real process-host binding in a test class without
/// <c>[Trait("Category", "Integration")]</c> is a finding; strings, comments and non-test helpers are not),
/// plus negative controls proving the finding assertion rejects a missing, extra, or misattributed finding.
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
    public void ScriptedSource_YieldsRuleFindings(string name)
    {
        var (source, expected) = Cases[name];
        AssertFindings(expected, UnitSuitePlatformScanner.Scan(name + ".cs", source));
    }

    /// <summary>The theory above covers every scripted case.</summary>
    [Fact]
    public void ScriptedTheory_CoversEveryCase() => Assert.Equal(17, Cases.Count);

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

    /// <summary>Stripping keeps every line and removes the text inside strings, comments and characters.</summary>
    [Fact]
    public void StripCommentsAndStrings_KeepsLinesAndBlanksLiterals()
    {
        var (source, _) = Cases["verbatim-raw-and-char"];
        var stripped = UnitSuitePlatformScanner.StripCommentsAndStrings(source);

        Assert.Equal(source.Count(ch => ch == '\n'), stripped.Count(ch => ch == '\n'));
        Assert.DoesNotContain("still inside", stripped, StringComparison.Ordinal);
        Assert.DoesNotContain("IsMacOS", stripped, StringComparison.Ordinal);
        Assert.Contains("_ = OperatingSystem.IsLinux();", stripped, StringComparison.Ordinal);
    }

    /// <summary>
    /// Stripping keeps interpolation holes as code and blanks the literal text around them, escaped braces,
    /// strings nested in a hole, and a hole's format clause.
    /// </summary>
    [Fact]
    public void StripCommentsAndStrings_KeepsInterpolationHolesAsCode()
    {
        var (source, _) = Cases["interpolation-holes"];
        var stripped = UnitSuitePlatformScanner.StripCommentsAndStrings(source);

        Assert.Equal(source.Count(ch => ch == '\n'), stripped.Count(ch => ch == '\n'));
        Assert.Contains("{OperatingSystem.IsWindows()}", stripped, StringComparison.Ordinal);
        Assert.Contains("RuntimeInformation.IsOSPlatform(OSPlatform.Linux)", stripped, StringComparison.Ordinal);
        Assert.Contains("{{OperatingSystem.IsFreeBSD()}}", stripped, StringComparison.Ordinal);
        Assert.DoesNotContain("host", stripped, StringComparison.Ordinal);
        Assert.DoesNotContain("IsLinux()", stripped, StringComparison.Ordinal);
        Assert.DoesNotContain("IsMacOS", stripped, StringComparison.Ordinal);
        Assert.DoesNotContain("IsAndroid", stripped, StringComparison.Ordinal);
        Assert.DoesNotContain("IsIOS", stripped, StringComparison.Ordinal);
        Assert.DoesNotContain("\"l\"", stripped, StringComparison.Ordinal);
    }

    /// <summary>Assertion shared by the scripted cases and the negative controls.</summary>
    /// <param name="expected">Findings as "line kind class".</param>
    /// <param name="actual">Scanner output.</param>
    internal static void AssertFindings(IReadOnlyList<string> expected, IReadOnlyList<UnitSuitePlatformFinding> actual) =>
        Assert.Equal(expected, actual.Select(finding => $"{finding.Line} {finding.Kind} {finding.TestClass}").ToArray());
}
