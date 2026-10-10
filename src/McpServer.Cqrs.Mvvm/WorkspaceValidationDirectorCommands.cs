using CommunityToolkit.Mvvm.Input;

namespace McpServer.Cqrs.Mvvm;

/// <summary>FR-MCP-HYGIENE-005: Director command name for workspace validation.</summary>
public static class WorkspaceValidationDirectorCommands
{
    /// <summary>Validate workspace hygiene.</summary>
    public const string Validate = "validate-workspace";
}

/// <summary>FR-MCP-HYGIENE-005: Director executor contract.</summary>
public interface IWorkspaceValidationDirectorExecutor
{
    /// <summary>Run read-only validation.</summary>
    Task<object?> ValidateAsync(double? staleTurnThresholdHours, bool authenticated, CancellationToken cancellationToken);
}

/// <summary>FR-MCP-HYGIENE-005: Director validate-workspace command.</summary>
[ViewModelCommand("validate-workspace", Description = "Run read-only workspace hygiene validation.")]
public sealed class WorkspaceValidationDirectorCommand
{
    private readonly IWorkspaceValidationDirectorExecutor _executor;

    /// <summary>Constructor.</summary>
    public WorkspaceValidationDirectorCommand(IWorkspaceValidationDirectorExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        PrimaryCommand = new AsyncRelayCommand(ExecuteAsync);
    }

    /// <summary>Primary command.</summary>
    public IAsyncRelayCommand PrimaryCommand { get; }

    /// <summary>Result.</summary>
    public object? Result { get; private set; }

    /// <summary>Optional stale-turn threshold hours.</summary>
    public double? StaleTurnThresholdHours { get; set; }

    /// <summary>Authenticated caller. Defaults true for Director.</summary>
    public bool Authenticated { get; set; } = true;

    private async Task ExecuteAsync(CancellationToken cancellationToken)
        => Result = await _executor.ValidateAsync(StaleTurnThresholdHours, Authenticated, cancellationToken).ConfigureAwait(false);
}
