using McpServer.Support.Mcp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-103: LocalProxy executor for signed hub-authorized host operations.</summary>
public sealed class FederationLocalExecutionService : IFederationLocalExecutionService
{
    private const string DesktopLaunchMethod = "desktop_launch";
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Initializes a new instance of the <see cref="FederationLocalExecutionService"/> class.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve scoped host services.</param>
    public FederationLocalExecutionService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <inheritdoc />
    public async ValueTask<FederationLocalExecutionResult> ExecuteAsync(
        FederationLocalExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.Method, DesktopLaunchMethod, StringComparison.OrdinalIgnoreCase))
            return Failure($"Unsupported federation local execution method '{request.Method}'.");
        if (string.IsNullOrWhiteSpace(request.WorkspacePath))
            return Failure("workspacePath is required for federation desktop_launch execution.");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var desktopLaunchService = scope.ServiceProvider.GetRequiredService<DesktopLaunchService>();
        var result = await desktopLaunchService.LaunchAsync(
                request.WorkspacePath,
                new DesktopLaunchRequest
                {
                    ExecutablePath = request.ExecutablePath ?? string.Empty,
                    Arguments = request.Arguments,
                    WorkingDirectory = request.WorkingDirectory,
                    EnvironmentVariables = request.EnvironmentVariables,
                    CreateNoWindow = request.CreateNoWindow,
                    WindowStyle = string.IsNullOrWhiteSpace(request.WindowStyle) ? "Hidden" : request.WindowStyle,
                    WaitForExit = request.WaitForExit,
                    TimeoutMs = request.TimeoutMs,
                },
                cancellationToken)
            .ConfigureAwait(false);

        return new FederationLocalExecutionResult
        {
            Success = result.Success,
            Message = result.Success ? "desktop_launch completed." : result.ErrorMessage,
            ProcessId = result.ProcessId,
            ExitCode = result.ExitCode,
        };
    }

    private static FederationLocalExecutionResult Failure(string message)
        => new()
        {
            Success = false,
            Message = message,
        };
}
