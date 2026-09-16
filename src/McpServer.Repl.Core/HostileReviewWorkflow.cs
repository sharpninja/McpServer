using System.Globalization;
using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>FR-MCP-HOSTILEREVIEW-006: Production workflow.hostileReview wrapper that delegates to HostileReviewClient.</summary>
public sealed class HostileReviewWorkflow : IHostileReviewWorkflow
{
    private readonly HostileReviewClient _client;

    /// <summary>Initializes a new instance of the <see cref="HostileReviewWorkflow"/> class.</summary>
    public HostileReviewWorkflow(HostileReviewClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public async Task<object> SubmitAsync(IReadOnlyDictionary<string, object?> args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        var request = new HostileReviewSubmitRequest
        {
            TargetType = GetString(args, "targetType") ?? "code",
            Mode = GetString(args, "mode") ?? "adversarial",
            ScopeStatement = GetString(args, "scopeStatement") ?? string.Empty,
            RequestingAgent = GetString(args, "requestingAgent") ?? "repl",
            WorkspacePath = GetString(args, "workspacePath"),
        };
        return await _client.SubmitAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<object> StatusAsync(string requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id is required.", nameof(requestId));
        return await _client.StatusAsync(requestId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<object> GetAsync(string requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id is required.", nameof(requestId));
        return await _client.GetAsync(requestId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<object> QueryAsync(IReadOnlyDictionary<string, object?> args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        var request = new HostileReviewQueryRequest
        {
            Model = GetString(args, "model"),
            Effort = GetString(args, "effort"),
            RequestingAgent = GetString(args, "requestingAgent"),
            ReviewerAgent = GetString(args, "reviewerAgent"),
            TargetType = GetString(args, "targetType"),
            ArtifactType = GetString(args, "artifactType"),
            Severity = GetString(args, "severity"),
            Category = GetString(args, "category"),
        };
        return await _client.QueryAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static string? GetString(IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var value) || value is null)
            return null;
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
