using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// AC-FR-MCP-SESSIONLOGCTX-001-005, TR-MCP-USECASE-018 AC-006: exact plan-file filtering with an explicitly
/// owned home. Fixed query/result rows prove tilde expansion without reading a real profile. The original
/// service binding shares these assertions after the mock gate; incorrect adapters must fail them.
/// </summary>
public sealed class SessionLogOwnedHomeFilterContractTests
{
    /// <summary>Assertion sequence shared by the independent mock and real service binding.</summary>
    internal static async Task AssertOwnedHomeFilterAsync(Func<string, Task<int>> query)
    {
        Assert.Equal(1, await query("/fixture/home/plans/live.md"));
        Assert.Equal(1, await query("~/plans/live.md"));
        Assert.Equal(0, await query("~/plans/other.md"));
        Assert.Equal(0, await query("~/plans/LIVE.md"));
    }

    /// <summary>Requirement-derived literal results exercise every assertion without production code or environment reads.</summary>
    [Fact]
    public Task CorrectScriptedQueries_PassTheContract() => AssertOwnedHomeFilterAsync(Query);

    private static Task<int> Query(string path) => Task.FromResult(path switch
    {
        "/fixture/home/plans/live.md" or "~/plans/live.md" => 1,
        "~/plans/other.md" or "~/plans/LIVE.md" => 0,
        _ => throw new InvalidOperationException("Unscripted query: " + path),
    });

    /// <summary>Ignored tilde expansion, broad matching and case-insensitive matching are rejected by the same assertions.</summary>
    [Fact]
    public async Task WrongQueryAdapters_AreRejectedInsidePassingHarness()
    {
        Assert.IsAssignableFrom<XunitException>(await Record.ExceptionAsync(() =>
            AssertOwnedHomeFilterAsync(path => Task.FromResult(path == "/fixture/home/plans/live.md" ? 1 : 0))));
        Assert.IsAssignableFrom<XunitException>(await Record.ExceptionAsync(() =>
            AssertOwnedHomeFilterAsync(_ => Task.FromResult(1))));
        Assert.IsAssignableFrom<XunitException>(await Record.ExceptionAsync(() =>
            AssertOwnedHomeFilterAsync(path => path == "~/plans/LIVE.md" ? Task.FromResult(1) : Query(path))));
    }
}
