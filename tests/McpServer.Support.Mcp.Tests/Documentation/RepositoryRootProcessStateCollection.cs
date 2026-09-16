namespace McpServer.Support.Mcp.Tests.Documentation;

/// <summary>Serializes repository-root tests that temporarily mutate process-wide state.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RepositoryRootProcessStateCollection
{
    /// <summary>Collection name for external-artifact repository-root tests.</summary>
    public const string Name = "Repository root process state";
}
