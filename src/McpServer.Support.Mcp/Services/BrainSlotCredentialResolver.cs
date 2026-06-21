using Microsoft.Extensions.Configuration;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-QUAD-002: Resolves brain-slot credential references without persisting raw secrets.
/// </summary>
public sealed class BrainSlotCredentialResolver : IBrainSlotCredentialResolver
{
    private readonly IConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="BrainSlotCredentialResolver"/> class.</summary>
    public BrainSlotCredentialResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<string?> ResolveAsync(string credentialReference, CancellationToken cancellationToken = default)
    {
        if (!TrySplit(credentialReference, out var scheme, out var value))
            return null;

        return scheme switch
        {
            "env" => NormalizeSecret(Environment.GetEnvironmentVariable(value)),
            "config" => NormalizeSecret(_configuration[value]),
            "file" => await ResolveFileAsync(value, cancellationToken).ConfigureAwait(false),
            _ => null,
        };
    }

    /// <inheritdoc />
    public bool IsSupportedReference(string credentialReference)
        => TrySplit(credentialReference, out var scheme, out var value)
            && value.Length > 0
            && (scheme == "env" || scheme == "config" || scheme == "file");

    private static async Task<string?> ResolveFileAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return NormalizeSecret(text);
    }

    private static string? NormalizeSecret(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TrySplit(string? reference, out string scheme, out string value)
    {
        scheme = string.Empty;
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
            return false;

        var index = reference.IndexOf(':', StringComparison.Ordinal);
        if (index <= 0 || index == reference.Length - 1)
            return false;

        scheme = reference[..index].Trim().ToLowerInvariant();
        value = reference[(index + 1)..].Trim();
        return true;
    }
}
