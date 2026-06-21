// TR-MCP-REPL-005 / Phase 1d: GitHub/Desktop/Prompt Template MCP tools partial of FwhMcpTools.

using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    // ── GROUP C: GitHub tools ────────────────────────────────────────────

    /// <summary>TR-PLANNED-CORE-013: List GitHub issues.</summary>
    [McpServerTool(Name = "github_list_issues"), Description("List GitHub issues. Optional state filter and limit.")]
    public async Task<string> GitHubListIssues(
        [Description("State filter (open/closed/all)")] string? state = null,
        [Description("Max issues to return (default 30)")] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gitHubCliService.ListIssuesAsync(state, limit, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { issues = result.Issues });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-CORE-013: List GitHub pull requests.</summary>
    [McpServerTool(Name = "github_list_pulls"), Description("List GitHub pull requests. Optional state filter and limit.")]
    public async Task<string> GitHubListPulls(
        [Description("State filter (open/closed/all)")] string? state = null,
        [Description("Max PRs to return (default 30)")] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gitHubCliService.ListPullsAsync(state, limit, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { pulls = result.Pulls });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-CORE-013: Create a GitHub issue.</summary>
    [McpServerTool(Name = "github_create_issue"), Description("Create a GitHub issue with title and optional body.")]
    public async Task<string> GitHubCreateIssue(
        [Description("Issue title")] string title,
        [Description("Issue body")] string? body = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gitHubCliService.CreateIssueAsync(title, body, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true, number = result.Number, url = result.Url });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-CORE-013: Comment on a GitHub issue.</summary>
    [McpServerTool(Name = "github_comment_issue"), Description("Add a comment to a GitHub issue.")]
    public async Task<string> GitHubCommentIssue(
        [Description("Issue number or id")] string issueId,
        [Description("Comment body")] string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gitHubCliService.CommentOnIssueAsync(issueId, body, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-CORE-013: Comment on a GitHub pull request.</summary>
    [McpServerTool(Name = "github_comment_pull"), Description("Add a comment to a GitHub pull request.")]
    public async Task<string> GitHubCommentPull(
        [Description("PR number or id")] string prId,
        [Description("Comment body")] string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gitHubCliService.CommentOnPullAsync(prId, body, cancellationToken).ConfigureAwait(false);
            if (!result.Success) return JsonSerializer.Serialize(new { error = result.Error });
            return JsonSerializer.Serialize(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>Launch a process on the interactive desktop using CreateProcessWithTokenW.</summary>
    /// <returns>JSON result with processId, exitCode, or error.</returns>
    [McpServerTool(Name = "desktop_launch"), Description("Launch a desktop process using CreateProcessWithTokenW. Use this to open GUI applications on the user's interactive desktop.")]
    public async Task<string> DesktopLaunch(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Full path to executable")] string executablePath,
        [Description("Command-line arguments")] string? arguments = null,
        [Description("Working directory for the process")] string? workingDirectory = null,
        [Description("JSON object of environment variables to set")] string? environmentVariables = null,
        [Description("If true, launch without a visible window")] bool createNoWindow = false,
        [Description("Window style: Normal, Hidden, Minimized, Maximized")] string windowStyle = "Normal",
        [Description("If true, wait for the process to exit before returning")] bool waitForExit = false,
        [Description("Timeout in ms when waiting for exit")] int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            Dictionary<string, string>? environmentVariablesMap = null;
            if (!string.IsNullOrWhiteSpace(environmentVariables))
            {
                try
                {
                    environmentVariablesMap = JsonSerializer.Deserialize<Dictionary<string, string>>(environmentVariables, s_caseInsensitiveOptions);
                }
                catch (JsonException ex)
                {
                    return JsonSerializer.Serialize(
                        new DesktopLaunchResult
                        {
                            Success = false,
                            ErrorMessage = $"Invalid environmentVariables JSON: {ex.Message}"
                        },
                        s_caseInsensitiveOptions);
                }
            }

            var result = await _desktopLaunchService.LaunchAsync(
                    workspacePath,
                    new DesktopLaunchRequest
                    {
                        ExecutablePath = executablePath,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
                        EnvironmentVariables = environmentVariablesMap,
                        CreateNoWindow = createNoWindow,
                        WindowStyle = windowStyle,
                        WaitForExit = waitForExit,
                        TimeoutMs = timeoutMs
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(
                new DesktopLaunchResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                },
                s_caseInsensitiveOptions);
        }
    }

    // ── Prompt Template Tools ──

    /// <summary>MCP tool: list/filter prompt templates.</summary>
    [McpServerTool(Name = "prompt_template_list"), Description("List prompt templates. Optional filters: category, tag, keyword.")]
    public async Task<string> PromptTemplateList(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional category filter")] string? category = null,
        [Description("Optional tag filter")] string? tag = null,
        [Description("Optional keyword search")] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var result = await _promptTemplateService.QueryAsync(category, tag, keyword, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MCP tool: get a single prompt template.</summary>
    [McpServerTool(Name = "prompt_template_get"), Description("Get a single prompt template by ID.")]
    public async Task<string> PromptTemplateGet(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Template identifier")] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var result = await _promptTemplateService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (result is null)
                return JsonSerializer.Serialize(new { error = $"Template '{id}' not found." });
            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MCP tool: create a prompt template.</summary>
    [McpServerTool(Name = "prompt_template_create"), Description("Create a new prompt template.")]
    public async Task<string> PromptTemplateCreate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Unique kebab-case ID")] string id,
        [Description("Template title")] string title,
        [Description("Grouping category")] string category,
        [Description("Template body content (Handlebars)")] string content,
        [Description("Comma-separated tags")] string? tags = null,
        [Description("Template description")] string? description = null,
        [Description("Rendering engine (default: handlebars)")] string? engine = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var request = new Models.PromptTemplateCreateRequest
            {
                Id = id,
                Title = title,
                Category = category,
                Content = content,
                Tags = string.IsNullOrWhiteSpace(tags)
                    ? null
                    : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                Description = description,
                Engine = engine,
            };
            var result = await _promptTemplateService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MCP tool: update a prompt template.</summary>
    [McpServerTool(Name = "prompt_template_update"), Description("Update an existing prompt template. Null fields are not changed.")]
    public async Task<string> PromptTemplateUpdate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Template identifier")] string id,
        [Description("Updated title")] string? title = null,
        [Description("Updated category")] string? category = null,
        [Description("Updated content")] string? content = null,
        [Description("Updated comma-separated tags")] string? tags = null,
        [Description("Updated description")] string? description = null,
        [Description("Updated engine")] string? engine = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var request = new Models.PromptTemplateUpdateRequest
            {
                Title = title,
                Category = category,
                Content = content,
                Tags = tags is not null
                    ? tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                    : null,
                Description = description,
                Engine = engine,
            };
            var result = await _promptTemplateService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MCP tool: delete a prompt template.</summary>
    [McpServerTool(Name = "prompt_template_delete"), Description("Delete a prompt template by ID.")]
    public async Task<string> PromptTemplateDelete(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Template identifier")] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var result = await _promptTemplateService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>MCP tool: test/render a prompt template with sample data.</summary>
    [McpServerTool(Name = "prompt_template_test"), Description("Test/render a prompt template with sample variable data. Provide templateId for stored templates or inlineTemplate for ad-hoc testing.")]
    public async Task<string> PromptTemplateTest(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("JSON object of variable values")] string variablesJson,
        [Description("Template ID (for stored templates)")] string? templateId = null,
        [Description("Inline template content (for ad-hoc testing)")] string? inlineTemplate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var variables = string.IsNullOrWhiteSpace(variablesJson)
                ? new Dictionary<string, object?>()
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(variablesJson, s_caseInsensitiveOptions) ?? new();

            Models.PromptTemplateTestResult result;
            if (!string.IsNullOrWhiteSpace(templateId))
            {
                var request = new Models.PromptTemplateTestRequest { Variables = variables };
                result = await _promptTemplateService.TestAsync(templateId, request, cancellationToken).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(inlineTemplate))
            {
                var request = new Models.PromptTemplateTestRequest { Variables = variables, InlineTemplate = inlineTemplate };
                result = await _promptTemplateService.TestInlineAsync(request, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return JsonSerializer.Serialize(new { error = "Either templateId or inlineTemplate must be provided." });
            }

            return JsonSerializer.Serialize(result, s_caseInsensitiveOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
