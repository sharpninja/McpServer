using System.Text;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Web.Adapters;

internal sealed class WorkspaceApiClientAdapter : IWorkspaceApiClient
{
    private readonly WebMcpContext _context;
    private readonly ILogger<WorkspaceApiClientAdapter> _logger;

    public WorkspaceApiClientAdapter(WebMcpContext context, ILogger<WorkspaceApiClientAdapter>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<WorkspaceApiClientAdapter>.Instance;
    }

    public async Task<ListWorkspacesResult> ListWorkspacesAsync(CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var response = await client.Workspace.ListAsync(ct).ConfigureAwait(false);

        var items = response.Items
            .Select(ws => new WorkspaceSummary(
                ws.WorkspacePath,
                ws.Name,
                ws.IsPrimary,
                ws.IsEnabled))
            .ToList();

        return new ListWorkspacesResult(items, response.TotalCount);
    }

    public async Task<WorkspaceDetail?> GetWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        var key = EncodeWorkspaceKey(workspacePath);
        try
        {
            var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
            var dto = await client.Workspace.GetAsync(key, ct).ConfigureAwait(false);
            return MapWorkspaceDetail(dto);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<bool> UpdateWorkspacePolicyAsync(UpdateWorkspacePolicyCommand command, CancellationToken ct = default)
    {
        var key = EncodeWorkspaceKey(command.WorkspacePath);
        var request = new WorkspacePolicyUpdateRequestDto
        {
            BannedLicenses = command.BannedLicenses,
            BannedCountriesOfOrigin = command.BannedCountriesOfOrigin,
            BannedOrganizations = command.BannedOrganizations,
            BannedIndividuals = command.BannedIndividuals,
        };

        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.UpdateAsync(
            key,
            new WorkspaceUpdateRequest
            {
                BannedLicenses = request.BannedLicenses,
                BannedCountriesOfOrigin = request.BannedCountriesOfOrigin,
                BannedOrganizations = request.BannedOrganizations,
                BannedIndividuals = request.BannedIndividuals,
            },
            ct).ConfigureAwait(false);

        return result?.Success == true;
    }

    public async Task<WorkspaceInitInfo> InitWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentException("Workspace path is required.", nameof(workspacePath));

        var controlClient = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var seedResult = await controlClient.Agent.SeedDefinitionsAsync(ct).ConfigureAwait(false);
        _ = await controlClient.Agent.LogEventAsync(
            "system",
            new AgentEventRequest
            {
                AgentId = "system",
                EventType = 7,
                Details = "Workspace initialized via Web UI",
            },
            workspacePath,
            ct).ConfigureAwait(false);

        int? seeded = seedResult.Seeded;
        return new WorkspaceInitInfo(workspacePath, seeded);
    }

    public async Task<WorkspaceMutationOutcome> CreateWorkspaceAsync(CreateWorkspaceCommand command, CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.CreateAsync(
            new WorkspaceCreateRequest
            {
                WorkspacePath = command.WorkspacePath,
                Name = command.Name,
                TodoPath = command.TodoPath,
                DataDirectory = command.DataDirectory,
                TunnelProvider = command.TunnelProvider,
                RunAs = command.RunAs,
                IsPrimary = command.IsPrimary,
                IsEnabled = command.IsEnabled,
                PromptTemplate = command.PromptTemplate,
                StatusPrompt = command.StatusPrompt,
                ImplementPrompt = command.ImplementPrompt,
                PlanPrompt = command.PlanPrompt,
                BannedLicenses = command.BannedLicenses,
                BannedCountriesOfOrigin = command.BannedCountriesOfOrigin,
                BannedOrganizations = command.BannedOrganizations,
                BannedIndividuals = command.BannedIndividuals
            },
            ct).ConfigureAwait(false);

        return MapMutationOutcome(result);
    }

    public async Task<WorkspaceMutationOutcome> UpdateWorkspaceAsync(UpdateWorkspaceCommand command, CancellationToken ct = default)
    {
        var key = EncodeWorkspaceKey(command.WorkspacePath);
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.UpdateAsync(
            key,
            new WorkspaceUpdateRequest
            {
                Name = command.Name,
                TodoPath = command.TodoPath,
                DataDirectory = command.DataDirectory,
                TunnelProvider = command.TunnelProvider,
                RunAs = command.RunAs,
                IsPrimary = command.IsPrimary,
                IsEnabled = command.IsEnabled,
                PromptTemplate = command.PromptTemplate,
                StatusPrompt = command.StatusPrompt,
                ImplementPrompt = command.ImplementPrompt,
                PlanPrompt = command.PlanPrompt,
                BannedLicenses = command.BannedLicenses,
                BannedCountriesOfOrigin = command.BannedCountriesOfOrigin,
                BannedOrganizations = command.BannedOrganizations,
                BannedIndividuals = command.BannedIndividuals
            },
            ct).ConfigureAwait(false);

        return MapMutationOutcome(result);
    }

    public async Task<WorkspaceMutationOutcome> DeleteWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        var key = EncodeWorkspaceKey(workspacePath);
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.DeleteAsync(key, ct).ConfigureAwait(false);
        return MapMutationOutcome(result);
    }

    public async Task<WorkspaceRuntimeStatus> StartWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        var key = EncodeWorkspaceKey(workspacePath);
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.StartAsync(key, ct).ConfigureAwait(false);
        return MapRuntimeStatus(result);
    }

    public async Task<WorkspaceRuntimeStatus> StopWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        var key = EncodeWorkspaceKey(workspacePath);
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.StopAsync(key, ct).ConfigureAwait(false);
        return MapRuntimeStatus(result);
    }

    public async Task<WorkspaceRuntimeStatus> GetWorkspaceStatusAsync(string workspacePath, CancellationToken ct = default)
    {
        var key = EncodeWorkspaceKey(workspacePath);
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.GetStatusAsync(key, ct).ConfigureAwait(false);
        return MapRuntimeStatus(result);
    }

    public async Task<GlobalPromptInfo> GetGlobalPromptAsync(CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.GetGlobalPromptAsync(ct).ConfigureAwait(false);
        return new GlobalPromptInfo(result.Template, result.IsDefault);
    }

    public async Task<GlobalPromptInfo> UpdateGlobalPromptAsync(UpdateGlobalPromptCommand command, CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.UpdateGlobalPromptAsync(
            new GlobalPromptUpdateRequest { Template = command.Template },
            ct).ConfigureAwait(false);
        return new GlobalPromptInfo(result.Template, result.IsDefault);
    }

    private static WorkspaceDetail MapWorkspaceDetail(WorkspaceDto dto)
    {
        return new WorkspaceDetail(
            dto.WorkspacePath,
            dto.Name,
            dto.TodoPath,
            dto.DataDirectory,
            dto.TunnelProvider,
            dto.IsPrimary,
            dto.IsEnabled,
            dto.RunAs,
            dto.DateTimeCreated,
            dto.DateTimeModified,
            dto.BannedLicenses,
            dto.BannedCountriesOfOrigin,
            dto.BannedOrganizations,
            dto.BannedIndividuals);
    }

    private static WorkspaceRuntimeStatus MapRuntimeStatus(WorkspaceProcessStatus status)
    {
        return new WorkspaceRuntimeStatus(
            status.IsRunning,
            status.Pid,
            status.Uptime,
            status.Port,
            status.Error);
    }

    private static WorkspaceMutationOutcome MapMutationOutcome(WorkspaceMutationResult result)
    {
        var detail = result.Workspace is null ? null : MapWorkspaceDetail(result.Workspace);
        return new WorkspaceMutationOutcome(result.Success, result.Error, detail);
    }

    private static string EncodeWorkspaceKey(string path)
    {
        var bytes = Encoding.UTF8.GetBytes(path.Trim());
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private sealed class WorkspacePolicyUpdateRequestDto
    {
        public List<string>? BannedLicenses { get; set; }

        public List<string>? BannedCountriesOfOrigin { get; set; }

        public List<string>? BannedOrganizations { get; set; }

        public List<string>? BannedIndividuals { get; set; }
    }
}
