using System.Globalization;
using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>FR-MCP-HYGIENE-005: Production workspace.validate wrapper.</summary>
public sealed class WorkspaceValidationWorkflow : IWorkspaceValidationWorkflow
{
    private readonly WorkspaceValidationClient _client;

    /// <summary>Constructor.</summary>
    public WorkspaceValidationWorkflow(WorkspaceValidationClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public async Task<object> ValidateAsync(IReadOnlyDictionary<string, object?> args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        double? threshold = null;
        if (args.TryGetValue("staleTurnThresholdHours", out var raw) && raw is not null
            && double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var hours))
        {
            threshold = hours;
        }

        var authenticated = true;
        if (args.TryGetValue("authenticated", out var authRaw) && authRaw is bool flag)
            authenticated = flag;

        var offset = 0;
        if (args.TryGetValue("offset", out var offsetRaw) && offsetRaw is not null
            && int.TryParse(Convert.ToString(offsetRaw, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedOffset))
        {
            offset = parsedOffset;
        }

        var limit = 200;
        if (args.TryGetValue("limit", out var limitRaw) && limitRaw is not null
            && int.TryParse(Convert.ToString(limitRaw, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLimit))
        {
            limit = parsedLimit;
        }

        var request = new WorkspaceValidationRequest
        {
            Authenticated = authenticated,
            StaleTurnThresholdHours = threshold,
            Offset = offset,
            Limit = limit,
        };
        if (args.TryGetValue("ruleCodes", out var codesRaw) && codesRaw is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var code = Convert.ToString(item, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(code))
                    request.RuleCodes.Add(code);
            }
        }

        return await _client.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
