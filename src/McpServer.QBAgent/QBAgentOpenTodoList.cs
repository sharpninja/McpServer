using System.Text;
using McpServer.Client.Models;

namespace McpServer.QBAgent;

/// <summary>
/// Formats MCP TODO query results for the QBAgent console. Open means <c>done: false</c>.
/// </summary>
public static class QBAgentOpenTodoList
{
    /// <summary>Renders a compact id/priority/title listing.</summary>
    public static string Format(TodoQueryResult result, string? workspacePath = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        var items = result.Items ?? [];
        if (items.Count == 0)
        {
            return string.IsNullOrWhiteSpace(workspacePath)
                ? "No open TODOs (done: false)."
                : $"No open TODOs (done: false) in {workspacePath}.";
        }

        var builder = new StringBuilder();
        var count = result.TotalCount > 0 ? result.TotalCount : items.Count;
        builder.Append(count)
               .Append(" open TODO")
               .Append(count == 1 ? string.Empty : "s")
               .AppendLine(" (done: false):");
        foreach (var item in items.OrderBy(static i => i.Priority, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static i => i.Id, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(item.Id)
                   .Append('\t')
                   .Append(string.IsNullOrWhiteSpace(item.Priority) ? "-" : item.Priority.Trim())
                   .Append('\t')
                   .Append(item.Title)
                   .AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}
