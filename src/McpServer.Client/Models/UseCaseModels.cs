using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-001: Request body for creating a use case.
/// </summary>
public sealed class CreateUseCaseRequest
{
    /// <summary>Use case title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional brief description.</summary>
    [JsonPropertyName("briefDescription")]
    public string? BriefDescription { get; set; }

    /// <summary>Optional precondition text.</summary>
    [JsonPropertyName("precondition")]
    public string? Precondition { get; set; }

    /// <summary>Optional postcondition text.</summary>
    [JsonPropertyName("postcondition")]
    public string? Postcondition { get; set; }

    /// <summary>Optional scope label.</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>Numeric priority.</summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>Optional FR id to link on create.</summary>
    [JsonPropertyName("frId")]
    public string? FrId { get; set; }

    /// <summary>Link type when <see cref="FrId"/> is set; defaults to Realizes.</summary>
    [JsonPropertyName("linkType")]
    public string? LinkType { get; set; }

    /// <summary>When true, creates an empty Basic flow named Main.</summary>
    [JsonPropertyName("createBasicFlow")]
    public bool CreateBasicFlow { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-001: Request body for updating use case header fields.
/// </summary>
public sealed class UpdateUseCaseRequest
{
    /// <summary>Replacement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Replacement brief description.</summary>
    [JsonPropertyName("briefDescription")]
    public string? BriefDescription { get; set; }

    /// <summary>Replacement precondition.</summary>
    [JsonPropertyName("precondition")]
    public string? Precondition { get; set; }

    /// <summary>Replacement postcondition.</summary>
    [JsonPropertyName("postcondition")]
    public string? Postcondition { get; set; }

    /// <summary>Replacement scope.</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>Replacement priority.</summary>
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-002: Request to add a flow.
/// </summary>
public sealed class AddUseCaseFlowRequest
{
    /// <summary>Flow type: Basic, Alternative, or Exception.</summary>
    [JsonPropertyName("flowType")]
    public string FlowType { get; set; } = "Basic";

    /// <summary>Optional flow name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Optional sequence number.</summary>
    [JsonPropertyName("sequenceNumber")]
    public int? SequenceNumber { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-002: Request to add a step.
/// </summary>
public sealed class AddUseCaseStepRequest
{
    /// <summary>Optional step number.</summary>
    [JsonPropertyName("stepNumber")]
    public int? StepNumber { get; set; }

    /// <summary>Optional acting actor id.</summary>
    [JsonPropertyName("actorId")]
    public long? ActorId { get; set; }

    /// <summary>Actor action text.</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>Optional system response.</summary>
    [JsonPropertyName("systemResponse")]
    public string? SystemResponse { get; set; }

    /// <summary>Optional data-entity notes.</summary>
    [JsonPropertyName("dataEntities")]
    public string? DataEntities { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-002: Request to attach an actor.
/// </summary>
public sealed class AttachUseCaseActorRequest
{
    /// <summary>Existing actor id; when null, create from Name/Type.</summary>
    [JsonPropertyName("actorId")]
    public long? ActorId { get; set; }

    /// <summary>Actor name when creating.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Actor description when creating.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Actor type.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Primary";

    /// <summary>Whether the actor is primary for this use case.</summary>
    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-003: Request to link a use case to a functional requirement.
/// </summary>
public sealed class LinkUseCaseToFrRequest
{
    /// <summary>Functional requirement id (string).</summary>
    [JsonPropertyName("frId")]
    public string FrId { get; set; } = string.Empty;

    /// <summary>Link type; defaults to Realizes.</summary>
    [JsonPropertyName("linkType")]
    public string? LinkType { get; set; }

    /// <summary>Optional order among links.</summary>
    [JsonPropertyName("linkOrder")]
    public int LinkOrder { get; set; }

    /// <summary>Optional notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-004: Optional overrides when creating a use case from an FR.
/// </summary>
public sealed class CreateUseCaseFromFrRequest
{
    /// <summary>Optional title override.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Optional brief override.</summary>
    [JsonPropertyName("briefDescription")]
    public string? BriefDescription { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-001: Summary projection for list endpoints.
/// </summary>
public sealed class UseCaseSummary
{
    /// <summary>Surrogate use case id.</summary>
    [JsonPropertyName("useCaseId")]
    public long UseCaseId { get; set; }

    /// <summary>Title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional brief description.</summary>
    [JsonPropertyName("briefDescription")]
    public string? BriefDescription { get; set; }

    /// <summary>Optional scope.</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>Priority.</summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-001: Full aggregate projection.
/// </summary>
public sealed class UseCaseDetail
{
    /// <summary>Surrogate use case id.</summary>
    [JsonPropertyName("useCaseId")]
    public long UseCaseId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Brief description.</summary>
    [JsonPropertyName("briefDescription")]
    public string? BriefDescription { get; set; }

    /// <summary>Precondition.</summary>
    [JsonPropertyName("precondition")]
    public string? Precondition { get; set; }

    /// <summary>Postcondition.</summary>
    [JsonPropertyName("postcondition")]
    public string? Postcondition { get; set; }

    /// <summary>Scope.</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>Priority.</summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>FR-MCP-USECASE-008: Monotonic version number.</summary>
    [JsonPropertyName("versionNumber")]
    public int VersionNumber { get; set; } = 1;

    /// <summary>FR-MCP-USECASE-008: Approval status (Draft, Submitted, Approved, Rejected).</summary>
    [JsonPropertyName("approvalStatus")]
    public string ApprovalStatus { get; set; } = "Draft";

    /// <summary>FR-MCP-USECASE-009: Optional product membership key.</summary>
    [JsonPropertyName("productKey")]
    public string? ProductKey { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Attached actors.</summary>
    [JsonPropertyName("actors")]
    public IReadOnlyList<UseCaseActor> Actors { get; set; } = Array.Empty<UseCaseActor>();

    /// <summary>Flows with steps.</summary>
    [JsonPropertyName("flows")]
    public IReadOnlyList<UseCaseFlow> Flows { get; set; } = Array.Empty<UseCaseFlow>();

    /// <summary>FR links.</summary>
    [JsonPropertyName("frLinks")]
    public IReadOnlyList<UseCaseFrLink> FrLinks { get; set; } = Array.Empty<UseCaseFrLink>();
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-002: Actor attachment projection.
/// </summary>
public sealed class UseCaseActor
{
    /// <summary>Actor id.</summary>
    [JsonPropertyName("actorId")]
    public long ActorId { get; set; }

    /// <summary>Actor name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Actor description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Actor type.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Primary";

    /// <summary>Whether primary for the use case.</summary>
    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-002: Flow projection.
/// </summary>
public sealed class UseCaseFlow
{
    /// <summary>Flow id.</summary>
    [JsonPropertyName("flowId")]
    public long FlowId { get; set; }

    /// <summary>Parent use case id.</summary>
    [JsonPropertyName("useCaseId")]
    public long UseCaseId { get; set; }

    /// <summary>Flow type.</summary>
    [JsonPropertyName("flowType")]
    public string FlowType { get; set; } = "Basic";

    /// <summary>Optional name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Sequence number.</summary>
    [JsonPropertyName("sequenceNumber")]
    public int SequenceNumber { get; set; }

    /// <summary>Steps ordered by step number.</summary>
    [JsonPropertyName("steps")]
    public IReadOnlyList<UseCaseStep> Steps { get; set; } = Array.Empty<UseCaseStep>();
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-002: Step projection.
/// </summary>
public sealed class UseCaseStep
{
    /// <summary>Step id.</summary>
    [JsonPropertyName("stepId")]
    public long StepId { get; set; }

    /// <summary>Parent flow id.</summary>
    [JsonPropertyName("flowId")]
    public long FlowId { get; set; }

    /// <summary>Step number.</summary>
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    /// <summary>Optional actor id.</summary>
    [JsonPropertyName("actorId")]
    public long? ActorId { get; set; }

    /// <summary>Action text.</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>System response.</summary>
    [JsonPropertyName("systemResponse")]
    public string? SystemResponse { get; set; }

    /// <summary>Data entity notes.</summary>
    [JsonPropertyName("dataEntities")]
    public string? DataEntities { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-003: Use case to FR link projection.
/// </summary>
public sealed class UseCaseFrLink
{
    /// <summary>Link id.</summary>
    [JsonPropertyName("linkId")]
    public long LinkId { get; set; }

    /// <summary>Use case id.</summary>
    [JsonPropertyName("useCaseId")]
    public long UseCaseId { get; set; }

    /// <summary>FR id (string).</summary>
    [JsonPropertyName("frId")]
    public string FrId { get; set; } = string.Empty;

    /// <summary>Link type (default Realizes).</summary>
    [JsonPropertyName("linkType")]
    public string LinkType { get; set; } = "Realizes";

    /// <summary>Link order.</summary>
    [JsonPropertyName("linkOrder")]
    public int LinkOrder { get; set; }

    /// <summary>Optional notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-005: Mermaid diagram payload.
/// </summary>
public sealed class UseCaseDiagram
{
    /// <summary>Use case id.</summary>
    [JsonPropertyName("useCaseId")]
    public long UseCaseId { get; set; }

    /// <summary>Diagram format (mermaid).</summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "mermaid";

    /// <summary>Diagram source text.</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-006: Runtime UC↔FR Realizes coverage gaps.
/// Property names match live <c>GET /mcpserver/usecases/coverage</c> (<c>UseCaseFrCoverageDto</c>).
/// </summary>
public sealed class UseCaseFrCoverage
{
    /// <summary>Total non-deleted use cases in the workspace.</summary>
    [JsonPropertyName("totalUseCases")]
    public int TotalUseCases { get; set; }

    /// <summary>Total non-deleted functional requirements (kind fr).</summary>
    [JsonPropertyName("totalFunctionalRequirements")]
    public int TotalFunctionalRequirements { get; set; }

    /// <summary>Use cases that have at least one Realizes FR link.</summary>
    [JsonPropertyName("linkedUseCases")]
    public int LinkedUseCases { get; set; }

    /// <summary>FRs that have at least one Realizes use case link.</summary>
    [JsonPropertyName("linkedFunctionalRequirements")]
    public int LinkedFunctionalRequirements { get; set; }

    /// <summary>Use cases with no Realizes FR link.</summary>
    [JsonPropertyName("useCasesWithoutRealizesLink")]
    public IReadOnlyList<UseCaseSummary> UseCasesWithoutRealizesLink { get; set; } = Array.Empty<UseCaseSummary>();

    /// <summary>FR ids with no Realizes use case link.</summary>
    [JsonPropertyName("functionalRequirementsWithoutRealizesUseCase")]
    public IReadOnlyList<string> FunctionalRequirementsWithoutRealizesUseCase { get; set; } = Array.Empty<string>();
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-008: Request body for approval status transition.
/// </summary>
public sealed class SetUseCaseApprovalRequest
{
    /// <summary>Target status (Draft, Submitted, Approved, Rejected).</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// TR-MCP-CLIENT-001 / FR-MCP-USECASE-009: Request body for product key assignment.
/// </summary>
public sealed class SetUseCaseProductRequest
{
    /// <summary>Product key, or null/empty to clear.</summary>
    [JsonPropertyName("productKey")]
    public string? ProductKey { get; set; }
}

/// <summary>
/// FR-MCP-USECASE-012: UML use-case diagram graph DTO for canvas save/load.
/// </summary>
public sealed class UseCaseDiagramGraph
{
    /// <summary>Schema version (1).</summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Graph kind.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "uml-usecase";

    /// <summary>Optional system boundary.</summary>
    [JsonPropertyName("systemBoundary")]
    public UseCaseDiagramBoundary? SystemBoundary { get; set; }

    /// <summary>Nodes.</summary>
    [JsonPropertyName("nodes")]
    public List<UseCaseDiagramNode> Nodes { get; set; } = [];

    /// <summary>Edges.</summary>
    [JsonPropertyName("edges")]
    public List<UseCaseDiagramEdge> Edges { get; set; } = [];
}

/// <summary>FR-MCP-USECASE-012: System boundary DTO.</summary>
public sealed class UseCaseDiagramBoundary
{
    /// <summary>Id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Label.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = "System";

    /// <summary>X.</summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>Y.</summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>Width.</summary>
    [JsonPropertyName("width")]
    public double Width { get; set; }

    /// <summary>Height.</summary>
    [JsonPropertyName("height")]
    public double Height { get; set; }
}

/// <summary>FR-MCP-USECASE-012: Node DTO.</summary>
public sealed class UseCaseDiagramNode
{
    /// <summary>Id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Type: actor or usecase.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Label.</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>X.</summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>Y.</summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }
}

/// <summary>FR-MCP-USECASE-012: Edge DTO.</summary>
public sealed class UseCaseDiagramEdge
{
    /// <summary>Id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Type: association, include, extend, generalization.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Source node id.</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>Target node id.</summary>
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;
}
