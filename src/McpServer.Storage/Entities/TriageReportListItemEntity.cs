using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TRIAGE-001: 4NF generic string-list entity for a triage report. One row per value in
/// the report's affected-paths, affected-symbols, reproduction-hints, and tags lists.
/// Eliminates the multi-valued dependencies previously stored as the report's
/// <c>AffectedPathsJson</c>, <c>AffectedSymbolsJson</c>, <c>ReproductionHintsJson</c>, and
/// <c>TagsJson</c> columns.
/// </summary>
public sealed class TriageReportListItemEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Workspace discriminator used by MCP multi-tenant filters.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Foreign key to the parent triage report.</summary>
    [MaxLength(128)]
    public string ReportId { get; set; } = string.Empty;

    /// <summary>Discriminator identifying which list this item belongs to (AffectedPath, AffectedSymbol, ReproductionHint, Tag).</summary>
    [Required]
    [MaxLength(32)]
    public required string ListType { get; set; }

    /// <summary>Ordinal position within the list.</summary>
    public int Ordinal { get; set; }

    /// <summary>The string value of this list item.</summary>
    public required string Value { get; set; }

    /// <summary>Navigation to the parent triage report.</summary>
    public TriageReportEntity? TriageReport { get; set; }
}
