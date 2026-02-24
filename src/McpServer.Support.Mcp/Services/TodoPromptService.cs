using System.Runtime.CompilerServices;
using System.Text;
using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Generates agent-consumable prompts for TODO items and invokes Copilot CLI
/// in the workspace directory, streaming output line by line.
/// Extracted from VS2026 extension copilot functions (MVP-MCP-002).
/// Uses <see cref="IOptionsMonitor{TOptions}"/> so prompt templates are re-read
/// from configuration on every call instead of being cached at startup.
/// </summary>
public sealed class TodoPromptService(
    ITodoService todoService,
    ICopilotClient copilotClient,
    IWebHostEnvironment hostEnvironment,
    IOptionsMonitor<TodoPromptOptions> promptOptions,
    ILogger<TodoPromptService> logger) : ITodoPromptService
{
    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamStatusAsync(
        string id,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var item = await todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            yield return $"error: TODO '{id}' not found.";
            yield break;
        }

        var prompt = BuildPrompt(EffectiveStatusPrompt, item);
        logger.LogInformation("Streaming Copilot status for TODO {Id} in {Cwd}", id, hostEnvironment.ContentRootPath);

        await foreach (var line in InvokeCopilotStreaming(prompt, TimeSpan.FromMinutes(3), cancellationToken).ConfigureAwait(false))
            yield return line;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamImplementAsync(
        string id,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var item = await todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            yield return $"error: TODO '{id}' not found.";
            yield break;
        }

        var prompt = BuildPrompt(EffectiveImplementPrompt, item);
        logger.LogInformation("Streaming Copilot implement for TODO {Id} in {Cwd}", id, hostEnvironment.ContentRootPath);

        await foreach (var line in InvokeCopilotStreaming(prompt, TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false))
            yield return line;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamPlanAsync(
        string id,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var item = await todoService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            yield return $"error: TODO '{id}' not found.";
            yield break;
        }

        var prompt = BuildPrompt(EffectivePlanPrompt, item);
        logger.LogInformation("Streaming Copilot plan for TODO {Id} in {Cwd}", id, hostEnvironment.ContentRootPath);

        await foreach (var line in InvokeCopilotStreaming(prompt, TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false))
            yield return line;
    }

    private string EffectiveStatusPrompt => promptOptions.CurrentValue.StatusPrompt ?? TodoPromptDefaults.StatusPrompt;
    private string EffectiveImplementPrompt => promptOptions.CurrentValue.ImplementPrompt ?? TodoPromptDefaults.ImplementPrompt;
    private string EffectivePlanPrompt => promptOptions.CurrentValue.PlanPrompt ?? TodoPromptDefaults.PlanPrompt;

    private IAsyncEnumerable<string> InvokeCopilotStreaming(string prompt, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var current = promptOptions.CurrentValue;
        var options = new CopilotClientOptions
        {
            Timeout = timeout,
            WorkingDirectory = hostEnvironment.ContentRootPath,
            RunAs = current.RunAs,
            GitHubToken = current.GitHubToken,
        };

        if (!string.IsNullOrWhiteSpace(current.AgentPath))
            options.AgentPath = current.AgentPath;

        return copilotClient.InvokeStreamingAsync(prompt, options, cancellationToken);
    }

    /// <summary>
    /// Substitutes <c>{id}</c>, <c>{title}</c>, and <c>{baseUrl}</c> placeholders
    /// in the template, then appends the TODO item context block.
    /// </summary>
    private string BuildPrompt(string template, TodoFlatItem item)
    {
        var baseUrl = promptOptions.CurrentValue.BaseUrl;
        var rendered = template
            .Replace("{id}", item.Id, StringComparison.Ordinal)
            .Replace("{title}", item.Title, StringComparison.Ordinal)
            .Replace("{baseUrl}", baseUrl, StringComparison.Ordinal);

        var sb = new StringBuilder();
        sb.AppendLine(rendered);
        sb.AppendLine();
        sb.AppendLine("--- TODO ITEM ---");
        AppendItemContext(sb, item);
        return sb.ToString();
    }

    private static void AppendItemContext(StringBuilder sb, TodoFlatItem item)
    {
        sb.AppendLine($"Id: {item.Id}");
        sb.AppendLine($"Title: {item.Title}");
        sb.AppendLine($"Section: {item.Section}");
        sb.AppendLine($"Priority: {item.Priority}");
        sb.AppendLine($"Done: {(item.Done ? "Yes" : "No")}");

        if (!string.IsNullOrEmpty(item.Estimate))
            sb.AppendLine($"Estimate: {item.Estimate}");
        if (!string.IsNullOrEmpty(item.Note))
            sb.AppendLine($"Note: {item.Note}");

        if (item.Description is { Count: > 0 })
        {
            sb.AppendLine("Description:");
            foreach (var line in item.Description)
                sb.AppendLine($"  - {line}");
        }

        if (item.TechnicalDetails is { Count: > 0 })
        {
            sb.AppendLine("Technical Details:");
            foreach (var line in item.TechnicalDetails)
                sb.AppendLine($"  - {line}");
        }

        if (item.ImplementationTasks is { Count: > 0 })
        {
            var done = item.ImplementationTasks.Count(t => t.Done);
            var total = item.ImplementationTasks.Count;
            sb.AppendLine($"Implementation Tasks ({done}/{total} complete):");
            foreach (var task in item.ImplementationTasks)
                sb.AppendLine($"  - [{(task.Done ? "x" : " ")}] {task.Task}");
        }

        if (item.DependsOn is { Count: > 0 })
        {
            sb.AppendLine("Dependencies:");
            foreach (var dep in item.DependsOn)
                sb.AppendLine($"  - {dep}");
        }

        if (item.FunctionalRequirements is { Count: > 0 })
        {
            sb.AppendLine("Functional Requirements:");
            foreach (var fr in item.FunctionalRequirements)
                sb.AppendLine($"  - {fr}");
        }

        if (item.TechnicalRequirements is { Count: > 0 })
        {
            sb.AppendLine("Technical Requirements:");
            foreach (var tr in item.TechnicalRequirements)
                sb.AppendLine($"  - {tr}");
        }
    }
}
