using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.FederationAdapters;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for applying synchronized federation operations through adapters.</summary>
public sealed class FederationOperationApplyServiceTests
{
    /// <summary>Apply decodes BodyBase64 before passing payload JSON to the adapter.</summary>
    [Fact]
    public async Task ApplyAsync_DecodesBodyBase64BeforeAdapterApply()
    {
        var adapter = new CapturingAdapter("todo");
        var sut = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            HttpMethod = "PUT",
            Path = "/mcpserver/todo/PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{\"title\":\"Updated\"}"u8.ToArray()),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        Assert.Equal("{\"title\":\"Updated\"}", adapter.PayloadJson);
        Assert.Equal("PUT", adapter.HttpMethod);
        Assert.Equal("/mcpserver/todo/PLAN-FEDERATION-001", adapter.Path);
    }

    /// <summary>Invalid base64 bodies return a conflict result instead of calling the adapter.</summary>
    [Fact]
    public async Task ApplyAsync_InvalidBodyBase64DoesNotCallAdapter()
    {
        var adapter = new CapturingAdapter("todo");
        var sut = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            BodyBase64 = "not-base64",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.Conflict);
        Assert.Null(adapter.PayloadJson);
    }

    /// <summary>Local-only domains are rejected before adapter apply is attempted.</summary>
    [Fact]
    public async Task ApplyAsync_LocalOnlyDomainReturnsConflict()
    {
        var sut = new FederationOperationApplyService(new FederationStateAdapterRegistry(
            [new LocalOnlyFederationStateAdapter("marker_state", "host-specific trust material")]));

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "marker_state",
            ResourceId = "AGENTS-README-FIRST.yaml",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.Conflict);
        Assert.Contains("local-only", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Echo operations are acknowledged as already applied and do not call adapter apply.</summary>
    [Fact]
    public async Task ApplyAsync_EchoOperationSuppressesApply()
    {
        var adapter = new CapturingAdapter("todo") { Echo = true, Version = "v3" };
        var sut = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            SourceOperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{\"title\":\"Updated\"}"u8.ToArray()),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.AlreadyApplied);
        Assert.Equal("v3", result.Version);
        Assert.Null(adapter.PayloadJson);
    }

    /// <summary>Snapshot-only replicated domains return an explicit conflict instead of silent success.</summary>
    [Fact]
    public async Task ApplyAsync_SnapshotOnlyDomainReturnsConflict()
    {
        var adapter = new SnapshotOnlyAdapter("session_log");
        var sut = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "session_log",
            ResourceId = "Codex/session",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.Conflict);
        Assert.Contains("requires signed operation envelopes", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CapturingAdapter : IFederationStateAdapter
    {
        public CapturingAdapter(string domain)
        {
            Domain = domain;
        }

        public string Domain { get; }

        public bool IsLocalOnly => false;

        public string? PayloadJson { get; private set; }

        public string? HttpMethod { get; private set; }

        public string? Path { get; private set; }

        public bool Echo { get; set; }

        public string Version { get; set; } = "v1";

        public ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken)
            => new(new FederationStateSnapshot { Domain = Domain, ResourceId = resourceId, Version = "v1" });

        public ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
        {
            PayloadJson = operation.PayloadJson;
            HttpMethod = operation.HttpMethod;
            Path = operation.Path;
            return new ValueTask<FederationApplyResult>(new FederationApplyResult { Applied = true, Version = "v2" });
        }

        public ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken)
            => new(Version);

        public string GetIdempotencyKey(FederationStateOperation operation)
            => operation.OperationId;

        public bool IsEcho(FederationStateOperation operation)
            => Echo;
    }

    private sealed class SnapshotOnlyAdapter : FederationStateAdapterBase
    {
        public SnapshotOnlyAdapter(string domain)
            : base(domain)
        {
        }

        public override ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken)
            => new(new FederationStateSnapshot { Domain = Domain, ResourceId = resourceId, Version = "v1" });

        public override ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken)
            => new("v1");
    }
}
