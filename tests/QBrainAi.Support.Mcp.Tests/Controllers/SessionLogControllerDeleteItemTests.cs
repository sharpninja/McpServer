using System.Reflection;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Controllers;

/// <summary>
/// FR-SUPPORT-010G (triage-report-ed3b0381): a session-log turn item whose key contains a slash can be
/// deleted, because the key travels in the query string of DELETE .../sections/{section}/items; the
/// path-segment route stays for keys without a slash. ASP.NET Core keeps %2F encoded in a route value
/// and decodes query values, so the query route is the one that carries such keys unchanged.
/// Data: an ISessionLogService substitute; key "FR/TR/TEST".
/// </summary>
public sealed class SessionLogControllerDeleteItemTests
{
    private const string Agent = "ClaudeCode";
    private const string SessionId = "ClaudeCode-20261013T120000Z-a8";
    private const string RequestId = "req-20261013T120100Z-a8";

    /// <summary>
    /// The query-string delete passes the decoded key, slash included, to the session-log service and
    /// returns 200 with deleted=true.
    /// </summary>
    [Fact]
    public async Task DeleteTurnItemByQueryAsync_KeyWithSlash_DeletesThroughService()
    {
        var service = Substitute.For<ISessionLogService>();
        service.DeleteTurnItemAsync(Agent, SessionId, RequestId, "designDecisions", "FR/TR/TEST", Arg.Any<CancellationToken>())
            .Returns(true);
        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);

        var result = await controller.DeleteTurnItemByQueryAsync(
                Agent, SessionId, RequestId, "designDecisions", "FR/TR/TEST", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.IsType<OkObjectResult>(result);
        await service.Received(1)
            .DeleteTurnItemAsync(Agent, SessionId, RequestId, "designDecisions", "FR/TR/TEST", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>
    /// The query-string action is routed at DELETE {agent}/{sessionId}/{requestId}/sections/{section}/items
    /// and binds itemKey from the query string; the path-segment action keeps its route.
    /// </summary>
    [Fact]
    public void DeleteTurnItemRoutes_QueryActionBindsItemKeyFromQueryAndPathActionRemains()
    {
        var queryAction = typeof(SessionLogController).GetMethod(nameof(SessionLogController.DeleteTurnItemByQueryAsync))!;
        Assert.Equal(
            "{agent}/{sessionId}/{requestId}/sections/{section}/items",
            queryAction.GetCustomAttribute<HttpDeleteAttribute>()!.Template);
        var itemKey = queryAction.GetParameters().Single(parameter => parameter.Name == "itemKey");
        Assert.NotNull(itemKey.GetCustomAttribute<FromQueryAttribute>());

        var pathAction = typeof(SessionLogController).GetMethod(nameof(SessionLogController.DeleteTurnItemAsync))!;
        Assert.Equal(
            "{agent}/{sessionId}/{requestId}/sections/{section}/items/{itemKey}",
            pathAction.GetCustomAttribute<HttpDeleteAttribute>()!.Template);
    }
}
