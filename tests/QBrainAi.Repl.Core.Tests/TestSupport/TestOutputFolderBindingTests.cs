using QBrainAi.TestSupport.TemporaryInstance;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 (second criterion, generated artifacts in test-owned locations): binds the unchanged
/// <see cref="TestOutputFolderContractTests"/> rows and assertions, mock-gated in ug-r9-mockgate-s-1, to the shared
/// <see cref="TemporaryInstanceChecks.CheckTestOutputFolder"/>. Strings only: no folder is created or read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 (second criterion): binds the mock-gated rows to the production check (operator 2026-11-02: "Fix in scope").</description></item>
/// </list>
/// </remarks>
public sealed class TestOutputFolderBindingTests
{
    /// <summary>The shared check accepts every test-owned folder outside the checkout and refuses every other row, naming the folder.</summary>
    [Fact]
    public void SharedCheck_SatisfiesMockGatedRows() =>
        TestOutputFolderContractTests.AssertCheck(TemporaryInstanceChecks.CheckTestOutputFolder);
}
