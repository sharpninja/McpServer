using System.Net;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-QUAD-001 and TR-MCP-QUAD-002: Shared validation helpers for brain-slot services.
/// </summary>
internal static class BrainSlotValidation
{
    private static readonly HashSet<string> ProviderKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "OpenAI",
        "OpenAICompatible",
    };

    /// <summary>Normalizes a role or throws for unknown values.</summary>
    public static string NormalizeRole(string role)
    {
        var match = BrainSlotRoles.All.FirstOrDefault(item => string.Equals(item, role?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
            throw new BrainSlotValidationException("role must be one of: " + string.Join(", ", BrainSlotRoles.All) + ".", BrainSlotReasonCodes.InvalidRole);
        return match;
    }

    /// <summary>Normalizes a provider kind or throws for unknown values.</summary>
    public static string NormalizeProviderKind(string providerKind)
    {
        var trimmed = providerKind?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || !ProviderKinds.Contains(trimmed))
            throw new BrainSlotValidationException("providerKind must be OpenAI or OpenAICompatible.");
        return ProviderKinds.First(item => string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Builds the default party id for a role.</summary>
    public static string DefaultPartyId(string role)
        => role switch
        {
            BrainSlotRoles.LeftHemisphere => "brain-slot:left-hemisphere",
            BrainSlotRoles.RightHemisphere => "brain-slot:right-hemisphere",
            BrainSlotRoles.CuriosityEngine => "brain-slot:curiosity-engine",
            BrainSlotRoles.ArbiterOfTruth => "brain-slot:arbiter-of-truth",
            _ => "brain-slot:" + role.Trim().ToLowerInvariant(),
        };

    /// <summary>Returns the default signing key id for a party.</summary>
    public static string SigningKeyId(string partyId) => $"{partyId}:signing:1";

    /// <summary>Validates endpoint policy for a slot.</summary>
    public static void ValidateEndpoint(string providerKind, string? endpoint, IOptionsMonitor<BrainSlotOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var kind = NormalizeProviderKind(providerKind);
        var trimmed = string.IsNullOrWhiteSpace(endpoint) ? null : endpoint.Trim();

        if (string.Equals(kind, "OpenAI", StringComparison.OrdinalIgnoreCase) && trimmed is null)
            return;

        if (string.Equals(kind, "OpenAICompatible", StringComparison.OrdinalIgnoreCase) && trimmed is null)
            throw new BrainSlotValidationException("OpenAICompatible slots require endpoint.", BrainSlotReasonCodes.EndpointNotAllowed);

        if (trimmed is null)
            return;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
            throw new BrainSlotValidationException("endpoint must be an absolute URI.", BrainSlotReasonCodes.EndpointNotAllowed);

        if (IsLoopbackOrPrivate(uri.Host) && !options.CurrentValue.AllowLoopbackEndpoints)
            throw new BrainSlotValidationException("loopback/private endpoints require Mcp:BrainSlots:AllowLoopbackEndpoints=true.", BrainSlotReasonCodes.EndpointNotAllowed);

        var allowed = options.CurrentValue.AllowedEndpointHosts ?? [];
        if (!allowed.Any(host => string.Equals(host.Trim(), uri.Host, StringComparison.OrdinalIgnoreCase)))
            throw new BrainSlotValidationException("endpoint host is not allowlisted.", BrainSlotReasonCodes.EndpointNotAllowed);
    }

    /// <summary>Returns true when the host is loopback or private-address scoped.</summary>
    public static bool IsLoopbackOrPrivate(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!IPAddress.TryParse(host, out var address))
            return false;

        if (IPAddress.IsLoopback(address))
            return true;

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return bytes[0] == 10
                || bytes[0] == 127
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168);
        }

        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;
    }
}
