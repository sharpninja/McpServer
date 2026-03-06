using System.Threading.Tasks;

namespace McpServer.UI.Core.Commands;

/// <summary>
/// Target interface for TODO Copilot prompt commands.
/// Implemented by the ViewModel layer to handle streaming prompt generation.
/// </summary>
public interface ITodoCopilotTarget
{
    Task CopilotStatusAsync();
    Task CopilotPlanAsync();
    Task CopilotImplementAsync();
}

