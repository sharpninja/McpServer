namespace McpServer.Support.Mcp.UseCases.Models;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Request payload to create a use case header
/// with optional FR link and optional initial basic flow/steps.
/// </summary>
public sealed class CreateUseCaseRequest
{
    /// <summary>Use case title (required, max 200).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional brief description.</summary>
    public string? BriefDescription { get; set; }

    /// <summary>Optional precondition text.</summary>
    public string? Precondition { get; set; }

    /// <summary>Optional postcondition text.</summary>
    public string? Postcondition { get; set; }

    /// <summary>Optional scope label.</summary>
    public string? Scope { get; set; }

    /// <summary>Numeric priority (default 0).</summary>
    public int Priority { get; set; }

    /// <summary>Optional functional requirement id to link at create time.</summary>
    public string? FrId { get; set; }

    /// <summary>Link type when <see cref="FrId"/> is set; defaults to Realizes.</summary>
    public string? LinkType { get; set; }

    /// <summary>Optional link order when linking an FR.</summary>
    public int LinkOrder { get; set; }

    /// <summary>Optional notes for the FR link.</summary>
    public string? Notes { get; set; }

    /// <summary>When true, creates an empty Basic flow (sequence 1) if no initial steps are supplied.</summary>
    public bool CreateBasicFlow { get; set; }

    /// <summary>Optional initial steps; when non-empty, a Basic flow is created and steps are attached.</summary>
    public IReadOnlyList<CreateUseCaseStepRequest>? InitialSteps { get; set; }
}

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Header-only update payload for a use case.
/// </summary>
public sealed class UpdateUseCaseRequest
{
    /// <summary>Optional title replacement.</summary>
    public string? Title { get; set; }

    /// <summary>Optional brief description replacement.</summary>
    public string? BriefDescription { get; set; }

    /// <summary>Optional precondition replacement.</summary>
    public string? Precondition { get; set; }

    /// <summary>Optional postcondition replacement.</summary>
    public string? Postcondition { get; set; }

    /// <summary>Optional scope replacement.</summary>
    public string? Scope { get; set; }

    /// <summary>Optional priority replacement.</summary>
    public int? Priority { get; set; }
}

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Request to add a flow under a use case.
/// </summary>
public sealed class AddUseCaseFlowRequest
{
    /// <summary>Flow type: Basic, Alternative, or Exception (default Basic).</summary>
    public string FlowType { get; set; } = "Basic";

    /// <summary>Optional flow name.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// Sequence number among flows. When null, the next available sequence is assigned.
    /// </summary>
    public int? SequenceNumber { get; set; }
}

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Request to add a step under a flow.
/// </summary>
public sealed class CreateUseCaseStepRequest
{
    /// <summary>
    /// Step number within the flow. When null, the next available number is assigned.
    /// </summary>
    public int? StepNumber { get; set; }

    /// <summary>Optional actor id for the step.</summary>
    public long? ActorId { get; set; }

    /// <summary>Actor action text (required).</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Optional system response text.</summary>
    public string? SystemResponse { get; set; }

    /// <summary>Optional data-entity notes.</summary>
    public string? DataEntities { get; set; }
}

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Request to attach an actor to a use case.
/// Provide <see cref="ActorId"/> for an existing actor, or name/type to create one.
/// </summary>
public sealed class AttachUseCaseActorRequest
{
    /// <summary>Existing actor id (preferred when known).</summary>
    public long? ActorId { get; set; }

    /// <summary>Actor name when creating a new actor.</summary>
    public string? Name { get; set; }

    /// <summary>Actor description when creating a new actor.</summary>
    public string? Description { get; set; }

    /// <summary>Actor type: Primary, Secondary, System, or External (default Primary).</summary>
    public string Type { get; set; } = "Primary";

    /// <summary>Whether this actor is primary for the use case.</summary>
    public bool IsPrimary { get; set; }
}

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-002: Request to link a use case to an FR string id.
/// </summary>
public sealed class LinkUseCaseToFrRequest
{
    /// <summary>Functional requirement id (string).</summary>
    public string FrId { get; set; } = string.Empty;

    /// <summary>Link type; defaults to Realizes when null or empty.</summary>
    public string? LinkType { get; set; }

    /// <summary>Optional ordering among links.</summary>
    public int LinkOrder { get; set; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Summary projection for list endpoints.
/// </summary>
public sealed class UseCaseSummaryDto
{
    /// <summary>Use case surrogate id.</summary>
    public long UseCaseId { get; init; }

    /// <summary>Title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Optional brief description.</summary>
    public string? BriefDescription { get; init; }

    /// <summary>Optional scope.</summary>
    public string? Scope { get; init; }

    /// <summary>Priority.</summary>
    public int Priority { get; init; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAtUtc { get; init; }

    /// <summary>Count of non-deleted FR links.</summary>
    public int FrLinkCount { get; init; }
}

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Full aggregate DTO for get/create/update.
/// </summary>
public sealed class UseCaseDetailDto
{
    /// <summary>Use case surrogate id.</summary>
    public long UseCaseId { get; init; }

    /// <summary>Workspace discriminator.</summary>
    public string WorkspaceId { get; init; } = string.Empty;

    /// <summary>Title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Optional brief description.</summary>
    public string? BriefDescription { get; init; }

    /// <summary>Optional precondition.</summary>
    public string? Precondition { get; init; }

    /// <summary>Optional postcondition.</summary>
    public string? Postcondition { get; init; }

    /// <summary>Optional scope.</summary>
    public string? Scope { get; init; }

    /// <summary>Priority.</summary>
    public int Priority { get; init; }

    /// <summary>FR-MCP-USECASE-008: Version number.</summary>
    public int VersionNumber { get; init; } = 1;

    /// <summary>FR-MCP-USECASE-008: Approval status.</summary>
    public string ApprovalStatus { get; init; } = "Draft";

    /// <summary>FR-MCP-USECASE-009: Optional product key for multi-workspace sharing.</summary>
    public string? ProductKey { get; init; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAtUtc { get; init; }

    /// <summary>Attached actors.</summary>
    public IReadOnlyList<UseCaseActorDto> Actors { get; init; } = [];

    /// <summary>Flows with steps.</summary>
    public IReadOnlyList<UseCaseFlowDto> Flows { get; init; } = [];

    /// <summary>Special requirements.</summary>
    public IReadOnlyList<UseCaseSpecialRequirementDto> SpecialRequirements { get; init; } = [];

    /// <summary>Extension points.</summary>
    public IReadOnlyList<UseCaseExtensionPointDto> ExtensionPoints { get; init; } = [];

    /// <summary>FR links.</summary>
    public IReadOnlyList<UseCaseFrLinkDto> FrLinks { get; init; } = [];
}

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Actor association DTO.
/// </summary>
public sealed class UseCaseActorDto
{
    /// <summary>Actor id.</summary>
    public long ActorId { get; init; }

    /// <summary>Actor name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Actor type.</summary>
    public string Type { get; init; } = "Primary";

    /// <summary>Whether primary for the use case.</summary>
    public bool IsPrimary { get; init; }
}

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Flow DTO with ordered steps.
/// </summary>
public sealed class UseCaseFlowDto
{
    /// <summary>Flow id.</summary>
    public long FlowId { get; init; }

    /// <summary>Parent use case id.</summary>
    public long UseCaseId { get; init; }

    /// <summary>Flow type.</summary>
    public string FlowType { get; init; } = "Basic";

    /// <summary>Optional name.</summary>
    public string? Name { get; init; }

    /// <summary>Sequence among flows.</summary>
    public int SequenceNumber { get; init; }

    /// <summary>Steps ordered by step number.</summary>
    public IReadOnlyList<UseCaseStepDto> Steps { get; init; } = [];
}

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-002: Step DTO.
/// </summary>
public sealed class UseCaseStepDto
{
    /// <summary>Step id.</summary>
    public long StepId { get; init; }

    /// <summary>Parent flow id.</summary>
    public long FlowId { get; init; }

    /// <summary>Step number.</summary>
    public int StepNumber { get; init; }

    /// <summary>Optional actor id.</summary>
    public long? ActorId { get; init; }

    /// <summary>Optional actor name when resolved.</summary>
    public string? ActorName { get; init; }

    /// <summary>Action text.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Optional system response.</summary>
    public string? SystemResponse { get; init; }

    /// <summary>Optional data entities notes.</summary>
    public string? DataEntities { get; init; }
}

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Special requirement DTO.
/// </summary>
public sealed class UseCaseSpecialRequirementDto
{
    /// <summary>Special requirement id.</summary>
    public long SpecialReqId { get; init; }

    /// <summary>Optional category.</summary>
    public string? Category { get; init; }

    /// <summary>Requirement text.</summary>
    public string RequirementText { get; init; } = string.Empty;

    /// <summary>Optional priority.</summary>
    public int? Priority { get; init; }
}

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Extension point DTO.
/// </summary>
public sealed class UseCaseExtensionPointDto
{
    /// <summary>Extension point id.</summary>
    public long ExtensionPointId { get; init; }

    /// <summary>Name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }
}

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-002: Use case to FR link DTO (FrId is string).
/// </summary>
public sealed class UseCaseFrLinkDto
{
    /// <summary>Link id.</summary>
    public long LinkId { get; init; }

    /// <summary>Use case id.</summary>
    public long UseCaseId { get; init; }

    /// <summary>Functional requirement id (string).</summary>
    public string FrId { get; init; } = string.Empty;

    /// <summary>Link type (default Realizes).</summary>
    public string LinkType { get; init; } = UseCaseConstants.DefaultLinkType;

    /// <summary>Link order.</summary>
    public int LinkOrder { get; init; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; init; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }
}

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-006: Linked use case projection for FR get payloads.
/// </summary>
public sealed class LinkedUseCaseDto
{
    /// <summary>Use case id.</summary>
    public long UseCaseId { get; init; }

    /// <summary>Use case title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Link type.</summary>
    public string LinkType { get; init; } = UseCaseConstants.DefaultLinkType;

    /// <summary>Link order.</summary>
    public int LinkOrder { get; init; }
}

/// <summary>
/// TR-MCP-USECASE-004: Mermaid diagram payload for a use case aggregate.
/// </summary>
public sealed class UseCaseDiagramDto
{
    /// <summary>Use case id.</summary>
    public long UseCaseId { get; init; }

    /// <summary>Diagram format (mermaid).</summary>
    public string Format { get; init; } = "mermaid";

    /// <summary>Diagram source text.</summary>
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-006: Coverage gaps for Realizes links.
/// </summary>
public sealed class UseCaseFrCoverageDto
{
    /// <summary>Total non-deleted use cases in the workspace.</summary>
    public int TotalUseCases { get; init; }

    /// <summary>Total non-deleted functional requirements (kind fr) in the workspace.</summary>
    public int TotalFunctionalRequirements { get; init; }

    /// <summary>Use cases that have at least one Realizes FR link.</summary>
    public int LinkedUseCases { get; init; }

    /// <summary>FRs that have at least one Realizes use case link.</summary>
    public int LinkedFunctionalRequirements { get; init; }

    /// <summary>Use cases with no Realizes FR link.</summary>
    public IReadOnlyList<UseCaseSummaryDto> UseCasesWithoutRealizesLink { get; init; } = [];

    /// <summary>FR ids with no Realizes use case link.</summary>
    public IReadOnlyList<string> FunctionalRequirementsWithoutRealizesUseCase { get; init; } = [];
}

/// <summary>
/// FR-MCP-USECASE-* / TR-MCP-USECASE-002: Shared constants for use case CQRS.
/// </summary>
public static class UseCaseConstants
{
    /// <summary>Default UC-FR link type.</summary>
    public const string DefaultLinkType = "Realizes";

    /// <summary>Requirement kind for functional requirements.</summary>
    public const string FrKind = "fr";

    /// <summary>Supported flow types.</summary>
    public static readonly HashSet<string> FlowTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Basic",
        "Alternative",
        "Exception",
    };

    /// <summary>Supported actor types.</summary>
    public static readonly HashSet<string> ActorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Primary",
        "Secondary",
        "System",
        "External",
    };

    /// <summary>Canonicalizes a known flow type to Pascal-Case storage form.</summary>
    /// <param name="flowType">Raw flow type.</param>
    /// <returns>Canonical form, or null when invalid.</returns>
    public static string? CanonicalizeFlowType(string? flowType)
    {
        if (string.IsNullOrWhiteSpace(flowType))
            return "Basic";

        foreach (var known in FlowTypes)
        {
            if (string.Equals(known, flowType.Trim(), StringComparison.OrdinalIgnoreCase))
                return known;
        }

        return null;
    }

    /// <summary>Canonicalizes a known actor type to Pascal-Case storage form.</summary>
    /// <param name="actorType">Raw actor type.</param>
    /// <returns>Canonical form, or null when invalid.</returns>
    public static string? CanonicalizeActorType(string? actorType)
    {
        if (string.IsNullOrWhiteSpace(actorType))
            return "Primary";

        foreach (var known in ActorTypes)
        {
            if (string.Equals(known, actorType.Trim(), StringComparison.OrdinalIgnoreCase))
                return known;
        }

        return null;
    }

    /// <summary>Canonicalizes link type; empty becomes Realizes.</summary>
    /// <param name="linkType">Raw link type.</param>
    /// <returns>Non-empty link type string.</returns>
    public static string CanonicalizeLinkType(string? linkType)
        => string.IsNullOrWhiteSpace(linkType) ? DefaultLinkType : linkType.Trim();
}
