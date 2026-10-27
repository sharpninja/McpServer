// SPDX-License-Identifier: Apache-2.0
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QBrainAi.Client;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.Models;
using ClientTurn = QBrainAi.Client.Models.UnifiedRequestEntryDto;

namespace QBrainAi.Support.Mcp.Tests.Services;

public sealed partial class ServerAuditSqliteFixture
{
    /// <summary>
    /// AC/TEST-QB-005/007/008/009/069: Exercise actual typed lifecycle methods,
    /// controller operations and independent SQLite reads for common host cases.
    /// Only the HTTP handler is injected; no network or native dispatch is claimed.
    /// </summary>
    /// <param name="operation">Begin, ReadTurn, Complete or Fail.</param>
    /// <param name="inputJson">Native JSON containing captured root and complete data.</param>
    /// <returns>Actual typed reply or independently projected owning turn.</returns>
    public string InvokeHostTyped(string operation, string inputJson)
        => InvokeHostTypedAsync(operation, inputJson).GetAwaiter().GetResult();

    private async Task<string> InvokeHostTypedAsync(string operation, string inputJson)
    {
        using var document = JsonDocument.Parse(inputJson);
        var root = document.RootElement.GetProperty("root");
        var data = document.RootElement.GetProperty("data");
        var workspace = Text(root, "workspacePath");
        var agent = Text(root, "agentIdentity");
        var session = Text(root, "mcpSessionId");
        var request = Text(root, "requestId");
        using var handler = new HostLifecycleHandler(this, root.Clone());
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://host-fixture.invalid/") };
        var client = new SessionLogClient(http, new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://host-fixture.invalid/"), WorkspacePath = workspace,
            ApiKey = "synthetic-task-owned-fixture-key",
        });
        object? reply;
        if (operation == "ReadTurn")
        {
            QBrainAi.Client.Models.UnifiedSessionLogDto? sessionLog;
            try { sessionLog = await client.GetAsync(agent, session).ConfigureAwait(false); }
            catch (McpNotFoundException) { sessionLog = null; }
            var turn = sessionLog?.Turns?.SingleOrDefault(item => item.RequestId == request);
            reply = turn is null ? null : await ReadHostTurnShapeAsync(root, turn).ConfigureAwait(false);
        }
        else if (operation == "Begin")
        {
            _ = await client.OpenSessionAsync(agent, session, "Synthetic host contract", "contract-fixture").ConfigureAwait(false);
            reply = await client.BeginTurnAsync(agent, session, request,
                queryTitle: Text(data, "queryTitle"), queryText: Text(data, "queryText"),
                model: "contract-fixture", planFile: "None", todoId: "None").ConfigureAwait(false);
        }
        else
        {
            var payload = JsonSerializer.Deserialize<ClientTurn>(data.GetRawText(), JsonOptions)
                ?? throw new InvalidOperationException("Actual terminal data missing.");
            reply = operation switch
            {
                "Complete" => await client.CompleteTurnAsync(agent, session, request, payload).ConfigureAwait(false),
                "Fail" => await client.FailTurnAsync(agent, session, request, payload).ConfigureAwait(false),
                _ => throw new ArgumentOutOfRangeException(nameof(operation)),
            };
        }
        _trace.Enqueue(new { operation = "TypedHostTransport", hostOperation = operation, root = root.Clone(), requests = handler.Requests.ToArray() });
        return JsonSerializer.Serialize(new { reply, requests = handler.Requests.ToArray(), databasePath = DatabasePath }, JsonOptions);
    }

    private async Task<object> ReadHostTurnShapeAsync(JsonElement root, ClientTurn turn)
    {
        var workspace = Text(root, "workspacePath");
        var agent = Text(root, "agentIdentity");
        var session = Text(root, "mcpSessionId");
        var request = Text(root, "requestId");
        await using var db = OpenDb(workspacePath: workspace);
        var workspaceId = db.CurrentWorkspaceId;
        // Child sets intentionally have no global filter. Resolve the exact parent
        // workspace/session/turn first; every child query uses that unique parent ID.
        var owning = await db.SessionLogTurns.AsNoTracking().SingleAsync(row =>
            row.SessionLog!.WorkspaceId == workspaceId && row.SessionLog.SourceType == agent
            && row.SessionLog.SessionId == session && row.RequestId == request).ConfigureAwait(false);
        var actions = await db.SessionLogActions.AsNoTracking().Where(row => row.SessionLogTurnId == owning.Id)
            .OrderBy(row => row.AuditEventOrder).ThenBy(row => row.Id)
            .Select(row => new { eventId = row.AuditEventId, body = new { row.Order, row.Type, row.Description, row.Status, row.FilePath } })
            .ToArrayAsync().ConfigureAwait(false);
        var dialog = await db.SessionLogProcessingDialogs.AsNoTracking().Where(row => row.SessionLogTurnId == owning.Id)
            .OrderBy(row => row.Ordinal).ThenBy(row => row.Id)
            .Select(row => new { eventId = row.AuditEventId, body = new { row.Content, row.Category, row.Role, row.OriginalTimestamp } })
            .ToArrayAsync().ConfigureAwait(false);
        var decisions = await db.SessionLogTurnStringLists.AsNoTracking().Where(row => row.SessionLogTurnId == owning.Id && row.ListType == "DesignDecision")
            .OrderBy(row => row.Ordinal).ThenBy(row => row.Id)
            .Select(row => new { eventId = row.AuditEventId, conclusion = row.Value }).ToArrayAsync().ConfigureAwait(false);
        return new
        {
            workspacePath = workspace, agentIdentity = agent, mcpSessionId = session, requestId = request,
            turn.QueryText, turn.QueryTitle, response = turn.Response ?? string.Empty, turn.Status,
            failureNote = turn.FailureNote ?? string.Empty, actions, decisions, dialog,
        };
    }

    private sealed class HostLifecycleHandler(ServerAuditSqliteFixture fixture, JsonElement root) : HttpMessageHandler
    {
        internal List<object> Requests { get; } = [];

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri ?? throw new InvalidOperationException("Actual typed client omitted URI.");
            if (uri.Host != "host-fixture.invalid") throw new InvalidOperationException("Injected host handler refuses another host.");
            var agent = Text(root, "agentIdentity");
            var session = Text(root, "mcpSessionId");
            var requestId = Text(root, "requestId");
            var workspace = Text(root, "workspacePath");
            var prefix = $"/qbrainai/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(session)}";
            var getPath = $"/mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(session)}";
            var raw = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Requests.Add(new { method = request.Method.Method, path = uri.AbsolutePath, body = raw });
            await using var db = fixture.OpenDb(workspacePath: workspace);
            var controller = new SessionLogController(Service(db, workspace), NullLogger<SessionLogController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            };
            IActionResult result;
            if (request.Method == HttpMethod.Get && uri.AbsolutePath == getPath)
            {
                var response = await controller.GetByIdAsync(agent, session, cancellationToken).ConfigureAwait(false);
                result = response.Result ?? throw new InvalidOperationException("Actual GET omitted action result.");
            }
            else if (request.Method == HttpMethod.Post && uri.AbsolutePath == prefix + "/open")
                result = await controller.OpenSessionAsync(agent, session, JsonSerializer.Deserialize<SessionLifecycleOpenRequest>(raw, JsonOptions), cancellationToken).ConfigureAwait(false);
            else if (request.Method == HttpMethod.Post && uri.AbsolutePath == prefix + "/" + Uri.EscapeDataString(requestId) + "/begin")
                result = await controller.BeginTurnAsync(agent, session, requestId, JsonSerializer.Deserialize<SessionLifecycleBeginRequest>(raw, JsonOptions), cancellationToken).ConfigureAwait(false);
            else if (request.Method == HttpMethod.Post && uri.AbsolutePath == prefix + "/" + Uri.EscapeDataString(requestId) + "/complete")
                result = await controller.CompleteTurnAsync(agent, session, requestId, JsonSerializer.Deserialize<UnifiedRequestEntryDto>(raw, JsonOptions), cancellationToken).ConfigureAwait(false);
            else if (request.Method == HttpMethod.Post && uri.AbsolutePath == prefix + "/" + Uri.EscapeDataString(requestId) + "/fail")
                result = await controller.FailTurnAsync(agent, session, requestId, JsonSerializer.Deserialize<UnifiedRequestEntryDto>(raw, JsonOptions), cancellationToken).ConfigureAwait(false);
            else throw new InvalidOperationException("Unexpected actual typed host route or method.");
            var status = result switch { ObjectResult value => value.StatusCode ?? 200, StatusCodeResult value => value.StatusCode, _ => throw new InvalidOperationException("Unexpected controller result.") };
            return new HttpResponseMessage((HttpStatusCode)status)
            {
                RequestMessage = request,
                Content = new StringContent(result is ObjectResult body ? JsonSerializer.Serialize(body.Value, JsonOptions) : string.Empty, Encoding.UTF8, "application/json"),
            };
        }
    }
}
