using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-015: Explicit promote from sessionlog/context into memory with provenance.
/// CQRS-owned; never mutates sessionlog source rows.
/// </summary>
public sealed class MemoryPromoteOperations
{
    private static readonly Regex SessionRefRegex = new(
        @"^sessionlog://turn/(?<turn>\d+)/action/(?<action>\d+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly McpDbContext _db;
    private readonly string _workspacePath;

    /// <summary>Creates operations bound to an already-scoped <see cref="McpDbContext"/>.</summary>
    public MemoryPromoteOperations(McpDbContext db, string workspacePath)
    {
        _db = db;
        _workspacePath = workspacePath;
    }

    /// <summary>Promotes a source into a new memory with SourceKind/SourceRef provenance.</summary>
    public async Task<MemoryPromoteResult> PromoteAsync(
        MemoryPromoteRequest? request,
        bool readOnlyCaller,
        CancellationToken cancellationToken)
    {
        if (readOnlyCaller)
            return new MemoryPromoteResult(403, FailureKind: MemoryMutationFailureKind.Validation, Error: "Read-only caller cannot promote.");

        if (request is null)
            return new MemoryPromoteResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Request body is required.");

        if (string.IsNullOrWhiteSpace(request.SourceKind))
            return new MemoryPromoteResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "SourceKind is required.");

        var kind = request.SourceKind.Trim();
        if (!kind.Equals(MemoryPromoteSourceKinds.SessionLog, StringComparison.OrdinalIgnoreCase)
            && !kind.Equals(MemoryPromoteSourceKinds.Context, StringComparison.OrdinalIgnoreCase))
        {
            return new MemoryPromoteResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Unsupported SourceKind.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceRef))
            return new MemoryPromoteResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "SourceRef is required.");

        var sourceRef = request.SourceRef.Trim();
        string content;
        string normalizedKind;

        if (kind.Equals(MemoryPromoteSourceKinds.SessionLog, StringComparison.OrdinalIgnoreCase))
        {
            normalizedKind = MemoryPromoteSourceKinds.SessionLog;
            var match = SessionRefRegex.Match(sourceRef);
            if (!match.Success)
                return new MemoryPromoteResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Invalid sessionlog SourceRef.");

            var turnId = long.Parse(match.Groups["turn"].Value);
            var actionId = long.Parse(match.Groups["action"].Value);

            var action = await _db.SessionLogActions
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == actionId && a.SessionLogTurnId == turnId, cancellationToken)
                .ConfigureAwait(false);

            if (action is null)
                return new MemoryPromoteResult(404, FailureKind: MemoryMutationFailureKind.NotFound, Error: "Session turn/action not found.");

            if (!string.Equals(action.WorkspaceId, _workspacePath, StringComparison.OrdinalIgnoreCase))
                return new MemoryPromoteResult(403, FailureKind: MemoryMutationFailureKind.Validation, Error: "Cross-workspace promote forbidden.");

            content = request.Content ?? action.Description ?? string.Empty;
        }
        else
        {
            normalizedKind = MemoryPromoteSourceKinds.Context;
            content = request.Content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content))
                return new MemoryPromoteResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Context promote requires Content.");
        }

        var service = new MemoryService(_db, Microsoft.Extensions.Logging.Abstractions.NullLogger<MemoryService>.Instance);
        var add = await service.AddAsync(new MemoryAddRequest
        {
            Category = "promoted",
            Scope = MemoryScope.Workspace,
            Text = content,
            Content = content,
            Summary = request.Summary,
            Type = "fact",
            SourceKind = normalizedKind,
            SourceRef = sourceRef,
        }, cancellationToken).ConfigureAwait(false);

        if (!add.Success || add.Memory is null)
        {
            var status = add.FailureKind switch
            {
                MemoryMutationFailureKind.Conflict => 409,
                MemoryMutationFailureKind.NotFound => 404,
                _ => 400,
            };
            return new MemoryPromoteResult(status, FailureKind: add.FailureKind, Error: add.Error);
        }

        var memory = add.Memory with
        {
            SourceKind = normalizedKind,
            SourceRef = sourceRef,
            Content = add.Memory.Content ?? content,
            Text = add.Memory.Text ?? content,
        };

        return new MemoryPromoteResult(
            200,
            Memory: memory,
            SourceKind: normalizedKind,
            SourceRef: sourceRef);
    }
}
