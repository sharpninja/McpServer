namespace McpServer.ToolRegistry.Validation.Models;

/// <summary>
/// Validation contract type <c>ToolDto</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
/// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
/// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
/// </remarks>
public sealed class ToolDto
{
    /// <summary>
    /// Gets or sets <c>Id</c> for validation payload/state handling.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets <c>Name</c> for validation payload/state handling.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Description</c> for validation payload/state handling.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Tags</c> for validation payload/state handling.
    /// </summary>
    public List<string> Tags { get; set; } = [];
    /// <summary>
    /// Gets or sets <c>ParameterSchema</c> for validation payload/state handling.
    /// </summary>
    public string? ParameterSchema { get; set; }
    /// <summary>
    /// Gets or sets <c>CommandTemplate</c> for validation payload/state handling.
    /// </summary>
    public string? CommandTemplate { get; set; }
    /// <summary>
    /// Gets or sets <c>WorkspacePath</c> for validation payload/state handling.
    /// </summary>
    public string? WorkspacePath { get; set; }
    /// <summary>
    /// Gets or sets <c>DateTimeCreated</c> for validation payload/state handling.
    /// </summary>
    public DateTimeOffset DateTimeCreated { get; set; }
    /// <summary>
    /// Gets or sets <c>DateTimeModified</c> for validation payload/state handling.
    /// </summary>
    public DateTimeOffset DateTimeModified { get; set; }
}

/// <summary>
/// Validation contract type <c>ToolSearchResult</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
/// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
/// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
/// </remarks>
public sealed class ToolSearchResult
{
    /// <summary>
    /// Gets or sets <c>Tools</c> for validation payload/state handling.
    /// </summary>
    public List<ToolDto> Tools { get; set; } = [];
    /// <summary>
    /// Gets or sets <c>TotalCount</c> for validation payload/state handling.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Validation contract type <c>ToolMutationResult</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
/// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
/// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
/// </remarks>
public sealed class ToolMutationResult
{
    /// <summary>
    /// Gets or sets <c>Success</c> for validation payload/state handling.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets <c>Error</c> for validation payload/state handling.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets <c>Tool</c> for validation payload/state handling.
    /// </summary>
    public ToolDto? Tool { get; set; }
}

/// <summary>
/// Validation contract type <c>BucketDto</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
/// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
/// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
/// </remarks>
public sealed class BucketDto
{
    /// <summary>
    /// Gets or sets <c>Id</c> for validation payload/state handling.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets <c>Name</c> for validation payload/state handling.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Owner</c> for validation payload/state handling.
    /// </summary>
    public string Owner { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Repo</c> for validation payload/state handling.
    /// </summary>
    public string Repo { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Branch</c> for validation payload/state handling.
    /// </summary>
    public string Branch { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>ManifestPath</c> for validation payload/state handling.
    /// </summary>
    public string ManifestPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>DateTimeCreated</c> for validation payload/state handling.
    /// </summary>
    public DateTimeOffset DateTimeCreated { get; set; }
    /// <summary>
    /// Gets or sets <c>DateTimeLastSynced</c> for validation payload/state handling.
    /// </summary>
    public DateTimeOffset? DateTimeLastSynced { get; set; }
}

/// <summary>
/// Validation contract type <c>BucketListResult</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
/// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
/// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
/// </remarks>
public sealed class BucketListResult
{
    /// <summary>
    /// Gets or sets <c>Buckets</c> for validation payload/state handling.
    /// </summary>
    public List<BucketDto> Buckets { get; set; } = [];
    /// <summary>
    /// Gets or sets <c>TotalCount</c> for validation payload/state handling.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Validation contract type <c>BucketMutationResult</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
/// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
/// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
/// </remarks>
public sealed class BucketMutationResult
{
    /// <summary>
    /// Gets or sets <c>Success</c> for validation payload/state handling.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets <c>Error</c> for validation payload/state handling.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets <c>Bucket</c> for validation payload/state handling.
    /// </summary>
    public BucketDto? Bucket { get; set; }
}

/// <summary>
/// Validation contract type <c>ToolManifest</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
/// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
/// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
/// </remarks>
public sealed class ToolManifest
{
    /// <summary>
    /// Gets or sets <c>Name</c> for validation payload/state handling.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Description</c> for validation payload/state handling.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets <c>Tags</c> for validation payload/state handling.
    /// </summary>
    public List<string> Tags { get; set; } = [];
    /// <summary>
    /// Gets or sets <c>ParameterSchema</c> for validation payload/state handling.
    /// </summary>
    public string? ParameterSchema { get; set; }
    /// <summary>
    /// Gets or sets <c>CommandTemplate</c> for validation payload/state handling.
    /// </summary>
    public string? CommandTemplate { get; set; }
    /// <summary>
    /// Gets or sets <c>ManifestFile</c> for validation payload/state handling.
    /// </summary>
    public string ManifestFile { get; set; } = string.Empty;
}

/// <summary>
/// Validation contract type <c>BucketBrowseResult</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
/// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
/// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
/// </remarks>
public sealed class BucketBrowseResult
{
    /// <summary>
    /// Gets or sets <c>Success</c> for validation payload/state handling.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets <c>Error</c> for validation payload/state handling.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets <c>Tools</c> for validation payload/state handling.
    /// </summary>
    public List<ToolManifest>? Tools { get; set; }
}

/// <summary>
/// Validation contract type <c>BucketSyncResult</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
/// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
/// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
/// </remarks>
public sealed class BucketSyncResult
{
    /// <summary>
    /// Gets or sets <c>Success</c> for validation payload/state handling.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets <c>Error</c> for validation payload/state handling.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets <c>Updated</c> for validation payload/state handling.
    /// </summary>
    public int Updated { get; set; }
    /// <summary>
    /// Gets or sets <c>Added</c> for validation payload/state handling.
    /// </summary>
    public int Added { get; set; }
    /// <summary>
    /// Gets or sets <c>Unchanged</c> for validation payload/state handling.
    /// </summary>
    public int Unchanged { get; set; }
}
