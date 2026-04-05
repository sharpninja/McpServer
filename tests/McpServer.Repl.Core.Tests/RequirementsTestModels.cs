using System;
using System.Collections.Generic;

namespace McpServer.Repl.Core.Tests;

public sealed class RequirementDto
{
    public int Id { get; set; }
    public string RequirementId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class CreateRequirementRequest
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Priority { get; set; }
    public string? Area { get; set; }
    public string? Subarea { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateRequirementRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Notes { get; set; }
}

public sealed class RequirementQueryResult
{
    public List<RequirementDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
}
