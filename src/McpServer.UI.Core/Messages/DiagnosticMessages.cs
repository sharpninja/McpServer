using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query for <c>/mcpserver/diagnostic/execution-path</c>.</summary>
public sealed record GetDiagnosticExecutionPathQuery : IQuery<DiagnosticExecutionPathSnapshot>;

/// <summary>Query for <c>/mcpserver/diagnostic/appsettings-path</c>.</summary>
public sealed record GetDiagnosticAppSettingsPathQuery : IQuery<DiagnosticAppSettingsSnapshot>;

/// <summary>Execution-path diagnostic snapshot.</summary>
public sealed record DiagnosticExecutionPathSnapshot(
    string? ProcessPath,
    string? BaseDirectory,
    DateTimeOffset RetrievedAt);

/// <summary>Appsettings-path diagnostic snapshot.</summary>
public sealed record DiagnosticAppSettingsSnapshot(
    string? EnvironmentName,
    string? ContentRootPath,
    IReadOnlyList<DiagnosticPathFileView> Files,
    DateTimeOffset RetrievedAt);

/// <summary>Single appsettings path candidate row.</summary>
public sealed record DiagnosticPathFileView(string? Path, bool Exists);
