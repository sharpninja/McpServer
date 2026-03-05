namespace McpServer.Cqrs.Mvvm;

/// <summary>
/// TR-MCP-DIR-003: Marks a ViewModel class with a CLI alias for the Director <c>exec</c> command.
/// The <see cref="IViewModelRegistry"/> discovers ViewModels decorated with this attribute.
/// </summary>
/// <param name="alias">The CLI alias used in <c>director exec {alias}</c>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ViewModelCommandAttribute(string alias) : Attribute
{
    /// <summary>The CLI alias for this ViewModel (e.g. "login", "ban-agent", "list-workspaces").</summary>
    public string Alias { get; } = alias;

    /// <summary>Optional description shown in CLI help.</summary>
    public string? Description { get; init; }
}
