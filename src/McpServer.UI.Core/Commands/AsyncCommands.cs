using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using McpServer.Cqrs;
using Microsoft.Extensions.Logging;
using McpServer.UI.Core.Models;
using McpServer.UI.Core.Models.Json;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.ViewModels;

namespace McpServer.UI.Core.Commands;

internal static class CommandLogger
{
    internal static readonly ILogger Logger = AppLogService.Instance.CreateLogger("Commands");
}

// --- Initialize from MCP (full tree build + first load) ---

public sealed record InitializeFromMcpCommand() : ICommand<bool>;

public sealed class InitializeFromMcpHandler(IUiDispatchTarget dispatch, ISessionDataTarget data) : ICommandHandler<InitializeFromMcpCommand, bool>
{
    public Task<Result<bool>> HandleAsync(InitializeFromMcpCommand command, CallContext context)
    {
        dispatch.DispatchToUi(() => dispatch.StatusMessage = "Loading sessions from MCP...");
        dispatch.TrackBackgroundWork(Task.Run(async () =>
        {
            try
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                    || System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)
                    || System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                {
                    Services.OllamaLogAgentService.TryStartOllamaIfNeeded();
                }
                await data.ReloadFromMcpAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                CommandLogger.Logger.LogError(ex, "InitializeFromMcp failed");
                dispatch.DispatchToUi(() => dispatch.StatusMessage = $"Failed to load tree: {ex.Message}");
            }
        }));
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Refresh and Load All JSON (aggregated view) ---

public sealed record RefreshAndLoadAllJsonCommand(string? PreselectedAgent = null) : ICommand<bool>
{
    /// <summary>Pre-fetched sessions to avoid a redundant MCP round-trip (e.g. on startup).</summary>
    public IReadOnlyList<UnifiedSessionLog>? CachedSessions { get; init; }
}

public sealed class RefreshAndLoadAllJsonHandler(IUiDispatchTarget dispatch, ISessionDataTarget data) : ICommandHandler<RefreshAndLoadAllJsonCommand, bool>
{
    public Task<Result<bool>> HandleAsync(RefreshAndLoadAllJsonCommand command, CallContext context)
    {
        dispatch.DispatchToUi(() => dispatch.StatusMessage = "Refreshing from MCP...");
        dispatch.TrackBackgroundWork(Task.Run(async () =>
        {
            try
            {
                IReadOnlyList<UnifiedSessionLog> sessions;
                Dictionary<string, UnifiedSessionLog> byPath;
                List<UnifiedSessionLog> uniqueSessions;

                if (command.CachedSessions is { Count: > 0 })
                {
                    // Reuse sessions already fetched by ReloadFromMcpAsync to
                    // avoid a redundant MCP round-trip on startup / tree rebuild.
                    uniqueSessions = command.CachedSessions.ToList();
                    byPath = data.BuildSessionsByPathDict(uniqueSessions);
                }
                else
                {
                    sessions = await data.McpSessionService.GetAllSessionsAsync(context.CancellationToken).ConfigureAwait(true);
                    byPath = data.BuildSessionsByPathDict(sessions);
                    uniqueSessions = data.OrderAndDeduplicateSessions(byPath);
                }

                // Stamp agent on entries
                foreach (var log in uniqueSessions)
                {
                    var agent = string.IsNullOrWhiteSpace(log.SourceType) ? "Unknown" : log.SourceType.Trim();
                    if (log.Entries == null) continue;
                    foreach (var entry in log.Entries)
                    {
                        if (entry != null && string.IsNullOrWhiteSpace(entry.Agent))
                            entry.Agent = agent;
                    }
                }

                var allEntries = uniqueSessions.SelectMany(l => l.Entries).OrderByDescending(e => e.Timestamp).ToList();
                var deduped = MainWindowViewModel.DeduplicateUnifiedEntries(allEntries);

                var masterLog = new UnifiedSessionLog
                {
                    SourceType = "Aggregated",
                    SessionId = "ALL-JSON",
                    Title = "All Requests",
                    Model = "Various",
                    Started = DateTime.Now,
                    Status = "Aggregated",
                    EntryCount = deduped.Count,
                    Entries = deduped,
                    TotalTokens = uniqueSessions.Sum(l => l.TotalTokens)
                };

                var summary = new JsonLogSummary();
                data.BuildUnifiedSummaryAndIndex(masterLog, summary);
                summary.SummaryLines.Clear();
                summary.SummaryLines.Add($"Type: {masterLog.SourceType}");
                summary.SummaryLines.Add($"Total Entries: {masterLog.EntryCount}");
                summary.SummaryLines.Add($"Total Tokens: {masterLog.TotalTokens:N0}");
                summary.SummaryLines.Add($"Aggregated at: {masterLog.Started}");

                var root = new JsonTreeNode("Root", "Aggregated Unified Log", "Object") { IsExpanded = true };
                // Lightweight summary tree instead of serializing the entire master log
                // (which creates thousands of JsonTreeNodes and causes ANR on Android).
                root.Children.Add(new JsonTreeNode("sourceType", masterLog.SourceType ?? "Aggregated", "String"));
                root.Children.Add(new JsonTreeNode("sessionId", masterLog.SessionId ?? "ALL-JSON", "String"));
                root.Children.Add(new JsonTreeNode("title", masterLog.Title ?? "All Requests", "String"));
                root.Children.Add(new JsonTreeNode("entryCount", masterLog.EntryCount.ToString(), "Number"));
                root.Children.Add(new JsonTreeNode("totalTokens", $"{masterLog.TotalTokens:N0}", "Number"));
                root.Children.Add(new JsonTreeNode("sessions", $"{uniqueSessions.Count} sessions", "Number"));

                var reqCount = deduped.Count;
                var sessionCount = uniqueSessions.Count;
                var preselectedAgent = command.PreselectedAgent;

                dispatch.DispatchToUi(() =>
                {
                    try
                    {
                        data.SetMcpSessionState(uniqueSessions, byPath);
                        data.JsonLogSummary = summary;
                        data.JsonTree.Clear();
                        data.JsonTree.Add(root);
                        data.UpdateFilteredSearchEntries();
                        data.AgentFilter = string.IsNullOrWhiteSpace(preselectedAgent) ? "" : preselectedAgent.Trim();
                        dispatch.StatusMessage = string.IsNullOrWhiteSpace(preselectedAgent)
                            ? $"Loaded {reqCount} requests from {sessionCount} sessions."
                            : $"Loaded {reqCount} requests from {sessionCount} sessions. Filtered by agent: {data.AgentFilter}.";
                    }
                    catch (Exception ex)
                    {
                        dispatch.StatusMessage = $"Error building UI: {ex.Message}";
                        CommandLogger.Logger.LogError(ex, "UI Build Error");
                    }
                });
            }
            catch (Exception ex)
            {
                dispatch.DispatchToUi(() => dispatch.StatusMessage = $"Error aggregating MCP sessions: {ex.Message}");
                CommandLogger.Logger.LogError(ex, "Aggregation Error");
            }
        }));
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Refresh and Load Agent JSON (filtered by agent) ---

public sealed record RefreshAndLoadAgentJsonCommand(string AgentName) : ICommand<bool>;

public sealed class RefreshAndLoadAgentJsonHandler(IUiDispatchTarget dispatch, ISessionDataTarget data) : ICommandHandler<RefreshAndLoadAgentJsonCommand, bool>
{
    public Task<Result<bool>> HandleAsync(RefreshAndLoadAgentJsonCommand command, CallContext context)
    {
        // Reuse the AllJson handler with a preselected agent filter
        var inner = new RefreshAndLoadAllJsonCommand(command.AgentName);
        return new RefreshAndLoadAllJsonHandler(dispatch, data).HandleAsync(inner, context);
    }
}

// --- Refresh and Load Single Session ---

public sealed record RefreshAndLoadSessionCommand(string VirtualPath) : ICommand<bool>;

public sealed class RefreshAndLoadSessionHandler(IUiDispatchTarget dispatch, ISessionDataTarget data) : ICommandHandler<RefreshAndLoadSessionCommand, bool>
{
    public Task<Result<bool>> HandleAsync(RefreshAndLoadSessionCommand command, CallContext context)
    {
        dispatch.DispatchToUi(() => dispatch.StatusMessage = "Refreshing from MCP...");
        dispatch.TrackBackgroundWork(Task.Run(async () =>
        {
            try
            {
                var sessions = await data.McpSessionService.GetAllSessionsAsync(context.CancellationToken).ConfigureAwait(true);
                var byPath = data.BuildSessionsByPathDict(sessions);
                var uniqueSessions = data.OrderAndDeduplicateSessions(byPath);

                if (!byPath.TryGetValue(command.VirtualPath, out var session))
                {
                    dispatch.DispatchToUi(() => dispatch.StatusMessage = "Session not found. Click Refresh.");
                    return;
                }

                dispatch.DispatchToUi(() => dispatch.StatusMessage = $"Loading {session.SourceType}/{session.SessionId}...");

                var summary = new JsonLogSummary();
                data.BuildUnifiedSummaryAndIndex(session, summary);
                summary.SummaryLines.Clear();
                summary.SummaryLines.Add($"Type: {session.SourceType}");
                summary.SummaryLines.Add($"Session: {session.SessionId}");
                summary.SummaryLines.Add($"Entries: {session.EntryCount}");
                if (!string.IsNullOrEmpty(session.Model))
                    summary.SummaryLines.Add($"Model: {session.Model}");
                if (session.LastUpdated.HasValue)
                    summary.SummaryLines.Add($"Last Updated: {session.LastUpdated}");

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
                var unifiedNode = JsonSerializer.SerializeToNode(session, options);
                var root = new JsonTreeNode("Root", $"{session.SourceType} (Unified)", "Object") { IsExpanded = true };
                data.BuildJsonTree(unifiedNode, root, null);

                dispatch.DispatchToUi(() =>
                {
                    data.SetMcpSessionState(uniqueSessions, byPath);
                    data.JsonTree.Clear();
                    data.JsonLogSummary = summary;
                    data.JsonTree.Add(root);
                    data.UpdateFilteredSearchEntries();
                    dispatch.StatusMessage = $"Loaded {session.SourceType}/{session.SessionId}";
                });
            }
            catch (Exception ex)
            {
                dispatch.DispatchToUi(() => dispatch.StatusMessage = $"Error loading session: {ex.Message}");
            }
        }));
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Load JSON File (local file) ---

public sealed record LoadJsonFileCommand(string FilePath) : ICommand<bool>;

public sealed class LoadJsonFileHandler(IUiDispatchTarget dispatch, ISessionDataTarget data) : ICommandHandler<LoadJsonFileCommand, bool>
{
    public Task<Result<bool>> HandleAsync(LoadJsonFileCommand command, CallContext context)
    {
        dispatch.DispatchToUi(() => dispatch.StatusMessage = "Loading JSON...");
        data.LoadJson(command.FilePath);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Navigate to Node (GenerateAndNavigate) ---

public sealed record NavigateToNodeCommand(FileNode? Node) : ICommand<bool>;

public sealed class NavigateToNodeHandler(INavigationTarget target) : ICommandHandler<NavigateToNodeCommand, bool>
{
    public Task<Result<bool>> HandleAsync(NavigateToNodeCommand command, CallContext context)
    {
        target.GenerateAndNavigate(command.Node);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Archive Current File ---

public sealed record ArchiveCommand() : ICommand<bool>;

public sealed class ArchiveHandler(IArchiveTarget target) : ICommandHandler<ArchiveCommand, bool>
{
    public Task<Result<bool>> HandleAsync(ArchiveCommand command, CallContext context)
    {
        target.Archive();
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Refresh View ---

public sealed record RefreshCommand() : ICommand<bool>;

public sealed class RefreshHandler(INavigationTarget target) : ICommandHandler<RefreshCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(RefreshCommand command, CallContext context)
    {
        await target.RefreshAsync();
        return Result<bool>.Success(true);
    }
}

// --- Load Markdown File ---

public sealed record LoadMarkdownFileCommand(FileNode Node) : ICommand<bool>;

public sealed class LoadMarkdownFileHandler(ISessionDataTarget target) : ICommandHandler<LoadMarkdownFileCommand, bool>
{
    public Task<Result<bool>> HandleAsync(LoadMarkdownFileCommand command, CallContext context)
    {
        target.LoadMarkdownFile(command.Node);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Load Source File ---

public sealed record LoadSourceFileCommand(FileNode Node) : ICommand<bool>;

public sealed class LoadSourceFileHandler(ISessionDataTarget target) : ICommandHandler<LoadSourceFileCommand, bool>
{
    public Task<Result<bool>> HandleAsync(LoadSourceFileCommand command, CallContext context)
    {
        target.LoadSourceFile(command.Node);
        return Task.FromResult(Result<bool>.Success(true));
    }
}

