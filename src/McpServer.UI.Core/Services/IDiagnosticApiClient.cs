using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>Host-provided API abstraction for diagnostic endpoints.</summary>
public interface IDiagnosticApiClient
{
    /// <summary>Gets execution-path diagnostic details.</summary>
    Task<DiagnosticExecutionPathSnapshot> GetExecutionPathAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets appsettings-path diagnostic details.</summary>
    Task<DiagnosticAppSettingsSnapshot> GetAppSettingsPathAsync(CancellationToken cancellationToken = default);
}
