using System.Security.Cryptography;
using System.Text;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-HANDOFF-TODO-001: One shared normalized TODO payload fingerprint.
/// Same idempotency key plus exact fingerprint heals; any semantic mismatch is a collision.
/// </summary>
public static class TodoPayloadFingerprint
{
    /// <summary>Computes the fingerprint for a create request.</summary>
    public static string Compute(TodoCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Compute(
            request.Id,
            request.Title,
            request.Section,
            request.Priority,
            request.Estimate,
            request.Description,
            request.TechnicalDetails,
            request.ImplementationTasks?.Select(task => (task.Task, task.Done)),
            request.DependsOn,
            request.FunctionalRequirements,
            request.TechnicalRequirements);
    }

    /// <summary>Computes the fingerprint for a stored flat item.</summary>
    public static string Compute(TodoFlatItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Compute(
            item.Id,
            item.Title,
            item.Section,
            item.Priority,
            item.Estimate,
            item.Description,
            item.TechnicalDetails,
            item.ImplementationTasks?.Select(task => (task.Task, task.Done)),
            item.DependsOn,
            item.FunctionalRequirements,
            item.TechnicalRequirements);
    }

    /// <summary>True when both payloads have the same normalized fingerprint.</summary>
    public static bool AreEquivalent(TodoCreateRequest request, TodoFlatItem existing)
        => string.Equals(Compute(request), Compute(existing), StringComparison.Ordinal);

    /// <summary>Computes a SHA-256 fingerprint over the normalized semantic payload.</summary>
    public static string Compute(
        string? id,
        string? title,
        string? section,
        string? priority,
        string? estimate,
        IEnumerable<string>? description,
        IEnumerable<string>? technicalDetails,
        IEnumerable<(string Task, bool Done)>? tasks,
        IEnumerable<string>? dependsOn,
        IEnumerable<string>? functionalRequirements,
        IEnumerable<string>? technicalRequirements)
    {
        var builder = new StringBuilder(256);
        Append(builder, "id", Normalize(id));
        Append(builder, "title", Normalize(title));
        Append(builder, "section", Normalize(section));
        Append(builder, "priority", Normalize(priority).ToLowerInvariant());
        Append(builder, "estimate", Normalize(estimate));
        AppendList(builder, "description", description);
        AppendList(builder, "technicalDetails", technicalDetails);
        builder.Append("tasks=");
        if (tasks is not null)
        {
            foreach (var task in tasks)
            {
                builder.Append(Normalize(task.Task).Length);
                builder.Append(':');
                builder.Append(Normalize(task.Task));
                builder.Append('|');
                builder.Append(task.Done ? '1' : '0');
                builder.Append(';');
            }
        }

        builder.Append('\n');
        AppendList(builder, "dependsOn", dependsOn);
        AppendList(builder, "functionalRequirements", functionalRequirements);
        AppendList(builder, "technicalRequirements", technicalRequirements);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void Append(StringBuilder builder, string name, string value)
    {
        builder.Append(name);
        builder.Append('=');
        builder.Append(value.Length);
        builder.Append(':');
        builder.Append(value);
        builder.Append('\n');
    }

    private static void AppendList(StringBuilder builder, string name, IEnumerable<string>? values)
    {
        builder.Append(name);
        builder.Append('=');
        if (values is not null)
        {
            foreach (var value in values)
            {
                var normalized = Normalize(value);
                builder.Append(normalized.Length);
                builder.Append(':');
                builder.Append(normalized);
                builder.Append(';');
            }
        }

        builder.Append('\n');
    }

    private static string Normalize(string? value)
        => (value ?? string.Empty).Trim();
}
