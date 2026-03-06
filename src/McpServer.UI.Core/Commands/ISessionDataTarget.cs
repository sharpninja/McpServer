using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using McpServer.UI.Core.Models;
using McpServer.UI.Core.Models.Json;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Commands;

/// <summary>
/// Session data loading, JSON tree building, and MCP data operations.
/// </summary>
public interface ISessionDataTarget
{
    Task ReloadFromMcpAsync();
    void BuildUnifiedSummaryAndIndex(UnifiedSessionLog session, JsonLogSummary summary);
    void BuildJsonTree(JsonNode? node, JsonTreeNode root, string? pathPrefix);
    void LoadJson(string filePath);
    void LoadMarkdownFile(FileNode node);
    void LoadSourceFile(FileNode node);
    void UpdateFilteredSearchEntries();
    McpSessionLogService McpSessionService { get; }
    JsonLogSummary JsonLogSummary { get; set; }
    ObservableCollection<JsonTreeNode> JsonTree { get; }
    string AgentFilter { get; set; }
    Dictionary<string, UnifiedSessionLog> BuildSessionsByPathDict(IReadOnlyList<UnifiedSessionLog> sessions);
    List<UnifiedSessionLog> OrderAndDeduplicateSessions(Dictionary<string, UnifiedSessionLog> byPath);
    void SetMcpSessionState(List<UnifiedSessionLog> sessions, Dictionary<string, UnifiedSessionLog> byPath);
}

