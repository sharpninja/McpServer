using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.UseCases.Commands;
using McpServer.Support.Mcp.UseCases.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// TR-MCP-USECASE-002: DI registration for use case CQRS handlers and diagram service.
/// </summary>
public static class UseCaseServiceCollectionExtensions
{
    /// <summary>
    /// Registers use case CQRS handlers, diagram service, and related infrastructure.
    /// Call from both HTTP host (<c>Program.cs</c>) and STDIO host (<c>McpStdioHost.cs</c>).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddUseCaseCqrs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IUseCaseDiagramService, MermaidUseCaseDiagramService>();
        services.AddSingleton<IUseCaseUmlSerializationService, UseCaseUmlSerializationService>();

        services.AddTransient<ICommandHandler<CreateUseCaseCommand, UseCaseDetailDto>, CreateUseCaseCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateUseCaseCommand, UseCaseDetailDto>, UpdateUseCaseCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteUseCaseCommand, bool>, DeleteUseCaseCommandHandler>();
        services.AddTransient<ICommandHandler<AddUseCaseFlowCommand, UseCaseFlowDto>, AddUseCaseFlowCommandHandler>();
        services.AddTransient<ICommandHandler<AddUseCaseStepCommand, UseCaseStepDto>, AddUseCaseStepCommandHandler>();
        services.AddTransient<ICommandHandler<AttachUseCaseActorCommand, UseCaseActorDto>, AttachUseCaseActorCommandHandler>();
        services.AddTransient<ICommandHandler<LinkUseCaseToFrCommand, UseCaseFrLinkDto>, LinkUseCaseToFrCommandHandler>();
        services.AddTransient<ICommandHandler<UnlinkUseCaseFromFrCommand, bool>, UnlinkUseCaseFromFrCommandHandler>();
        services.AddTransient<ICommandHandler<CreateUseCaseFromFrCommand, UseCaseDetailDto>, CreateUseCaseFromFrCommandHandler>();
        services.AddTransient<ICommandHandler<SetUseCaseApprovalStatusCommand, UseCaseDetailDto>, SetUseCaseApprovalStatusCommandHandler>();
        services.AddTransient<ICommandHandler<SetUseCaseProductKeyCommand, UseCaseDetailDto>, SetUseCaseProductKeyCommandHandler>();
        services.AddTransient<ICommandHandler<PutUseCaseDiagramGraphCommand, UseCaseDiagramGraphDto>, PutUseCaseDiagramGraphCommandHandler>();

        services.AddTransient<IQueryHandler<GetUseCaseQuery, UseCaseDetailDto>, GetUseCaseQueryHandler>();
        services.AddTransient<IQueryHandler<ListUseCasesQuery, IReadOnlyList<UseCaseSummaryDto>>, ListUseCasesQueryHandler>();
        services.AddTransient<IQueryHandler<ListUseCasesByProductQuery, IReadOnlyList<UseCaseSummaryDto>>, ListUseCasesByProductQueryHandler>();
        services.AddTransient<IQueryHandler<GetUseCaseDiagramQuery, UseCaseDiagramDto>, GetUseCaseDiagramQueryHandler>();
        services.AddTransient<IQueryHandler<GetUseCaseDiagramGraphQuery, UseCaseDiagramGraphDto>, GetUseCaseDiagramGraphQueryHandler>();
        services.AddTransient<IQueryHandler<GetUseCasesForFrQuery, IReadOnlyList<LinkedUseCaseDto>>, GetUseCasesForFrQueryHandler>();
        services.AddTransient<IQueryHandler<GetUseCaseFrCoverageQuery, UseCaseFrCoverageDto>, GetUseCaseFrCoverageQueryHandler>();

        return services;
    }
}
