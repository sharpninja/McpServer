namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Options for the <c>/pair</c> web login flow and API key management.
/// Bound from <c>Mcp</c> configuration section.
/// </summary>
public sealed class PairingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp";

    /// <summary>The server API key. When non-empty, mutating endpoints require this key.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Users permitted to authenticate at <c>/pair</c> to view the API key.
    /// Empty list disables the pairing page.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Options binding")]
    public List<PairingUser> PairingUsers { get; set; } = [];
}
