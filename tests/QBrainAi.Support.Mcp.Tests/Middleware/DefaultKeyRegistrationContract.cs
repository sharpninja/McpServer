using System.Net;
using Xunit;

namespace QBrainAi.Support.Mcp.TestContracts;

/// <summary>Shares the Director registration cases and assertions across the fake and QBrain host.</summary>
/// <remarks>2026-11-26: FR-MCP-009, TR-MCP-WS-004, FR-MCP-132 ac-1. Round 2
/// uses the same complete case matrix, HTTP observations and persisted state assertions.</remarks>
public static class DefaultKeyRegistrationContract
{
    /// <summary>The fixed ordinary workspace name sent in successful registration tests.</summary>
    public const string WorkspaceName = "director-registration-test";

    /// <summary>Describes authorization input without carrying credentials.</summary>
    public enum KeyPlacement
    {
        /// <summary>One header containing the key from the issuance response.</summary>
        Header,
        /// <summary>A header with a different, invalid key.</summary>
        Invalid,
        /// <summary>The issued key in the query alone.</summary>
        Query,
        /// <summary>Two X-Api-Key header values.</summary>
        Duplicate
    }

    /// <summary>Defines the requested input and expected outcome for one named case.</summary>
    public sealed record Scenario(
        string Id,
        RequestShape Request,
        HttpStatusCode ExpectedStatus,
        bool Registers);

    /// <summary>Non-secret request details observed by both providers.</summary>
    public sealed record RequestShape(
        string Method,
        string Route,
        string? Peer,
        KeyPlacement Key,
        string? ForbiddenField = null,
        bool? Enabled = null,
        bool Socket = false);

    /// <summary>Registry and filesystem observations for a fresh workspace path.</summary>
    public sealed record WorkspaceState(
        bool Registered,
        bool DirectoryExists,
        bool HasMarker,
        string? Name,
        bool? IsEnabled,
        bool? IsPrimary);

    /// <summary>Issuance, dispatch and side effects measured by a case provider.</summary>
    public sealed record Evidence(
        HttpStatusCode IssuanceStatus,
        bool IssuedKeyPresent,
        IReadOnlyList<string> Interactions,
        RequestShape Request,
        HttpStatusCode ResponseStatus,
        WorkspaceState Before,
        WorkspaceState After,
        bool ExistingWorkspaceUnchanged);

    private static readonly Dictionary<string, Scenario> s_scenarios =
        BuildScenarios().ToDictionary(value => value.Id, StringComparer.Ordinal);

    private static readonly WorkspaceState s_absent = new(false, false, false, null, null, null);

    /// <summary>Enumerates stable theory IDs with the same order for both gates.</summary>
    public static TheoryData<string> CaseIds => new(s_scenarios.Keys);

    /// <summary>Returns the canonical input for a stable theory ID.</summary>
    public static Scenario Get(string caseId) => s_scenarios[caseId];

    /// <summary>Checks issuance, interaction order, request, status and independent side effects.</summary>
    public static void Verify(Scenario scenario, Evidence actual)
    {
        Assert.Equal(HttpStatusCode.OK, actual.IssuanceStatus);
        Assert.True(actual.IssuedKeyPresent);
        Assert.Equal(["GET /api-key", "inspect-before", "send", "inspect-after"], actual.Interactions);
        Assert.Equal(scenario.Request, actual.Request);
        Assert.Equal(scenario.ExpectedStatus, actual.ResponseStatus);
        Assert.Equal(s_absent, actual.Before);
        Assert.True(actual.ExistingWorkspaceUnchanged);

        var expectedAfter = scenario.Registers
            ? new WorkspaceState(true, true, true, WorkspaceName, scenario.Request.Enabled ?? true, false)
            : s_absent;
        Assert.Equal(expectedAfter, actual.After);
    }

    /// <summary>Checks the original Round 1 response and registration observations.</summary>
    public static void Verify(HttpStatusCode expectedStatus, bool expectRegistration, Observation actual)
    {
        Assert.Equal(expectedStatus, actual.Status);
        Assert.Equal(expectRegistration, actual.IsRegistered);
        Assert.Equal(expectRegistration, actual.HasMarker);
    }

    /// <summary>HTTP and storage observations used by the original Round 1 binding.</summary>
    public sealed record Observation(HttpStatusCode Status, bool IsRegistered, bool HasMarker);

    /// <summary>Builds equivalent authorization and side-effect cases for each product prefix.</summary>
    private static IEnumerable<Scenario> BuildScenarios()
    {
        foreach (var (prefix, peer, enabled) in new[] { ("qbrainai", "127.0.0.1", true), ("mcpserver", "::1", false) })
        {
            var route = $"/{prefix}/workspace";
            var registration = new RequestShape("POST", route, peer, KeyPlacement.Header, Enabled: enabled);
            yield return new($"{prefix}/register", registration, HttpStatusCode.Created, Registers: true);
            yield return new($"{prefix}/socket", registration with { Peer = "127.0.0.1", Socket = true, Enabled = null },
                HttpStatusCode.Created, Registers: true);

            var restricted = registration with { Enabled = null };
            yield return new($"{prefix}/non-loopback", restricted with { Peer = "192.0.2.10" },
                HttpStatusCode.Forbidden, Registers: false);
            yield return new($"{prefix}/missing-peer", restricted with { Peer = null },
                HttpStatusCode.Forbidden, Registers: false);
            yield return new($"{prefix}/invalid-key", restricted with { Key = KeyPlacement.Invalid },
                HttpStatusCode.Unauthorized, Registers: false);
            yield return new($"{prefix}/query-key", restricted with { Key = KeyPlacement.Query },
                HttpStatusCode.Forbidden, Registers: false);
            yield return new($"{prefix}/duplicate-header", restricted with { Key = KeyPlacement.Duplicate },
                HttpStatusCode.Forbidden, Registers: false);

            foreach (var field in new[] { "isPrimary", "RunAs", "dumpPath", "unknown" })
                yield return new($"{prefix}/field/{field}", restricted with { ForbiddenField = field },
                    HttpStatusCode.Forbidden, Registers: false);

            yield return new($"{prefix}/read", restricted with { Method = "GET" }, HttpStatusCode.OK, Registers: false);
            yield return new($"{prefix}/update", restricted with { Method = "PUT", Route = $"{route}/{{key}}" },
                HttpStatusCode.Forbidden, Registers: false);
            yield return new($"{prefix}/delete", restricted with { Method = "DELETE", Route = $"{route}/{{key}}" },
                HttpStatusCode.Forbidden, Registers: false);
            yield return new($"{prefix}/regenerate", restricted with { Route = $"{route}/markers/regenerate" },
                HttpStatusCode.Forbidden, Registers: false);
            yield return new($"{prefix}/initialize", restricted with { Route = $"{route}/{{key}}/init" },
                HttpStatusCode.Forbidden, Registers: false);
            yield return new($"{prefix}/unrelated-write", restricted with { Route = $"/{prefix}/todo" },
                HttpStatusCode.Forbidden, Registers: false);
        }
    }
}
