using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-NUKE-019 AC-2: the test classes that open connections to the shared LocalDB instance run one after another
/// in this collection, so they never create and migrate databases on it at the same time (ug-r3-win-lane-4 failed a
/// migration command past its 120 s timeout under that load). The collection runs with parallelization disabled, as
/// the operator's choice of 2026-10-20 states ("join one xUnit collection with parallelization disabled"), so no
/// other collection runs beside it.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerLocalDbCollection
{
    /// <summary>The collection name.</summary>
    public const string Name = "SQL Server LocalDB";
}
