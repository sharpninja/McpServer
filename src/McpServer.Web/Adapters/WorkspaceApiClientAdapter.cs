using System.Text;
using System.Text.Json;
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
        var result = await UpdateWorkspaceAsync(
                new UpdateWorkspaceCommand
                {
                    WorkspacePath = command.WorkspacePath,
                    BannedLicenses = command.BannedLicenses,
                    BannedCountriesOfOrigin = command.BannedCountriesOfOrigin,
                    BannedOrganizations = command.BannedOrganizations,
                    BannedIndividuals = command.BannedIndividuals,
                },
                ct)
            .ConfigureAwait(false);

        return result.Success;
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
                BannedLicenses = command.BannedLicenses?.ToList(),
                BannedCountriesOfOrigin = command.BannedCountriesOfOrigin?.ToList(),
                BannedOrganizations = command.BannedOrganizations?.ToList(),
                BannedIndividuals = command.BannedIndividuals?.ToList()
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
                BannedLicenses = command.BannedLicenses?.ToList(),
                BannedCountriesOfOrigin = command.BannedCountriesOfOrigin?.ToList(),
                BannedOrganizations = command.BannedOrganizations?.ToList(),
                BannedIndividuals = command.BannedIndividuals?.ToList()
            },
            ct).ConfigureAwait(false);

        return MapMutationOutcome(result);
    }

    public async Task<WorkspaceMutationOutcome> DeleteWorkspaceAsync(DeleteWorkspaceCommand command, CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.DeleteAsync(EncodeWorkspaceKey(command.WorkspacePath), ct).ConfigureAwait(false);
        return MapMutationOutcome(result);
    }

    public async Task<WorkspaceProcessState> StartWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.StartAsync(EncodeWorkspaceKey(workspacePath), ct).ConfigureAwait(false);
        return MapProcessState(result);
    }

    public async Task<WorkspaceProcessState> StopWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.StopAsync(EncodeWorkspaceKey(workspacePath), ct).ConfigureAwait(false);
        return MapProcessState(result);
    }

    public async Task<WorkspaceProcessState> GetWorkspaceStatusAsync(string workspacePath, CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.GetStatusAsync(EncodeWorkspaceKey(workspacePath), ct).ConfigureAwait(false);
        return MapProcessState(result);
    }

    public async Task<WorkspaceHealthState> CheckWorkspaceHealthAsync(string workspacePath, CancellationToken ct = default)
    {
        var client = await ResolveHealthClientAsync(workspacePath, ct).ConfigureAwait(false);
        var result = await client.Health.GetAsync(ct).ConfigureAwait(false);
        return new WorkspaceHealthState(
            Success: string.Equals(result.Status, "Healthy", StringComparison.OrdinalIgnoreCase),
            StatusCode: 200,
            Url: $"{_context.BaseUrl.ToString().TrimEnd('/')}/health",
            Body: JsonSerializer.Serialize(result),
            Error: string.Equals(result.Status, "Healthy", StringComparison.OrdinalIgnoreCase)
                ? null
                : result.Status);
    }

    public async Task<WorkspaceGlobalPromptState> GetWorkspaceGlobalPromptAsync(CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.GetGlobalPromptAsync(ct).ConfigureAwait(false);
        return new WorkspaceGlobalPromptState(result.Template, result.IsDefault);
    }

    public async Task<WorkspaceGlobalPromptState> UpdateWorkspaceGlobalPromptAsync(UpdateWorkspaceGlobalPromptCommand command, CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.UpdateGlobalPromptAsync(
            new GlobalPromptUpdateRequest { Template = command.Template },
            ct).ConfigureAwait(false);
        return new WorkspaceGlobalPromptState(result.Template, result.IsDefault);
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
            dto.PromptTemplate,
            dto.StatusPrompt,
            dto.ImplementPrompt,
            dto.PlanPrompt,
            dto.DateTimeCreated,
            dto.DateTimeModified,
            dto.BannedLicenses,
            dto.BannedCountriesOfOrigin,
            dto.BannedOrganizations,
            dto.BannedIndividuals);
    }

    private static WorkspaceProcessState MapProcessState(WorkspaceProcessStatus status)
    {
        return new WorkspaceProcessState(
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

    private async Task<McpServerClient> ResolveHealthClientAsync(string workspacePath, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_context.ActiveWorkspacePath) &&
            string.Equals(_context.ActiveWorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase))
        {
            return await _context.GetRequiredActiveWorkspaceApiClientAsync(ct).ConfigureAwait(false);
        }

        var controlClient = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        return McpServerClientFactory.Create(new McpServerClientOptions
        {
            BaseUrl = _context.BaseUrl,
            ApiKey = string.IsNullOrWhiteSpace(controlClient.ApiKey) ? null : controlClient.ApiKey,
            BearerToken = string.IsNullOrWhiteSpace(controlClient.BearerToken) ? null : controlClient.BearerToken,
            WorkspacePath = workspacePath,
            Timeout = TimeSpan.FromMinutes(10),
        });
    }

    private static string EncodeWorkspaceKey(string path)
    {
        var bytes = Encoding.UTF8.GetBytes(path.Trim());
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
