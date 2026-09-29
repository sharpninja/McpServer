// FR-MCP-REQRECOVERY-001: REPL dispatcher preserves classified recovery error codes.
// TEST-MCP-REQRECOVERY-002: applyRecovery/getRecovery 409/503/404 are not method_invocation_error.

using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Ensures the requirements dispatcher classifies typed recovery failures instead of
/// collapsing them into non-retryable <c>method_invocation_error</c>.
/// </summary>
public sealed class RequirementsRecoveryDispatcherTests
{
    /// <summary>applyRecovery 409 surfaces code conflict (not method_invocation_error).</summary>
    [Fact]
    public async Task ApplyRecovery_Conflict_ReturnsClassifiedConflict()
    {
        var workflow = Substitute.For<IRequirementsWorkflow>();
        workflow.ApplyRecoveryAsync(Arg.Any<RequirementsRecoveryRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new McpConflictException(
                "Requirements recovery idempotency key 'recover-001' conflicts with a stored payload.",
                "conflict",
                retryable: false));

        var response = await DispatchAsync(
            workflow,
            RequirementsCommandShapes.ApplyRecoveryMethod,
            SampleRecoveryParams()).ConfigureAwait(true);

        var error = AssertError(response);
        Assert.Equal("conflict", error.Code);
        Assert.False(error.Retryable);
        Assert.DoesNotContain("method_invocation_error", error.Code, StringComparison.Ordinal);
    }

    /// <summary>applyRecovery 503 surfaces retryable backend_unavailable.</summary>
    [Fact]
    public async Task ApplyRecovery_ServiceUnavailable_ReturnsRetryableBackendUnavailable()
    {
        var workflow = Substitute.For<IRequirementsWorkflow>();
        workflow.ApplyRecoveryAsync(Arg.Any<RequirementsRecoveryRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new McpServerException(
                "The storage backend is currently unreachable.",
                503,
                "backend_unavailable",
                retryable: true));

        var response = await DispatchAsync(
            workflow,
            RequirementsCommandShapes.ApplyRecoveryMethod,
            SampleRecoveryParams()).ConfigureAwait(true);

        var error = AssertError(response);
        Assert.Equal("backend_unavailable", error.Code);
        Assert.True(error.Retryable);
    }

    /// <summary>getRecovery 404 surfaces not_found (KeyNotFoundException from workflow).</summary>
    [Fact]
    public async Task GetRecovery_NotFound_ReturnsClassifiedNotFound()
    {
        var workflow = Substitute.For<IRequirementsWorkflow>();
        workflow.GetRecoveryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Requirements recovery run 'missing' was not found."));

        var response = await DispatchAsync(
            workflow,
            RequirementsCommandShapes.GetRecoveryMethod,
            new Dictionary<string, object?> { ["idempotencyKey"] = "missing" }).ConfigureAwait(true);

        var error = AssertError(response);
        Assert.Equal("not_found", error.Code);
        Assert.False(error.Retryable);
    }

    /// <summary>Unclassified exceptions still map to method_invocation_error.</summary>
    [Fact]
    public async Task ApplyRecovery_UnclassifiedException_KeepsMethodInvocationError()
    {
        var workflow = Substitute.For<IRequirementsWorkflow>();
        workflow.ApplyRecoveryAsync(Arg.Any<RequirementsRecoveryRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unexpected boom"));

        var response = await DispatchAsync(
            workflow,
            RequirementsCommandShapes.ApplyRecoveryMethod,
            SampleRecoveryParams()).ConfigureAwait(true);

        var error = AssertError(response);
        Assert.Equal("method_invocation_error", error.Code);
        Assert.False(error.Retryable);
    }

    private static async Task<IYamlEnvelope> DispatchAsync(
        IRequirementsWorkflow workflow,
        string method,
        Dictionary<string, object?> parameters)
    {
        var sut = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(),
            requirementsWorkflow: workflow);

        return await sut.DispatchAsync(
            new YamlEnvelope
            {
                Type = "request",
                Payload = new RequestPayload
                {
                    RequestId = $"req-recovery-{Guid.NewGuid().ToString("N")[..8]}",
                    Method = method,
                    Params = parameters,
                },
            },
            CancellationToken.None).ConfigureAwait(false);
    }

    private static IErrorPayload AssertError(IYamlEnvelope response)
    {
        Assert.Equal("error", response.Type);
        return Assert.IsAssignableFrom<IErrorPayload>(response.Payload);
    }

    private static Dictionary<string, object?> SampleRecoveryParams() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["idempotencyKey"] = "recover-001",
        ["items"] = new object[]
        {
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["kind"] = "fr",
                ["id"] = "FR-MCP-REQRECOVERY-001",
                ["title"] = "Recover",
                ["body"] = "Atomic apply",
            },
        },
    };
}
