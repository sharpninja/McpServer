namespace McpServer.UI.Core.Models;

/// <summary>
/// Prompt template with display name and prompt content.
/// </summary>
public sealed class PromptTemplate
{
    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Prompt template body.</summary>
    public string Template { get; set; } = string.Empty;
}
