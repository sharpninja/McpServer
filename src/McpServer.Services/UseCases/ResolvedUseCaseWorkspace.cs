namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// Workspace resolution for creation, separating the opaque database key from filesystem identity.
/// </summary>
/// <param name="WorkspaceId">Opaque persisted primary key used by foreign keys.</param>
/// <param name="CanonicalPath">Canonical requested filesystem path.</param>
/// <param name="StorageIdentity">Provider-independent canonical filesystem identity.</param>
internal sealed record ResolvedUseCaseWorkspace(
    string WorkspaceId,
    string CanonicalPath,
    string StorageIdentity);
