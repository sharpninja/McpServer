using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.UI.Core.Services;

internal sealed class UiCoreWorkspaceApiClientAdapter : IWorkspaceApiClient
{
    private readonly McpWorkspaceService _service;
    private readonly ILogger<UiCoreWorkspaceApiClientAdapter> _logger;

    public UiCoreWorkspaceApiClientAdapter(
        McpWorkspaceService service,
        ILogger<UiCoreWorkspaceApiClientAdapter>? logger = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? NullLogger<UiCoreWorkspaceApiClientAdapter>.Instance;
    }

    public async Task<ListWorkspacesResult> ListWorkspacesAsync(CancellationToken ct = default)
    {
        var result = await _service.QueryAsync(ct);
        return UiCoreMessageMapper.ToListWorkspacesResult(result);
    }

    public async Task<WorkspaceDetail?> GetWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        try
        {
            var item = await _service.GetByIdAsync(workspacePath, ct);
            return item is null ? null : UiCoreMessageMapper.ToWorkspaceDetail(item);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<bool> UpdateWorkspacePolicyAsync(UpdateWorkspacePolicyCommand command, CancellationToken ct = default)
    {
        var result = await _service.UpdateAsync(
                command.WorkspacePath,
                new McpWorkspaceUpdateRequest
                {
                    BannedLicenses = command.BannedLicenses?.ToList(),
                    BannedCountriesOfOrigin = command.BannedCountriesOfOrigin?.ToList(),
                    BannedOrganizations = command.BannedOrganizations?.ToList(),
                    BannedIndividuals = command.BannedIndividuals?.ToList(),
                },
                ct)
            ;

        return result.Success;
    }

    public async Task<WorkspaceMutationOutcome> CreateWorkspaceAsync(CreateWorkspaceCommand command, CancellationToken ct = default)
    {
        try
        {
            var result = await _service.CreateAsync(
                    UiCoreMessageMapper.ToWorkspaceCreateRequest(command),
                    ct)
                ;

            return UiCoreMessageMapper.ToWorkspaceMutationOutcome(result);
        }
        catch (McpConflictException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new WorkspaceMutationOutcome(false, ex.Message, null);
        }
        catch (McpValidationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new WorkspaceMutationOutcome(false, ex.Message, null);
        }
    }

    public async Task<WorkspaceMutationOutcome> UpdateWorkspaceAsync(UpdateWorkspaceCommand command, CancellationToken ct = default)
    {
        try
        {
            var result = await _service.UpdateAsync(
                    command.WorkspacePath,
                    UiCoreMessageMapper.ToWorkspaceUpdateRequest(command),
                    ct)
                ;

            return UiCoreMessageMapper.ToWorkspaceMutationOutcome(result);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new WorkspaceMutationOutcome(false, ex.Message, null);
        }
        catch (McpValidationException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new WorkspaceMutationOutcome(false, ex.Message, null);
        }
    }

    public async Task<WorkspaceMutationOutcome> DeleteWorkspaceAsync(DeleteWorkspaceCommand command, CancellationToken ct = default)
    {
        try
        {
            var result = await _service.DeleteAsync(command.WorkspacePath, ct);
            return UiCoreMessageMapper.ToWorkspaceMutationOutcome(result);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new WorkspaceMutationOutcome(false, ex.Message, null);
        }
    }

    public async Task<WorkspaceProcessState> GetWorkspaceStatusAsync(string workspacePath, CancellationToken ct = default)
    {
        var result = await _service.GetStatusAsync(workspacePath, ct);
        return UiCoreMessageMapper.ToWorkspaceProcessState(result);
    }

    public async Task<WorkspaceProcessState> StartWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        var result = await _service.StartAsync(workspacePath, ct);
        return UiCoreMessageMapper.ToWorkspaceProcessState(result);
    }

    public async Task<WorkspaceProcessState> StopWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        var result = await _service.StopAsync(workspacePath, ct);
        return UiCoreMessageMapper.ToWorkspaceProcessState(result);
    }

    public async Task<WorkspaceHealthState> CheckWorkspaceHealthAsync(string workspacePath, CancellationToken ct = default)
    {
        var result = await _service.GetHealthAsync(workspacePath, ct);
        return UiCoreMessageMapper.ToWorkspaceHealthState(result);
    }

    public async Task<WorkspaceGlobalPromptState> GetWorkspaceGlobalPromptAsync(CancellationToken ct = default)
    {
        var result = await _service.GetGlobalPromptAsync(ct);
        return UiCoreMessageMapper.ToWorkspaceGlobalPromptState(result);
    }

    public async Task<WorkspaceGlobalPromptState> UpdateWorkspaceGlobalPromptAsync(UpdateWorkspaceGlobalPromptCommand command, CancellationToken ct = default)
    {
        var result = await _service.UpdateGlobalPromptAsync(
                new McpWorkspaceGlobalPromptUpdateRequest { Template = command.Template },
                ct)
            ;

        return UiCoreMessageMapper.ToWorkspaceGlobalPromptState(result);
    }

    public async Task<WorkspaceInitInfo> InitWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        var result = await _service.InitAsync(workspacePath, ct);
        if (!result.Success)
            throw new InvalidOperationException(result.Error ?? $"Workspace initialization failed for '{workspacePath}'.");

        return new WorkspaceInitInfo(workspacePath, null);
    }
}

