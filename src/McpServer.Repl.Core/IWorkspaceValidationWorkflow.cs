namespace McpServer.Repl.Core;

/// <summary>FR-MCP-HYGIENE-005: REPL workspace.validate workflow.</summary>
public interface IWorkspaceValidationWorkflow
{
    /// <summary>Run read-only validation.</summary>
    Task<object> ValidateAsync(IReadOnlyDictionary<string, object?> args, CancellationToken cancellationToken);
}
