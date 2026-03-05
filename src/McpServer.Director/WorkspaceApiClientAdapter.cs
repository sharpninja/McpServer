using System.Text.Json;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director;

/// <summary>
/// Director-specific implementation of <see cref="IWorkspaceApiClient"/> backed by <see cref="DirectorMcpContext"/>
/// using typed clients where available and composite raw calls for Director workflows.
/// </summary>
internal sealed class WorkspaceApiClientAdapter : IWorkspaceApiClient
{
    private readonly DirectorMcpContext _context;
    private readonly ILogger<WorkspaceApiClientAdapter> _logger;


    public WorkspaceApiClientAdapter(DirectorMcpContext context,
        ILogger<WorkspaceApiClientAdapter>? logger = null)
    {
        _logger = logger ?? NullLogger<WorkspaceApiClientAdapter>.Instance;
        _context = context;
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
                    BannedIndividuals = command.BannedIndividuals?.ToList(),
                },
                ct)
            .ConfigureAwait(false);

        return MapMutationOutcome(result);
    }

    public async Task<WorkspaceMutationOutcome> UpdateWorkspaceAsync(UpdateWorkspaceCommand command, CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.UpdateAsync(
                EncodeWorkspaceKey(command.WorkspacePath),
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
                    BannedIndividuals = command.BannedIndividuals?.ToList(),
                },
                ct)
            .ConfigureAwait(false);

        return MapMutationOutcome(result);
    }

    public async Task<WorkspaceMutationOutcome> DeleteWorkspaceAsync(DeleteWorkspaceCommand command, CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.DeleteAsync(EncodeWorkspaceKey(command.WorkspacePath), ct).ConfigureAwait(false);
        return MapMutationOutcome(result);
    }

    public async Task<WorkspaceProcessState> GetWorkspaceStatusAsync(string workspacePath, CancellationToken ct = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(ct).ConfigureAwait(false);
        var result = await client.Workspace.GetStatusAsync(EncodeWorkspaceKey(workspacePath), ct).ConfigureAwait(false);
        return MapProcessState(result);
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

    public async Task<WorkspaceHealthState> CheckWorkspaceHealthAsync(string workspacePath, CancellationToken ct = default)
    {
        var client = ResolveHealthClient(workspacePath);
        var raw = await client.GetStringAsync("/health", ct).ConfigureAwait(false);
        return new WorkspaceHealthState(
            Success: true,
            StatusCode: 200,
            Url: $"{client.BaseUrl.TrimEnd('/')}/health",
            Body: raw,
            Error: null);
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
                ct)
            .ConfigureAwait(false);

        return new WorkspaceGlobalPromptState(result.Template, result.IsDefault);
    }

    public async Task<WorkspaceInitInfo> InitWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentException("Workspace path is required.", nameof(workspacePath));

        var client = _context.HasControlConnection
            ? _context.GetRequiredControlHttpClient()
            : _context.GetRequiredActiveWorkspaceHttpClient();

        var seedResult = await client.PostAsync<JsonElement>("/mcpserver/agents/definitions/seed", ct: ct).ConfigureAwait(false);
        var path = Uri.EscapeDataString(workspacePath);
        var eventBody = new
        {
            agentId = "system",
            eventType = 7, // AgentEventType.Init
            details = "Workspace initialized via Director TUI",
        };
        await client.PostAsync<JsonElement>($"/mcpserver/agents/system/events?workspace={path}", eventBody, ct).ConfigureAwait(false);

        int? seeded = null;
        if (seedResult.TryGetProperty("seeded", out var seededProp) && seededProp.ValueKind == JsonValueKind.Number
            && seededProp.TryGetInt32(out var seededCount))
        {
            seeded = seededCount;
        }

        return new WorkspaceInitInfo(workspacePath, seeded);
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

    private static WorkspaceMutationOutcome MapMutationOutcome(WorkspaceMutationResult result)
        => new(
            result.Success,
            result.Error,
            result.Workspace is null ? null : MapWorkspaceDetail(result.Workspace));

    private static WorkspaceProcessState MapProcessState(WorkspaceProcessStatus result)
        => new(
            result.IsRunning,
            result.Pid,
            result.Uptime,
            result.Port,
            result.Error);

    private McpHttpClient ResolveHealthClient(string workspacePath)
    {
        if (!string.IsNullOrWhiteSpace(_context.ActiveWorkspacePath) &&
            string.Equals(_context.ActiveWorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase) &&
            _context.HasActiveWorkspaceConnection)
        {
            return _context.GetRequiredActiveWorkspaceHttpClient();
        }

        if (_context.HasControlConnection)
            return _context.GetRequiredControlHttpClient();

        return _context.GetRequiredActiveWorkspaceHttpClient();
    }

    private static string EncodeWorkspaceKey(string path)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(path.Trim());
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

}
