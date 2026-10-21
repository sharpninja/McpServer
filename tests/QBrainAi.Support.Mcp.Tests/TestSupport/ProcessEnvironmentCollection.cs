using Xunit;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008: the test classes that write the process environment run
/// one at a time in this collection, with parallelization disabled, so no other test runs while one of them has a
/// variable changed (operator 2026-10-21: "Fix both, strengthen ACs" and "Approve, strengthen AC-006/008
/// (Recommended)"; hostile validation round 5 finding R5-F2). Each such test also restores every variable it sets.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessEnvironmentCollection
{
    /// <summary>The collection name.</summary>
    public const string Name = "Process environment";
}
