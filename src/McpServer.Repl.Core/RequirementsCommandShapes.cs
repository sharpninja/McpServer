// FR-MCP-REPL-003: Command Namespace Parity - Requirements command structures
// TR-MCP-REPL-001: YAML Envelope Protocol - Requirements command envelope data models
// TR-MCP-REPL-004: Command Registry and Dispatcher - Requirements command shapes
// TEST-MCP-REPL-009: Requirements REPL commands match REST endpoint semantics

// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Requirements workflow command shapes
// FR-MCP-REPL-003: Command Namespace Parity - Requirements operation contract models
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Requirements command namespace shapes
// TEST-MCP-REPL-009: Requirements management operations validate requirement identifier rules

namespace McpServer.Repl.Core;

/// <summary>
/// Defines YAML command shapes for the <c>workflow.requirements.*</c> namespace.
/// All commands follow the REPL protocol request envelope structure with method-specific parameters.
/// </summary>
/// <remarks>
/// <para>
/// Command methods in this namespace:
/// <list type="bullet">
/// <item><c>workflow.requirements.listFr</c> — List functional requirements with filtering</item>
/// <item><c>workflow.requirements.getFr</c> — Get specific FR by ID</item>
/// <item><c>workflow.requirements.createFr</c> — Create new FR</item>
/// <item><c>workflow.requirements.updateFr</c> — Update existing FR (uses selected FR if no ID)</item>
/// <item><c>workflow.requirements.deleteFr</c> — Delete FR by ID</item>
/// <item><c>workflow.requirements.listTr</c> — List technical requirements with filtering</item>
/// <item><c>workflow.requirements.getTr</c> — Get specific TR by ID</item>
/// <item><c>workflow.requirements.createTr</c> — Create new TR</item>
/// <item><c>workflow.requirements.updateTr</c> — Update existing TR (uses selected TR if no ID)</item>
/// <item><c>workflow.requirements.deleteTr</c> — Delete TR by ID</item>
/// <item><c>workflow.requirements.listTest</c> — List test requirements with filtering</item>
/// <item><c>workflow.requirements.getTest</c> — Get specific TEST by ID</item>
/// <item><c>workflow.requirements.createTest</c> — Create new TEST</item>
/// <item><c>workflow.requirements.updateTest</c> — Update existing TEST (uses selected TEST if no ID)</item>
/// <item><c>workflow.requirements.deleteTest</c> — Delete TEST by ID</item>
/// <item><c>workflow.requirements.listMappings</c> — List requirement mappings with filtering</item>
/// <item><c>workflow.requirements.createMapping</c> — Create new requirement mapping</item>
/// <item><c>workflow.requirements.deleteMapping</c> — Delete requirement mapping</item>
/// <item><c>workflow.requirements.generateDocument</c> — Generate formatted requirement document</item>
/// <item><c>workflow.requirements.ingestDocument</c> — Ingest external requirement document</item>
/// <item><c>workflow.requirements.currentSelection</c> — Get current FR/TR/TEST selection state</item>
/// </list>
/// </para>
/// <para>
/// All request envelopes follow the structure:
/// <code>
/// type: request
/// payload:
///   requestId: &lt;unique-request-id&gt;
///   method: workflow.requirements.&lt;operation&gt;
///   params:
///     &lt;operation-specific-parameters&gt;
/// </code>
/// </para>
/// <para>
/// All successful responses follow the structure:
/// <code>
/// type: result
/// payload:
///   requestId: &lt;matching-request-id&gt;
///   result:
///     &lt;operation-specific-result&gt;
/// </code>
/// </para>
/// <para>
/// All error responses follow the structure defined in <see cref="IRequirementsError"/>.
/// </para>
/// <para><strong>Mapping CRUD Behavior:</strong></para>
/// <para>
/// Requirement mappings link FR, TR, and TEST items to establish traceability. The <c>createMapping</c>
/// operation validates that all referenced requirement IDs exist before creating the mapping.
/// The <c>deleteMapping</c> operation removes a mapping by specifying the exact FR, TR, and/or TEST
/// combination. The <c>listMappings</c> operation returns all mappings, optionally filtered by
/// FR ID, TR ID, or TEST ID using AND logic. Mappings are stored redundantly across requirement
/// documents to enable fast lookups. When a requirement is deleted, all mappings referencing it
/// are automatically removed to maintain referential integrity.
/// </para>
/// <para><strong>Generated Document Response Formats:</strong></para>
/// <para>
/// The <c>generateDocument</c> operation produces formatted requirement documents in Markdown or YAML:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>Markdown Format</term>
/// <description>
/// Produces human-readable Markdown with headings, tables, and status indicators. Suitable for
/// documentation sites, GitHub wikis, and stakeholder reviews. Includes requirement metadata,
/// descriptions, mapping cross-references, and formatted tables for easy navigation.
/// </description>
/// </item>
/// <item>
/// <term>YAML Format</term>
/// <description>
/// Produces structured YAML suitable for machine processing, version control, and automated tooling.
/// Includes all requirement metadata, mappings, and validation rules in a format optimized for
/// parsing and diff-friendly storage.
/// </description>
/// </item>
/// </list>
/// <para>
/// The <c>ingestDocument</c> operation parses external documents and synchronizes them with the workspace:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>overwrite</term>
/// <description>Replaces existing requirements with ingested data. Use when the external document is authoritative.</description>
/// </item>
/// <item>
/// <term>merge</term>
/// <description>Combines ingested data with existing requirements, preserving local changes where possible.</description>
/// </item>
/// <item>
/// <term>skip</term>
/// <description>Ignores conflicts and only creates new requirements. Existing requirements are not modified.</description>
/// </item>
/// </list>
/// </remarks>
public static class RequirementsCommandShapes
{
    /// <summary>
    /// The namespace prefix for all requirements workflow commands.
    /// </summary>
    public const string MethodNamespace = "workflow.requirements";

    /// <summary>
    /// Command method for listing functional requirements.
    /// Method: <c>workflow.requirements.listFr</c>
    /// </summary>
    public const string ListFrMethod = "workflow.requirements.listFr";

    /// <summary>
    /// Command method for getting a specific functional requirement.
    /// Method: <c>workflow.requirements.getFr</c>
    /// </summary>
    public const string GetFrMethod = "workflow.requirements.getFr";

    /// <summary>
    /// Command method for creating a new functional requirement.
    /// Method: <c>workflow.requirements.createFr</c>
    /// </summary>
    public const string CreateFrMethod = "workflow.requirements.createFr";

    /// <summary>
    /// Command method for updating an existing functional requirement.
    /// Method: <c>workflow.requirements.updateFr</c>
    /// </summary>
    public const string UpdateFrMethod = "workflow.requirements.updateFr";

    /// <summary>
    /// Command method for deleting a functional requirement.
    /// Method: <c>workflow.requirements.deleteFr</c>
    /// </summary>
    public const string DeleteFrMethod = "workflow.requirements.deleteFr";

    /// <summary>
    /// Command method for listing technical requirements.
    /// Method: <c>workflow.requirements.listTr</c>
    /// </summary>
    public const string ListTrMethod = "workflow.requirements.listTr";

    /// <summary>
    /// Command method for getting a specific technical requirement.
    /// Method: <c>workflow.requirements.getTr</c>
    /// </summary>
    public const string GetTrMethod = "workflow.requirements.getTr";

    /// <summary>
    /// Command method for creating a new technical requirement.
    /// Method: <c>workflow.requirements.createTr</c>
    /// </summary>
    public const string CreateTrMethod = "workflow.requirements.createTr";

    /// <summary>
    /// Command method for updating an existing technical requirement.
    /// Method: <c>workflow.requirements.updateTr</c>
    /// </summary>
    public const string UpdateTrMethod = "workflow.requirements.updateTr";

    /// <summary>
    /// Command method for deleting a technical requirement.
    /// Method: <c>workflow.requirements.deleteTr</c>
    /// </summary>
    public const string DeleteTrMethod = "workflow.requirements.deleteTr";

    /// <summary>
    /// Command method for listing test requirements.
    /// Method: <c>workflow.requirements.listTest</c>
    /// </summary>
    public const string ListTestMethod = "workflow.requirements.listTest";

    /// <summary>
    /// Command method for getting a specific test requirement.
    /// Method: <c>workflow.requirements.getTest</c>
    /// </summary>
    public const string GetTestMethod = "workflow.requirements.getTest";

    /// <summary>
    /// Command method for creating a new test requirement.
    /// Method: <c>workflow.requirements.createTest</c>
    /// </summary>
    public const string CreateTestMethod = "workflow.requirements.createTest";

    /// <summary>
    /// Command method for updating an existing test requirement.
    /// Method: <c>workflow.requirements.updateTest</c>
    /// </summary>
    public const string UpdateTestMethod = "workflow.requirements.updateTest";

    /// <summary>
    /// Command method for deleting a test requirement.
    /// Method: <c>workflow.requirements.deleteTest</c>
    /// </summary>
    public const string DeleteTestMethod = "workflow.requirements.deleteTest";

    /// <summary>
    /// Command method for listing requirement mappings.
    /// Method: <c>workflow.requirements.listMappings</c>
    /// </summary>
    public const string ListMappingsMethod = "workflow.requirements.listMappings";

    /// <summary>
    /// Command method for creating a new requirement mapping.
    /// Method: <c>workflow.requirements.createMapping</c>
    /// </summary>
    public const string CreateMappingMethod = "workflow.requirements.createMapping";

    /// <summary>
    /// Command method for deleting a requirement mapping.
    /// Method: <c>workflow.requirements.deleteMapping</c>
    /// </summary>
    public const string DeleteMappingMethod = "workflow.requirements.deleteMapping";

    /// <summary>
    /// Command method for generating a formatted requirement document.
    /// Method: <c>workflow.requirements.generateDocument</c>
    /// </summary>
    public const string GenerateDocumentMethod = "workflow.requirements.generateDocument";

    /// <summary>
    /// Command method for ingesting an external requirement document.
    /// Method: <c>workflow.requirements.ingestDocument</c>
    /// </summary>
    public const string IngestDocumentMethod = "workflow.requirements.ingestDocument";

    /// <summary>
    /// Command method for getting current FR/TR/TEST selection state.
    /// Method: <c>workflow.requirements.currentSelection</c>
    /// </summary>
    public const string CurrentSelectionMethod = "workflow.requirements.currentSelection";
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.listFr</c> command.
/// All fields are optional filters; omitted fields return all matching FRs.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-listfr-001
///   method: workflow.requirements.listFr
///   params:
///     area: MCP
///     status: in_progress
/// </code>
/// </remarks>
public interface IListFrParams
{
    /// <summary>
    /// Gets the optional area filter (e.g., "MCP", "AUTH", "API").
    /// </summary>
    string? Area { get; }

    /// <summary>
    /// Gets the optional status filter.
    /// Valid values: "pending", "in_progress", "completed", "deferred".
    /// </summary>
    string? Status { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.listFr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-listfr-001
///   result:
///     items:
///       - id: FR-MCP-001
///         title: Agent authentication
///         description: System must authenticate AI agents via API key
///         status: completed
///         priority: critical
///         area: MCP
///         notes: null
///         createdAt: 2026-03-01T10:00:00Z
///         updatedAt: 2026-03-04T11:30:00Z
///       - id: FR-MCP-002
///         title: Workspace isolation
///         description: Each workspace must be isolated from others
///         status: in_progress
///         priority: high
///         area: MCP
///         notes: null
///         createdAt: 2026-03-01T10:30:00Z
///         updatedAt: 2026-03-04T11:45:00Z
///     totalCount: 2
/// </code>
/// </remarks>
public interface IListFrResult
{
    /// <summary>
    /// Gets the FR items matching the query.
    /// </summary>
    IReadOnlyList<IFrItem> Items { get; }

    /// <summary>
    /// Gets the total number of FRs matching the filter.
    /// </summary>
    int TotalCount { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.getFr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-getfr-001
///   method: workflow.requirements.getFr
///   params:
///     id: FR-MCP-001
/// </code>
/// </remarks>
public interface IGetFrParams
{
    /// <summary>
    /// Gets the FR identifier to retrieve.
    /// Must match canonical format: <c>^FR-[A-Z]+-\d{3}$</c>
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.getFr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-getfr-001
///   result:
///     item:
///       id: FR-MCP-001
///       title: Agent authentication
///       description: System must authenticate AI agents via API key
///       status: completed
///       priority: critical
///       area: MCP
///       notes: Implemented with rotating API keys
///       createdAt: 2026-03-01T10:00:00Z
///       updatedAt: 2026-03-04T11:30:00Z
/// </code>
/// </remarks>
public interface IGetFrResult
{
    /// <summary>
    /// Gets the requested FR item.
    /// </summary>
    IFrItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.createFr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-createfr-001
///   method: workflow.requirements.createFr
///   params:
///     id: FR-MCP-003
///     title: Context search
///     description: System must support semantic search across workspace documents
///     priority: high
///     area: MCP
///     notes: Use hybrid search with BM25 and embeddings
/// </code>
/// </remarks>
public interface ICreateFrParams
{
    /// <summary>
    /// Gets the FR identifier. Must match <c>^FR-[A-Z]+-\d{3}$</c>.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the brief title. Required.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the detailed description. Required.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the priority level. Required.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string Priority { get; }

    /// <summary>
    /// Gets the area/category. Required.
    /// </summary>
    string Area { get; }

    /// <summary>
    /// Gets additional notes or context.
    /// </summary>
    string? Notes { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.createFr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-createfr-001
///   result:
///     success: true
///     item:
///       id: FR-MCP-003
///       title: Context search
///       description: System must support semantic search across workspace documents
///       status: pending
///       priority: high
///       area: MCP
///       notes: Use hybrid search with BM25 and embeddings
///       createdAt: 2026-03-04T11:50:00Z
///       updatedAt: 2026-03-04T11:50:00Z
/// </code>
/// </remarks>
public interface ICreateFrResult
{
    /// <summary>
    /// Gets whether the creation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the created FR item.
    /// </summary>
    IFrItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.updateFr</c> command.
/// All fields are optional; only provided fields are updated.
/// If no id is provided, uses the currently selected FR from <see cref="IRequirementsSelectionState"/>.
/// </summary>
/// <remarks>
/// Example YAML with explicit ID:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-updatefr-001
///   method: workflow.requirements.updateFr
///   params:
///     id: FR-MCP-001
///     status: completed
///     notes: Fully implemented and tested
/// </code>
/// Example YAML using selected FR:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-updatefr-002
///   method: workflow.requirements.updateFr
///   params:
///     status: in_progress
/// </code>
/// </remarks>
public interface IUpdateFrParams
{
    /// <summary>
    /// Gets the FR identifier to update.
    /// If null, uses the currently selected FR from <see cref="IRequirementsSelectionState"/>.
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Gets the updated title. Null preserves existing value.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the updated description. Null preserves existing value.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the updated status. Null preserves existing value.
    /// Valid values: "pending", "in_progress", "completed", "deferred".
    /// </summary>
    string? Status { get; }

    /// <summary>
    /// Gets the updated priority. Null preserves existing value.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string? Priority { get; }

    /// <summary>
    /// Gets the updated notes. Null preserves existing value.
    /// </summary>
    string? Notes { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.updateFr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-updatefr-001
///   result:
///     success: true
///     item:
///       id: FR-MCP-001
///       title: Agent authentication
///       description: System must authenticate AI agents via API key
///       status: completed
///       priority: critical
///       area: MCP
///       notes: Fully implemented and tested
///       createdAt: 2026-03-01T10:00:00Z
///       updatedAt: 2026-03-04T12:00:00Z
/// </code>
/// </remarks>
public interface IUpdateFrResult
{
    /// <summary>
    /// Gets whether the update succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the updated FR item.
    /// </summary>
    IFrItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.deleteFr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-deletefr-001
///   method: workflow.requirements.deleteFr
///   params:
///     id: FR-MCP-003
/// </code>
/// </remarks>
public interface IDeleteFrParams
{
    /// <summary>
    /// Gets the FR identifier to delete.
    /// Must match canonical format: <c>^FR-[A-Z]+-\d{3}$</c>
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.deleteFr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-deletefr-001
///   result:
///     deleted: true
///     id: FR-MCP-003
/// </code>
/// </remarks>
public interface IDeleteFrResult
{
    /// <summary>
    /// Gets whether the deletion succeeded.
    /// </summary>
    bool Deleted { get; }

    /// <summary>
    /// Gets the identifier of the deleted FR.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.listTr</c> command.
/// All fields are optional filters; omitted fields return all matching TRs.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-listtr-001
///   method: workflow.requirements.listTr
///   params:
///     area: MCP
///     subarea: ARCH
///     status: completed
/// </code>
/// </remarks>
public interface IListTrParams
{
    /// <summary>
    /// Gets the optional area filter (e.g., "MCP", "AUTH", "API").
    /// </summary>
    string? Area { get; }

    /// <summary>
    /// Gets the optional subarea filter (e.g., "ARCH", "PERF", "SEC").
    /// </summary>
    string? Subarea { get; }

    /// <summary>
    /// Gets the optional status filter.
    /// Valid values: "pending", "in_progress", "completed", "deferred".
    /// </summary>
    string? Status { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.listTr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-listtr-001
///   result:
///     items:
///       - id: TR-MCP-ARCH-001
///         title: Plugin architecture
///         description: Use plugin-based architecture for extensibility
///         status: completed
///         priority: high
///         area: MCP
///         subarea: ARCH
///         notes: Implemented with MEF
///         createdAt: 2026-03-01T10:00:00Z
///         updatedAt: 2026-03-04T11:30:00Z
///     totalCount: 1
/// </code>
/// </remarks>
public interface IListTrResult
{
    /// <summary>
    /// Gets the TR items matching the query.
    /// </summary>
    IReadOnlyList<ITrItem> Items { get; }

    /// <summary>
    /// Gets the total number of TRs matching the filter.
    /// </summary>
    int TotalCount { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.getTr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-gettr-001
///   method: workflow.requirements.getTr
///   params:
///     id: TR-MCP-ARCH-001
/// </code>
/// </remarks>
public interface IGetTrParams
{
    /// <summary>
    /// Gets the TR identifier to retrieve.
    /// Must match canonical format: <c>^TR-[A-Z]+-[A-Z]+-\d{3}$</c>
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.getTr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-gettr-001
///   result:
///     item:
///       id: TR-MCP-ARCH-001
///       title: Plugin architecture
///       description: Use plugin-based architecture for extensibility
///       status: completed
///       priority: high
///       area: MCP
///       subarea: ARCH
///       notes: Implemented with MEF
///       createdAt: 2026-03-01T10:00:00Z
///       updatedAt: 2026-03-04T11:30:00Z
/// </code>
/// </remarks>
public interface IGetTrResult
{
    /// <summary>
    /// Gets the requested TR item.
    /// </summary>
    ITrItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.createTr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-createtr-001
///   method: workflow.requirements.createTr
///   params:
///     id: TR-MCP-PERF-001
///     title: Response time SLA
///     description: All API endpoints must respond within 500ms p99
///     priority: high
///     area: MCP
///     subarea: PERF
///     notes: Measure at gateway, exclude network time
/// </code>
/// </remarks>
public interface ICreateTrParams
{
    /// <summary>
    /// Gets the TR identifier. Must match <c>^TR-[A-Z]+-[A-Z]+-\d{3}$</c>.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the brief title. Required.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the detailed description. Required.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the priority level. Required.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string Priority { get; }

    /// <summary>
    /// Gets the area/category. Required.
    /// </summary>
    string Area { get; }

    /// <summary>
    /// Gets the subarea/subcategory. Required.
    /// </summary>
    string Subarea { get; }

    /// <summary>
    /// Gets additional notes or context.
    /// </summary>
    string? Notes { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.createTr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-createtr-001
///   result:
///     success: true
///     item:
///       id: TR-MCP-PERF-001
///       title: Response time SLA
///       description: All API endpoints must respond within 500ms p99
///       status: pending
///       priority: high
///       area: MCP
///       subarea: PERF
///       notes: Measure at gateway, exclude network time
///       createdAt: 2026-03-04T11:50:00Z
///       updatedAt: 2026-03-04T11:50:00Z
/// </code>
/// </remarks>
public interface ICreateTrResult
{
    /// <summary>
    /// Gets whether the creation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the created TR item.
    /// </summary>
    ITrItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.updateTr</c> command.
/// All fields are optional; only provided fields are updated.
/// If no id is provided, uses the currently selected TR from <see cref="IRequirementsSelectionState"/>.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-updatetr-001
///   method: workflow.requirements.updateTr
///   params:
///     id: TR-MCP-PERF-001
///     status: in_progress
///     notes: Added performance monitoring
/// </code>
/// </remarks>
public interface IUpdateTrParams
{
    /// <summary>
    /// Gets the TR identifier to update.
    /// If null, uses the currently selected TR from <see cref="IRequirementsSelectionState"/>.
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Gets the updated title. Null preserves existing value.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the updated description. Null preserves existing value.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the updated status. Null preserves existing value.
    /// Valid values: "pending", "in_progress", "completed", "deferred".
    /// </summary>
    string? Status { get; }

    /// <summary>
    /// Gets the updated priority. Null preserves existing value.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string? Priority { get; }

    /// <summary>
    /// Gets the updated notes. Null preserves existing value.
    /// </summary>
    string? Notes { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.updateTr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-updatetr-001
///   result:
///     success: true
///     item:
///       id: TR-MCP-PERF-001
///       title: Response time SLA
///       description: All API endpoints must respond within 500ms p99
///       status: in_progress
///       priority: high
///       area: MCP
///       subarea: PERF
///       notes: Added performance monitoring
///       createdAt: 2026-03-04T11:50:00Z
///       updatedAt: 2026-03-04T12:00:00Z
/// </code>
/// </remarks>
public interface IUpdateTrResult
{
    /// <summary>
    /// Gets whether the update succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the updated TR item.
    /// </summary>
    ITrItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.deleteTr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-deletetr-001
///   method: workflow.requirements.deleteTr
///   params:
///     id: TR-MCP-PERF-001
/// </code>
/// </remarks>
public interface IDeleteTrParams
{
    /// <summary>
    /// Gets the TR identifier to delete.
    /// Must match canonical format: <c>^TR-[A-Z]+-[A-Z]+-\d{3}$</c>
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.deleteTr</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-deletetr-001
///   result:
///     deleted: true
///     id: TR-MCP-PERF-001
/// </code>
/// </remarks>
public interface IDeleteTrResult
{
    /// <summary>
    /// Gets whether the deletion succeeded.
    /// </summary>
    bool Deleted { get; }

    /// <summary>
    /// Gets the identifier of the deleted TR.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.listTest</c> command.
/// All fields are optional filters; omitted fields return all matching TESTs.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-listtest-001
///   method: workflow.requirements.listTest
///   params:
///     area: MCP
///     status: pending
/// </code>
/// </remarks>
public interface IListTestParams
{
    /// <summary>
    /// Gets the optional area filter (e.g., "MCP", "AUTH", "API").
    /// </summary>
    string? Area { get; }

    /// <summary>
    /// Gets the optional status filter.
    /// Valid values: "pending", "in_progress", "completed", "deferred".
    /// </summary>
    string? Status { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.listTest</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-listtest-001
///   result:
///     items:
///       - id: TEST-MCP-001
///         title: Authentication integration test
///         description: Verify end-to-end authentication flow
///         status: pending
///         priority: high
///         area: MCP
///         testType: integration
///         notes: Requires test environment
///         createdAt: 2026-03-01T10:00:00Z
///         updatedAt: 2026-03-04T11:30:00Z
///     totalCount: 1
/// </code>
/// </remarks>
public interface IListTestResult
{
    /// <summary>
    /// Gets the TEST items matching the query.
    /// </summary>
    IReadOnlyList<ITestItem> Items { get; }

    /// <summary>
    /// Gets the total number of TESTs matching the filter.
    /// </summary>
    int TotalCount { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.getTest</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-gettest-001
///   method: workflow.requirements.getTest
///   params:
///     id: TEST-MCP-001
/// </code>
/// </remarks>
public interface IGetTestParams
{
    /// <summary>
    /// Gets the TEST identifier to retrieve.
    /// Must match canonical format: <c>^TEST-[A-Z]+-\d{3}$</c>
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.getTest</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-gettest-001
///   result:
///     item:
///       id: TEST-MCP-001
///       title: Authentication integration test
///       description: Verify end-to-end authentication flow
///       status: pending
///       priority: high
///       area: MCP
///       testType: integration
///       notes: Requires test environment
///       createdAt: 2026-03-01T10:00:00Z
///       updatedAt: 2026-03-04T11:30:00Z
/// </code>
/// </remarks>
public interface IGetTestResult
{
    /// <summary>
    /// Gets the requested TEST item.
    /// </summary>
    ITestItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.createTest</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-createtest-001
///   method: workflow.requirements.createTest
///   params:
///     id: TEST-MCP-002
///     title: Performance benchmark
///     description: Verify response time meets SLA
///     priority: high
///     area: MCP
///     testType: performance
///     notes: Run with production-like load
/// </code>
/// </remarks>
public interface ICreateTestParams
{
    /// <summary>
    /// Gets the TEST identifier. Must match <c>^TEST-[A-Z]+-\d{3}$</c>.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the brief title. Required.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the detailed description. Required.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the priority level. Required.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string Priority { get; }

    /// <summary>
    /// Gets the area/category. Required.
    /// </summary>
    string Area { get; }

    /// <summary>
    /// Gets the test type. Required.
    /// Valid values: "unit", "integration", "e2e", "performance".
    /// </summary>
    string TestType { get; }

    /// <summary>
    /// Gets additional notes or context.
    /// </summary>
    string? Notes { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.createTest</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-createtest-001
///   result:
///     success: true
///     item:
///       id: TEST-MCP-002
///       title: Performance benchmark
///       description: Verify response time meets SLA
///       status: pending
///       priority: high
///       area: MCP
///       testType: performance
///       notes: Run with production-like load
///       createdAt: 2026-03-04T11:50:00Z
///       updatedAt: 2026-03-04T11:50:00Z
/// </code>
/// </remarks>
public interface ICreateTestResult
{
    /// <summary>
    /// Gets whether the creation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the created TEST item.
    /// </summary>
    ITestItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.updateTest</c> command.
/// All fields are optional; only provided fields are updated.
/// If no id is provided, uses the currently selected TEST from <see cref="IRequirementsSelectionState"/>.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-updatetest-001
///   method: workflow.requirements.updateTest
///   params:
///     id: TEST-MCP-002
///     status: in_progress
///     notes: Test harness implemented
/// </code>
/// </remarks>
public interface IUpdateTestParams
{
    /// <summary>
    /// Gets the TEST identifier to update.
    /// If null, uses the currently selected TEST from <see cref="IRequirementsSelectionState"/>.
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Gets the updated title. Null preserves existing value.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the updated description. Null preserves existing value.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the updated status. Null preserves existing value.
    /// Valid values: "pending", "in_progress", "completed", "deferred".
    /// </summary>
    string? Status { get; }

    /// <summary>
    /// Gets the updated priority. Null preserves existing value.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string? Priority { get; }

    /// <summary>
    /// Gets the updated notes. Null preserves existing value.
    /// </summary>
    string? Notes { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.updateTest</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-updatetest-001
///   result:
///     success: true
///     item:
///       id: TEST-MCP-002
///       title: Performance benchmark
///       description: Verify response time meets SLA
///       status: in_progress
///       priority: high
///       area: MCP
///       testType: performance
///       notes: Test harness implemented
///       createdAt: 2026-03-04T11:50:00Z
///       updatedAt: 2026-03-04T12:00:00Z
/// </code>
/// </remarks>
public interface IUpdateTestResult
{
    /// <summary>
    /// Gets whether the update succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the updated TEST item.
    /// </summary>
    ITestItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.deleteTest</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-deletetest-001
///   method: workflow.requirements.deleteTest
///   params:
///     id: TEST-MCP-002
/// </code>
/// </remarks>
public interface IDeleteTestParams
{
    /// <summary>
    /// Gets the TEST identifier to delete.
    /// Must match canonical format: <c>^TEST-[A-Z]+-\d{3}$</c>
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.deleteTest</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-deletetest-001
///   result:
///     deleted: true
///     id: TEST-MCP-002
/// </code>
/// </remarks>
public interface IDeleteTestResult
{
    /// <summary>
    /// Gets whether the deletion succeeded.
    /// </summary>
    bool Deleted { get; }

    /// <summary>
    /// Gets the identifier of the deleted TEST.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.listMappings</c> command.
/// All fields are optional filters; omitted fields return all mappings.
/// If multiple filters are provided, only mappings matching ALL filters are returned (AND logic).
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-listmap-001
///   method: workflow.requirements.listMappings
///   params:
///     frId: FR-MCP-001
/// </code>
/// </remarks>
public interface IListMappingsParams
{
    /// <summary>
    /// Gets the optional FR ID filter. If provided, returns only mappings referencing this FR.
    /// </summary>
    string? FrId { get; }

    /// <summary>
    /// Gets the optional TR ID filter. If provided, returns only mappings referencing this TR.
    /// </summary>
    string? TrId { get; }

    /// <summary>
    /// Gets the optional TEST ID filter. If provided, returns only mappings referencing this TEST.
    /// </summary>
    string? TestId { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.listMappings</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-listmap-001
///   result:
///     items:
///       - frId: FR-MCP-001
///         trId: TR-MCP-ARCH-001
///         testId: TEST-MCP-001
///         createdAt: 2026-03-01T10:00:00Z
///         notes: Core authentication flow
///       - frId: FR-MCP-001
///         trId: TR-MCP-SEC-001
///         testId: null
///         createdAt: 2026-03-01T10:30:00Z
///         notes: null
///     totalCount: 2
/// </code>
/// </remarks>
public interface IListMappingsResult
{
    /// <summary>
    /// Gets the mapping items matching the query.
    /// </summary>
    IReadOnlyList<IMappingItem> Items { get; }

    /// <summary>
    /// Gets the total number of mappings matching the filter.
    /// </summary>
    int TotalCount { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.createMapping</c> command.
/// At least one requirement ID must be provided; all provided IDs must reference existing requirements.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-createmap-001
///   method: workflow.requirements.createMapping
///   params:
///     frId: FR-MCP-002
///     trId: TR-MCP-ARCH-002
///     testId: TEST-MCP-003
///     notes: Workspace isolation complete traceability
/// </code>
/// </remarks>
public interface ICreateMappingParams
{
    /// <summary>
    /// Gets the functional requirement ID to link, or null.
    /// </summary>
    string? FrId { get; }

    /// <summary>
    /// Gets the technical requirement ID to link, or null.
    /// </summary>
    string? TrId { get; }

    /// <summary>
    /// Gets the test requirement ID to link, or null.
    /// </summary>
    string? TestId { get; }

    /// <summary>
    /// Gets optional notes about this mapping.
    /// </summary>
    string? Notes { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.createMapping</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-createmap-001
///   result:
///     success: true
///     item:
///       frId: FR-MCP-002
///       trId: TR-MCP-ARCH-002
///       testId: TEST-MCP-003
///       createdAt: 2026-03-04T11:50:00Z
///       notes: Workspace isolation complete traceability
/// </code>
/// </remarks>
public interface ICreateMappingResult
{
    /// <summary>
    /// Gets whether the creation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the created mapping item.
    /// </summary>
    IMappingItem Item { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.deleteMapping</c> command.
/// At least one requirement ID must be provided to identify the mapping.
/// The combination of provided IDs must exactly match an existing mapping.
/// If multiple filters are provided, only the mapping matching ALL filters is deleted (AND logic).
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-deletemap-001
///   method: workflow.requirements.deleteMapping
///   params:
///     frId: FR-MCP-002
///     trId: TR-MCP-ARCH-002
///     testId: TEST-MCP-003
/// </code>
/// </remarks>
public interface IDeleteMappingParams
{
    /// <summary>
    /// Gets the optional FR ID. If provided, the mapping must reference this FR.
    /// </summary>
    string? FrId { get; }

    /// <summary>
    /// Gets the optional TR ID. If provided, the mapping must reference this TR.
    /// </summary>
    string? TrId { get; }

    /// <summary>
    /// Gets the optional TEST ID. If provided, the mapping must reference this TEST.
    /// </summary>
    string? TestId { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.deleteMapping</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-deletemap-001
///   result:
///     deleted: true
///     frId: FR-MCP-002
///     trId: TR-MCP-ARCH-002
///     testId: TEST-MCP-003
/// </code>
/// </remarks>
public interface IDeleteMappingResult
{
    /// <summary>
    /// Gets whether the deletion succeeded.
    /// </summary>
    bool Deleted { get; }

    /// <summary>
    /// Gets the FR ID of the deleted mapping, or null.
    /// </summary>
    string? FrId { get; }

    /// <summary>
    /// Gets the TR ID of the deleted mapping, or null.
    /// </summary>
    string? TrId { get; }

    /// <summary>
    /// Gets the TEST ID of the deleted mapping, or null.
    /// </summary>
    string? TestId { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.generateDocument</c> command.
/// </summary>
/// <remarks>
/// Example YAML for Markdown matrix:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-gendoc-001
///   method: workflow.requirements.generateDocument
///   params:
///     format: markdown
///     docType: matrix
/// </code>
/// Example YAML for YAML FR document:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-gendoc-002
///   method: workflow.requirements.generateDocument
///   params:
///     format: yaml
///     docType: fr
/// </code>
/// </remarks>
public interface IGenerateDocumentParams
{
    /// <summary>
    /// Gets the output format.
    /// Valid values: "markdown", "yaml".
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets the document type.
    /// Valid values: "fr" (functional requirements), "tr" (technical requirements),
    /// "test" (test requirements), "matrix" (requirement traceability matrix), "all" (complete document).
    /// </summary>
    string DocType { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.generateDocument</c> command.
/// </summary>
/// <remarks>
/// Example YAML for Markdown output:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-gendoc-001
///   result:
///     success: true
///     content: |
///       # Requirement Traceability Matrix
///       
///       | FR ID | TR ID | TEST ID | Status |
///       |-------|-------|---------|--------|
///       | FR-MCP-001 | TR-MCP-ARCH-001 | TEST-MCP-001 | ✓ |
///       | FR-MCP-002 | TR-MCP-ARCH-002 | TEST-MCP-003 | ○ |
///     format: markdown
///     docType: matrix
///     generatedAt: 2026-03-04T11:50:00Z
/// </code>
/// Example YAML for YAML output:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-gendoc-002
///   result:
///     success: true
///     content: |
///       requirements:
///         - id: FR-MCP-001
///           title: Agent authentication
///           description: System must authenticate AI agents via API key
///           status: completed
///           priority: critical
///           area: MCP
///     format: yaml
///     docType: fr
///     generatedAt: 2026-03-04T11:50:00Z
/// </code>
/// </remarks>
public interface IGenerateDocumentResult
{
    /// <summary>
    /// Gets whether the generation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the generated document content.
    /// Format depends on the requested format (Markdown or YAML).
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Gets the document format that was generated.
    /// Valid values: "markdown", "yaml".
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets the document type that was generated.
    /// Valid values: "fr", "tr", "test", "matrix", "all".
    /// </summary>
    string DocType { get; }

    /// <summary>
    /// Gets the generation timestamp (ISO 8601).
    /// </summary>
    DateTimeOffset GeneratedAt { get; }
}

/// <summary>
/// Represents the parameters for the <c>workflow.requirements.ingestDocument</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-ingest-001
///   method: workflow.requirements.ingestDocument
///   params:
///     content: |
///       # Functional Requirements
///       ## FR-MCP-004: Context caching
///       System must cache frequently accessed context to reduce latency
///       Priority: high
///       Status: pending
///     format: markdown
///     mergeStrategy: merge
/// </code>
/// </remarks>
public interface IIngestDocumentParams
{
    /// <summary>
    /// Gets the document content to ingest (Markdown or YAML format).
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Gets the document format.
    /// Valid values: "markdown", "yaml".
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets the conflict resolution strategy.
    /// Valid values: "overwrite" (replace existing), "merge" (combine with existing), "skip" (ignore conflicts).
    /// </summary>
    string MergeStrategy { get; }
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.ingestDocument</c> command.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-ingest-001
///   result:
///     success: true
///     frCreated: 1
///     frUpdated: 0
///     trCreated: 0
///     trUpdated: 0
///     testCreated: 0
///     testUpdated: 0
///     mappingsCreated: 0
///     conflicts:
///       - requirementId: FR-MCP-001
///         conflictType: data_mismatch
///         description: Existing priority is critical, ingested priority is high
///         resolution: merged
///     ingestedAt: 2026-03-04T11:50:00Z
/// </code>
/// </remarks>
public interface IIngestDocumentResult
{
    /// <summary>
    /// Gets whether the ingestion succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the number of FR items created.
    /// </summary>
    int FrCreated { get; }

    /// <summary>
    /// Gets the number of FR items updated.
    /// </summary>
    int FrUpdated { get; }

    /// <summary>
    /// Gets the number of TR items created.
    /// </summary>
    int TrCreated { get; }

    /// <summary>
    /// Gets the number of TR items updated.
    /// </summary>
    int TrUpdated { get; }

    /// <summary>
    /// Gets the number of TEST items created.
    /// </summary>
    int TestCreated { get; }

    /// <summary>
    /// Gets the number of TEST items updated.
    /// </summary>
    int TestUpdated { get; }

    /// <summary>
    /// Gets the number of mapping items created.
    /// </summary>
    int MappingsCreated { get; }

    /// <summary>
    /// Gets the list of conflicts detected during ingestion.
    /// Each conflict includes the requirement ID, conflict type, and resolution action taken.
    /// </summary>
    IReadOnlyList<IIngestionConflict> Conflicts { get; }

    /// <summary>
    /// Gets the ingestion timestamp (ISO 8601).
    /// </summary>
    DateTimeOffset IngestedAt { get; }
}



/// <summary>
/// Represents the parameters for the <c>workflow.requirements.currentSelection</c> command.
/// This command takes no parameters.
/// </summary>
/// <remarks>
/// Example YAML:
/// <code>
/// type: request
/// payload:
///   requestId: req-20260304T113901Z-currsel-001
///   method: workflow.requirements.currentSelection
///   params: {}
/// </code>
/// </remarks>
public interface IRequirementsCurrentSelectionParams
{
}

/// <summary>
/// Represents the result for the <c>workflow.requirements.currentSelection</c> command.
/// Returns null/empty if no requirements are selected.
/// </summary>
/// <remarks>
/// Example YAML when requirements are selected:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-currsel-001
///   result:
///     frId: FR-MCP-001
///     trId: TR-MCP-ARCH-001
///     testId: TEST-MCP-001
///     selectedAt: 2026-03-04T11:45:23Z
/// </code>
/// Example YAML when no requirements are selected:
/// <code>
/// type: result
/// payload:
///   requestId: req-20260304T113901Z-currsel-001
///   result: null
/// </code>
/// </remarks>
public interface IRequirementsCurrentSelectionResult
{
    /// <summary>
    /// Gets the selected FR identifier, or null if no FR is selected.
    /// </summary>
    string? FrId { get; }

    /// <summary>
    /// Gets the selected TR identifier, or null if no TR is selected.
    /// </summary>
    string? TrId { get; }

    /// <summary>
    /// Gets the selected TEST identifier, or null if no TEST is selected.
    /// </summary>
    string? TestId { get; }

    /// <summary>
    /// Gets the timestamp when selection occurred, or null if no requirements are selected.
    /// </summary>
    DateTimeOffset? SelectedAt { get; }
}

/// <summary>
/// Defines structured error envelopes for requirements workflow operations.
/// All errors follow the REPL protocol error envelope structure with standardized codes.
/// </summary>
/// <remarks>
/// <para>
/// Error envelope structure:
/// <code>
/// type: error
/// payload:
///   requestId: &lt;matching-request-id&gt;
///   code: &lt;error-code&gt;
///   message: &lt;human-readable-message&gt;
///   details:
///     &lt;optional-context-specific-details&gt;
/// </code>
/// </para>
/// <para>
/// Standard error codes for requirements operations:
/// <list type="bullet">
/// <item><c>requirement_not_found</c> — Requirement with specified ID does not exist</item>
/// <item><c>requirement_already_exists</c> — Requirement with same ID already exists</item>
/// <item><c>invalid_requirement_id</c> — Requirement ID violates canonical identifier rules</item>
/// <item><c>invalid_parameter</c> — Required parameter missing or invalid</item>
/// <item><c>no_selection</c> — No requirement is currently selected</item>
/// <item><c>mapping_not_found</c> — Mapping with specified combination does not exist</item>
/// <item><c>mapping_already_exists</c> — Mapping with same combination already exists</item>
/// <item><c>invalid_mapping</c> — Mapping references non-existent requirements</item>
/// <item><c>document_generation_error</c> — Document generation failed</item>
/// <item><c>document_ingestion_error</c> — Document ingestion failed</item>
/// <item><c>storage_error</c> — Underlying storage operation failed</item>
/// <item><c>internal_error</c> — Unexpected internal error</item>
/// </list>
/// </para>
/// </remarks>
public interface IRequirementsError
{
    /// <summary>
    /// Gets the request ID that this error corresponds to.
    /// Must match the request ID from the failed command.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the error code indicating the failure category.
    /// See remarks for standard error codes.
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets optional additional error details or context.
    /// Structure depends on the error code and operation.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Details { get; }
}

/// <summary>
/// Provides standard error code constants for requirements operations.
/// </summary>
public static class RequirementsErrorCodes
{
    /// <summary>
    /// Requirement with specified ID does not exist.
    /// </summary>
    public const string RequirementNotFound = "requirement_not_found";

    /// <summary>
    /// Requirement with same ID already exists when attempting to create.
    /// </summary>
    public const string RequirementAlreadyExists = "requirement_already_exists";

    /// <summary>
    /// Requirement ID does not conform to canonical identifier rules.
    /// FR format: <c>^FR-[A-Z]+-\d{3}$</c>
    /// TR format: <c>^TR-[A-Z]+-[A-Z]+-\d{3}$</c>
    /// TEST format: <c>^TEST-[A-Z]+-\d{3}$</c>
    /// </summary>
    public const string InvalidRequirementId = "invalid_requirement_id";

    /// <summary>
    /// Required parameter is missing, empty, or contains invalid data.
    /// </summary>
    public const string InvalidParameter = "invalid_parameter";

    /// <summary>
    /// No requirement is currently selected when attempting an operation that requires selection.
    /// </summary>
    public const string NoSelection = "no_selection";

    /// <summary>
    /// Mapping with specified FR/TR/TEST combination does not exist.
    /// </summary>
    public const string MappingNotFound = "mapping_not_found";

    /// <summary>
    /// Mapping with same FR/TR/TEST combination already exists.
    /// </summary>
    public const string MappingAlreadyExists = "mapping_already_exists";

    /// <summary>
    /// Mapping references one or more non-existent requirements.
    /// </summary>
    public const string InvalidMapping = "invalid_mapping";

    /// <summary>
    /// Document generation failed due to formatting or validation errors.
    /// </summary>
    public const string DocumentGenerationError = "document_generation_error";

    /// <summary>
    /// Document ingestion failed due to parsing or validation errors.
    /// </summary>
    public const string DocumentIngestionError = "document_ingestion_error";

    /// <summary>
    /// Underlying storage operation (file I/O, database, etc.) failed.
    /// </summary>
    public const string StorageError = "storage_error";

    /// <summary>
    /// Unexpected internal error occurred during operation.
    /// </summary>
    public const string InternalError = "internal_error";
}
