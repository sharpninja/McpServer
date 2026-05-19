using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>
/// Database-backed requirements service. FR/TR/TEST rows and traceability links
/// are stored in <see cref="McpDbContext"/> and scoped by the active workspace.
/// Markdown files are used only for bootstrap import and export rendering.
/// </summary>
public sealed class RequirementsDatabaseDocumentService : IRequirementsDocumentService, IDisposable
{
    private const string FrKind = "fr";
    private const string TrKind = "tr";
    private const string TestKind = "test";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RequirementsOptions _options;
    private readonly ILogger<RequirementsDatabaseDocumentService> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IChangeEventBus? _eventBus;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>Initializes a new DB-backed requirements service.</summary>
    public RequirementsDatabaseDocumentService(
        IServiceScopeFactory scopeFactory,
        IOptions<RequirementsOptions> options,
        ILogger<RequirementsDatabaseDocumentService> logger,
        IHttpContextAccessor? httpContextAccessor = null,
        IChangeEventBus? eventBus = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor;
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public void Dispose() => _writeLock.Dispose();

    /// <inheritdoc />
    public async Task<IReadOnlyList<FrEntry>> GetAllFrAsync(CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        return await scope.Context.Requirements
            .AsNoTracking()
            .Where(x => x.Kind == FrKind)
            .OrderBy(x => x.Id)
            .Select(x => new FrEntry(x.Id, x.Title, x.Body, x.WorkspaceId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FrEntry?> GetFrAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var row = await FindRequirementAsync(scope.Context, FrKind, id, asTracking: false, ct).ConfigureAwait(false);
        return row is null ? null : new FrEntry(row.Id, row.Title, row.Body, row.WorkspaceId);
    }

    /// <inheritdoc />
    public async Task AddFrAsync(FrEntry entry, CancellationToken ct = default)
    {
        ValidateFr(entry);
        await AddRequirementAsync(FrKind, entry.Id, entry.Title, entry.Body, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateFrAsync(FrEntry entry, CancellationToken ct = default)
    {
        ValidateFr(entry);
        await UpdateRequirementAsync(FrKind, entry.Id, entry.Title, entry.Body, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteFrAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var row = await FindRequirementAsync(ctx, FrKind, id, asTracking: true, ct).ConfigureAwait(false)
                ?? throw new RequirementsNotFoundException($"FR '{id}' was not found.");
            ctx.Requirements.Remove(row);
            ctx.RequirementTraceabilityLinks.RemoveRange(ctx.RequirementTraceabilityLinks.Where(x => x.FrId == id));
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Deleted, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrEntry>> GetAllTrAsync(CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        return await scope.Context.Requirements
            .AsNoTracking()
            .Where(x => x.Kind == TrKind)
            .OrderBy(x => x.Id)
            .Select(x => new TrEntry(x.Id, x.Title, x.Body, x.WorkspaceId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TrEntry?> GetTrAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var row = await FindRequirementAsync(scope.Context, TrKind, id, asTracking: false, ct).ConfigureAwait(false);
        return row is null ? null : new TrEntry(row.Id, row.Title, row.Body, row.WorkspaceId);
    }

    /// <inheritdoc />
    public async Task AddTrAsync(TrEntry entry, CancellationToken ct = default)
    {
        ValidateTr(entry);
        await AddRequirementAsync(TrKind, entry.Id, entry.Title, entry.Body, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateTrAsync(TrEntry entry, CancellationToken ct = default)
    {
        ValidateTr(entry);
        await UpdateRequirementAsync(TrKind, entry.Id, entry.Title, entry.Body, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteTrAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await DeleteRequirementAndTargetLinksAsync(TrKind, id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TestEntry>> GetAllTestAsync(CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        return await scope.Context.Requirements
            .AsNoTracking()
            .Where(x => x.Kind == TestKind)
            .OrderBy(x => x.Id)
            .Select(x => new TestEntry(x.Id, x.Body, x.WorkspaceId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TestEntry?> GetTestAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var row = await FindRequirementAsync(scope.Context, TestKind, id, asTracking: false, ct).ConfigureAwait(false);
        return row is null ? null : new TestEntry(row.Id, row.Body, row.WorkspaceId);
    }

    /// <inheritdoc />
    public async Task AddTestAsync(TestEntry entry, CancellationToken ct = default)
    {
        ValidateTest(entry);
        await AddRequirementAsync(TestKind, entry.Id, string.Empty, entry.Condition, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateTestAsync(TestEntry entry, CancellationToken ct = default)
    {
        ValidateTest(entry);
        await UpdateRequirementAsync(TestKind, entry.Id, string.Empty, entry.Condition, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteTestAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await DeleteRequirementAndTargetLinksAsync(TestKind, id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FrTrMapping>> GetAllMappingsAsync(CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var links = await scope.Context.RequirementTraceabilityLinks
            .AsNoTracking()
            .OrderBy(x => x.FrId)
            .ThenBy(x => x.TargetKind)
            .ThenBy(x => x.TargetId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return links
            .GroupBy(x => x.FrId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new FrTrMapping(
                group.Key,
                group.Where(x => x.TargetKind == TrKind).Select(x => x.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                group.Where(x => x.TargetKind == TestKind).Select(x => x.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                group.First().WorkspaceId))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<FrTrMapping?> GetMappingAsync(string frId, CancellationToken ct = default)
    {
        ValidateId(frId, nameof(frId));
        var all = await GetAllMappingsAsync(ct).ConfigureAwait(false);
        return all.FirstOrDefault(x => IdEquals(x.FrId, frId));
    }

    /// <inheritdoc />
    public async Task UpsertMappingAsync(FrTrMapping mapping, CancellationToken ct = default)
    {
        ValidateMapping(mapping);
        var normalizedTrIds = NormalizeIds(mapping.TrIds);
        var normalizedTestIds = NormalizeIds(mapping.TestIds);

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            await ValidateMappingTargetsAsync(ctx, mapping.FrId, normalizedTrIds, normalizedTestIds, ct).ConfigureAwait(false);

            var existingLinks = await ctx.RequirementTraceabilityLinks
                .Where(x => x.FrId == mapping.FrId)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            ctx.RequirementTraceabilityLinks.RemoveRange(existingLinks);
            var now = Now();
            var workspaceId = RequireWorkspaceId(ctx);
            foreach (var trId in normalizedTrIds)
                ctx.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity { WorkspaceId = workspaceId, FrId = mapping.FrId, TargetKind = TrKind, TargetId = trId, CreatedAtUtc = now });
            foreach (var testId in normalizedTestIds)
                ctx.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity { WorkspaceId = workspaceId, FrId = mapping.FrId, TargetKind = TestKind, TargetId = testId, CreatedAtUtc = now });

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Updated, mapping.FrId, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteMappingAsync(string frId, CancellationToken ct = default)
    {
        ValidateId(frId, nameof(frId));
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var links = await ctx.RequirementTraceabilityLinks.Where(x => x.FrId == frId).ToListAsync(ct).ConfigureAwait(false);
            if (links.Count == 0)
                throw new RequirementsNotFoundException($"Mapping row '{frId}' was not found.");
            ctx.RequirementTraceabilityLinks.RemoveRange(links);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Deleted, frId, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<(string Content, string MimeType)> GenerateDocumentAsync(RequirementsDocType docType, CancellationToken ct = default)
    {
        return docType switch
        {
            RequirementsDocType.Functional => (RequirementsDocumentRenderer.RenderFunctional(await GetAllFrAsync(ct).ConfigureAwait(false)), "text/markdown"),
            RequirementsDocType.Technical => (RequirementsDocumentRenderer.RenderTechnical(await GetAllTrAsync(ct).ConfigureAwait(false)), "text/markdown"),
            RequirementsDocType.Testing => (RequirementsDocumentRenderer.RenderTesting(await GetAllTestAsync(ct).ConfigureAwait(false)), "text/markdown"),
            RequirementsDocType.Mapping => (RequirementsDocumentRenderer.RenderMapping(await GetAllMappingsAsync(ct).ConfigureAwait(false)), "text/markdown"),
            RequirementsDocType.Matrix => (RequirementsDocumentRenderer.RenderMatrix(
                await GetAllFrAsync(ct).ConfigureAwait(false),
                await GetAllTrAsync(ct).ConfigureAwait(false),
                await GetAllTestAsync(ct).ConfigureAwait(false),
                ReadExistingMatrixForExport(null)), "text/markdown"),
            RequirementsDocType.All => throw new ArgumentOutOfRangeException(nameof(docType), "Use GenerateAllAsync for docType=All."),
            _ => throw new ArgumentOutOfRangeException(nameof(docType), docType, "Unknown requirements document type.")
        };
    }

    /// <inheritdoc />
    public async Task<RequirementsDocumentExportResult> GenerateAllAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default)
    {
        var fr = await GetAllFrAsync(ct).ConfigureAwait(false);
        var tr = await GetAllTrAsync(ct).ConfigureAwait(false);
        var test = await GetAllTestAsync(ct).ConfigureAwait(false);
        var mapping = await GetAllMappingsAsync(ct).ConfigureAwait(false);

        var generated = (generatedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var documents = RequirementsWikiDocumentRenderer.RenderCanonicalFiles(fr, tr, test, mapping, ReadExistingMatrixForExport(outputRootPath));
        return await RequirementsDocumentExportWriter.WriteAsync(
            outputRootPath,
            "markdown",
            "all",
            generated,
            documents,
            ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RequirementsDocumentExportResult> GenerateWikiAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default)
    {
        var fr = await GetAllFrAsync(ct).ConfigureAwait(false);
        var tr = await GetAllTrAsync(ct).ConfigureAwait(false);
        var test = await GetAllTestAsync(ct).ConfigureAwait(false);
        var mapping = await GetAllMappingsAsync(ct).ConfigureAwait(false);

        var generated = (generatedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var documents = RequirementsWikiDocumentRenderer.RenderWikiFiles(fr, tr, test, mapping, generated, ReadExistingMatrixForWikiExport(outputRootPath));
        return await RequirementsDocumentExportWriter.WriteAsync(
            outputRootPath,
            "wiki",
            "all",
            generated,
            documents,
            [RequirementsWikiDocumentRenderer.AzureFolder, RequirementsWikiDocumentRenderer.GitHubFolder],
            ct).ConfigureAwait(false);
    }

    private async Task AddRequirementAsync(string kind, string id, string title, string body, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            if (await ctx.Requirements.AnyAsync(x => x.Kind == kind && x.Id == id, ct).ConfigureAwait(false))
                throw new RequirementsConflictException($"{kind.ToUpperInvariant()} '{id}' already exists.");

            var now = Now();
            ctx.Requirements.Add(new RequirementEntity { WorkspaceId = RequireWorkspaceId(ctx), Kind = kind, Id = id, Title = title, Body = body, CreatedAtUtc = now, UpdatedAtUtc = now });
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Created, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task UpdateRequirementAsync(string kind, string id, string title, string body, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var row = await FindRequirementAsync(ctx, kind, id, asTracking: true, ct).ConfigureAwait(false)
                ?? throw new RequirementsNotFoundException($"{kind.ToUpperInvariant()} '{id}' was not found.");
            row.Title = title;
            row.Body = body;
            row.UpdatedAtUtc = Now();
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Updated, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task DeleteRequirementAndTargetLinksAsync(string kind, string id, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var row = await FindRequirementAsync(ctx, kind, id, asTracking: true, ct).ConfigureAwait(false)
                ?? throw new RequirementsNotFoundException($"{kind.ToUpperInvariant()} '{id}' was not found.");
            ctx.Requirements.Remove(row);
            ctx.RequirementTraceabilityLinks.RemoveRange(ctx.RequirementTraceabilityLinks.Where(x => x.TargetKind == kind && x.TargetId == id));
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Deleted, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ValidateMappingTargetsAsync(
        McpDbContext ctx,
        string frId,
        IReadOnlyList<string> trIds,
        IReadOnlyList<string> testIds,
        CancellationToken ct)
    {
        var workspaceId = RequireWorkspaceId(ctx);
        var requirements = ctx.Requirements.IgnoreQueryFilters().Where(x => x.WorkspaceId == workspaceId);

        if (!await requirements.AnyAsync(x => x.Kind == FrKind && x.Id == frId, ct).ConfigureAwait(false))
            throw new ArgumentException($"FR '{frId}' does not exist.", nameof(frId));

        foreach (var trId in trIds)
        {
            if (!await requirements.AnyAsync(x => x.Kind == TrKind && x.Id == trId, ct).ConfigureAwait(false))
                throw new ArgumentException($"TR '{trId}' does not exist.", nameof(trIds));
        }

        foreach (var testId in testIds)
        {
            if (!await requirements.AnyAsync(x => x.Kind == TestKind && x.Id == testId, ct).ConfigureAwait(false))
                throw new ArgumentException($"TEST '{testId}' does not exist.", nameof(testIds));
        }
    }

    private async Task<RequirementEntity?> FindRequirementAsync(McpDbContext ctx, string kind, string id, bool asTracking, CancellationToken ct)
    {
        var query = asTracking ? ctx.Requirements : ctx.Requirements.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Kind == kind && x.Id == id, ct).ConfigureAwait(false);
    }

    private async Task EnsureBootstrappedAsync(McpDbContext ctx, CancellationToken ct)
    {
        if (await ctx.Requirements.AnyAsync(ct).ConfigureAwait(false))
            return;

        var paths = ResolveDocumentPaths(ctx.CurrentWorkspaceId);
        if (!File.Exists(paths.Functional) && !File.Exists(paths.Technical) && !File.Exists(paths.Testing) && !File.Exists(paths.Mapping))
            return;

        var now = Now();
        var workspaceId = RequireWorkspaceId(ctx);
        var staleLinks = await ctx.RequirementTraceabilityLinks.ToListAsync(ct).ConfigureAwait(false);
        if (staleLinks.Count > 0)
        {
            ctx.RequirementTraceabilityLinks.RemoveRange(staleLinks);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var importedRequirements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in RequirementsDocumentParser.ParseFunctional(ReadFileIfExists(paths.Functional)))
        {
            if (!importedRequirements.Add($"{FrKind}\0{entry.Id}"))
                continue;
            ctx.Requirements.Add(new RequirementEntity { WorkspaceId = workspaceId, Kind = FrKind, Id = entry.Id, Title = entry.Title, Body = entry.Body, CreatedAtUtc = now, UpdatedAtUtc = now });
        }
        foreach (var entry in RequirementsDocumentParser.ParseTechnical(ReadFileIfExists(paths.Technical)))
        {
            if (!importedRequirements.Add($"{TrKind}\0{entry.Id}"))
                continue;
            ctx.Requirements.Add(new RequirementEntity { WorkspaceId = workspaceId, Kind = TrKind, Id = entry.Id, Title = entry.Title, Body = entry.Body, CreatedAtUtc = now, UpdatedAtUtc = now });
        }
        foreach (var entry in RequirementsDocumentParser.ParseTesting(ReadFileIfExists(paths.Testing)))
        {
            if (!importedRequirements.Add($"{TestKind}\0{entry.Id}"))
                continue;
            ctx.Requirements.Add(new RequirementEntity { WorkspaceId = workspaceId, Kind = TestKind, Id = entry.Id, Title = string.Empty, Body = entry.Condition, CreatedAtUtc = now, UpdatedAtUtc = now });
        }

        var importedLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in RequirementsDocumentParser.ParseMapping(ReadFileIfExists(paths.Mapping)))
        {
            if (!importedRequirements.Contains($"{FrKind}\0{mapping.FrId}"))
                continue;

            foreach (var trId in NormalizeIds(mapping.TrIds))
            {
                if (!importedRequirements.Contains($"{TrKind}\0{trId}") || !importedLinks.Add($"{mapping.FrId}\0{TrKind}\0{trId}"))
                    continue;
                ctx.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity { WorkspaceId = workspaceId, FrId = mapping.FrId, TargetKind = TrKind, TargetId = trId, CreatedAtUtc = now });
            }

            foreach (var testId in NormalizeIds(mapping.TestIds))
            {
                if (!importedRequirements.Contains($"{TestKind}\0{testId}") || !importedLinks.Add($"{mapping.FrId}\0{TestKind}\0{testId}"))
                    continue;
                ctx.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity { WorkspaceId = workspaceId, FrId = mapping.FrId, TargetKind = TestKind, TargetId = testId, CreatedAtUtc = now });
            }
        }

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private DbScope CreateScope()
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var requestCtx = _httpContextAccessor?.HttpContext?.RequestServices.GetService<WorkspaceContext>();
        var workspacePath = requestCtx?.WorkspacePath;
        if (string.IsNullOrWhiteSpace(workspacePath))
            workspacePath = TryInferWorkspacePathFromOptions();
        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            workspacePath = Path.GetFullPath(workspacePath);
            var scopedWorkspace = scope.ServiceProvider.GetService<WorkspaceContext>();
            if (scopedWorkspace is not null)
            {
                scopedWorkspace.WorkspacePath = workspacePath;
                scopedWorkspace.WorkspaceName = requestCtx?.WorkspaceName ?? Path.GetFileName(workspacePath);
            }
        }

        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        if (!string.IsNullOrWhiteSpace(workspacePath))
            ctx.OverrideWorkspaceId(Path.GetFullPath(workspacePath));

        return new DbScope(scope, ctx);
    }

    private RequirementDocumentPaths ResolveDocumentPaths(string workspaceId)
    {
        if (!string.IsNullOrWhiteSpace(workspaceId))
        {
            var projectDir = Path.Combine(workspaceId, "docs", "Project");
            return new RequirementDocumentPaths(
                Path.Combine(projectDir, RequirementsDocumentRenderer.FunctionalFileName),
                Path.Combine(projectDir, RequirementsDocumentRenderer.TechnicalFileName),
                Path.Combine(projectDir, RequirementsDocumentRenderer.TestingFileName),
                Path.Combine(projectDir, RequirementsDocumentRenderer.MappingFileName),
                Path.Combine(projectDir, RequirementsDocumentRenderer.MatrixFileName));
        }

        return new RequirementDocumentPaths(
            _options.FunctionalRequirementsPath,
            _options.TechnicalRequirementsPath,
            _options.TestingRequirementsPath,
            _options.MappingPath,
            _options.MatrixPath);
    }

    private string? ReadExistingMatrixForExport(string? outputRootPath)
    {
        if (!string.IsNullOrWhiteSpace(outputRootPath))
        {
            var outputMatrix = Path.Combine(outputRootPath, RequirementsDocumentRenderer.MatrixFileName);
            var outputMatrixMarkdown = ReadFileIfExists(outputMatrix);
            if (outputMatrixMarkdown is not null)
                return outputMatrixMarkdown;
        }

        var workspacePath = TryGetRequestWorkspacePath();
        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            var workspaceMatrix = Path.Combine(workspacePath, "docs", "Project", RequirementsDocumentRenderer.MatrixFileName);
            var workspaceMatrixMarkdown = ReadFileIfExists(workspaceMatrix);
            if (workspaceMatrixMarkdown is not null)
                return workspaceMatrixMarkdown;
        }

        return ReadFileIfExists(_options.MatrixPath);
    }

    private string? ReadExistingMatrixForWikiExport(string outputRootPath)
    {
        var projectRoot = Directory.GetParent(Path.GetFullPath(outputRootPath))?.FullName;
        if (!string.IsNullOrWhiteSpace(projectRoot))
        {
            var projectMatrix = Path.Combine(projectRoot, RequirementsDocumentRenderer.MatrixFileName);
            var projectMatrixMarkdown = ReadFileIfExists(projectMatrix);
            if (projectMatrixMarkdown is not null)
                return projectMatrixMarkdown;
        }

        return ReadExistingMatrixForExport(null);
    }

    private string? TryInferWorkspacePathFromOptions()
    {
        var functional = _options.FunctionalRequirementsPath;
        if (string.IsNullOrWhiteSpace(functional))
            return null;
        var projectDir = Path.GetDirectoryName(Path.GetFullPath(functional));
        var docsDir = projectDir is null ? null : Directory.GetParent(projectDir)?.FullName;
        return docsDir is null ? null : Directory.GetParent(docsDir)?.FullName;
    }

    private string? TryGetRequestWorkspacePath()
    {
        var requestCtx = _httpContextAccessor?.HttpContext?.RequestServices.GetService<WorkspaceContext>();
        var workspacePath = requestCtx?.WorkspacePath;
        if (string.IsNullOrWhiteSpace(workspacePath))
            workspacePath = TryInferWorkspacePathFromOptions();
        return string.IsNullOrWhiteSpace(workspacePath) ? null : Path.GetFullPath(workspacePath);
    }

    private static IReadOnlyList<string> NormalizeIds(IEnumerable<string>? ids) =>
        ids is null
            ? []
            : ids.Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

    private static string? ReadFileIfExists(string? path) =>
        string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? null : File.ReadAllText(path);

    private static void ValidateFr(FrEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id));
        if (string.IsNullOrWhiteSpace(entry.Title))
            throw new ArgumentException("FR title is required.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Body))
            throw new ArgumentException("FR body is required.", nameof(entry));
    }

    private static void ValidateTr(TrEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id));
        if (string.IsNullOrWhiteSpace(entry.Body))
            throw new ArgumentException("TR body is required.", nameof(entry));
    }

    private static void ValidateTest(TestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id));
        if (string.IsNullOrWhiteSpace(entry.Condition))
            throw new ArgumentException("TEST condition is required.", nameof(entry));
    }

    private static void ValidateMapping(FrTrMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ValidateId(mapping.FrId, nameof(mapping.FrId));
    }

    private static void ValidateId(string id, string paramName)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID is required.", paramName);
    }

    private static bool IdEquals(string left, string right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string RequireWorkspaceId(McpDbContext ctx) =>
        string.IsNullOrWhiteSpace(ctx.CurrentWorkspaceId)
            ? throw new InvalidOperationException("Requirements operations require a resolved workspace.")
            : ctx.CurrentWorkspaceId;

    private static string Now() => DateTime.UtcNow.ToString("O");

    private async Task PublishRequirementsChangeSafeAsync(string action, string entityId, CancellationToken ct)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.Requirements,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/requirements/{entityId}",
                },
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing requirements change event for {EntityId}", entityId);
        }
    }

    private readonly record struct RequirementDocumentPaths(string Functional, string Technical, string Testing, string Mapping, string Matrix);

    private readonly struct DbScope : IAsyncDisposable
    {
        private readonly AsyncServiceScope _scope;

        public DbScope(AsyncServiceScope scope, McpDbContext context)
        {
            _scope = scope;
            Context = context;
        }

        public McpDbContext Context { get; }

        public ValueTask DisposeAsync() => _scope.DisposeAsync();
    }
}
