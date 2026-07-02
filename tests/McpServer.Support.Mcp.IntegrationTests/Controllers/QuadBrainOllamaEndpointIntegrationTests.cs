using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.McpAgent;
using McpServer.QBAgent;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-QBOLLAMA-001: Exercises the OpenAI-compatible QuadBrain endpoint against the local Ollama server,
/// proving FR-MCP-134 and FR-MCP-QBOPENAI-001 execute the normal Left/Right/Arbiter workflow without faking the LLM calls.
/// </summary>
[Trait("Category", "Integration")]
public sealed class QuadBrainOllamaEndpointIntegrationTests
{
    private const string Endpoint = "v1/chat/completions";
    private const string SourceType = "QBAgent";
    private const string QBAgentVisibleModel = "QuadBrain";
    private const string QBAgentVisibleEndpoint = "McpServer /v1/chat/completions";
    private const string OllamaOpenAiEndpoint = "http://localhost:11434/v1";
    private const string OllamaTagsEndpoint = "http://localhost:11434/api/tags";
    private static readonly object ArtifactGate = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions ArtifactJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    /// <summary>
    /// TEST-MCP-QBOLLAMA-001: Assigns the local Ollama default model to all four slots and verifies representative
    /// prompts trigger the normal Left, Right, and Arbiter workflow with non-empty OpenAI-compatible responses.
    /// </summary>
    [Theory]
    [InlineData("Hello")]
    [InlineData("Create Hello World in C++")]
    public async Task ChatCompletions_LocalOllamaDefaultModel_TriggersNormalBrainSlots(string prompt)
    {
        var modelId = await ResolveDefaultOllamaModelAsync().ConfigureAwait(true);
        var chatClientFactory = new RecordingBrainSlotChatClientFactory();
        using var app = BuildFactory(chatClientFactory);
        using var seedClient = SeedClient(app);
        var promptSlug = NormalizeIdentifierSuffix(prompt);
        var sessionId = BuildSessionId("ollama-" + promptSlug);
        var turnId = NewRequestId("ollama-" + promptSlug);

        await SeedOllamaQuadAsync(seedClient, modelId).ConfigureAwait(true);
        await AssertQuadReadyAsync(seedClient).ConfigureAwait(true);
        await OpenSessionAsync(seedClient, sessionId).ConfigureAwait(true);
        await BeginTurnAsync(seedClient, sessionId, turnId, prompt).ConfigureAwait(true);

        using var client = Authorized(app, sessionId, turnId);
        var body = await PostAsync(client, RequestWithPrompt(prompt)).ConfigureAwait(true);

        var choice = Assert.Single(body.Choices);
        Assert.Equal("qbagent", body.Model);
        Assert.Equal("stop", choice.FinishReason);
        Assert.False(
            string.IsNullOrWhiteSpace(choice.Message.Content),
            "Expected non-empty assistant content. Per-role Ollama outputs: " + string.Join(" | ", chatClientFactory.InvokedOutputs));
        AssertQBAgentWorkflowRoles(chatClientFactory.InvokedRoles);
        Assert.All(chatClientFactory.InvokedModelIds, value => Assert.Equal(modelId, value));
        Assert.All(chatClientFactory.InvokedEndpoints, value => Assert.Equal(OllamaOpenAiEndpoint, value));
        await AssertSessionLogCapturedBrainOutputsAsync(
                seedClient,
                nameof(ChatCompletions_LocalOllamaDefaultModel_TriggersNormalBrainSlots),
                prompt,
                sessionId,
                turnId,
                modelId,
                OllamaOpenAiEndpoint,
                chatClientFactory.InvokedRoles,
                chatClientFactory.InvokedOutputs,
                choice.Message.Content)
            .ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBOLLAMA-001: Sends a representative prompt through QBAgent and fails if QBAgent does not display the
    /// QuadBrain response returned by the OpenAI-compatible endpoint without leaking the underlying Ollama model.
    /// </summary>
    [Theory]
    [InlineData("Hello")]
    public async Task QBAgentRunLoop_LocalOllamaDefaultModel_DisplaysQuadBrainResponse(string prompt)
    {
        var modelId = await ResolveDefaultOllamaModelAsync().ConfigureAwait(true);
        var chatClientFactory = new RecordingBrainSlotChatClientFactory();
        using var app = BuildFactory(chatClientFactory);
        using var seedClient = SeedClient(app);
        var promptSlug = NormalizeIdentifierSuffix(prompt);
        var sessionId = BuildSessionId("qbagent-ollama-" + promptSlug);
        var turnId = NewRequestId("qbagent-ollama-" + promptSlug);

        await SeedOllamaQuadAsync(seedClient, modelId).ConfigureAwait(true);
        await AssertQuadReadyAsync(seedClient).ConfigureAwait(true);
        await OpenSessionAsync(seedClient, sessionId).ConfigureAwait(true);
        await BeginTurnAsync(seedClient, sessionId, turnId, prompt).ConfigureAwait(true);

        var token = seedClient.DefaultRequestHeaders.GetValues("X-Api-Key").First();
        using var qbagentTransport = new HttpClient(app.Server.CreateHandler()) { Timeout = TimeSpan.FromMinutes(10) };
        qbagentTransport.DefaultRequestHeaders.Add("X-Session-Id", sessionId);
        qbagentTransport.DefaultRequestHeaders.Add("X-Turn-Id", turnId);
        using var chatClient = QBAgentChatClientFactory.Create(
            new McpAgentOptions { BaseUrl = new Uri("http://localhost"), ApiKey = token },
            qbagentTransport);
        string? qbagentResponse = null;
        QBAgentPromptRunner runner = async (value, _) =>
        {
            var response = await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, value)]).ConfigureAwait(false);
            qbagentResponse = ExtractResponseText(response);
            return qbagentResponse;
        };
        using var input = new StringReader(prompt + Environment.NewLine + "exit" + Environment.NewLine);
        using var output = new StringWriter();

        var processed = await QBAgentRunLoop.RunAsync(runner, input, output).ConfigureAwait(true);

        var displayed = output.ToString();
        Assert.Equal(1, processed);
        Assert.False(string.IsNullOrWhiteSpace(qbagentResponse), "Expected QBAgent to receive a non-empty QuadBrain response.");
        var receivedResponse = qbagentResponse!;
        Assert.False(
            string.Equals("OK", receivedResponse.Trim(), StringComparison.OrdinalIgnoreCase),
            "QBAgent must not pass with a placeholder acknowledgement.");
        Assert.DoesNotContain("(no response)", displayed, StringComparison.Ordinal);
        Assert.Contains(receivedResponse.Trim(), displayed, StringComparison.Ordinal);
        var qbagentTranscriptPath = WriteQBAgentInteractionTranscript(
            nameof(QBAgentRunLoop_LocalOllamaDefaultModel_DisplaysQuadBrainResponse),
            prompt,
            sessionId,
            turnId,
            processed,
            receivedResponse,
            displayed,
            chatClientFactory.InvokedRoles,
            chatClientFactory.InvokedOutputs);
        Assert.True(File.Exists(qbagentTranscriptPath), $"Expected QBAgent interaction transcript at {qbagentTranscriptPath}.");
        Assert.Contains(
            File.ReadLines(qbagentTranscriptPath),
            line => line.Contains("\"recordType\":\"qbagentDisplayedOutput\"", StringComparison.Ordinal));
        var qbagentTranscript = File.ReadAllText(qbagentTranscriptPath);
        Assert.Contains("\"model\":\"QuadBrain\"", qbagentTranscript, StringComparison.Ordinal);
        Assert.DoesNotContain(modelId, qbagentTranscript, StringComparison.Ordinal);
        Assert.Contains("\"endpoint\":\"McpServer /v1/chat/completions\"", qbagentTranscript, StringComparison.Ordinal);
        Assert.DoesNotContain("http://localhost/v1", qbagentTranscript, StringComparison.Ordinal);
        Assert.DoesNotContain(OllamaOpenAiEndpoint, qbagentTranscript, StringComparison.Ordinal);
        AssertNormalWorkflowRoles(chatClientFactory.InvokedRoles);
        Assert.All(chatClientFactory.InvokedModelIds, value => Assert.Equal(modelId, value));
        Assert.All(chatClientFactory.InvokedEndpoints, value => Assert.Equal(OllamaOpenAiEndpoint, value));
        await AssertSessionLogCapturedBrainOutputsAsync(
                seedClient,
                nameof(QBAgentRunLoop_LocalOllamaDefaultModel_DisplaysQuadBrainResponse),
                prompt,
                sessionId,
                turnId,
                modelId,
                OllamaOpenAiEndpoint,
                chatClientFactory.InvokedRoles,
                chatClientFactory.InvokedOutputs,
                receivedResponse)
            .ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-QBOLLAMA-001: OpenAI-compatible content, reasoning, and reasoning_content outputs are persisted to
    /// the MCP session processing dialog for the turn that invoked QuadBrain.
    /// </summary>
    [Fact]
    public async Task ChatCompletions_OpenAiCompatibleFieldVariants_AreLoggedToSessionTurn()
    {
        var chatClientFactory = new FieldVariantBrainSlotChatClientFactory();
        using var app = BuildFactory(chatClientFactory);
        using var seedClient = SeedClient(app);
        var sessionId = BuildSessionId("field-variants");
        var turnId = NewRequestId("field-variants");

        await SeedOllamaQuadAsync(seedClient, "field-variant-model").ConfigureAwait(true);
        await AssertQuadReadyAsync(seedClient).ConfigureAwait(true);
        await OpenSessionAsync(seedClient, sessionId).ConfigureAwait(true);
        await BeginTurnAsync(seedClient, sessionId, turnId).ConfigureAwait(true);

        using var client = Authorized(app, sessionId, turnId);
        var body = await PostAsync(client, RequestWithPrompt("Hello")).ConfigureAwait(true);

        var choice = Assert.Single(body.Choices);
        Assert.Equal("reasoning-content-field-captured", choice.Message.Content);
        await AssertSessionLogCapturedBrainOutputsAsync(
                seedClient,
                nameof(ChatCompletions_OpenAiCompatibleFieldVariants_AreLoggedToSessionTurn),
                "Hello",
                sessionId,
                turnId,
                "field-variant-model",
                OllamaOpenAiEndpoint,
                [BrainSlotRoles.LeftHemisphere, BrainSlotRoles.RightHemisphere, BrainSlotRoles.ArbiterOfTruth],
                chatClientFactory.InvokedOutputs,
                choice.Message.Content)
            .ConfigureAwait(true);
    }

    private static CustomWebApplicationFactory BuildFactory(IBrainSlotChatClientFactory chatClientFactory)
        => new(
            services =>
            {
                services.RemoveAll<IBrainSlotChatClientFactory>();
                services.AddSingleton<IBrainSlotChatClientFactory>(chatClientFactory);
                services.RemoveAll<IBrainSlotCredentialResolver>();
                services.AddSingleton<IBrainSlotCredentialResolver, StubCredentialResolver>();
                services.RemoveAll<ITurnTransactionCoordinator>();
                services.AddSingleton<ITurnTransactionCoordinator, CommittingTurnTransactionCoordinator>();
            },
            new Dictionary<string, string?>
            {
                ["Mcp:BrainSlots:ExecutionEnabled"] = "true",
                ["Mcp:BrainSlots:AllowLoopbackEndpoints"] = "true",
                ["Mcp:BrainSlots:AllowedEndpointHosts:0"] = "localhost",
                ["Mcp:TurnTransactions:Enabled"] = "true",
                ["Mcp:TurnTransactions:RequiredForMutations"] = "true",
            });

    private static async Task SeedOllamaQuadAsync(HttpClient client, string modelId)
    {
        foreach (var role in BrainSlotRoles.All)
        {
            var response = await client.PutAsJsonAsync(
                new Uri($"mcpserver/brain-slots/{role.ToLowerInvariant()}-ollama", UriKind.Relative),
                new UpsertBrainSlotRequest
                {
                    Role = role,
                    ProviderKind = "OpenAICompatible",
                    ModelId = modelId,
                    Endpoint = OllamaOpenAiEndpoint,
                    CredentialReference = "env:OLLAMA_TEST_API_KEY",
                    Enabled = true,
                    TimeoutSeconds = 300,
                    MaxOutputTokens = 96,
                    SystemPrompt = BuildSystemPrompt(role),
                    ReplaceExisting = true,
                }).ConfigureAwait(true);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            Assert.True(response.IsSuccessStatusCode, responseText);
        }
    }

    private static async Task AssertQuadReadyAsync(HttpClient client)
    {
        var status = await client.GetFromJsonAsync<BrainSlotStatusResponse>(
            new Uri("mcpserver/brain-slots/status", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(status);
        Assert.True(status!.QuadReady, string.Join("; ", status.ValidationErrors));
    }

    private static async Task OpenSessionAsync(HttpClient client, string sessionId)
    {
        var response = await client.PostAsJsonAsync(
            new Uri($"mcpserver/sessionlog/{SourceType}/{sessionId}/open", UriKind.Relative),
            new { title = "QuadBrain Ollama integration test", model = "qbagent" }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task BeginTurnAsync(HttpClient client, string sessionId, string turnId, string prompt = "Hello")
    {
        var response = await client.PostAsJsonAsync(
            new Uri($"mcpserver/sessionlog/{SourceType}/{sessionId}/{turnId}/begin", UriKind.Relative),
            new { queryTitle = "QuadBrain Ollama " + prompt, queryText = prompt, model = "qbagent" }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task AssertSessionLogCapturedBrainOutputsAsync(
        HttpClient client,
        string testName,
        string prompt,
        string sessionId,
        string turnId,
        string modelId,
        string endpoint,
        IReadOnlyList<string> expectedRoles,
        IReadOnlyList<string> expectedOutputs,
        string? expectedResponse)
    {
        var session = await client.GetFromJsonAsync<UnifiedSessionLogDto>(
            new Uri($"mcpserver/sessionlog/{SourceType}/{sessionId}", UriKind.Relative)).ConfigureAwait(true);
        Assert.NotNull(session);
        var turn = Assert.Single(session!.Turns!, item => string.Equals(item.RequestId, turnId, StringComparison.Ordinal));
        Assert.Equal(prompt, turn.QueryText);
        Assert.Equal("completed", turn.Status);
        Assert.False(string.IsNullOrWhiteSpace(turn.Response), $"Expected completed session turn {turnId} to include the assistant response.");
        Assert.Equal(expectedResponse, turn.Response);
        var dialog = turn.ProcessingDialog ?? [];
        Assert.True(dialog.Count >= expectedRoles.Count * 2, $"Expected prompt/output dialog for invoked roles, found {dialog.Count}.");
        foreach (var role in expectedRoles)
        {
            Assert.Contains(dialog, item => item.Content?.Contains($"[{role}] prompt:", StringComparison.Ordinal) == true);
            Assert.Contains(dialog, item => item.Content?.Contains($"[{role}] output:", StringComparison.Ordinal) == true);
        }

        var nonEmptyOutputs = expectedOutputs.Where(static output => !string.IsNullOrWhiteSpace(output)).ToList();
        Assert.Equal(expectedRoles.Count, nonEmptyOutputs.Count);
        foreach (var output in nonEmptyOutputs)
            Assert.Contains(dialog, item => item.Content?.Contains(output, StringComparison.Ordinal) == true);

        var (artifactPath, transcriptPath) = WriteSessionArtifact(testName, prompt, sessionId, turnId, modelId, endpoint, expectedOutputs, session!);
        Assert.True(File.Exists(artifactPath), $"Expected session artifact at {artifactPath}.");
        Assert.True(File.Exists(transcriptPath), $"Expected session transcript at {transcriptPath}.");
        Assert.Contains(File.ReadLines(transcriptPath), line => line.Contains("\"recordType\":\"processingDialog\"", StringComparison.Ordinal));
    }

    private static (string ArtifactPath, string TranscriptPath) WriteSessionArtifact(
        string testName,
        string prompt,
        string sessionId,
        string turnId,
        string modelId,
        string endpoint,
        IReadOnlyList<string> selectedOutputs,
        UnifiedSessionLogDto session)
    {
        var artifactRoot = Path.Combine(
            CustomWebApplicationFactory.ResolveSolutionRoot(),
            "TestResults",
            nameof(QuadBrainOllamaEndpointIntegrationTests));
        var artifactPath = Path.Combine(artifactRoot, $"{sessionId}.json");
        var transcriptPath = Path.Combine(artifactRoot, $"{sessionId}.transcript.jsonl");
        var indexPath = Path.Combine(artifactRoot, "session-artifacts.jsonl");
        var createdAtUtc = DateTimeOffset.UtcNow;
        var artifact = new
        {
            testName,
            prompt,
            sourceType = SourceType,
            sessionId,
            turnId,
            modelId,
            endpoint,
            createdAtUtc,
            sessionLogRoute = $"/mcpserver/sessionlog/{SourceType}/{sessionId}",
            turnRoute = $"/mcpserver/sessionlog/{SourceType}/{sessionId}/{turnId}",
            transcriptPath,
            selectedOutputCount = selectedOutputs.Count,
            selectedOutputs,
            session,
        };
        var index = new
        {
            testName,
            prompt,
            sourceType = SourceType,
            sessionId,
            turnId,
            modelId,
            endpoint,
            createdAtUtc,
            artifactPath,
            transcriptPath,
        };

        lock (ArtifactGate)
        {
            Directory.CreateDirectory(artifactRoot);
            File.WriteAllText(artifactPath, JsonSerializer.Serialize(artifact, ArtifactJsonOptions));
            File.WriteAllLines(transcriptPath, BuildSessionTranscriptJsonl(session, turnId, createdAtUtc));
            File.AppendAllText(indexPath, JsonSerializer.Serialize(index, JsonOptions) + Environment.NewLine);
        }

        return (artifactPath, transcriptPath);
    }

    private static string WriteQBAgentInteractionTranscript(
        string testName,
        string prompt,
        string sessionId,
        string turnId,
        int processedPromptCount,
        string responseText,
        string displayedOutput,
        IReadOnlyList<string> invokedRoles,
        IReadOnlyList<string> invokedOutputs)
    {
        var artifactRoot = Path.Combine(
            CustomWebApplicationFactory.ResolveSolutionRoot(),
            "TestResults",
            nameof(QuadBrainOllamaEndpointIntegrationTests));
        var createdAtUtc = DateTimeOffset.UtcNow;
        var promptSlug = NormalizeIdentifierSuffix(prompt);
        var transcriptPath = Path.Combine(artifactRoot, $"{sessionId}.qbagent-interactions.jsonl");
        var indexPath = Path.Combine(artifactRoot, "qbagent-interaction-artifacts.jsonl");
        var lines = BuildQBAgentInteractionTranscriptJsonl(
                testName,
                prompt,
                sessionId,
                turnId,
                processedPromptCount,
                responseText,
                displayedOutput,
                invokedRoles,
                invokedOutputs,
                createdAtUtc)
            .ToArray();
        var index = new
        {
            testName,
            prompt,
            promptSlug,
            sourceType = SourceType,
            sessionId,
            turnId,
            model = QBAgentVisibleModel,
            endpoint = QBAgentVisibleEndpoint,
            createdAtUtc,
            transcriptPath,
        };

        lock (ArtifactGate)
        {
            Directory.CreateDirectory(artifactRoot);
            File.WriteAllLines(transcriptPath, lines);
            File.AppendAllText(indexPath, JsonSerializer.Serialize(index, JsonOptions) + Environment.NewLine);
        }

        return transcriptPath;
    }

    private static IEnumerable<string> BuildQBAgentInteractionTranscriptJsonl(
        string testName,
        string prompt,
        string sessionId,
        string turnId,
        int processedPromptCount,
        string responseText,
        string displayedOutput,
        IReadOnlyList<string> invokedRoles,
        IReadOnlyList<string> invokedOutputs,
        DateTimeOffset createdAtUtc)
    {
        yield return JsonSerializer.Serialize(new
        {
            recordType = "qbagentRun",
            createdAtUtc,
            testName,
            sourceType = SourceType,
            sessionId,
            turnId,
            model = QBAgentVisibleModel,
            endpoint = QBAgentVisibleEndpoint,
            processedPromptCount,
        }, JsonOptions);
        yield return JsonSerializer.Serialize(new
        {
            recordType = "qbagentPrompt",
            createdAtUtc,
            sessionId,
            turnId,
            prompt,
        }, JsonOptions);
        yield return JsonSerializer.Serialize(new
        {
            recordType = "qbagentReceivedResponse",
            createdAtUtc,
            sessionId,
            turnId,
            responseText,
        }, JsonOptions);
        yield return JsonSerializer.Serialize(new
        {
            recordType = "qbagentDisplayedOutput",
            createdAtUtc,
            sessionId,
            turnId,
            displayedOutput,
        }, JsonOptions);

        for (var index = 0; index < invokedRoles.Count; index++)
        {
            yield return JsonSerializer.Serialize(new
            {
                recordType = "quadbrainRoleInteraction",
                createdAtUtc,
                sessionId,
                turnId,
                role = invokedRoles[index],
                output = index < invokedOutputs.Count ? invokedOutputs[index] : null,
            }, JsonOptions);
        }
    }

    private static IEnumerable<string> BuildSessionTranscriptJsonl(
        UnifiedSessionLogDto session,
        string turnId,
        DateTimeOffset createdAtUtc)
    {
        yield return JsonSerializer.Serialize(new
        {
            recordType = "session",
            createdAtUtc,
            session.SourceType,
            session.SessionId,
            session.Title,
            session.Model,
            session.Status,
            session.Started,
            session.LastUpdated,
            session.TurnCount,
            selectedTurnId = turnId,
        }, JsonOptions);

        foreach (var turn in session.Turns ?? [])
        {
            yield return JsonSerializer.Serialize(new
            {
                recordType = "turn",
                createdAtUtc,
                session.SourceType,
                session.SessionId,
                turn.RequestId,
                isSelectedTurn = string.Equals(turn.RequestId, turnId, StringComparison.Ordinal),
                turn.Timestamp,
                turn.QueryTitle,
                turn.QueryText,
                turn.Response,
                turn.Status,
                turn.Model,
            }, JsonOptions);

            var ordinal = 0;
            foreach (var item in turn.ProcessingDialog ?? [])
            {
                yield return JsonSerializer.Serialize(new
                {
                    recordType = "processingDialog",
                    createdAtUtc,
                    session.SourceType,
                    session.SessionId,
                    turn.RequestId,
                    ordinal = ordinal++,
                    item.Timestamp,
                    item.Role,
                    item.Category,
                    item.Content,
                }, JsonOptions);
            }
        }
    }

    private static string BuildSystemPrompt(string role)
    {
        if (string.Equals(role, BrainSlotRoles.ArbiterOfTruth, StringComparison.Ordinal))
        {
            return """
                You are the QuadBrain ArbiterOfTruth for a live integration test.
                Return plain displayable text only. Do not return JSON, tool_calls, hidden reasoning, or thinking steps.
                Never answer with a placeholder acknowledgement such as OK.
                If the user asks to create Hello World in C++, return one concise QBAgent action response:
                write hello.cpp with #include <iostream>, int main, and std::cout, then compile with g++ hello.cpp -o hello.
                For simple greetings, return one concise assistant response.
                """;
        }

        return $"""
            You are the QuadBrain {role} for a live integration test.
            Return one concise sentence only. Do not return hidden reasoning or thinking steps.
            Never answer with a placeholder acknowledgement such as OK.
            Produce role evidence for ArbiterOfTruth. For code creation prompts, include concrete implementation details.
            """;
    }

    private static void AssertQBAgentWorkflowRoles(IReadOnlyList<string> invokedRoles)
    {
        Assert.Contains(BrainSlotRoles.LeftHemisphere, invokedRoles);
        Assert.Contains(BrainSlotRoles.RightHemisphere, invokedRoles);
        Assert.Contains(BrainSlotRoles.ArbiterOfTruth, invokedRoles);
        Assert.DoesNotContain(BrainSlotRoles.CuriosityEngine, invokedRoles);
        var roles = invokedRoles.ToList();
        var arbiterIndex = roles.LastIndexOf(BrainSlotRoles.ArbiterOfTruth);
        Assert.True(arbiterIndex > roles.IndexOf(BrainSlotRoles.LeftHemisphere), "ArbiterOfTruth must run after LeftHemisphere.");
        Assert.True(arbiterIndex > roles.IndexOf(BrainSlotRoles.RightHemisphere), "ArbiterOfTruth must run after RightHemisphere.");
    }

    private static HttpClient SeedClient(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        TestAuthHelper.AddAuthHeader(client, factory.Services);
        return client;
    }

    private static HttpClient Authorized(CustomWebApplicationFactory factory, string? sessionId = null, string? turnId = null)
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        TestAuthHelper.AddAuthHeader(client, factory.Services);
        if (client.DefaultRequestHeaders.TryGetValues("X-Api-Key", out var keys))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", keys.First());
        if (!string.IsNullOrWhiteSpace(sessionId))
            client.DefaultRequestHeaders.Add("X-Session-Id", sessionId);
        if (!string.IsNullOrWhiteSpace(turnId))
            client.DefaultRequestHeaders.Add("X-Turn-Id", turnId);
        return client;
    }

    private static OpenAiChatCompletionRequest RequestWithPrompt(string prompt)
        => new() { Model = "qbagent", Messages = [new OpenAiChatMessage { Role = "user", Content = prompt }] };

    private static string ExtractResponseText(ChatResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.Text))
            return response.Text;

        return string.Join(
            Environment.NewLine,
            response.Messages
                .Select(static message => message.Text)
                .Where(static text => !string.IsNullOrWhiteSpace(text)));
    }

    private static void AssertNormalWorkflowRoles(IReadOnlyList<string> invokedRoles)
    {
        Assert.True(invokedRoles.Count >= 3, "QuadBrain must invoke both hemispheres and ArbiterOfTruth at minimum.");
        Assert.Contains(BrainSlotRoles.LeftHemisphere, invokedRoles);
        Assert.Contains(BrainSlotRoles.RightHemisphere, invokedRoles);
        Assert.Contains(BrainSlotRoles.ArbiterOfTruth, invokedRoles);
        Assert.DoesNotContain(BrainSlotRoles.CuriosityEngine, invokedRoles);
        var roles = invokedRoles.ToList();
        var allowedRoles = new[]
        {
            BrainSlotRoles.LeftHemisphere,
            BrainSlotRoles.RightHemisphere,
            BrainSlotRoles.ArbiterOfTruth,
        };
        Assert.All(
            roles,
            role => Assert.Contains(role, allowedRoles));
        var arbiterIndex = roles.LastIndexOf(BrainSlotRoles.ArbiterOfTruth);
        Assert.Equal(roles.Count - 1, arbiterIndex);
        Assert.True(arbiterIndex > roles.LastIndexOf(BrainSlotRoles.LeftHemisphere), "ArbiterOfTruth must run after LeftHemisphere.");
        Assert.True(arbiterIndex > roles.LastIndexOf(BrainSlotRoles.RightHemisphere), "ArbiterOfTruth must run after RightHemisphere.");
    }

    private static async Task<OpenAiChatCompletionResponse> PostAsync(HttpClient client, OpenAiChatCompletionRequest request)
    {
        var response = await client.PostAsJsonAsync(new Uri(Endpoint, UriKind.Relative), request).ConfigureAwait(true);
        var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.True(response.StatusCode == HttpStatusCode.OK, responseText);
        var body = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(responseText, JsonOptions);
        Assert.NotNull(body);
        return body!;
    }

    private static async Task<string> ResolveDefaultOllamaModelAsync()
    {
        var configured = Environment.GetEnvironmentVariable("MCP_QUADBRAIN_OLLAMA_MODEL");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(new Uri(OllamaTagsEndpoint), CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException(
                "Local Ollama is required for TEST-MCP-QBOLLAMA-001. Start Ollama on http://localhost:11434 or set MCP_QUADBRAIN_OLLAMA_MODEL after installing a model.",
                ex);
        }

        var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.True(response.IsSuccessStatusCode, responseText);
        var tags = JsonSerializer.Deserialize<OllamaTagsResponse>(responseText, JsonOptions);
        var model = tags?.Models.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Name))?.Name;
        Assert.False(string.IsNullOrWhiteSpace(model), "Local Ollama has no installed models; install one before running TEST-MCP-QBOLLAMA-001.");
        return model!;
    }

    private static string BuildSessionId(string suffix)
        => $"{SourceType}-{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}-{NormalizeIdentifierSuffix(suffix)}-{Guid.NewGuid().ToString("N")[..8]}";

    private static string NewRequestId(string suffix)
        => $"req-{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}-{NormalizeIdentifierSuffix(suffix)}-{Guid.NewGuid().ToString("N")[..8]}";

    private static string NormalizeIdentifierSuffix(string suffix)
    {
        var normalized = new string((suffix ?? string.Empty)
            .ToLowerInvariant()
            .Select(static c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray()).Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "quadbrain" : normalized;
    }

    private sealed class RecordingBrainSlotChatClientFactory : IBrainSlotChatClientFactory
    {
        private readonly object _gate = new();
        private readonly IBrainSlotChatClientFactory _inner = new BrainSlotChatClientFactory();
        private readonly List<string> _invokedRoles = [];
        private readonly List<string> _invokedModelIds = [];
        private readonly List<string?> _invokedEndpoints = [];
        private readonly List<string> _invokedOutputs = [];

        public IReadOnlyList<string> InvokedRoles
        {
            get
            {
                lock (_gate)
                    return [.. _invokedRoles];
            }
        }

        public IReadOnlyList<string> InvokedModelIds
        {
            get
            {
                lock (_gate)
                    return [.. _invokedModelIds];
            }
        }

        public IReadOnlyList<string?> InvokedEndpoints
        {
            get
            {
                lock (_gate)
                    return [.. _invokedEndpoints];
            }
        }

        public IReadOnlyList<string> InvokedOutputs
        {
            get
            {
                lock (_gate)
                    return [.. _invokedOutputs];
            }
        }

        public IBrainSlotChatClient Create(BrainSlotDefinitionEntity slot, string credential)
            => new RecordingBrainSlotChatClient(this, _inner.Create(slot, credential));

        private void Record(BrainSlotDefinitionEntity slot)
        {
            lock (_gate)
            {
                _invokedRoles.Add(slot.Role);
                _invokedModelIds.Add(slot.ModelId);
                _invokedEndpoints.Add(slot.Endpoint);
            }
        }

        private void RecordOutput(string output)
        {
            lock (_gate)
                _invokedOutputs.Add(output);
        }

        private sealed class RecordingBrainSlotChatClient(
            RecordingBrainSlotChatClientFactory owner,
            IBrainSlotChatClient inner) : IBrainSlotChatClient
        {
            public async Task<string> CompleteAsync(
                BrainSlotDefinitionEntity slot,
                string input,
                double? temperature,
                CancellationToken cancellationToken = default)
            {
                owner.Record(slot);
                var output = await inner.CompleteAsync(slot, input, temperature, cancellationToken).ConfigureAwait(false);
                owner.RecordOutput(output);
                return output;
            }
        }
    }

    private sealed class FieldVariantBrainSlotChatClientFactory : IBrainSlotChatClientFactory
    {
        private readonly object _gate = new();
        private readonly List<string> _invokedOutputs = [];

        public IReadOnlyList<string> InvokedOutputs
        {
            get
            {
                lock (_gate)
                    return [.. _invokedOutputs];
            }
        }

        public IBrainSlotChatClient Create(BrainSlotDefinitionEntity slot, string credential)
            => new FieldVariantBrainSlotChatClient(this);

        private void RecordOutput(string output)
        {
            lock (_gate)
                _invokedOutputs.Add(output);
        }

        private sealed class FieldVariantBrainSlotChatClient(FieldVariantBrainSlotChatClientFactory owner) : IBrainSlotChatClient
        {
            public Task<string> CompleteAsync(
                BrainSlotDefinitionEntity slot,
                string input,
                double? temperature,
                CancellationToken cancellationToken = default)
            {
                var output = slot.Role switch
                {
                    BrainSlotRoles.LeftHemisphere => BrainSlotChatClientFactory.ExtractOpenAiCompatibleMessageText("""
                        {"choices":[{"message":{"content":"content-field-captured","reasoning":"reasoning-field-ignored","reasoning_content":"reasoning-content-field-ignored"}}]}
                        """),
                    BrainSlotRoles.RightHemisphere => BrainSlotChatClientFactory.ExtractOpenAiCompatibleMessageText("""
                        {"choices":[{"message":{"content":"","reasoning":"reasoning-field-captured","reasoning_content":"reasoning-content-field-ignored"}}]}
                        """),
                    BrainSlotRoles.ArbiterOfTruth => BrainSlotChatClientFactory.ExtractOpenAiCompatibleMessageText("""
                        {"choices":[{"message":{"content":"","reasoning":"","reasoning_content":"reasoning-content-field-captured"}}]}
                        """),
                    _ => "unknown-field-captured",
                };
                owner.RecordOutput(output);
                return Task.FromResult(output);
            }
        }
    }

    private sealed class StubCredentialResolver : IBrainSlotCredentialResolver
    {
        public Task<string?> ResolveAsync(string credentialReference, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("ollama-local-test-key");

        public bool IsSupportedReference(string credentialReference)
            => !string.IsNullOrWhiteSpace(credentialReference);
    }

    private sealed class CommittingTurnTransactionCoordinator : ITurnTransactionCoordinator
    {
        public async Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            var mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
            return new TurnTransactionResult
            {
                TransactionId = "txn-" + Guid.NewGuid().ToString("N"),
                Status = "committed",
                DiffgramId = "diffgram-1",
                MutationResult = mutationResult,
                MutationApplied = mutationResult.Success,
            };
        }

        public TurnTransactionStatusResponse GetStatus()
            => new() { Enabled = true, Degraded = false };
    }

    private sealed class OllamaTagsResponse
    {
        public List<OllamaModelTag> Models { get; set; } = [];
    }

    private sealed class OllamaModelTag
    {
        public string Name { get; set; } = string.Empty;
    }
}
