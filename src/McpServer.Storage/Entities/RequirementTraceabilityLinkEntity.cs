using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// Authoritative traceability link from one functional requirement to a
/// technical or testing requirement in the same workspace.
/// </summary>
public sealed class RequirementTraceabilityLinkEntity
{
    /// <summary>Resolved workspace discriminator, normally the absolute workspace path.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Source requirement kind. DB-FK-001 fixes this to <c>fr</c>.</summary>
    [Required]
    [StringLength(16)]
    public string SourceKind { get; set; } = "fr";

    /// <summary>Source functional requirement identifier.</summary>
    [Required]
    [StringLength(128)]
    public string FrId { get; set; } = string.Empty;

    /// <summary>Target requirement kind: <c>tr</c> or <c>test</c>.</summary>
    [Required]
    [StringLength(16)]
    public string TargetKind { get; set; } = string.Empty;

    /// <summary>Target technical or testing requirement identifier.</summary>
    [Required]
    [StringLength(128)]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the link was created.</summary>
    [Required]
    [StringLength(64)]
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>Source functional requirement navigation.</summary>
    public RequirementEntity? SourceRequirement { get; set; }

    /// <summary>Target technical or testing requirement navigation.</summary>
    public RequirementEntity? TargetRequirement { get; set; }
}
