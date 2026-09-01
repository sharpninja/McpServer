using System.ComponentModel;
using System.Text.Json;
using McpServer.SessionLog.Transcripts;
using McpServer.Support.Mcp.Models;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    /// <summary>TEST-MCP-TRANSCRIPT-008: ingest a server-local transcript file or folder by path.</summary>
    [McpServerTool(Name = "sessionlog_ingest_path"), Description("Ingest a transcript file or folder from a workspace-contained or allowlisted server-local path.")]
    public async Task<string> SessionLogIngestPath(
        [Description("Workspace path (required).")] string workspacePath,
        [Description("Transcript file or folder path, workspace-relative or allowed absolute path.")] string path,
        [Description("Agent that owns .mcpServer transcript artifacts.")] string agent,
        [Description("Optional transcript source: Auto, Claude, Codex, Grok, Cline, Copilot, or OpenCode.")] string? source = null,
        [Description("Whether folder discovery recurses.")] bool recursive = true,
        [Description("Whether malformed records fail the bundle.")] bool strict = true,
        [Description("Whether to persist through the ingestion persistence path.")] bool persist = true,
        [Description("Optional compatibility profile: None, Claude, Codex, or Grok.")] string? compatibilityProfile = null,
        [Description("Whether to emit the selected compatibility profile artifact.")] bool emitNormalizedProfile = false,
        CancellationToken cancellationToken = default)
    {
        if (_transcriptIngestionService is null)
            return JsonSerializer.Serialize(new { error = "Transcript ingestion service is not registered." }, s_camelCaseOptions);

        try
        {
            using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
            var profile = ParseTranscriptEnum(compatibilityProfile, TranscriptCompatibilityProfile.None, nameof(compatibilityProfile));
            var request = new TranscriptIngestionRequest(path)
            {
                SourceKind = ParseTranscriptEnum(source, TranscriptSourceKind.Auto, nameof(source)),
                Recursive = recursive,
                Strict = strict,
                Persist = persist,
                CompatibilityProfile = profile,
                Agent = agent,
                WorkspacePath = workspacePath,
            };
            var result = await _transcriptIngestionService.IngestPathAsync(request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(TranscriptIngestRunResponse.FromResult(result), s_camelCaseOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, exceptionType = ex.GetType().FullName }, s_camelCaseOptions);
        }
    }

    /// <summary>TEST-MCP-TRANSCRIPT-008: normalize a server-local transcript path to a selected compatibility profile.</summary>
    [McpServerTool(Name = "sessionlog_normalize_path"), Description("Normalize a transcript file or folder to canonical Session Log YAML and a selected Claude, Codex, or Grok compatibility profile.")]
    public async Task<string> SessionLogNormalizePath(
        [Description("Workspace path (required).")] string workspacePath,
        [Description("Transcript file or folder path, workspace-relative or allowed absolute path.")] string path,
        [Description("Agent that owns .mcpServer transcript artifacts.")] string agent,
        [Description("Required target profile: Claude, Codex, or Grok.")] string targetProfile,
        [Description("Optional transcript source: Auto, Claude, Codex, Grok, Cline, Copilot, or OpenCode.")] string? source = null,
        [Description("Whether folder discovery recurses.")] bool recursive = true,
        [Description("Whether malformed records fail the bundle.")] bool strict = true,
        [Description("Whether to persist through the ingestion persistence path. Defaults false for manual normalization.")] bool persist = false,
        CancellationToken cancellationToken = default)
    {
        if (_transcriptIngestionService is null)
            return JsonSerializer.Serialize(new { error = "Transcript ingestion service is not registered." }, s_camelCaseOptions);

        try
        {
            using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
            var profile = ParseTranscriptEnum(targetProfile, TranscriptCompatibilityProfile.None, nameof(targetProfile));
            if (profile == TranscriptCompatibilityProfile.None)
                throw new ArgumentException("targetProfile must be Claude, Codex, or Grok.", nameof(targetProfile));

            var request = new TranscriptIngestionRequest(path)
            {
                SourceKind = ParseTranscriptEnum(source, TranscriptSourceKind.Auto, nameof(source)),
                Recursive = recursive,
                Strict = strict,
                Persist = persist,
                CompatibilityProfile = profile,
                Agent = agent,
                WorkspacePath = workspacePath,
            };
            var result = await _transcriptIngestionService.IngestPathAsync(request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(TranscriptIngestRunResponse.FromResult(result), s_camelCaseOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, exceptionType = ex.GetType().FullName }, s_camelCaseOptions);
        }
    }

    private static TEnum ParseTranscriptEnum<TEnum>(string? value, TEnum defaultValue, string parameterName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException($"Invalid {parameterName}: {value}", parameterName);
    }
}
