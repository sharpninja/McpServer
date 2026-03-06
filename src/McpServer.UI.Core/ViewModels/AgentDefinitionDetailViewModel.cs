using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for global agent definition detail and mutations.
/// </summary>
[ViewModelCommand("agent-definition-detail", Description = "Get/update a global agent definition")]
public sealed class AgentDefinitionDetailViewModel : AreaDetailViewModelBase<AgentDefinitionDetail>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<AgentDefinitionDetailViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentDefinitionDetailViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public AgentDefinitionDetailViewModel(
        Dispatcher dispatcher,
        ILogger<AgentDefinitionDetailViewModel> logger)
        : base(McpArea.Agents)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Loads a global agent definition by ID.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The loaded detail, if found.</returns>
    public async Task<AgentDefinitionDetail?> LoadAsync(string agentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            ErrorMessage = "Agent ID is required.";
            return null;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Loading definition '{agentId}'...";
        try
        {
            var result = await _dispatcher.QueryAsync(new GetAgentDefinitionQuery(agentId), ct).ConfigureAwait(true);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to load definition.";
                StatusMessage = "Definition load failed.";
                return null;
            }

            Detail = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is null
                ? $"Definition '{agentId}' was not found."
                : $"Loaded definition '{agentId}'.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Definition load failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Creates or updates a global definition.
    /// </summary>
    /// <param name="command">Upsert command payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mutation outcome, if dispatch succeeded.</returns>
    public async Task<AgentMutationOutcome?> UpsertAsync(UpsertAgentDefinitionCommand command, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Saving definition '{command.Id}'...";
        try
        {
            var result = await _dispatcher.SendAsync(command, ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to save definition.";
                StatusMessage = "Definition save failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.Error ?? "Definition save failed.";
                StatusMessage = "Definition save failed.";
                return result.Value;
            }

            await LoadAsync(command.Id, ct).ConfigureAwait(true);
            StatusMessage = $"Saved definition '{command.Id}'.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Definition save failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Creates a basic definition using the ID for display and launch defaults.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mutation outcome, if dispatch succeeded.</returns>
    public Task<AgentMutationOutcome?> CreateBasicAsync(string agentId, CancellationToken ct = default)
    {
        var normalized = (agentId ?? string.Empty).Trim();
        return UpsertAsync(new UpsertAgentDefinitionCommand
        {
            Id = normalized,
            DisplayName = normalized,
            DefaultLaunchCommand = string.Empty,
            DefaultInstructionFile = string.Empty,
            DefaultModels = [],
            DefaultBranchStrategy = "feature/{agent}/{task}",
            DefaultSeedPrompt = string.Empty
        }, ct);
    }

    /// <summary>
    /// Deletes a global definition by ID.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mutation outcome, if dispatch succeeded.</returns>
    public async Task<AgentMutationOutcome?> DeleteAsync(string agentId, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Deleting definition '{agentId}'...";
        try
        {
            var result = await _dispatcher.SendAsync(new DeleteAgentDefinitionCommand(agentId), ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to delete definition.";
                StatusMessage = "Definition delete failed.";
                return null;
            }

            if (result.Value.Success && Detail is not null &&
                string.Equals(Detail.Id, agentId, StringComparison.OrdinalIgnoreCase))
            {
                Detail = null;
            }

            StatusMessage = result.Value.Success
                ? $"Deleted definition '{agentId}'."
                : (result.Value.Error ?? "Definition delete failed.");
            if (!result.Value.Success)
                ErrorMessage = result.Value.Error;
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Definition delete failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Seeds built-in definitions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Seed result on success, otherwise null.</returns>
    public async Task<AgentSeedOutcome?> SeedDefaultsAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Seeding built-in definitions...";
        try
        {
            var result = await _dispatcher.SendAsync(new SeedAgentDefaultsCommand(), ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to seed definitions.";
                StatusMessage = "Seed failed.";
                return null;
            }

            StatusMessage = $"Seeded {result.Value.Seeded} definitions.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Seed failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
