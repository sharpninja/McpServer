using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="GetDiagnosticExecutionPathQuery"/>.</summary>
internal sealed class GetDiagnosticExecutionPathQueryHandler : IQueryHandler<GetDiagnosticExecutionPathQuery, DiagnosticExecutionPathSnapshot>
{
    private readonly IDiagnosticApiClient _diagnosticApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public GetDiagnosticExecutionPathQueryHandler(IDiagnosticApiClient diagnosticApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
        _diagnosticApiClient = diagnosticApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<DiagnosticExecutionPathSnapshot>> HandleAsync(GetDiagnosticExecutionPathQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.DiagnosticExecutionPath))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.DiagnosticExecutionPath);
            return Result<DiagnosticExecutionPathSnapshot>.Failure(
                string.IsNullOrWhiteSpace(requiredRole) ? "Permission denied." : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _diagnosticApiClient.GetExecutionPathAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<DiagnosticExecutionPathSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return Result<DiagnosticExecutionPathSnapshot>.Failure(ex);
        }
    }
}
