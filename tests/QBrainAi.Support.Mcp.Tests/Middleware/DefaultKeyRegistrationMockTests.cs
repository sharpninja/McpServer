using System.Net;
using QBrainAi.Support.Mcp.TestContracts;
using Xunit;
using static QBrainAi.Support.Mcp.TestContracts.DefaultKeyRegistrationContract;

namespace QBrainAi.Support.Mcp.Tests.Middleware;

/// <summary>Runs every Director registration case against independently scripted observations.</summary>
/// <remarks>2026-11-26: FR-MCP-009, TR-MCP-WS-004, FR-MCP-132 ac-1. Each case
/// proves that corrupt issuance, dispatch, response, registry or marker evidence is rejected.</remarks>
public sealed class DefaultKeyRegistrationMockTests
{
    /// <summary>Feeds a correct fake and independent negative controls through the shared assertion.</summary>
    [Theory]
    [MemberData(nameof(CaseIds), MemberType = typeof(DefaultKeyRegistrationContract))]
    public void ContractDetectsCorruptEvidence(string caseId)
    {
        var scenario = Get(caseId);
        var good = ScriptedResponse(caseId);
        Verify(scenario, good);

        foreach (var corrupted in Corrupt(good))
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Verify(scenario, corrupted));
    }

    /// <summary>Returns independent inputs and scripted outcomes without using the scenario table.</summary>
    private static Evidence ScriptedResponse(string caseId)
    {
        var separator = caseId.IndexOf('/', StringComparison.Ordinal);
        var alias = caseId[..separator];
        var operation = caseId[(separator + 1)..];
        var canonical = alias == "qbrainai";
        var root = canonical ? "/qbrainai" : "/mcpserver";
        var route = root + "/workspace";
        string? peer = canonical ? "127.0.0.1" : "::1";
        string? field = null;
        bool? enabled = null;
        var socket = false;
        var method = "POST";
        var key = KeyPlacement.Header;
        var status = HttpStatusCode.Forbidden;
        var before = new WorkspaceState(false, false, false, null, null, null);
        var after = before;

        switch (operation)
        {
            case "register":
                enabled = canonical;
                status = HttpStatusCode.Created;
                after = new(true, true, true, "director-registration-test", canonical, false);
                break;
            case "socket":
                socket = true;
                peer = "127.0.0.1";
                status = HttpStatusCode.Created;
                after = new(true, true, true, "director-registration-test", true, false);
                break;
            case "non-loopback":
                peer = "192.0.2.10";
                break;
            case "missing-peer":
                peer = null;
                break;
            case "invalid-key":
                key = KeyPlacement.Invalid;
                status = HttpStatusCode.Unauthorized;
                break;
            case "query-key":
                key = KeyPlacement.Query;
                break;
            case "duplicate-header":
                key = KeyPlacement.Duplicate;
                break;
            case "field/isPrimary":
                field = "isPrimary";
                break;
            case "field/RunAs":
                field = "RunAs";
                break;
            case "field/dumpPath":
                field = "dumpPath";
                break;
            case "field/unknown":
                field = "unknown";
                break;
            case "read":
                method = "GET";
                status = HttpStatusCode.OK;
                break;
            case "update":
                method = "PUT";
                route += "/{key}";
                break;
            case "delete":
                method = "DELETE";
                route += "/{key}";
                break;
            case "regenerate":
                route += "/markers/regenerate";
                break;
            case "initialize":
                route += "/{key}/init";
                break;
            case "unrelated-write":
                route = root + "/todo";
                break;
            default:
                throw new ArgumentException("No scripted response for the case.", nameof(caseId));
        }

        return new(HttpStatusCode.OK, true, ["GET /api-key", "inspect-before", "send", "inspect-after"],
            new(method, route, peer, key, field, enabled, socket), status, before, after, true);
    }

    /// <summary>Independently corrupts each assertion, including every persisted workspace field.</summary>
    private static IEnumerable<Evidence> Corrupt(Evidence good)
    {
        yield return good with { IssuanceStatus = HttpStatusCode.Forbidden };
        yield return good with { IssuedKeyPresent = false };
        yield return good with { Interactions = ["GET /api-key", "send", "inspect-after"] };
        yield return good with { Interactions = ["GET /api-key", "send", "inspect-before", "inspect-after"] };
        yield return good with { Interactions = ["GET /api-key", "inspect-before", "send", "send", "inspect-after"] };
        yield return good with { Request = good.Request with { Method = "PATCH" } };
        yield return good with { Request = good.Request with { Route = "/unexpected" } };
        yield return good with { Request = good.Request with { Peer = "198.51.100.20" } };
        yield return good with { Request = good.Request with { Key = (KeyPlacement)(-1) } };
        yield return good with { Request = good.Request with { ForbiddenField = "extra" } };
        yield return good with { Request = good.Request with { Enabled = good.Request.Enabled is null ? true : null } };
        yield return good with { Request = good.Request with { Socket = !good.Request.Socket } };
        yield return good with { ResponseStatus = HttpStatusCode.BadGateway };
        yield return good with { Before = good.Before with { HasMarker = true } };
        yield return good with { After = good.After with { Registered = !good.After.Registered } };
        yield return good with { After = good.After with { DirectoryExists = !good.After.DirectoryExists } };
        yield return good with { After = good.After with { HasMarker = !good.After.HasMarker } };
        yield return good with { After = good.After with { Name = "corrupt" } };
        yield return good with { After = good.After with { IsEnabled = !(good.After.IsEnabled ?? false) } };
        yield return good with { After = good.After with { IsPrimary = true } };
        yield return good with { ExistingWorkspaceUnchanged = false };
    }
}
