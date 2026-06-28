// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Requirements workflow interface
// FR-MCP-REPL-003: Command Namespace Parity - Requirements operation contracts
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Requirements workflow contract
// TEST-MCP-REPL-009: Requirements management operations validate requirement identifier rules

using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Defines the canonical Requirements workflow operations for managing functional requirements (FR),
/// technical requirements (TR), test requirements (TEST), requirement mappings, and document generation/ingestion.
/// All operations enforce requirement identifier rules, mapping constraints, and document format semantics.
/// </summary>
/// <remarks>
/// <para><strong>Canonical Identifier Rules:</strong></para>
/// <list type="bullet">
/// <item>
/// <term>Functional Requirement ID</term>
/// <description>Format: <c>FR-&lt;AREA&gt;[-&lt;QUALIFIER&gt;]-###</c>. Regex: <c>^FR-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}$</c></description>
/// </item>
/// <item>
/// <term>Technical Requirement ID</term>
/// <description>Format: <c>TR-&lt;AREA&gt;-&lt;SUBAREA&gt;[-&lt;QUALIFIER&gt;]-###</c>. Regex: <c>^TR-[A-Z0-9]+(?:-[A-Z0-9]+)+-\d{3}$</c></description>
/// </item>
/// <item>
/// <term>Test Requirement ID</term>
/// <description>Format: <c>TEST-&lt;AREA&gt;[-&lt;QUALIFIER&gt;]-###</c>. Regex: <c>^TEST-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}$</c></description>
/// </item>
/// <item>
/// <term>Valid examples</term>
/// <description><c>FR-MCP-001</c>, <c>FR-MCP-MEMORY-001</c>, <c>TR-MCP-ARCH-001</c>, <c>TR-MCP-MEMORY-001</c>, <c>TEST-MCP-001</c>, <c>TEST-MCP-MEMORY-001</c></description>
/// </item>
/// <item>
/// <term>Invalid examples</term>
/// <description><c>fr-mcp-001</c>, <c>FR-MCP-1</c>, <c>TR-MCP-001</c>, <c>TEST-001</c></description>
/// </item>
/// </list>
/// <para><strong>Selection State Convenience:</strong></para>
/// <para>
/// The workflow maintains an <see cref="IRequirementsSelectionState"/> tracking the currently selected
/// FR, TR, and TEST items. Operations like <see cref="UpdateFrAsync(IFrUpdateRequest, CancellationToken)"/>,
/// <see cref="UpdateTrAsync(ITrUpdateRequest, CancellationToken)"/>, and <see cref="UpdateTestAsync(ITestUpdateRequest, CancellationToken)"/>
/// can use the selected requirement without passing the ID explicitly. This reduces command verbosity
/// in interactive sessions where agents work on one requirement at a time.
/// </para>
/// <para><strong>Mapping CRUD Behavior:</strong></para>
/// <para>
/// Requirement mappings link FR, TR, and TEST items to establish traceability. The <see cref="CreateMappingAsync"/>
/// operation creates a new mapping relationship, validating that all referenced requirement IDs exist.
/// The <see cref="DeleteMappingAsync"/> operation removes a mapping by specifying the exact FR, TR, and/or TEST
/// combination. The <see cref="ListMappingsAsync"/> operation returns all mappings, optionally filtered by
/// FR ID, TR ID, or TEST ID. Mappings are stored redundantly across requirement documents to enable fast lookups.
/// </para>
/// <para><strong>Generated Document Response Formats:</strong></para>
/// <para>
/// The <see cref="GenerateDocumentAsync"/> operation produces formatted requirement documents in Markdown/YAML or workspace export metadata for multi-document exports.
/// For Markdown output, the response includes full document content with headings, tables, and status indicators.
/// For YAML output, the response includes structured requirement data suitable for machine processing.
/// The <c>IngestDocumentAsync</c> operation parses external requirement documents and synchronizes them
/// with the workspace, validating identifier rules and updating existing requirements or creating new ones as needed.
/// Ingestion supports incremental updates and conflict detection.
/// </para>
/// </remarks>
public interface IRequirementsWorkflow
{
    /// <summary>
    /// FR-MCP-REQSCOPE-001: Lists ordered requirement scope layers for the active workspace.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Layer catalog with deterministic order and total count.</returns>
    Task<RequirementScopeLayerQueryResult> ListRequirementLayersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-MCP-REQSCOPE-001: Creates a new requirement scope layer.
    /// </summary>
    /// <param name="request">Layer creation payload.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created layer.</returns>
    Task<RequirementScopeLayer> CreateRequirementLayerAsync(RequirementScopeLayerCreateRequestModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-MCP-REQSCOPE-001: Updates mutable requirement scope layer metadata.
    /// </summary>
    /// <param name="request">Layer update payload.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated layer.</returns>
    Task<RequirementScopeLayer> UpdateRequirementLayerAsync(RequirementScopeLayerUpdateRequestModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-MCP-REQSCOPE-003: Returns requirements effective at a specific layer or the workspace current layer.
    /// </summary>
    /// <param name="layerKey">Optional preview layer key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The effective requirements and mappings for the resolved layer.</returns>
    Task<EffectiveRequirementsResult> GetEffectiveRequirementsAsync(string? layerKey = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all functional requirements with optional filtering.
    /// </summary>
    /// <param name="area">Optional area filter (e.g., "MCP", "AUTH", "API").</param>
    /// <param name="status">Optional status filter (e.g., "pending", "in_progress", "completed", "deferred").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous query operation, containing matching FR items.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query fails due to storage errors.</exception>
    Task<IFrQueryResult> ListFrAsync(
        string? area = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges invalid placeholder FRs (non-canonical IDs) from the catalog.
    /// Returns the number of entries removed.
    /// </summary>
    Task<int> PurgeInvalidPlaceholdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific functional requirement by its identifier.
    /// </summary>
    /// <param name="id">The FR identifier conforming to canonical rules (e.g., "FR-MCP-001").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous get operation, containing the FR item.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the FR is not found or a storage error occurs.</exception>
    Task<IFrItem> GetFrAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new functional requirement with the specified metadata.
    /// </summary>
    /// <param name="request">The FR creation request with all required fields.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous creation operation, containing the created FR.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="ArgumentException">Thrown if required fields are missing or violate identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an FR with the same ID already exists or a storage error occurs.</exception>
    Task<IFrMutationResult> CreateFrAsync(IFrCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing functional requirement with the specified changes.
    /// Uses the FR ID from <see cref="IRequirementsSelectionState"/> if available.
    /// </summary>
    /// <param name="request">The update request containing fields to modify. Only provided fields are updated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous update operation, containing the updated FR.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no FR is selected, the FR is not found, or a storage error occurs.</exception>
    Task<IFrMutationResult> UpdateFrAsync(IFrUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific functional requirement.
    /// </summary>
    /// <param name="id">The FR identifier to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous deletion operation.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the FR is not found or a storage error occurs.</exception>
    /// <remarks>
    /// Deleting an FR also removes all mappings that reference it. This operation cannot be undone.
    /// </remarks>
    Task DeleteFrAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all technical requirements with optional filtering.
    /// </summary>
    /// <param name="area">Optional area filter (e.g., "MCP", "AUTH", "API").</param>
    /// <param name="subarea">Optional subarea filter (e.g., "ARCH", "PERF", "SEC").</param>
    /// <param name="status">Optional status filter (e.g., "pending", "in_progress", "completed", "deferred").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous query operation, containing matching TR items.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query fails due to storage errors.</exception>
    Task<ITrQueryResult> ListTrAsync(
        string? area = null,
        string? subarea = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific technical requirement by its identifier.
    /// </summary>
    /// <param name="id">The TR identifier conforming to canonical rules (e.g., "TR-MCP-ARCH-001").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous get operation, containing the TR item.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TR is not found or a storage error occurs.</exception>
    Task<ITrItem> GetTrAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new technical requirement with the specified metadata.
    /// </summary>
    /// <param name="request">The TR creation request with all required fields.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous creation operation, containing the created TR.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="ArgumentException">Thrown if required fields are missing or violate identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a TR with the same ID already exists or a storage error occurs.</exception>
    Task<ITrMutationResult> CreateTrAsync(ITrCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing technical requirement with the specified changes.
    /// Uses the TR ID from <see cref="IRequirementsSelectionState"/> if available.
    /// </summary>
    /// <param name="request">The update request containing fields to modify. Only provided fields are updated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous update operation, containing the updated TR.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no TR is selected, the TR is not found, or a storage error occurs.</exception>
    Task<ITrMutationResult> UpdateTrAsync(ITrUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific technical requirement.
    /// </summary>
    /// <param name="id">The TR identifier to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous deletion operation.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TR is not found or a storage error occurs.</exception>
    /// <remarks>
    /// Deleting a TR also removes all mappings that reference it. This operation cannot be undone.
    /// </remarks>
    Task DeleteTrAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all test requirements with optional filtering.
    /// </summary>
    /// <param name="area">Optional area filter (e.g., "MCP", "AUTH", "API").</param>
    /// <param name="status">Optional status filter (e.g., "pending", "in_progress", "completed", "deferred").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous query operation, containing matching TEST items.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query fails due to storage errors.</exception>
    Task<ITestQueryResult> ListTestAsync(
        string? area = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific test requirement by its identifier.
    /// </summary>
    /// <param name="id">The TEST identifier conforming to canonical rules (e.g., "TEST-MCP-001").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous get operation, containing the TEST item.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TEST is not found or a storage error occurs.</exception>
    Task<ITestItem> GetTestAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new test requirement with the specified metadata.
    /// </summary>
    /// <param name="request">The TEST creation request with all required fields.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous creation operation, containing the created TEST.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="ArgumentException">Thrown if required fields are missing or violate identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a TEST with the same ID already exists or a storage error occurs.</exception>
    Task<ITestMutationResult> CreateTestAsync(ITestCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing test requirement with the specified changes.
    /// Uses the TEST ID from <see cref="IRequirementsSelectionState"/> if available.
    /// </summary>
    /// <param name="request">The update request containing fields to modify. Only provided fields are updated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous update operation, containing the updated TEST.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no TEST is selected, the TEST is not found, or a storage error occurs.</exception>
    Task<ITestMutationResult> UpdateTestAsync(ITestUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific test requirement.
    /// </summary>
    /// <param name="id">The TEST identifier to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous deletion operation.</returns>
    /// <exception cref="ArgumentException">Thrown if id is null, empty, or violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the TEST is not found or a storage error occurs.</exception>
    /// <remarks>
    /// Deleting a TEST also removes all mappings that reference it. This operation cannot be undone.
    /// </remarks>
    Task DeleteTestAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple functional requirements atomically from a YAML records array.
    /// </summary>
    /// <param name="request">Batch request containing FR records.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the batch mutation result.</returns>
    Task<RequirementsBatchResult> CreateFrBatchAsync(CreateFrBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple functional requirements atomically from a YAML records array.
    /// </summary>
    /// <param name="request">Batch request containing FR records.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the batch mutation result.</returns>
    Task<RequirementsBatchResult> UpdateFrBatchAsync(UpdateFrBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple technical requirements atomically from a YAML records array.
    /// </summary>
    /// <param name="request">Batch request containing TR records.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the batch mutation result.</returns>
    Task<RequirementsBatchResult> CreateTrBatchAsync(CreateTrBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple technical requirements atomically from a YAML records array.
    /// </summary>
    /// <param name="request">Batch request containing TR records.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the batch mutation result.</returns>
    Task<RequirementsBatchResult> UpdateTrBatchAsync(UpdateTrBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple testing requirements atomically from a YAML records array.
    /// </summary>
    /// <param name="request">Batch request containing TEST records.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the batch mutation result.</returns>
    Task<RequirementsBatchResult> CreateTestBatchAsync(CreateTestBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple testing requirements atomically from a YAML records array.
    /// </summary>
    /// <param name="request">Batch request containing TEST records.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the batch mutation result.</returns>
    Task<RequirementsBatchResult> UpdateTestBatchAsync(UpdateTestBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates mixed FR/TR/TEST requirements atomically from a YAML records array.
    /// </summary>
    /// <param name="request">Batch request containing mixed requirement records.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the batch mutation result.</returns>
    Task<RequirementsBatchResult> CreateBatchAsync(CreateRequirementsBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates mixed FR/TR/TEST requirements atomically from a YAML records array.
    /// </summary>
    /// <param name="request">Batch request containing mixed requirement records.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the batch mutation result.</returns>
    Task<RequirementsBatchResult> UpdateBatchAsync(UpdateRequirementsBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all requirement mappings with optional filtering.
    /// Returns mappings linking FR, TR, and TEST items.
    /// </summary>
    /// <param name="frId">Optional FR ID filter. If provided, returns only mappings referencing this FR.</param>
    /// <param name="trId">Optional TR ID filter. If provided, returns only mappings referencing this TR.</param>
    /// <param name="testId">Optional TEST ID filter. If provided, returns only mappings referencing this TEST.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous query operation, containing matching mappings.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query fails due to storage errors.</exception>
    /// <remarks>
    /// If multiple filters are provided, only mappings matching ALL filters are returned (AND logic).
    /// If no filters are provided, all mappings are returned.
    /// </remarks>
    Task<IMappingQueryResult> ListMappingsAsync(
        string? frId = null,
        string? trId = null,
        string? testId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new requirement mapping linking FR, TR, and/or TEST items.
    /// At least one requirement ID must be provided; all provided IDs must reference existing requirements.
    /// </summary>
    /// <param name="request">The mapping creation request specifying the requirement IDs to link.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous creation operation, containing the created mapping.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="ArgumentException">Thrown if no requirement IDs are provided or any ID violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if any referenced requirement does not exist, the mapping already exists, or a storage error occurs.</exception>
    /// <remarks>
    /// Mappings are bidirectional and stored redundantly across requirement documents for fast lookups.
    /// Duplicate mappings (same combination of FR/TR/TEST) are rejected.
    /// </remarks>
    Task<IMappingMutationResult> CreateMappingAsync(IMappingCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing requirement mapping by specifying the exact FR, TR, and/or TEST combination.
    /// At least one requirement ID must be provided to identify the mapping.
    /// </summary>
    /// <param name="frId">Optional FR ID. If provided, the mapping must reference this FR.</param>
    /// <param name="trId">Optional TR ID. If provided, the mapping must reference this TR.</param>
    /// <param name="testId">Optional TEST ID. If provided, the mapping must reference this TEST.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous deletion operation.</returns>
    /// <exception cref="ArgumentException">Thrown if no requirement IDs are provided or any ID violates identifier rules.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the mapping is not found or a storage error occurs.</exception>
    /// <remarks>
    /// The combination of provided IDs must exactly match an existing mapping. Partial matches are not deleted.
    /// If multiple filters are provided, only the mapping matching ALL filters is deleted (AND logic).
    /// </remarks>
    Task DeleteMappingAsync(
        string? frId = null,
        string? trId = null,
        string? testId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a formatted requirement document in the specified format (Markdown or YAML).
    /// The document includes all FR, TR, TEST items and their mappings.
    /// </summary>
    /// <param name="format">The output format. Valid values: "markdown", "yaml".</param>
    /// <param name="docType">The document type. Valid values: "fr" (functional requirements), "tr" (technical requirements), "test" (test requirements), "matrix" (requirement mapping matrix), "all" (complete document).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous generation operation, containing the formatted document content.</returns>
    /// <exception cref="ArgumentException">Thrown if format or docType is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown if document generation fails due to storage errors.</exception>
    /// <remarks>
    /// <para><strong>Markdown Format:</strong></para>
    /// <para>
    /// Produces human-readable Markdown with headings, tables, and requirement details.
    /// Includes status indicators, mapping cross-references, and formatting suitable for rendering.
    /// </para>
    /// <para><strong>YAML Format:</strong></para>
    /// <para>
    /// Produces structured YAML suitable for machine processing and version control.
    /// Includes all requirement metadata, mappings, and validation rules.
    /// </para>
    /// <para><strong>Document Types:</strong></para>
    /// <list type="bullet">
    /// <item><c>fr</c> — Generates functional requirements document only</item>
    /// <item><c>tr</c> — Generates technical requirements document only</item>
    /// <item><c>test</c> — Generates test requirements document only</item>
    /// <item><c>matrix</c> — Generates requirement traceability matrix showing all mappings</item>
    /// <item><c>all</c> — Generates complete requirements package with all sections</item>
    /// </list>
    /// </remarks>
    Task<IDocumentGenerationResult> GenerateDocumentAsync(
        string format,
        string docType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests an external requirement document and synchronizes it with the workspace.
    /// Parses the document, validates identifiers, and creates or updates requirements as needed.
    /// </summary>
    /// <param name="content">The document content to ingest (Markdown or YAML format).</param>
    /// <param name="format">The document format. Valid values: "markdown", "yaml".</param>
    /// <param name="mergeStrategy">The conflict resolution strategy. Valid values: "overwrite" (replace existing), "merge" (combine with existing), "skip" (ignore conflicts).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous ingestion operation, containing ingestion statistics and conflict reports.</returns>
    /// <exception cref="ArgumentException">Thrown if content is null/empty, format is invalid, or mergeStrategy is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown if document parsing fails or a storage error occurs.</exception>
    /// <remarks>
    /// <para><strong>Merge Strategies:</strong></para>
    /// <list type="bullet">
    /// <item><c>overwrite</c> — Replaces existing requirements with ingested data. Use when the external document is authoritative.</item>
    /// <item><c>merge</c> — Combines ingested data with existing requirements, preserving local changes where possible.</item>
    /// <item><c>skip</c> — Ignores conflicts and only creates new requirements. Existing requirements are not modified.</item>
    /// </list>
    /// <para>
    /// Ingestion validates all requirement identifiers, checks for duplicate IDs, and reports conflicts.
    /// The operation is transactional; if any validation fails, no changes are persisted.
    /// </para>
    /// </remarks>
    Task<IDocumentIngestionResult> IngestDocumentAsync(
        string content,
        string format,
        string mergeStrategy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests one or more path-keyed requirements documents, including Azure/GitHub wiki document sets.
    /// </summary>
    /// <param name="content">Optional canonical document content used when no document map is supplied.</param>
    /// <param name="format">The import format. Valid values: markdown, yaml, or wiki.</param>
    /// <param name="mergeStrategy">The merge strategy retained for backward-compatible callers.</param>
    /// <param name="documents">Optional path-keyed content map for wiki imports.</param>
    /// <param name="sourceFormat">Source format selector: auto, canonical, or wiki.</param>
    /// <param name="preferredWikiFormat">Preferred wiki platform when timestamp checks disagree.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous ingestion operation.</returns>
    Task<IDocumentIngestionResult> IngestDocumentAsync(
        string content,
        string format,
        string mergeStrategy,
        IReadOnlyDictionary<string, RequirementsIngestDocument>? documents,
        string? sourceFormat,
        string? preferredWikiFormat,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current requirements selection state.
    /// Returns null if no requirements are currently selected.
    /// </summary>
    /// <returns>The current selection state, or null if no requirements are selected.</returns>
    IRequirementsSelectionState? CurrentSelection();
}

/// <summary>
/// Represents the runtime state of the active requirements selection.
/// Used to track the current FR, TR, and TEST context for operations that don't require explicit ID parameters.
/// </summary>
public interface IRequirementsSelectionState
{
    /// <summary>
    /// Gets the selected functional requirement identifier, or null if no FR is selected.
    /// </summary>
    string? FrId { get; }

    /// <summary>
    /// Gets the selected technical requirement identifier, or null if no TR is selected.
    /// </summary>
    string? TrId { get; }

    /// <summary>
    /// Gets the selected test requirement identifier, or null if no TEST is selected.
    /// </summary>
    string? TestId { get; }

    /// <summary>
    /// Gets the timestamp when the selection was last updated.
    /// </summary>
    DateTimeOffset SelectedAt { get; }
}

/// <summary>
/// Represents the result of an FR query operation.
/// </summary>
public interface IFrQueryResult
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
/// Represents a functional requirement item with all metadata fields.
/// </summary>
public interface IFrItem
{
    /// <summary>
    /// Gets the unique FR identifier (e.g., "FR-MCP-001").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the brief title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the detailed description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the requirement status.
    /// Valid values: "pending", "in_progress", "completed", "deferred".
    /// </summary>
    string Status { get; }

    /// <summary>
    /// Gets the priority level.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string Priority { get; }

    /// <summary>
    /// Gets the area/category (e.g., "MCP", "AUTH", "API").
    /// </summary>
    string Area { get; }

    /// <summary>
    /// Gets additional notes or context.
    /// </summary>
    string? Notes { get; }

    /// <summary>
    /// FR-MCP-REQAC-001: gets the structured acceptance criteria attached to this requirement.
    /// Null when the requirement has no criteria; never used to signal "absent" - callers should
    /// treat null and empty list identically.
    /// </summary>
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; }

    /// <summary>FR-MCP-REQSCOPE-002: first requirement layer where this FR applies.</summary>
    string ScopeStartLayerKey { get; }

    /// <summary>FR-MCP-REQSCOPE-002: optional last requirement layer where this FR applies.</summary>
    string? ScopeEndLayerKey { get; }

    /// <summary>
    /// Gets the creation timestamp (ISO 8601).
    /// </summary>
    string CreatedAt { get; }

    /// <summary>
    /// Gets the last update timestamp (ISO 8601).
    /// </summary>
    string UpdatedAt { get; }
}

/// <summary>
/// Represents a request to create a new functional requirement.
/// </summary>
public interface IFrCreateRequest
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

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria attached to this create request.</summary>
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; }

    /// <summary>FR-MCP-REQSCOPE-002: first requirement layer where this FR applies.</summary>
    string? ScopeStartLayerKey { get; }

    /// <summary>FR-MCP-REQSCOPE-002: optional last requirement layer where this FR applies.</summary>
    string? ScopeEndLayerKey { get; }
}

/// <summary>
/// Represents a request to update an existing functional requirement.
/// All fields are optional; only provided fields are updated.
/// </summary>
public interface IFrUpdateRequest
{
    /// <summary>
    /// Gets the FR identifier to update. Null uses the current selection.
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

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves existing value.</summary>
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; }

    /// <summary>FR-MCP-REQSCOPE-002: updated first requirement layer where this FR applies.</summary>
    string? ScopeStartLayerKey { get; }

    /// <summary>FR-MCP-REQSCOPE-002: updated optional last requirement layer where this FR applies.</summary>
    string? ScopeEndLayerKey { get; }
}

/// <summary>
/// Represents the result of an FR creation or update operation.
/// </summary>
public interface IFrMutationResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the FR item after the mutation.
    /// </summary>
    IFrItem Item { get; }
}

/// <summary>
/// Represents the result of a TR query operation.
/// </summary>
public interface ITrQueryResult
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
/// Represents a technical requirement item with all metadata fields.
/// </summary>
public interface ITrItem
{
    /// <summary>
    /// Gets the unique TR identifier (e.g., "TR-MCP-ARCH-001").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the brief title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the detailed description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the requirement status.
    /// Valid values: "pending", "in_progress", "completed", "deferred".
    /// </summary>
    string Status { get; }

    /// <summary>
    /// Gets the priority level.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string Priority { get; }

    /// <summary>
    /// Gets the area/category (e.g., "MCP", "AUTH", "API").
    /// </summary>
    string Area { get; }

    /// <summary>
    /// Gets the subarea/subcategory (e.g., "ARCH", "PERF", "SEC").
    /// </summary>
    string Subarea { get; }

    /// <summary>
    /// Gets additional notes or context.
    /// </summary>
    string? Notes { get; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria attached to this requirement.</summary>
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; }

    /// <summary>FR-MCP-REQSCOPE-002: first requirement layer where this TR applies.</summary>
    string ScopeStartLayerKey { get; }

    /// <summary>FR-MCP-REQSCOPE-002: optional last requirement layer where this TR applies.</summary>
    string? ScopeEndLayerKey { get; }

    /// <summary>
    /// Gets the creation timestamp (ISO 8601).
    /// </summary>
    string CreatedAt { get; }

    /// <summary>
    /// Gets the last update timestamp (ISO 8601).
    /// </summary>
    string UpdatedAt { get; }
}

/// <summary>
/// Represents a request to create a new technical requirement.
/// </summary>
public interface ITrCreateRequest
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

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria attached to this create request.</summary>
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; }

    /// <summary>FR-MCP-REQSCOPE-002: first requirement layer where this TR applies.</summary>
    string? ScopeStartLayerKey { get; }

    /// <summary>FR-MCP-REQSCOPE-002: optional last requirement layer where this TR applies.</summary>
    string? ScopeEndLayerKey { get; }
}

/// <summary>
/// Represents a request to update an existing technical requirement.
/// All fields are optional; only provided fields are updated.
/// </summary>
public interface ITrUpdateRequest
{
    /// <summary>
    /// Gets the TR identifier to update. Null uses the current selection.
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

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves existing value.</summary>
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; }

    /// <summary>FR-MCP-REQSCOPE-002: updated first requirement layer where this TR applies.</summary>
    string? ScopeStartLayerKey { get; }

    /// <summary>FR-MCP-REQSCOPE-002: updated optional last requirement layer where this TR applies.</summary>
    string? ScopeEndLayerKey { get; }
}

/// <summary>
/// Represents the result of a TR creation or update operation.
/// </summary>
public interface ITrMutationResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the TR item after the mutation.
    /// </summary>
    ITrItem Item { get; }
}

/// <summary>
/// Represents the result of a TEST query operation.
/// </summary>
public interface ITestQueryResult
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
/// Represents a test requirement item with all metadata fields.
/// </summary>
public interface ITestItem
{
    /// <summary>
    /// Gets the unique TEST identifier (e.g., "TEST-MCP-001").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the brief title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the detailed description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the requirement status.
    /// Valid values: "pending", "in_progress", "completed", "deferred".
    /// </summary>
    string Status { get; }

    /// <summary>
    /// Gets the priority level.
    /// Valid values: "critical", "high", "medium", "low".
    /// </summary>
    string Priority { get; }

    /// <summary>
    /// Gets the area/category (e.g., "MCP", "AUTH", "API").
    /// </summary>
    string Area { get; }

    /// <summary>
    /// Gets the test type (e.g., "unit", "integration", "e2e", "performance").
    /// </summary>
    string TestType { get; }

    /// <summary>
    /// Gets additional notes or context.
    /// </summary>
    string? Notes { get; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria attached to this requirement.</summary>
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; }

    /// <summary>FR-MCP-REQSCOPE-002: first requirement layer where this TEST applies.</summary>
    string ScopeStartLayerKey { get; }

    /// <summary>FR-MCP-REQSCOPE-002: optional last requirement layer where this TEST applies.</summary>
    string? ScopeEndLayerKey { get; }

    /// <summary>
    /// Gets the creation timestamp (ISO 8601).
    /// </summary>
    string CreatedAt { get; }

    /// <summary>
    /// Gets the last update timestamp (ISO 8601).
    /// </summary>
    string UpdatedAt { get; }
}

/// <summary>
/// Represents a request to create a new test requirement.
/// </summary>
public interface ITestCreateRequest
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

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria attached to this create request.</summary>
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; }

    /// <summary>FR-MCP-REQSCOPE-002: first requirement layer where this TEST applies.</summary>
    string? ScopeStartLayerKey { get; }

    /// <summary>FR-MCP-REQSCOPE-002: optional last requirement layer where this TEST applies.</summary>
    string? ScopeEndLayerKey { get; }
}

/// <summary>
/// Represents a request to update an existing test requirement.
/// All fields are optional; only provided fields are updated.
/// </summary>
public interface ITestUpdateRequest
{
    /// <summary>
    /// Gets the TEST identifier to update. Null uses the current selection.
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

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves existing value.</summary>
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; }

    /// <summary>FR-MCP-REQSCOPE-002: updated first requirement layer where this TEST applies.</summary>
    string? ScopeStartLayerKey { get; }

    /// <summary>FR-MCP-REQSCOPE-002: updated optional last requirement layer where this TEST applies.</summary>
    string? ScopeEndLayerKey { get; }
}

/// <summary>
/// Represents the result of a TEST creation or update operation.
/// </summary>
public interface ITestMutationResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the TEST item after the mutation.
    /// </summary>
    ITestItem Item { get; }
}

/// <summary>
/// Represents the result of a mapping query operation.
/// </summary>
public interface IMappingQueryResult
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
/// Represents a requirement mapping linking FR, TR, and/or TEST items.
/// </summary>
public interface IMappingItem
{
    /// <summary>
    /// Gets the functional requirement ID, or null if not mapped.
    /// </summary>
    string? FrId { get; }

    /// <summary>
    /// Gets the technical requirement ID, or null if not mapped.
    /// </summary>
    string? TrId { get; }

    /// <summary>
    /// Gets the test requirement ID, or null if not mapped.
    /// </summary>
    string? TestId { get; }

    /// <summary>
    /// Gets the creation timestamp (ISO 8601).
    /// </summary>
    string CreatedAt { get; }

    /// <summary>
    /// Gets optional notes about this mapping.
    /// </summary>
    string? Notes { get; }
}

/// <summary>
/// Represents a request to create a new requirement mapping.
/// At least one requirement ID must be provided.
/// </summary>
public interface IMappingCreateRequest
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
    /// Gets technical requirement IDs to link. Empty means no TR links.
    /// </summary>
    IReadOnlyList<string>? TrIds { get; }

    /// <summary>
    /// Gets the test requirement ID to link, or null.
    /// </summary>
    string? TestId { get; }

    /// <summary>
    /// Gets test requirement IDs to link. Empty means no TEST links.
    /// </summary>
    IReadOnlyList<string>? TestIds { get; }

    /// <summary>
    /// Gets optional notes about this mapping.
    /// </summary>
    string? Notes { get; }
}

/// <summary>
/// Represents the result of a mapping creation operation.
/// </summary>
public interface IMappingMutationResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the mapping item after the mutation.
    /// </summary>
    IMappingItem Item { get; }
}

/// <summary>
/// Represents the result of a document generation operation.
/// </summary>
public interface IDocumentGenerationResult
{
    /// <summary>
    /// Gets whether the generation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the generated document content.
    /// Format depends on the requested format (Markdown or YAML). Multi-document exports are returned in OutputRoot and Files.
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Gets generated binary content as Base64 for legacy binary output.
    /// </summary>
    string? ContentBase64 { get; }

    /// <summary>
    /// Gets the generated document media type.
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// Gets the generated output file name.
    /// </summary>
    string? FileName { get; }

    /// <summary>
    /// Gets the workspace output root for multi-document exports.
    /// </summary>
    string? OutputRoot { get; }

    /// <summary>
    /// Gets metadata for files written by a multi-document export.
    /// </summary>
    IReadOnlyList<RequirementsDocumentExportFile> Files { get; }

    /// <summary>
    /// Gets the document format that was generated.
    /// Valid values: "markdown", "yaml", "wiki".
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
    string GeneratedAt { get; }
}

/// <summary>
/// Represents the result of a document ingestion operation.
/// </summary>
public interface IDocumentIngestionResult
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
    string IngestedAt { get; }
}

/// <summary>
/// Represents a conflict detected during document ingestion.
/// </summary>
public interface IIngestionConflict
{
    /// <summary>
    /// Gets the requirement ID that caused the conflict.
    /// </summary>
    string RequirementId { get; }

    /// <summary>
    /// Gets the conflict type.
    /// Valid values: "duplicate_id", "invalid_format", "missing_reference", "data_mismatch".
    /// </summary>
    string ConflictType { get; }

    /// <summary>
    /// Gets a description of the conflict.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the resolution action taken.
    /// Valid values: "skipped", "overwritten", "merged", "failed".
    /// </summary>
    string Resolution { get; }
}
