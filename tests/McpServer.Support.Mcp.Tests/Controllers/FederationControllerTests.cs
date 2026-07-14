using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="FederationController"/>. Validates management API behaviour:
/// enable/disable, target CRUD, workspace routing, and tunnel discovery. FR-MCP-077.
/// </summary>
public sealed class FederationControllerTests
{
    private static FederationRegistry CreateRegistry(Action<FederationOptions>? configure = null)
    {
        var opts = new FederationOptions();
        configure?.Invoke(opts);
        return new FederationRegistry(Microsoft.Extensions.Options.Options.Create(opts));
    }

    private static TunnelRegistry CreateEmptyTunnelRegistry()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new TunnelOptions());
        return new TunnelRegistry(
            [],
            opts,
            NullLogger<TunnelRegistry>.Instance);
    }

    private static FederationEnvelopeSigner CreateSigner()
    {
        var monitor = Substitute.For<IOptionsMonitor<FederationOptions>>();
        monitor.CurrentValue.Returns(new FederationOptions
        {
            EnrollmentToken = "test-secret",
            Signing = new FederationSigningOptions
            {
                Enabled = true,
                EnvelopeTtlSeconds = 300,
            },
        });
        return new FederationEnvelopeSigner(monitor);
    }

    private static FederationController CreateController(
        FederationRegistry? registry = null,
        TunnelRegistry? tunnels = null,
        ITurnTransactionCoordinator? transactionCoordinator = null,
        TurnTransactionOptions? transactionOptions = null)
    {
        registry ??= CreateRegistry();
        tunnels ??= CreateEmptyTunnelRegistry();
        return new FederationController(
            registry,
            tunnels,
            transactionCoordinator: transactionCoordinator,
            transactionOptions: Microsoft.Extensions.Options.Options.Create(
                transactionOptions ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));
    }

    // --- GetStatus ---

    /// <summary>GetStatus returns disabled state when federation is not enabled.</summary>
    [Fact]
    public void GetStatus_Default_ReturnsDisabled()
    {
        var controller = CreateController();
        var result = controller.GetStatus();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FederationStatusResponse>(ok.Value);
        Assert.False(status.Enabled);
        Assert.Empty(status.Targets);
    }

    /// <summary>Adapter diagnostics expose the registered coverage rows through the controller.</summary>
    [Fact]
    public void GetAdapterCoverage_ReturnsRegistryCoverage()
    {
        var registry = new FederationStateAdapterRegistry(
        [
            new TestStateAdapter("todo", applySupported: true),
            new TestStateAdapter("marker_state", localOnly: true),
        ]);
        var controller = new FederationController(
            CreateRegistry(),
            CreateEmptyTunnelRegistry(),
            adapterRegistry: registry);

        var result = controller.GetAdapterCoverage();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var coverage = Assert.IsAssignableFrom<IReadOnlyList<FederationStateAdapterCoverage>>(ok.Value);
        Assert.Contains(coverage, row => row.Domain == "todo" && row.Covered && row.ApplySupported && !row.LocalOnly);
        Assert.Contains(coverage, row => row.Domain == "marker_state" && row.Covered && row.LocalOnly && !row.ApplySupported);
        Assert.Contains(coverage, row => row.Domain == "workspace" && !row.Covered && !row.LocalOnly && !row.ApplySupported);
    }

    /// <summary>Adapter diagnostics return an explicit not-configured response when no registry is available.</summary>
    [Fact]
    public void GetAdapterCoverage_WithoutRegistry_Returns501()
    {
        var controller = CreateController();

        var result = controller.GetAdapterCoverage();

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(501, status.StatusCode);
    }

    /// <summary>Signed envelope intake applies newly accepted operations on the hub.</summary>
    [Fact]
    public async Task RecordEnvelope_AppliesAcceptedOperation()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        var apply = Substitute.For<IFederationOperationApplyService>();
        var signer = CreateSigner();
        var operation = new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            HttpMethod = "PUT",
            Path = "/mcpserver/todo/PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{\"done\":true}"u8.ToArray()),
        };
        var envelope = signer.Sign(operation, "PAYTON-LEGION2");
        topology.RecordOperationAsync(operation, Arg.Any<CancellationToken>())
            .Returns(new FederationOperationResponse { OperationId = "op-1", Status = "accepted", Created = true });
        topology.AcknowledgeOperationAsync("op-1", Arg.Any<FederationOperationAckRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FederationOperationResponse { OperationId = "op-1", Status = "applied", Created = false });
        apply.ApplyAsync(operation, Arg.Any<CancellationToken>())
            .Returns(new FederationApplyResult { Applied = true, Version = "v2" });
        var controller = new FederationController(
            CreateRegistry(),
            CreateEmptyTunnelRegistry(),
            topologyService: topology,
            envelopeSigner: signer,
            operationApplyService: apply);

        var result = await controller.RecordEnvelope(envelope, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FederationOperationResponse>(ok.Value);
        Assert.Equal("applied", response.Status);
        await apply.Received(1)
            .ApplyAsync(operation, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await topology.Received(1)
            .AcknowledgeOperationAsync(
                "op-1",
                Arg.Is<FederationOperationAckRequest>(request =>
                    request != null &&
                    request.Status == "applied" &&
                    request.HubVersion == "v2"),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Signed envelope intake applies existing non-terminal operation rows instead of returning accepted.</summary>
    [Fact]
    public async Task RecordEnvelope_AppliesExistingAcceptedOperation()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        var apply = Substitute.For<IFederationOperationApplyService>();
        var signer = CreateSigner();
        var operation = new FederationOperationRequest
        {
            OperationId = "op-replay-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{\"done\":true}"u8.ToArray()),
        };
        var envelope = signer.Sign(operation, "PAYTON-LEGION2");
        topology.RecordOperationAsync(operation, Arg.Any<CancellationToken>())
            .Returns(new FederationOperationResponse { OperationId = "op-replay-1", Status = "accepted", Created = false });
        topology.AcknowledgeOperationAsync("op-replay-1", Arg.Any<FederationOperationAckRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FederationOperationResponse { OperationId = "op-replay-1", Status = "applied", Created = false });
        apply.ApplyAsync(operation, Arg.Any<CancellationToken>())
            .Returns(new FederationApplyResult { Applied = true, Version = "v2" });
        var controller = new FederationController(
            CreateRegistry(),
            CreateEmptyTunnelRegistry(),
            topologyService: topology,
            envelopeSigner: signer,
            operationApplyService: apply);

        var result = await controller.RecordEnvelope(envelope, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FederationOperationResponse>(ok.Value);
        Assert.Equal("applied", response.Status);
        await apply.Received(1)
            .ApplyAsync(operation, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Signed envelope intake waits for transactional apply completion before acknowledging applied.</summary>
    [Fact]
    public async Task RecordEnvelope_WaitsForTransactionalApplyBeforeAcknowledgingApplied()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        var apply = new BlockingOperationApplyService(new FederationApplyResult { Applied = true, Version = "v2" });
        var signer = CreateSigner();
        var operation = new FederationOperationRequest
        {
            OperationId = "op-wait-apply",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            HttpMethod = "PUT",
            Path = "/mcpserver/todo/PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{\"done\":true}"u8.ToArray()),
        };
        var envelope = signer.Sign(operation, "PAYTON-LEGION2");
        topology.RecordOperationAsync(operation, Arg.Any<CancellationToken>())
            .Returns(new FederationOperationResponse { OperationId = "op-wait-apply", Status = "accepted", Created = true });
        topology.AcknowledgeOperationAsync("op-wait-apply", Arg.Any<FederationOperationAckRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FederationOperationResponse { OperationId = "op-wait-apply", Status = "applied", Created = false });
        var controller = new FederationController(
            CreateRegistry(),
            CreateEmptyTunnelRegistry(),
            topologyService: topology,
            envelopeSigner: signer,
            operationApplyService: apply);

        var resultTask = controller.RecordEnvelope(envelope, CancellationToken.None);
        await apply.WaitForApplyAsync().ConfigureAwait(true);

        Assert.False(resultTask.IsCompleted);
        await topology.DidNotReceive()
            .AcknowledgeOperationAsync(
                "op-wait-apply",
                Arg.Any<FederationOperationAckRequest>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);

        apply.Release();
        var result = await resultTask.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FederationOperationResponse>(ok.Value);
        Assert.Equal("applied", response.Status);
        await topology.Received(1)
            .AcknowledgeOperationAsync(
                "op-wait-apply",
                Arg.Is<FederationOperationAckRequest>(request =>
                    request != null &&
                    request.Status == "applied" &&
                    request.HubVersion == "v2"),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Signed envelope intake records a conflict acknowledgement when transactional apply reports degraded.</summary>
    [Fact]
    public async Task RecordEnvelope_WhenTransactionalApplyReportsDegraded_AcknowledgesConflict()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        var apply = Substitute.For<IFederationOperationApplyService>();
        var signer = CreateSigner();
        var operation = new FederationOperationRequest
        {
            OperationId = "op-degraded-apply",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{\"done\":true}"u8.ToArray()),
        };
        var envelope = signer.Sign(operation, "PAYTON-LEGION2");
        topology.RecordOperationAsync(operation, Arg.Any<CancellationToken>())
            .Returns(new FederationOperationResponse { OperationId = "op-degraded-apply", Status = "accepted", Created = true });
        topology.AcknowledgeOperationAsync("op-degraded-apply", Arg.Any<FederationOperationAckRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FederationOperationResponse { OperationId = "op-degraded-apply", Status = "conflict", Created = false });
        apply.ApplyAsync(operation, Arg.Any<CancellationToken>())
            .Returns(new FederationApplyResult
            {
                Applied = false,
                Conflict = true,
                Version = "v-degraded",
                Message = "turn transaction coordinator is degraded",
            });
        var controller = new FederationController(
            CreateRegistry(),
            CreateEmptyTunnelRegistry(),
            topologyService: topology,
            envelopeSigner: signer,
            operationApplyService: apply);

        var result = await controller.RecordEnvelope(envelope, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FederationOperationResponse>(ok.Value);
        Assert.Equal("conflict", response.Status);
        await topology.Received(1)
            .AcknowledgeOperationAsync(
                "op-degraded-apply",
                Arg.Is<FederationOperationAckRequest>(request =>
                    request != null &&
                    request.Status == "conflict" &&
                    request.HubVersion == "v-degraded" &&
                    request.Error == "turn transaction coordinator is degraded"),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Hub sync signs local-execution outbox rows with local-execution apply mode.</summary>
    [Fact]
    public async Task Sync_LocalExecutionItemSignsEnvelopeWithLocalExecutionApplyMode()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        var signer = CreateSigner();
        topology.GetSyncItemsAsync("PAYTON-LEGION2", 0, Arg.Any<CancellationToken>())
            .Returns([new FederationSyncItem
            {
                Sequence = 7,
                OperationId = "op-local-1",
                ProxyId = "PAYTON-DESKTOP",
                Domain = "local_execution",
                Method = "desktop_launch",
                BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
            }]);
        var controller = new FederationController(
            CreateRegistry(),
            CreateEmptyTunnelRegistry(),
            topologyService: topology,
            envelopeSigner: signer);

        var result = await controller.Sync("PAYTON-LEGION2", 0, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<FederationSyncItem>>(ok.Value);
        var item = Assert.Single(items);
        Assert.NotNull(item.Envelope);
        Assert.Equal("local_execution", item.Envelope.ApplyMode);
        Assert.Equal("PAYTON-LEGION2", item.Envelope.TargetProxyId);
    }

    // --- Enable / Disable ---

    /// <summary>Enable sets federation to enabled.</summary>
    [Fact]
    public void Enable_SetsEnabledTrue()
    {
        var controller = CreateController();
        var result = controller.Enable();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FederationStatusResponse>(ok.Value);
        Assert.True(status.Enabled);
    }

    /// <summary>TEST-MCP-161: Enable fails closed before mutating registry state when required turn transactions are active.</summary>
    [Fact]
    public void Enable_WhenTransactionsRequired_ReturnsConflictWithoutChangingRegistry()
    {
        var registry = CreateRegistry();
        var controller = CreateController(registry, transactionCoordinator: new CapturingCoordinator(enabled: true));

        var result = controller.Enable();

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.False(registry.IsEnabled);
    }

    /// <summary>Disable sets federation to disabled.</summary>
    [Fact]
    public void Disable_SetsEnabledFalse()
    {
        var registry = CreateRegistry(o => o.Enabled = true);
        var controller = CreateController(registry);

        var result = controller.Disable();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FederationStatusResponse>(ok.Value);
        Assert.False(status.Enabled);
    }

    /// <summary>TEST-MCP-161: Degraded transaction state blocks federation control-plane mutations before registry writes.</summary>
    [Fact]
    public void Disable_WhenCoordinatorDegraded_ReturnsConflictWithoutChangingRegistry()
    {
        var registry = CreateRegistry(o => o.Enabled = true);
        var controller = CreateController(
            registry,
            transactionCoordinator: new CapturingCoordinator(enabled: true, degraded: true, message: "txn degraded"));

        var result = controller.Disable();

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("txn degraded", conflict.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.True(registry.IsEnabled);
    }

    // --- ListTargets ---

    /// <summary>ListTargets returns empty list when no targets are configured.</summary>
    [Fact]
    public void ListTargets_NoTargets_ReturnsEmpty()
    {
        var result = CreateController().ListTargets();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var targets = Assert.IsAssignableFrom<IReadOnlyList<FederationTargetInfo>>(ok.Value);
        Assert.Empty(targets);
    }

    // --- AddTarget ---

    /// <summary>Adding a valid target returns 201 Created with the new target info.</summary>
    [Fact]
    public void AddTarget_ValidOptions_Returns201()
    {
        var controller = CreateController();
        var opts = new FederationTargetOptions { Name = "remote", BaseUrl = "https://x.ngrok.io" };

        var result = controller.AddTarget(opts);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var info = Assert.IsType<FederationTargetInfo>(created.Value);
        Assert.Equal("remote", info.Name);
    }

    /// <summary>TEST-MCP-161: Target registration fails closed before adding a federation target.</summary>
    [Fact]
    public void AddTarget_WhenTransactionsRequired_ReturnsConflictWithoutAddingTarget()
    {
        var registry = CreateRegistry();
        var controller = CreateController(registry, transactionCoordinator: new CapturingCoordinator(enabled: true));

        var result = controller.AddTarget(new FederationTargetOptions { Name = "remote", BaseUrl = "https://x.ngrok.io" });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Empty(registry.List());
    }

    /// <summary>Adding a duplicate target name returns 409 Conflict.</summary>
    [Fact]
    public void AddTarget_DuplicateName_Returns409()
    {
        var controller = CreateController();
        var opts = new FederationTargetOptions { Name = "remote", BaseUrl = "https://x.ngrok.io" };
        controller.AddTarget(opts);

        var result = controller.AddTarget(opts);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // --- RemoveTarget ---

    /// <summary>Removing an existing target returns 204 No Content.</summary>
    [Fact]
    public void RemoveTarget_Existing_Returns204()
    {
        var registry = CreateRegistry(o =>
            o.Targets.Add(new FederationTargetOptions { Name = "remote", BaseUrl = "https://x.ngrok.io" }));
        var controller = CreateController(registry);

        var result = controller.RemoveTarget("remote");

        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>Removing a non-existent target returns 404.</summary>
    [Fact]
    public void RemoveTarget_NonExistent_Returns404()
    {
        var result = CreateController().RemoveTarget("ghost");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // --- SetDefault / ClearDefault ---

    /// <summary>SetDefault with a valid name returns updated status with new default.</summary>
    [Fact]
    public void SetDefault_ValidTarget_UpdatesStatus()
    {
        var registry = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
        });
        var controller = CreateController(registry);

        var result = controller.SetDefault("t1");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FederationStatusResponse>(ok.Value);
        Assert.True(status.Targets.Single().IsDefault);
    }

    /// <summary>SetDefault with a non-existent target returns 404.</summary>
    [Fact]
    public void SetDefault_UnknownTarget_Returns404()
    {
        var result = CreateController().SetDefault("ghost");
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>ClearDefault removes the default target and returns updated status.</summary>
    [Fact]
    public void ClearDefault_ReturnsStatusWithNoDefault()
    {
        var registry = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.DefaultTarget = "t1";
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
        });
        var controller = CreateController(registry);

        controller.ClearDefault();
        var result = controller.GetStatus();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FederationStatusResponse>(ok.Value);
        Assert.DoesNotContain(status.Targets, t => t.IsDefault);
    }

    // --- AddRoute / RemoveRoute ---

    /// <summary>Adding a valid route returns 200 with updated route list.</summary>
    [Fact]
    public void AddRoute_ValidTargetName_Returns200()
    {
        var registry = CreateRegistry(o =>
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" }));
        var controller = CreateController(registry);

        var result = controller.AddRoute(new WorkspaceRouteOptions
        {
            WorkspacePath = @"C:\ws\alpha",
            TargetName = "t1",
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var routes = Assert.IsAssignableFrom<IReadOnlyList<WorkspaceRouteInfo>>(ok.Value);
        Assert.Single(routes);
    }

    /// <summary>TEST-MCP-161: Workspace route writes fail closed before mutating route state.</summary>
    [Fact]
    public void AddRoute_WhenTransactionsRequired_ReturnsConflictWithoutAddingRoute()
    {
        var registry = CreateRegistry(o =>
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" }));
        var controller = CreateController(registry, transactionCoordinator: new CapturingCoordinator(enabled: true));

        var result = controller.AddRoute(new WorkspaceRouteOptions
        {
            WorkspacePath = @"C:\ws\alpha",
            TargetName = "t1",
        });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Empty(registry.ListRoutes());
    }

    /// <summary>Adding a route with an unknown target returns 404.</summary>
    [Fact]
    public void AddRoute_UnknownTarget_Returns404()
    {
        var result = CreateController().AddRoute(new WorkspaceRouteOptions
        {
            WorkspacePath = @"C:\ws\alpha",
            TargetName = "ghost",
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>Removing an existing route returns 204.</summary>
    [Fact]
    public void RemoveRoute_Existing_Returns204()
    {
        var registry = CreateRegistry(o =>
        {
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
            o.WorkspaceRoutes.Add(new WorkspaceRouteOptions { WorkspacePath = @"C:\ws\alpha", TargetName = "t1" });
        });
        var controller = CreateController(registry);

        var result = controller.RemoveRoute(new WorkspaceRouteOptions { WorkspacePath = @"C:\ws\alpha" });

        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>Removing a non-existent route returns 404.</summary>
    [Fact]
    public void RemoveRoute_NonExistent_Returns404()
    {
        var result = CreateController().RemoveRoute(new WorkspaceRouteOptions { WorkspacePath = @"C:\ghost" });
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // --- DiscoverFromTunnels ---

    /// <summary>DiscoverFromTunnels returns 0 discovered when no tunnels are running.</summary>
    [Fact]
    public async Task DiscoverFromTunnels_NoRunningTunnels_Returns0Discovered()
    {
        var controller = CreateController();
        var result = await controller.DiscoverFromTunnels(CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var discovery = Assert.IsType<TunnelDiscoveryResult>(ok.Value);
        Assert.Equal(0, discovery.Discovered);
    }

    /// <summary>TEST-MCP-161: Direct topology operation recording fails closed before topology mutation.</summary>
    [Fact]
    public async Task RecordOperation_WhenTransactionsRequired_ReturnsConflictWithoutCallingTopology()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        var controller = new FederationController(
            CreateRegistry(),
            CreateEmptyTunnelRegistry(),
            topologyService: topology,
            transactionCoordinator: new CapturingCoordinator(enabled: true),
            transactionOptions: Microsoft.Extensions.Options.Options.Create(
                new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

        var result = await controller.RecordOperation(
                new FederationOperationRequest { OperationId = "op-block", ProxyId = "proxy-1", Domain = "todo" },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.IsType<ConflictObjectResult>(result.Result);
        await topology.DidNotReceive()
            .RecordOperationAsync(Arg.Any<FederationOperationRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-161: Signed federation envelope intake remains protocol-internal and reaches transactional apply.</summary>
    [Fact]
    public async Task RecordEnvelope_WhenTransactionsRequired_StillReachesTransactionalApply()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        var apply = Substitute.For<IFederationOperationApplyService>();
        var signer = CreateSigner();
        var operation = new FederationOperationRequest
        {
            OperationId = "op-signed-txn",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{\"done\":true}"u8.ToArray()),
        };
        var envelope = signer.Sign(operation, "PAYTON-LEGION2");
        topology.RecordOperationAsync(operation, Arg.Any<CancellationToken>())
            .Returns(new FederationOperationResponse { OperationId = "op-signed-txn", Status = "accepted", Created = true });
        topology.AcknowledgeOperationAsync("op-signed-txn", Arg.Any<FederationOperationAckRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FederationOperationResponse { OperationId = "op-signed-txn", Status = "applied", Created = false });
        apply.ApplyAsync(operation, Arg.Any<CancellationToken>())
            .Returns(new FederationApplyResult { Applied = true, Version = "v2" });
        var controller = new FederationController(
            CreateRegistry(),
            CreateEmptyTunnelRegistry(),
            topologyService: topology,
            envelopeSigner: signer,
            operationApplyService: apply,
            transactionCoordinator: new CapturingCoordinator(enabled: true),
            transactionOptions: Microsoft.Extensions.Options.Options.Create(
                new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

        var result = await controller.RecordEnvelope(envelope, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FederationOperationResponse>(ok.Value);
        Assert.Equal("applied", response.Status);
        await apply.Received(1)
            .ApplyAsync(operation, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    private sealed class TestStateAdapter : IFederationStateAdapter
    {
        private readonly bool _applySupported;

        public TestStateAdapter(string domain, bool localOnly = false, bool applySupported = false)
        {
            Domain = domain;
            IsLocalOnly = localOnly;
            _applySupported = applySupported;
        }

        public string Domain { get; }

        public bool IsLocalOnly { get; }

        public bool SupportsApply => _applySupported;

        public ValueTask<FederationStateSnapshot> SnapshotAsync(
            string? resourceId,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new FederationStateSnapshot
            {
                Domain = Domain,
                ResourceId = resourceId ?? string.Empty,
                Version = "v1",
                PayloadJson = "{}",
            });

        public ValueTask<FederationApplyResult> ApplyAsync(
            FederationStateOperation operation,
            CancellationToken cancellationToken)
            => _applySupported
                ? ValueTask.FromResult(new FederationApplyResult { Applied = true, Version = "v2" })
                : ValueTask.FromResult(new FederationApplyResult { Conflict = true, Message = "Apply is not supported." });

        public ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken)
            => ValueTask.FromResult<string?>("v1");

        public string GetIdempotencyKey(FederationStateOperation operation)
            => operation.SourceOperationId ?? operation.OperationId;

        public bool IsEcho(FederationStateOperation operation)
            => !string.IsNullOrWhiteSpace(operation.SourceOperationId) &&
               string.Equals(operation.SourceOperationId, operation.OperationId, StringComparison.Ordinal);
    }

    private sealed class BlockingOperationApplyService : IFederationOperationApplyService
    {
        private readonly FederationApplyResult _result;
        private readonly TaskCompletionSource _applyStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseApply = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingOperationApplyService(FederationApplyResult result)
        {
            _result = result;
        }

        public async ValueTask<FederationApplyResult> ApplyAsync(
            FederationOperationRequest operation,
            CancellationToken cancellationToken)
        {
            _applyStarted.TrySetResult();
            await _releaseApply.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return _result;
        }

        public Task WaitForApplyAsync()
            => _applyStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        public void Release()
            => _releaseApply.TrySetResult();
    }

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        private readonly TurnTransactionStatusResponse _status;

        public CapturingCoordinator(bool enabled, bool degraded = false, string message = "")
        {
            _status = new TurnTransactionStatusResponse
            {
                Enabled = enabled,
                Degraded = degraded,
                Message = message,
            };
        }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public TurnTransactionStatusResponse GetStatus() => _status;
    }
}
