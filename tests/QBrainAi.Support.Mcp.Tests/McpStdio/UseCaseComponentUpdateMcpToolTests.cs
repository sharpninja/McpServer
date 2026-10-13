using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.McpStdio;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Services.AgentHelp;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.UseCases;
using QBrainAi.Support.Mcp.UseCases.Commands;
using QBrainAi.Support.Mcp.UseCases.Models;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-USECASE-021: verifies the supported MCP tools dispatch full existing-id component updates.
/// </summary>
public sealed class UseCaseComponentUpdateMcpToolTests : IDisposable
{
    private const string Workspace = @"Q:\__mcp_unit_test__\usecase-component-update-tool";
    private readonly McpDbContext _db;
    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
    private readonly FwhMcpTools _tools;

    /// <summary>Creates a tool host with a substituted CQRS dispatcher.</summary>
    public UseCaseComponentUpdateMcpToolTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"UseCaseComponentUpdateMcpToolTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _tools = CreateTools(_db, _dispatcher);
    }

    /// <summary>Actor, flow, and step tools preserve route identities and every editable field.</summary>
    [Fact]
    public async Task UseCaseComponentUpdateTools_DispatchFullReplacementCommands()
    {
        var actor = new UseCaseActorDto
        {
            UseCaseId = 125,
            ActorId = 20,
            Name = "LAB-OMARCHY Tentacle",
            Description = "Octopus target",
            Type = "System",
            IsPrimary = true,
        };
        var flow = new UseCaseFlowDto
        {
            UseCaseId = 125,
            FlowId = 314,
            FlowType = "Basic",
            Name = "Octopus-owned deployment",
            SequenceNumber = 1,
        };
        var step = new UseCaseStepDto
        {
            FlowId = 314,
            StepId = 383,
            StepNumber = 4,
            ActorId = 20,
            Action = "Run Nuke",
            SystemResponse = "Deployment succeeds",
            DataEntities = "Release",
        };
        _dispatcher.SendAsync(Arg.Any<UpdateUseCaseActorCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseActorDto>.Success(actor));
        _dispatcher.SendAsync(Arg.Any<UpdateUseCaseFlowCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseFlowDto>.Success(flow));
        _dispatcher.SendAsync(Arg.Any<UpdateUseCaseStepCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseStepDto>.Success(step));

        var actorJson = await _tools.UseCaseUpdateActor(
            Workspace,
            125,
            20,
            "LAB-OMARCHY Tentacle",
            "System",
            true,
            "Octopus target",
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var flowJson = await _tools.UseCaseUpdateFlow(
            Workspace,
            125,
            314,
            "Basic",
            1,
            "Octopus-owned deployment",
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var stepJson = await _tools.UseCaseUpdateStep(
            Workspace,
            125,
            314,
            383,
            4,
            "Run Nuke",
            20,
            "Deployment succeeds",
            "Release",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        using (var actorDocument = JsonDocument.Parse(actorJson))
        {
            Assert.Equal(20, actorDocument.RootElement.GetProperty("actorId").GetInt64());
            Assert.Equal(125, actorDocument.RootElement.GetProperty("useCaseId").GetInt64());
        }

        using (var flowDocument = JsonDocument.Parse(flowJson))
        {
            Assert.Equal(314, flowDocument.RootElement.GetProperty("flowId").GetInt64());
            Assert.Equal(125, flowDocument.RootElement.GetProperty("useCaseId").GetInt64());
        }

        using (var stepDocument = JsonDocument.Parse(stepJson))
        {
            Assert.Equal(383, stepDocument.RootElement.GetProperty("stepId").GetInt64());
            Assert.Equal(314, stepDocument.RootElement.GetProperty("flowId").GetInt64());
        }

        await _dispatcher.Received(1).SendAsync(
            Arg.Is<UpdateUseCaseActorCommand>(command =>
                command != null &&
                command.WorkspacePath == Workspace &&
                command.UseCaseId == 125 &&
                command.ActorId == 20 &&
                command.Request.Name == "LAB-OMARCHY Tentacle" &&
                command.Request.Description == "Octopus target" &&
                command.Request.Type == "System" &&
                command.Request.IsPrimary),
            Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).SendAsync(
            Arg.Is<UpdateUseCaseFlowCommand>(command =>
                command != null &&
                command.WorkspacePath == Workspace &&
                command.UseCaseId == 125 &&
                command.FlowId == 314 &&
                command.Request.FlowType == "Basic" &&
                command.Request.Name == "Octopus-owned deployment" &&
                command.Request.SequenceNumber == 1),
            Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).SendAsync(
            Arg.Is<UpdateUseCaseStepCommand>(command =>
                command != null &&
                command.WorkspacePath == Workspace &&
                command.UseCaseId == 125 &&
                command.FlowId == 314 &&
                command.StepId == 383 &&
                command.Request.StepNumber == 4 &&
                command.Request.ActorId == 20 &&
                command.Request.Action == "Run Nuke" &&
                command.Request.SystemResponse == "Deployment succeeds" &&
                command.Request.DataEntities == "Release"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Classified handler failures remain classified on the MCP surface.</summary>
    [Fact]
    public async Task UseCaseComponentUpdateTools_ReturnClassifiedFailureEnvelopes()
    {
        _dispatcher.SendAsync(Arg.Any<UpdateUseCaseActorCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseActorDto>.Failure(
                UseCaseResultCodes.NotFoundMsg("Actor '99' was not found on use case '125'.")));
        _dispatcher.SendAsync(Arg.Any<UpdateUseCaseFlowCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseFlowDto>.Failure(
                UseCaseResultCodes.ValidationMsg("SequenceNumber must be positive.")));

        var notFoundJson = await _tools.UseCaseUpdateActor(
            Workspace,
            125,
            99,
            "missing",
            "System",
            false,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var validationJson = await _tools.UseCaseUpdateFlow(
            Workspace,
            125,
            314,
            "Basic",
            0,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        using (var notFound = JsonDocument.Parse(notFoundJson))
        {
            Assert.Equal("not_found", notFound.RootElement.GetProperty("code").GetString());
            Assert.Equal("not_found", notFound.RootElement.GetProperty("error").GetString());
            Assert.False(notFound.RootElement.GetProperty("retryable").GetBoolean());
            Assert.Contains("was not found", notFound.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
        }

        using var validation = JsonDocument.Parse(validationJson);
        Assert.Equal("validation_error", validation.RootElement.GetProperty("code").GetString());
        Assert.Equal("validation_error", validation.RootElement.GetProperty("error").GetString());
        Assert.False(validation.RootElement.GetProperty("retryable").GetBoolean());
        Assert.Contains("must be positive", validation.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
    }


    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    private static FwhMcpTools CreateTools(McpDbContext db, IDispatcher dispatcher)
    {
        var workspaceContext = new WorkspaceContext { WorkspacePath = Workspace };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext
        {
            RequestServices = Substitute.For<IServiceProvider>(),
        });
        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions
        {
            RepoRoot = TestWorkspacePaths.UnusedRepoRoot,
        });
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var chunker = new Chunker();
        var coordinator = new IngestionCoordinator(
            db,
            new RepoIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<RepoIngestor>.Instance),
            new SessionLogIngestor(
                chunker,
                ingestionOptions,
                workspaceContext,
                Substitute.For<ISessionLogService>(),
                NullLogger<SessionLogIngestor>.Instance),
            new ExternalDocsIngestor(
                chunker,
                ingestionOptions,
                workspaceContext,
                NullLogger<ExternalDocsIngestor>.Instance),
            new GitHubIngestor(chunker, gitHubCliService, NullLogger<GitHubIngestor>.Instance),
            new IssueIngestor(chunker, gitHubCliService, NullLogger<IssueIngestor>.Instance),
            Substitute.For<IWebsiteIngestor>(),
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<IVectorIndexService>(),
            null,
            workspaceContext,
            NullLogger<IngestionCoordinator>.Instance);
        var todoService = Substitute.For<ITodoService>();
        var todoServiceResolver = new TodoServiceResolver(
            todoService,
            ingestionOptions,
            Substitute.For<ITodoServiceFactory>());
        var workspaceAccessor = new WorkspaceServiceAccessor(
            todoServiceResolver,
            httpContextAccessor,
            ingestionOptions);
        var desktopLaunchService = new DesktopLaunchService(
            Substitute.For<IConfiguration>(),
            MsOptions.Options.Create(new DesktopLaunchOptions()),
            Substitute.For<IProcessRunner>(),
            NullLogger<DesktopLaunchService>.Instance);

        return new FwhMcpTools(
            db,
            Substitute.For<IRepoFileService>(),
            coordinator,
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IContextSearchService>(),
            Substitute.For<IGraphRagService>(),
            workspaceAccessor,
            Substitute.For<ITodoPromptService>(),
            Substitute.For<ISessionLogService>(),
            Substitute.For<IMemoryService>(),
            gitHubCliService,
            Substitute.For<IRequirementsDocumentService>(),
            desktopLaunchService,
            httpContextAccessor,
            workspaceContext,
            Substitute.For<IWorkspaceService>(),
            Substitute.For<IWorkspacePolicyService>(),
            todoServiceResolver,
            new TodoCreationService(workspaceAccessor, gitHubCliService, NullLogger<TodoCreationService>.Instance),
            new TodoUpdateService(workspaceAccessor, null, NullLogger<TodoUpdateService>.Instance),
            Substitute.For<ITodoExecutionService>(),
            Substitute.For<IPromptTemplateService>(),
            NullLogger<FwhMcpTools>.Instance,
            agentHelpService: Substitute.For<IAgentHelpConversationService>(),
            dispatcher: dispatcher);
    }
}
