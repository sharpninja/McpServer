using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="IRequirementsApiClient"/> backed by <see cref="McpServerClient"/>.
/// </summary>
internal sealed class RequirementsApiClientAdapter : IRequirementsApiClient
{
    private readonly DirectorMcpContext _context;
    private readonly ILogger<RequirementsApiClientAdapter> _logger;

    public RequirementsApiClientAdapter(
        DirectorMcpContext context,
        ILogger<RequirementsApiClientAdapter>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<RequirementsApiClientAdapter>.Instance;
    }

    public async Task<FunctionalRequirementListResult> ListFunctionalRequirementsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var items = await client.Requirements.ListFrAsync(cancellationToken).ConfigureAwait(false);
        return new FunctionalRequirementListResult(items.Select(MapFr).ToList());
    }

    public async Task<FunctionalRequirementItem?> GetFunctionalRequirementAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
            var item = await client.Requirements.GetFrAsync(id, cancellationToken).ConfigureAwait(false);
            return MapFr(item);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<FunctionalRequirementItem> CreateFunctionalRequirementAsync(CreateFunctionalRequirementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var item = await client.Requirements.CreateFrAsync(
            new CreateFrRequest { Id = command.Id, Title = command.Title, Body = command.Body },
            cancellationToken).ConfigureAwait(false);
        return MapFr(item);
    }

    public async Task<FunctionalRequirementItem> UpdateFunctionalRequirementAsync(UpdateFunctionalRequirementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var item = await client.Requirements.UpdateFrAsync(
            command.Id,
            new UpdateFrRequest { Title = command.Title, Body = command.Body },
            cancellationToken).ConfigureAwait(false);
        return MapFr(item);
    }

    public async Task<RequirementsMutationOutcome> DeleteFunctionalRequirementAsync(DeleteFunctionalRequirementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Requirements.DeleteFrAsync(command.Id, cancellationToken).ConfigureAwait(false);
        return new RequirementsMutationOutcome(result.Success, result.Error);
    }

    public async Task<TechnicalRequirementListResult> ListTechnicalRequirementsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var items = await client.Requirements.ListTrAsync(cancellationToken).ConfigureAwait(false);
        return new TechnicalRequirementListResult(items.Select(MapTr).ToList());
    }

    public async Task<TechnicalRequirementItem?> GetTechnicalRequirementAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
            var item = await client.Requirements.GetTrAsync(id, cancellationToken).ConfigureAwait(false);
            return MapTr(item);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<TechnicalRequirementItem> CreateTechnicalRequirementAsync(CreateTechnicalRequirementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var item = await client.Requirements.CreateTrAsync(
            new CreateTrRequest { Id = command.Id, Title = command.Title, Body = command.Body },
            cancellationToken).ConfigureAwait(false);
        return MapTr(item);
    }

    public async Task<TechnicalRequirementItem> UpdateTechnicalRequirementAsync(UpdateTechnicalRequirementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var item = await client.Requirements.UpdateTrAsync(
            command.Id,
            new UpdateTrRequest { Title = command.Title, Body = command.Body },
            cancellationToken).ConfigureAwait(false);
        return MapTr(item);
    }

    public async Task<RequirementsMutationOutcome> DeleteTechnicalRequirementAsync(DeleteTechnicalRequirementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Requirements.DeleteTrAsync(command.Id, cancellationToken).ConfigureAwait(false);
        return new RequirementsMutationOutcome(result.Success, result.Error);
    }

    public async Task<TestingRequirementListResult> ListTestingRequirementsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var items = await client.Requirements.ListTestAsync(cancellationToken).ConfigureAwait(false);
        return new TestingRequirementListResult(items.Select(MapTest).ToList());
    }

    public async Task<TestingRequirementItem?> GetTestingRequirementAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
            var item = await client.Requirements.GetTestAsync(id, cancellationToken).ConfigureAwait(false);
            return MapTest(item);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<TestingRequirementItem> CreateTestingRequirementAsync(CreateTestingRequirementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var item = await client.Requirements.CreateTestAsync(
            new CreateTestRequest { Id = command.Id, Condition = command.Condition },
            cancellationToken).ConfigureAwait(false);
        return MapTest(item);
    }

    public async Task<TestingRequirementItem> UpdateTestingRequirementAsync(UpdateTestingRequirementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var item = await client.Requirements.UpdateTestAsync(
            command.Id,
            new UpdateTestRequest { Condition = command.Condition },
            cancellationToken).ConfigureAwait(false);
        return MapTest(item);
    }

    public async Task<RequirementsMutationOutcome> DeleteTestingRequirementAsync(DeleteTestingRequirementCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Requirements.DeleteTestAsync(command.Id, cancellationToken).ConfigureAwait(false);
        return new RequirementsMutationOutcome(result.Success, result.Error);
    }

    public async Task<RequirementMappingListResult> ListMappingsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var items = await client.Requirements.ListMappingsAsync(cancellationToken).ConfigureAwait(false);
        return new RequirementMappingListResult(items.Select(MapMapping).ToList());
    }

    public async Task<RequirementMappingItem?> GetMappingAsync(string frId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
            var item = await client.Requirements.GetMappingAsync(frId, cancellationToken).ConfigureAwait(false);
            return MapMapping(item);
        }
        catch (McpNotFoundException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return null;
        }
    }

    public async Task<RequirementMappingItem> UpsertMappingAsync(UpsertRequirementMappingCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var item = await client.Requirements.UpsertMappingAsync(
            command.FrId,
            new UpsertFrTrMappingRequest { TrIds = command.TrIds },
            cancellationToken).ConfigureAwait(false);
        return MapMapping(item);
    }

    public async Task<RequirementsMutationOutcome> DeleteMappingAsync(DeleteRequirementMappingCommand command, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Requirements.DeleteMappingAsync(command.FrId, cancellationToken).ConfigureAwait(false);
        return new RequirementsMutationOutcome(result.Success, result.Error);
    }

    public async Task<GeneratedRequirementsDocument> GenerateAsync(GenerateRequirementsDocumentQuery query, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Requirements.GenerateAsync(query.Doc, cancellationToken).ConfigureAwait(false);
        return new GeneratedRequirementsDocument(result.Content, result.ContentType);
    }

    private async Task<McpServerClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_context.HasControlConnection)
            return await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        return await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
    }

    private static FunctionalRequirementItem MapFr(FrEntry entry) => new(entry.Id, entry.Title, entry.Body);
    private static TechnicalRequirementItem MapTr(TrEntry entry) => new(entry.Id, entry.Title, entry.Body);
    private static TestingRequirementItem MapTest(TestEntry entry) => new(entry.Id, entry.Condition);
    private static RequirementMappingItem MapMapping(FrTrMapping mapping) => new(mapping.FrId, mapping.TrIds);
}
