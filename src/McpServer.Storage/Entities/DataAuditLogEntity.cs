using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO.Compression;
using System.Text;
using McpServer.Common.AgentCli;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-DB-004: Append-only generic audit ledger for mutable MCP database
/// entities. Domain-specific audit tables may remain for compatibility, but
/// mutations should also be mirrored here.
/// </summary>
public sealed class DataAuditLogEntity
{
    /// <summary>Stable audit row identifier.</summary>
    [Key]
    [StringLength(64)]
    public string AuditId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Workspace affected by the mutation, or empty for global rows.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Entity CLR type or domain kind.</summary>
    [Required]
    [StringLength(256)]
    public string EntityKind { get; set; } = string.Empty;

    /// <summary>Stable key for the affected entity.</summary>
    [Required]
    [StringLength(1024)]
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>Semantic action, such as create, update, or delete.</summary>
    [Required]
    [StringLength(64)]
    public string Action { get; set; } = string.Empty;

    /// <summary>Actor responsible for the mutation.</summary>
    [Required]
    [StringLength(256)]
    public string Actor { get; set; } = "system";

    /// <summary>Agent, service, import, federation, or other source type.</summary>
    [Required]
    [StringLength(128)]
    public string SourceType { get; set; } = "McpDbContext";

    /// <summary>Optional request identifier for correlation.</summary>
    [StringLength(256)]
    public string? RequestId { get; set; }

    /// <summary>Optional cross-system correlation identifier.</summary>
    [StringLength(256)]
    public string? CorrelationId { get; set; }

    /// <summary>Optional federation operation identifier.</summary>
    [StringLength(256)]
    public string? FederationOperationId { get; set; }

    /// <summary>UTC timestamp when the mutation was recorded.</summary>
    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>Null for legacy text rows; 1 for GZip-compressed UTF-8 payloads.</summary>
    public int? PayloadEncodingVersion { get; set; } = 1;

    /// <summary>Legacy text columns remain readable without rewriting historical rows.</summary>
    public string? PreviousSnapshotJsonLegacy { get; set; }
    /// <summary>Legacy current snapshot text.</summary>
    public string? CurrentSnapshotJsonLegacy { get; set; }
    /// <summary>Legacy diff text.</summary>
    public string? DiffJsonLegacy { get; set; }
    /// <summary>Legacy metadata text.</summary>
    public string? MetadataJsonLegacy { get; set; }

    /// <summary>Versioned binary payload columns for new audit rows.</summary>
    public byte[]? PreviousSnapshotPayload { get; set; }
    /// <summary>Compressed current snapshot.</summary>
    public byte[]? CurrentSnapshotPayload { get; set; }
    /// <summary>Compressed diff.</summary>
    public byte[]? DiffPayload { get; set; }
    /// <summary>Compressed metadata.</summary>
    public byte[]? MetadataPayload { get; set; }

    /// <summary>Sanitized JSON snapshot before the mutation.</summary>
    [NotMapped]
    public string? PreviousSnapshotJson
    {
        get => Decode(PreviousSnapshotPayload, PreviousSnapshotJsonLegacy);
        set { PreviousSnapshotPayload = Compress(value); PreviousSnapshotJsonLegacy = null; }
    }

    /// <summary>Sanitized JSON snapshot after the mutation.</summary>
    [NotMapped]
    public string? CurrentSnapshotJson
    {
        get => Decode(CurrentSnapshotPayload, CurrentSnapshotJsonLegacy);
        set { CurrentSnapshotPayload = Compress(value); CurrentSnapshotJsonLegacy = null; }
    }

    /// <summary>Optional sanitized JSON diff.</summary>
    [NotMapped]
    public string? DiffJson
    {
        get => Decode(DiffPayload, DiffJsonLegacy);
        set { DiffPayload = Compress(value); DiffJsonLegacy = null; }
    }

    /// <summary>Optional metadata JSON for domain-specific details.</summary>
    [NotMapped]
    public string? MetadataJson
    {
        get => Decode(MetadataPayload, MetadataJsonLegacy);
        set { MetadataPayload = Compress(value); MetadataJsonLegacy = null; }
    }

    private string? Decode(byte[]? payload, string? legacy)
    {
        if (payload is null)
            return legacy;
        if (PayloadEncodingVersion != 1)
            throw new InvalidDataException($"Unsupported audit payload encoding version {PayloadEncodingVersion}.");

        using var input = new MemoryStream(payload, writable: false);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private byte[]? Compress(string? value)
    {
        if (value is null)
            return null;

        PayloadEncodingVersion = 1;
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            var bytes = Encoding.UTF8.GetBytes(LineSanitizer.Sanitize(value));
            gzip.Write(bytes);
        }

        return output.ToArray();
    }

}
