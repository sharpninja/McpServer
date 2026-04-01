using System.Threading;
using System.Threading.Tasks;
using McpServer.Repl.Core;

namespace McpServer.Repl.Core.Tests;

public static class RequirementsWorkflowTestExtensions
{
    public static Task<RequirementDto> CreateRequirementAsync(
        this IRequirementsWorkflow workflow,
        CreateRequirementRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new RequirementDto());
    }

    public static Task<RequirementQueryResult> QueryRequirementsAsync(
        this IRequirementsWorkflow workflow,
        string? category,
        string? area = null,
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new RequirementQueryResult());
    }

    public static Task<RequirementDto> UpdateRequirementAsync(
        this IRequirementsWorkflow workflow,
        string requirementId,
        UpdateRequirementRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new RequirementDto());
    }
}
