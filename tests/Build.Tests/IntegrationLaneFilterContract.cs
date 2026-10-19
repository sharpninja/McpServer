namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-1): Contract cases and assertions for <see cref="IIntegrationLaneFilter"/>. The expected filters
/// come from the acceptance criterion; a small evaluator of the trait grammar the lane uses (terms joined by '&amp;',
/// Name=Value requires the trait, Name!=Value is met by any other value or by no trait) checks what each filter
/// selects for an untagged case, an own-host case, an other-host case and a non-integration case. The same
/// assertions run against the scripted filter (BDPv4 mock gate), wrong filters (negative controls), and the
/// production filter.
/// </summary>
internal static class IntegrationLaneFilterContract
{
    /// <summary>Every host name, in the order the contract checks them.</summary>
    internal static readonly string[] HostNames = ["windows", "linux"];

    /// <summary>Parses a contract host name.</summary>
    /// <param name="hostName">windows or linux.</param>
    internal static IntegrationLaneHost Parse(string hostName) => hostName switch
    {
        "windows" => IntegrationLaneHost.Windows,
        "linux" => IntegrationLaneHost.Linux,
        _ => throw new ArgumentOutOfRangeException(nameof(hostName), hostName, "Unknown contract host."),
    };

    /// <summary>The filter TR-MCP-NUKE-019 AC-1 requires for <paramref name="host"/>.</summary>
    /// <param name="host">Host the lane runs on.</param>
    internal static string ExpectedFilter(IntegrationLaneHost host) => host switch
    {
        IntegrationLaneHost.Windows => "Category=Integration&Platform!=Linux",
        IntegrationLaneHost.Linux => "Category=Integration&Platform!=Windows",
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Unknown host."),
    };

    /// <summary>Asserts that <paramref name="filter"/> meets the contract for the named host.</summary>
    /// <param name="hostName">windows or linux.</param>
    /// <param name="filter">Filter under test.</param>
    internal static void AssertHost(string hostName, IIntegrationLaneFilter filter)
    {
        var host = Parse(hostName);
        var actual = filter.Compose(host);

        Assert.Equal(ExpectedFilter(host), actual);
        AssertSelection(host, actual);
    }

    /// <summary>
    /// Asserts what <paramref name="filter"/> selects on <paramref name="host"/>: untagged and own-host integration
    /// cases run; other-host and non-integration cases do not. Independent of the exact filter text, so the negative
    /// controls prove each of these assertions rejects a wrong filter on its own.
    /// </summary>
    /// <param name="host">Host the lane runs on.</param>
    /// <param name="filter">Filter text under test.</param>
    internal static void AssertSelection(IntegrationLaneHost host, string filter)
    {
        var own = host == IntegrationLaneHost.Windows ? "Windows" : "Linux";
        var other = host == IntegrationLaneHost.Windows ? "Linux" : "Windows";

        Assert.True(Selects(filter, category: "Integration", platform: null), "An untagged integration case must run on every host.");
        Assert.True(Selects(filter, category: "Integration", platform: own), $"A Platform={own} integration case must run on its own host.");
        Assert.False(Selects(filter, category: "Integration", platform: other), $"A Platform={other} integration case must not run on a {own} host.");
        Assert.False(Selects(filter, category: null, platform: null), "A case without Category=Integration must not run in the integration lane.");
    }

    /// <summary>Evaluates the '&amp;'-joined Name=Value and Name!=Value terms against one case's traits.</summary>
    /// <param name="filter">Filter text.</param>
    /// <param name="category">The case's Category trait, or null.</param>
    /// <param name="platform">The case's Platform trait, or null.</param>
    internal static bool Selects(string filter, string? category, string? platform)
    {
        var traits = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Category"] = category,
            ["Platform"] = platform,
        };
        foreach (var term in filter.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var negated = term.Contains("!=", StringComparison.Ordinal);
            var parts = term.Split(negated ? "!=" : "=", 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !traits.TryGetValue(parts[0], out var value))
            {
                throw new FormatException($"Unsupported filter term '{term}'.");
            }

            var equal = string.Equals(value, parts[1], StringComparison.Ordinal);
            if (negated ? equal : !equal)
            {
                return false;
            }
        }

        return true;
    }
}
