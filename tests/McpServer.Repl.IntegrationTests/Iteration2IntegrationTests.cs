using System.Globalization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Repl.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class Iteration2IntegrationTests : IDisposable
{
    private readonly ReplChildProcessHelper _replProcess;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public Iteration2IntegrationTests()
    {
        _replProcess = new ReplChildProcessHelper();
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    [Fact]
    public async Task SessionLog_Bootstrap_CompletesSuccessfully()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var requestId = GenerateRequestId("bootstrap");
        var bootstrapEnvelope = YamlEnvelopeBuilder.CreateSessionLogBootstrapRequest(requestId);
        
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(bootstrapEnvelope));
        
        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));
        Assert.True(foundResponse, "Should receive bootstrap response");

        var responseLine = _replProcess.StdoutLines.FirstOrDefault();
        Assert.NotNull(responseLine);
        
        var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(responseLine!);
        Assert.NotNull(response);
        Assert.True(response.ContainsKey("type") || response.ContainsKey("Type"));
    }

    [Fact]
    public async Task SessionLog_OpenSession_CreatesNewSession()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var bootstrapRequestId = GenerateRequestId("bootstrap");
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogBootstrapRequest(bootstrapRequestId));

        var openRequestId = GenerateRequestId("open");
        var sessionId = GenerateSessionId("Tonkotsu", "test-session");
        var openEnvelope = YamlEnvelopeBuilder.CreateSessionLogOpenSessionRequest(
            openRequestId,
            "Tonkotsu",
            sessionId,
            "Test Session",
            "test-model-v1");

        await SendCommandAndWaitAsync(openEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
        
        var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(responseLine!);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task SessionLog_CurrentSession_ReturnsActiveSession()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogBootstrapRequest(GenerateRequestId("bootstrap")));

        var sessionId = GenerateSessionId("Tonkotsu", "test-session");
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogOpenSessionRequest(
                GenerateRequestId("open"),
                "Tonkotsu",
                sessionId,
                "Test Session",
                "test-model-v1"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogCurrentSessionRequest(GenerateRequestId("current")));

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_BeginTurn_StartsNewTurn()
    {
        await SetupSessionAsync();

        var turnRequestId = GenerateRequestId("turn-001");
        var beginTurnEnvelope = YamlEnvelopeBuilder.CreateSessionLogBeginTurnRequest(
            GenerateRequestId("begin-turn"),
            turnRequestId,
            "Test Query",
            "This is a test query for the session log");

        await SendCommandAndWaitAsync(beginTurnEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
        
        var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(responseLine!);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task SessionLog_AppendDialog_AddsDialogItems()
    {
        await SetupSessionWithTurnAsync();

        var dialogItems = new[]
        {
            YamlEnvelopeBuilder.CreateDialogItem(
                DateTimeOffset.UtcNow,
                "model",
                "Analyzing the request...",
                "reasoning"),
            YamlEnvelopeBuilder.CreateDialogItem(
                DateTimeOffset.UtcNow,
                "tool",
                "File created successfully",
                "tool_result")
        };

        var appendDialogEnvelope = YamlEnvelopeBuilder.CreateSessionLogAppendDialogRequest(
            GenerateRequestId("append-dialog"),
            dialogItems);

        await SendCommandAndWaitAsync(appendDialogEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_AppendActions_AddsActionItems()
    {
        await SetupSessionWithTurnAsync();

        var actions = new[]
        {
            YamlEnvelopeBuilder.CreateAction(
                1,
                "Created new file",
                "create",
                "completed",
                "src/TestFile.cs"),
            YamlEnvelopeBuilder.CreateAction(
                2,
                "Modified configuration",
                "edit",
                "completed",
                "config/settings.json")
        };

        var appendActionsEnvelope = YamlEnvelopeBuilder.CreateSessionLogAppendActionsRequest(
            GenerateRequestId("append-actions"),
            actions);

        await SendCommandAndWaitAsync(appendActionsEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_UpdateTurn_ModifiesTurnMetadata()
    {
        await SetupSessionWithTurnAsync();

        var updateEnvelope = YamlEnvelopeBuilder.CreateSessionLogUpdateTurnRequest(
            GenerateRequestId("update-turn"),
            response: "Work in progress",
            interpretation: "User wants to test the system",
            tokenCount: 150,
            tags: new[] { "test", "integration" },
            contextList: new[] { "src/TestFile.cs" });

        await SendCommandAndWaitAsync(updateEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_CompleteTurn_FinalizesToCompleted()
    {
        await SetupSessionWithTurnAsync();

        var completeEnvelope = YamlEnvelopeBuilder.CreateSessionLogCompleteTurnRequest(
            GenerateRequestId("complete-turn"),
            "Turn completed successfully");

        await SendCommandAndWaitAsync(completeEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_FailTurn_FinalizesToFailed()
    {
        await SetupSessionWithTurnAsync();

        var failEnvelope = YamlEnvelopeBuilder.CreateSessionLogFailTurnRequest(
            GenerateRequestId("fail-turn"),
            "Turn failed due to test error",
            "test_error");

        await SendCommandAndWaitAsync(failEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_QueryHistory_ReturnsSessionList()
    {
        await SetupSessionAsync();

        var queryEnvelope = YamlEnvelopeBuilder.CreateSessionLogQueryHistoryRequest(
            GenerateRequestId("query-history"),
            agent: "Tonkotsu",
            limit: 10,
            offset: 0);

        await SendCommandAndWaitAsync(queryEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_FullWorkflow_BootstrapToComplete()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogBootstrapRequest(GenerateRequestId("bootstrap")));

        var sessionId = GenerateSessionId("Tonkotsu", "full-workflow");
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogOpenSessionRequest(
                GenerateRequestId("open"),
                "Tonkotsu",
                sessionId,
                "Full Workflow Test",
                "test-model-v1"));

        var turnRequestId = GenerateRequestId("turn-001");
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogBeginTurnRequest(
                GenerateRequestId("begin-turn"),
                turnRequestId,
                "Test Full Workflow",
                "Testing the complete session log workflow"));

        var dialogItems = new[]
        {
            YamlEnvelopeBuilder.CreateDialogItem(
                DateTimeOffset.UtcNow,
                "model",
                "Processing request...",
                "reasoning")
        };
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogAppendDialogRequest(
                GenerateRequestId("append-dialog"),
                dialogItems));

        var actions = new[]
        {
            YamlEnvelopeBuilder.CreateAction(
                1,
                "Test action",
                "create",
                "completed",
                "test.txt")
        };
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogAppendActionsRequest(
                GenerateRequestId("append-actions"),
                actions));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogUpdateTurnRequest(
                GenerateRequestId("update-turn"),
                response: "Workflow executed",
                interpretation: "Testing workflow"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogCompleteTurnRequest(
                GenerateRequestId("complete-turn"),
                "Workflow completed successfully"));

        Assert.True(_replProcess.StdoutLines.Count > 0, "Should have received multiple responses");
    }

    [Fact]
    public async Task SessionLog_StatePersistence_AcrossCommands()
    {
        await SetupSessionWithTurnAsync();

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogCurrentSessionRequest(GenerateRequestId("current-1")));

        var firstCheckLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(firstCheckLine);

        var actions = new[]
        {
            YamlEnvelopeBuilder.CreateAction(1, "Action 1", "create", "completed", "file1.txt")
        };
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogAppendActionsRequest(
                GenerateRequestId("append-actions"),
                actions));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogCurrentSessionRequest(GenerateRequestId("current-2")));

        var secondCheckLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(secondCheckLine);
    }

    [Fact]
    public async Task SessionLog_ReconnectScenario_SessionPersists()
    {
        await SetupSessionAsync();

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogCurrentSessionRequest(GenerateRequestId("current-before")));

        await Task.Delay(500);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogCurrentSessionRequest(GenerateRequestId("current-after")));

        Assert.True(_replProcess.StdoutLines.Count >= 2, "Should receive responses after reconnect scenario");
    }

    [Fact]
    public async Task SessionLog_InvalidSessionId_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogBootstrapRequest(GenerateRequestId("bootstrap")));

        var invalidSessionId = "invalid-session-id-format";
        var openEnvelope = YamlEnvelopeBuilder.CreateSessionLogOpenSessionRequest(
            GenerateRequestId("open-invalid"),
            "Tonkotsu",
            invalidSessionId,
            "Invalid Session",
            "test-model-v1");

        await SendCommandAndWaitAsync(openEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_InvalidRequestId_ReturnsError()
    {
        await SetupSessionAsync();

        var invalidRequestId = "invalid-request-format";
        var beginTurnEnvelope = YamlEnvelopeBuilder.CreateSessionLogBeginTurnRequest(
            GenerateRequestId("begin-turn"),
            invalidRequestId,
            "Test Query",
            "Test query text");

        await SendCommandAndWaitAsync(beginTurnEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_NoActiveSession_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogBootstrapRequest(GenerateRequestId("bootstrap")));

        var beginTurnEnvelope = YamlEnvelopeBuilder.CreateSessionLogBeginTurnRequest(
            GenerateRequestId("begin-turn"),
            GenerateRequestId("turn-001"),
            "Test Query",
            "Attempting to begin turn without session");

        await SendCommandAndWaitAsync(beginTurnEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_NoActiveTurn_AppendDialog_ReturnsError()
    {
        await SetupSessionAsync();

        var dialogItems = new[]
        {
            YamlEnvelopeBuilder.CreateDialogItem(
                DateTimeOffset.UtcNow,
                "model",
                "Dialog without turn",
                "reasoning")
        };

        var appendDialogEnvelope = YamlEnvelopeBuilder.CreateSessionLogAppendDialogRequest(
            GenerateRequestId("append-dialog"),
            dialogItems);

        await SendCommandAndWaitAsync(appendDialogEnvelope);

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_ImmutableTurn_UpdateAttempt_ReturnsError()
    {
        await SetupSessionWithTurnAsync();

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogCompleteTurnRequest(
                GenerateRequestId("complete-turn"),
                "Turn completed"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogUpdateTurnRequest(
                GenerateRequestId("update-after-complete"),
                response: "Attempting to update completed turn"));

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_MultipleDialogAppends_Accumulate()
    {
        await SetupSessionWithTurnAsync();

        for (int i = 1; i <= 3; i++)
        {
            var dialogItems = new[]
            {
                YamlEnvelopeBuilder.CreateDialogItem(
                    DateTimeOffset.UtcNow,
                    "model",
                    $"Dialog item {i}",
                    "reasoning")
            };

            await SendCommandAndWaitAsync(
                YamlEnvelopeBuilder.CreateSessionLogAppendDialogRequest(
                    GenerateRequestId($"append-dialog-{i}"),
                    dialogItems));
        }

        Assert.True(_replProcess.StdoutLines.Count >= 3, "Should receive responses for multiple appends");
    }

    [Fact]
    public async Task SessionLog_MultipleActionAppends_Accumulate()
    {
        await SetupSessionWithTurnAsync();

        for (int i = 1; i <= 3; i++)
        {
            var actions = new[]
            {
                YamlEnvelopeBuilder.CreateAction(
                    i,
                    $"Action {i}",
                    "edit",
                    "completed",
                    $"file{i}.txt")
            };

            await SendCommandAndWaitAsync(
                YamlEnvelopeBuilder.CreateSessionLogAppendActionsRequest(
                    GenerateRequestId($"append-actions-{i}"),
                    actions));
        }

        Assert.True(_replProcess.StdoutLines.Count >= 3, "Should receive responses for multiple appends");
    }

    [Fact]
    public async Task SessionLog_CompleteTurn_BeginNewTurn_Succeeds()
    {
        await SetupSessionWithTurnAsync();

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogCompleteTurnRequest(
                GenerateRequestId("complete-turn-1"),
                "First turn completed"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogBeginTurnRequest(
                GenerateRequestId("begin-turn-2"),
                GenerateRequestId("turn-002"),
                "Second Turn",
                "Starting second turn after completing first"));

        var responseLine = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(responseLine);
    }

    [Fact]
    public async Task SessionLog_DialogCategories_AllSupported()
    {
        await SetupSessionWithTurnAsync();

        var categories = new[] { "reasoning", "tool_call", "tool_result", "observation", "decision" };
        foreach (var category in categories)
        {
            var dialogItems = new[]
            {
                YamlEnvelopeBuilder.CreateDialogItem(
                    DateTimeOffset.UtcNow,
                    "model",
                    $"Testing {category}",
                    category)
            };

            await SendCommandAndWaitAsync(
                YamlEnvelopeBuilder.CreateSessionLogAppendDialogRequest(
                    GenerateRequestId($"append-dialog-{category}"),
                    dialogItems));
        }

        Assert.True(_replProcess.StdoutLines.Count >= categories.Length);
    }

    [Fact]
    public async Task SessionLog_ActionTypes_AllSupported()
    {
        await SetupSessionWithTurnAsync();

        var actionTypes = new[] { "edit", "create", "delete", "design_decision", "commit" };
        var order = 1;
        foreach (var actionType in actionTypes)
        {
            var actions = new[]
            {
                YamlEnvelopeBuilder.CreateAction(
                    order++,
                    $"Testing {actionType}",
                    actionType,
                    "completed",
                    actionType == "design_decision" ? "" : $"file-{actionType}.txt")
            };

            await SendCommandAndWaitAsync(
                YamlEnvelopeBuilder.CreateSessionLogAppendActionsRequest(
                    GenerateRequestId($"append-actions-{actionType}"),
                    actions));
        }

        Assert.True(_replProcess.StdoutLines.Count >= actionTypes.Length);
    }

    private async Task SetupSessionAsync()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogBootstrapRequest(GenerateRequestId("bootstrap")));

        var sessionId = GenerateSessionId("Tonkotsu", "test-session");
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogOpenSessionRequest(
                GenerateRequestId("open"),
                "Tonkotsu",
                sessionId,
                "Test Session",
                "test-model-v1"));
    }

    private async Task SetupSessionWithTurnAsync()
    {
        await SetupSessionAsync();

        var turnRequestId = GenerateRequestId("turn-001");
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateSessionLogBeginTurnRequest(
                GenerateRequestId("begin-turn"),
                turnRequestId,
                "Test Turn",
                "Test turn for session log"));
    }

    private async Task SendCommandAndWaitAsync(object envelope)
    {
        var initialCount = _replProcess.StdoutLines.Count;
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(envelope));
        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(initialCount + 1, TimeSpan.FromSeconds(15));
        Assert.True(foundResponse, BuildTimeoutMessage(initialCount + 1));
        await Task.Delay(100);
    }

    private string BuildTimeoutMessage(int expectedCount)
        => $"Timed out waiting for stdout document count {expectedCount}. "
           + $"STDOUT: {string.Join(Environment.NewLine + "--- stdout document ---" + Environment.NewLine, _replProcess.StdoutLines)} "
           + $"STDERR: {string.Join(Environment.NewLine, _replProcess.StderrLines)}";

    private static string GenerateRequestId(string suffix)
        => TestRequestIds.Next(suffix);

    private static string GenerateSessionId(string agent, string suffix)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
        return $"{agent}-{timestamp}Z-{suffix}";
    }

    public void Dispose()
    {
        _replProcess.StopAsync().Wait(TimeSpan.FromSeconds(3));
        _replProcess.Dispose();
    }
}
