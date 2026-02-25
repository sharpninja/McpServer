using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="GetDiagnosticAppSettingsPathQuery"/>.</summary>
internal sealed class GetDiagnosticAppSettingsPathQueryHandler : IQueryHandler<GetDiagnosticAppSettingsPathQuery, DiagnosticAppSettingsSnapshot>
{
    private readonly IDiagnosticApiClient _diagnosticApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public GetDiagnosticAppSettingsPathQueryHandler(IDiagnosticApiClient diagnosticApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
        _diagnosticApiClient = diagnosticApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<DiagnosticAppSettingsSnapshot>> HandleAsync(GetDiagnosticAppSettingsPathQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.DiagnosticAppSettingsPath))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.DiagnosticAppSettingsPath);
            return Result<DiagnosticAppSettingsSnapshot>.Failure(
                string.IsNullOrWhiteSpace(requiredRole) ? "Permission denied." : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _diagnosticApiClient.GetAppSettingsPathAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<DiagnosticAppSettingsSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<DiagnosticAppSettingsSnapshot>.Failure(ex);
        }
    }
}
