using System.Threading.Tasks;
using McpServer.Cqrs;

namespace McpServer.UI.Core.Commands;

// --- Copilot Status Prompt ---

public sealed record CopilotStatusCommand() : ICommand<bool>;

public sealed class CopilotStatusHandler(IUiDispatchTarget dispatch, ITodoCopilotTarget target) : ICommandHandler<CopilotStatusCommand, bool>
{
    public Task<Result<bool>> HandleAsync(CopilotStatusCommand command, CallContext context)
    {
        dispatch.TrackBackgroundWork(target.CopilotStatusAsync());
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Copilot Plan Prompt ---

public sealed record CopilotPlanCommand() : ICommand<bool>;

public sealed class CopilotPlanHandler(IUiDispatchTarget dispatch, ITodoCopilotTarget target) : ICommandHandler<CopilotPlanCommand, bool>
{
    public Task<Result<bool>> HandleAsync(CopilotPlanCommand command, CallContext context)
    {
        dispatch.TrackBackgroundWork(target.CopilotPlanAsync());
        return Task.FromResult(Result<bool>.Success(true));
    }
}

// --- Copilot Implement Prompt ---

public sealed record CopilotImplementCommand() : ICommand<bool>;

public sealed class CopilotImplementHandler(IUiDispatchTarget dispatch, ITodoCopilotTarget target) : ICommandHandler<CopilotImplementCommand, bool>
{
    public Task<Result<bool>> HandleAsync(CopilotImplementCommand command, CallContext context)
    {
        dispatch.TrackBackgroundWork(target.CopilotImplementAsync());
        return Task.FromResult(Result<bool>.Success(true));
    }
}

