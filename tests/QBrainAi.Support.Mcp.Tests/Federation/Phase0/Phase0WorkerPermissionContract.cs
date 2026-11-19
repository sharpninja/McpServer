namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Test command for a named SQL principal and permitted target namespace; never contains credentials.</summary>
/// <param name="Principal">OrdinaryEditor or FederationWorker fixture principal.</param><param name="Operation">Explicit remote procedure or ref-write operation.</param>
/// <param name="Target">main or integration/private fixture ref.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185 ac-LocalAppUserCannotPush; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal sealed record Phase0WorkerPermissionAttempt(string Principal, string Operation, string Target);

/// <summary>Independent namespace and object observations, distinct from the procedure's return envelope.</summary>
/// <param name="PublicHead">Guarded remote main.</param><param name="LiveHead">Owning application's live branch.</param>
/// <param name="IntegrationHead">Worker's non-live integration branch.</param><param name="LocalTitle">Preserved application draft.</param>
/// <param name="DownloadedObjects">Objects fetched by a permitted worker.</param><param name="UploadedCandidates">Candidate objects uploaded without public-ref acceptance.</param>
/// <param name="PublishedObjects">Objects made public through the attempted bypass.</param><param name="DirectLiveWrites">Worker writes bypassing the owner API.</param>
/// <remarks>2026-11-19: B5; FR-MCP-185 ac-LocalAppUserCannotPush; TR-MCP-FEDERATION-012; TEST-MCP-233. Mock metadata is not native SQL grant evidence.</remarks>
internal sealed record Phase0WorkerPermissionState(string PublicHead, string LiveHead, string IntegrationHead,
    string LocalTitle, int DownloadedObjects, int UploadedCandidates, int PublishedObjects, int DirectLiveWrites);

/// <summary>Test-only permission seam; a later isolated native adapter resolves the exact pinned Dolt SQL surface.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185 ac-LocalAppUserCannotPush; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// Logical operation names map to native procedures in the binding, never a new public API or invented numeric SQL error.</remarks>
internal interface IPhase0WorkerPermissionProbe
{
    /// <summary>Prepares isolated ordinary-editor and worker principals, main C0/live B1/integration I0, and a retained draft.</summary>
    void Prepare();
    /// <summary>Attempts the actual accessible procedure/ref-write surface with the selected SQL principal.</summary>
    /// <param name="attempt">Non-secret explicit fixture request.</param><returns>Allowed or SqlPermissionDenied.</returns>
    string? Attempt(Phase0WorkerPermissionAttempt attempt);
    /// <summary>Independently reads all namespace/data effects.</summary><returns>Actual observations or null on read failure.</returns>
    Phase0WorkerPermissionState? ReadState();
    /// <summary>Reopens connections before checking durable permission and namespace effects.</summary>
    void Reopen();
}

/// <summary>Checks ordinary editing isolation and narrow worker rights without granting workers direct live/public ref writes.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185 ac-LocalAppUserCannotPush; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// Full owning-gate races reuse Task006's b9-worker fixtures for FR-MCP-189/TR-MCP-FEDERATION-016/TEST-MCP-237.</remarks>
internal static class Phase0WorkerPermissionContract
{
    /// <summary>Asserts one principal/operation/namespace combination with independent before/after/reopen effects.</summary>
    /// <param name="probe">Scripted or later native isolated boundary.</param><param name="scenario">Exact principal/operation fixture.</param>
    internal static void AssertScenario(IPhase0WorkerPermissionProbe probe, string scenario)
    {
        var parts = scenario.Split(':');
        var worker = parts[0] == "FederationWorker";
        var operation = parts[1];
        var target = operation is "WriteIntegrationRef" or "RemotePull" ? "integration/private" : "main";
        var allowed = worker && operation is "RemoteFetch" or "RemotePush" or "RemotePull" or "WriteIntegrationRef";
        probe.Prepare();
        Assert.Equal(new("C0", "B1", "I0", "offline draft café 雪", 0, 0, 0, 0), probe.ReadState());
        Assert.Equal(allowed ? "Allowed" : "SqlPermissionDenied", probe.Attempt(new(parts[0], operation, target)));
        AssertState(probe.ReadState(), allowed, operation);
        probe.Reopen();
        AssertState(probe.ReadState(), allowed, operation);
    }

    /// <summary>Checks publication, live branch and draft isolation independently of accepted temporary worker effects.</summary>
    private static void AssertState(Phase0WorkerPermissionState? state, bool allowed, string operation)
    {
        Assert.NotNull(state);
        Assert.Equal("C0", state.PublicHead);
        Assert.Equal("B1", state.LiveHead);
        Assert.Equal(allowed && operation is "RemotePull" or "WriteIntegrationRef" ? "I1" : "I0", state.IntegrationHead);
        Assert.Equal("offline draft café 雪", state.LocalTitle);
        Assert.Equal(allowed && operation is "RemoteFetch" or "RemotePull" ? 1 : 0, state.DownloadedObjects);
        Assert.Equal(allowed && operation == "RemotePush" ? 1 : 0, state.UploadedCandidates);
        Assert.Equal(0, state.PublishedObjects);
        Assert.Equal(0, state.DirectLiveWrites);
    }
}