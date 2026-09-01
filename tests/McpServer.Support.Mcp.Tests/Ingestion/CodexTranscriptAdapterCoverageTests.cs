using McpServer.SessionLog.Transcripts;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>
/// TR-MCP-TRANSCRIPT-002 / TR-MCP-TRANSCRIPT-003 / TEST-MCP-TRANSCRIPT-011 / FR-MCP-TRANSCRIPT-SEARCH-001 /
/// TR-MCP-TRANSCRIPT-SEARCH-001 / TEST-MCP-TRANSCRIPT-SEARCH-001: Codex adapter coverage for tool-call,
/// reasoning, turn-context, UI-mirror, tool_search, and inter_agent record classes observed in real Codex
/// CLI rollout files. Each test writes an inline JSONL fixture to a temp directory and normalizes it
/// through <see cref="TranscriptIngestionService.CreateDefault"/>. Persistence tests use a stub
/// <see cref="ITranscriptSessionPersister"/>.
/// </summary>
public sealed class CodexTranscriptAdapterCoverageTests
{
    /// <summary>
    /// TEST-MCP-TRANSCRIPT-011: Verifies function_call records normalize to assistant tool-call events and
    /// function_call_output records normalize to tool-role events, both without warning diagnostics.
    /// Fixture: session_meta + function_call (shell_command) + function_call_output pair.
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexFunctionCallsBecomeAssistantAndToolEvents()
    {
        var session = await NormalizeAsync([
            "{\"timestamp\":\"2026-07-03T01:03:38.075Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"function_call\",\"id\":\"fc-1\",\"name\":\"shell_command\",\"arguments\":\"{\\\"command\\\":\\\"git status\\\"}\",\"call_id\":\"call-1\"}}",
            "{\"timestamp\":\"2026-07-03T01:03:45.385Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"function_call_output\",\"call_id\":\"call-1\",\"output\":\"On branch main\"}}"
        ]).ConfigureAwait(true);

        Assert.Equal(2, session.Events.Count);
        var call = session.Events[0];
        Assert.Equal("assistant", call.Role);
        Assert.Equal("function_call", call.NativeType);
        Assert.Contains("shell_command", JoinText(call), StringComparison.Ordinal);
        Assert.Contains("git status", JoinText(call), StringComparison.Ordinal);
        Assert.Equal("call-1", call.Metadata["call_id"]);
        Assert.Equal("shell_command", call.Metadata["name"]);
        var output = session.Events[1];
        Assert.Equal("tool", output.Role);
        Assert.Equal("function_call_output", output.NativeType);
        Assert.Contains("On branch main", JoinText(output), StringComparison.Ordinal);
        Assert.Equal("call-1", output.Metadata["call_id"]);
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Severity == "warning");
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-011: Verifies custom_tool_call and custom_tool_call_output records normalize the
    /// same way as function calls, preserving tool name, input, output, and call pairing metadata.
    /// Fixture: custom_tool_call (apply_patch) + custom_tool_call_output pair.
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexCustomToolCallsBecomeAssistantAndToolEvents()
    {
        var session = await NormalizeAsync([
            "{\"timestamp\":\"2026-07-03T01:07:04.273Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"custom_tool_call\",\"id\":\"ctc-1\",\"status\":\"completed\",\"call_id\":\"call-2\",\"name\":\"apply_patch\",\"input\":\"*** Begin Patch\"}}",
            "{\"timestamp\":\"2026-07-03T01:07:09.556Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"custom_tool_call_output\",\"call_id\":\"call-2\",\"output\":\"Exit code: 0\"}}"
        ]).ConfigureAwait(true);

        Assert.Equal(2, session.Events.Count);
        var call = session.Events[0];
        Assert.Equal("assistant", call.Role);
        Assert.Equal("custom_tool_call", call.NativeType);
        Assert.Contains("apply_patch", JoinText(call), StringComparison.Ordinal);
        Assert.Contains("*** Begin Patch", JoinText(call), StringComparison.Ordinal);
        Assert.Equal("call-2", call.Metadata["call_id"]);
        Assert.Equal("completed", call.Metadata["status"]);
        var output = session.Events[1];
        Assert.Equal("tool", output.Role);
        Assert.Equal("custom_tool_call_output", output.NativeType);
        Assert.Contains("Exit code: 0", JoinText(output), StringComparison.Ordinal);
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Severity == "warning");
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-011: Verifies reasoning records with recoverable summary text normalize to
    /// assistant reasoning events. Fixture: reasoning record with one summary_text block.
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexReasoningSummaryBecomesAssistantEvent()
    {
        var session = await NormalizeAsync([
            "{\"timestamp\":\"2026-07-03T01:03:34.205Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"reasoning\",\"id\":\"rs-1\",\"summary\":[{\"type\":\"summary_text\",\"text\":\"Weighing adapter options\"}]}}"
        ]).ConfigureAwait(true);

        var reasoning = Assert.Single(session.Events);
        Assert.Equal("assistant", reasoning.Role);
        Assert.Equal("reasoning", reasoning.NativeType);
        Assert.Contains("Weighing adapter options", JoinText(reasoning), StringComparison.Ordinal);
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Severity == "warning");
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-011: Verifies encrypted-only reasoning records (no recoverable text) are skipped
    /// and reported once through an aggregate info diagnostic instead of per-record warnings.
    /// Fixture: two reasoning records carrying only encrypted_content.
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexEncryptedReasoningSkippedWithAggregateInfoDiagnostic()
    {
        var session = await NormalizeAsync([
            "{\"timestamp\":\"2026-07-03T01:03:34.205Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"reasoning\",\"id\":\"rs-1\",\"summary\":[],\"encrypted_content\":\"gAAAAA\"}}",
            "{\"timestamp\":\"2026-07-03T01:03:35.205Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"reasoning\",\"id\":\"rs-2\",\"summary\":[],\"encrypted_content\":\"gBBBBB\"}}"
        ]).ConfigureAwait(true);

        Assert.Empty(session.Events);
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Severity == "warning");
        var aggregate = Assert.Single(session.Diagnostics, diagnostic => diagnostic.Code == "codex_encrypted_reasoning");
        Assert.Equal("info", aggregate.Severity);
        Assert.Contains("2", aggregate.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-011: Verifies event_msg records (UI mirrors of response_item records) are skipped
    /// without warnings and summarized in one aggregate info diagnostic.
    /// Fixture: user_message, agent_message, and token_count event_msg records.
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexEventMsgRecordsSkippedWithoutWarnings()
    {
        var session = await NormalizeAsync([
            "{\"timestamp\":\"2026-07-03T01:03:25.238Z\",\"type\":\"event_msg\",\"payload\":{\"type\":\"user_message\",\"message\":\"hello\"}}",
            "{\"timestamp\":\"2026-07-03T01:03:38.074Z\",\"type\":\"event_msg\",\"payload\":{\"type\":\"agent_message\",\"message\":\"working on it\"}}",
            "{\"timestamp\":\"2026-07-03T01:03:45.551Z\",\"type\":\"event_msg\",\"payload\":{\"type\":\"token_count\",\"info\":{}}}"
        ]).ConfigureAwait(true);

        Assert.Empty(session.Events);
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Severity == "warning");
        var aggregate = Assert.Single(session.Diagnostics, diagnostic => diagnostic.Code == "codex_event_msg_skipped");
        Assert.Equal("info", aggregate.Severity);
        Assert.Contains("3", aggregate.Message, StringComparison.Ordinal);
        Assert.Contains("user_message", aggregate.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-011: Verifies turn_context records contribute the session model and workspace
    /// path without emitting diagnostics or events. Fixture: session_meta without cwd + turn_context with
    /// cwd and model.
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexTurnContextContributesModelAndWorkspacePath()
    {
        var session = await NormalizeAsync([
            "{\"timestamp\":\"2026-07-03T01:03:25.211Z\",\"type\":\"turn_context\",\"payload\":{\"turn_id\":\"turn-1\",\"cwd\":\"F:/GitHub/Sample\",\"model\":\"gpt-5.5\",\"effort\":\"xhigh\"}}",
            "{\"timestamp\":\"2026-07-03T01:03:26.211Z\",\"type\":\"response_item\",\"payload\":{\"id\":\"msg-1\",\"type\":\"message\",\"role\":\"user\",\"content\":[{\"type\":\"input_text\",\"text\":\"hi\"}]}}"
        ]).ConfigureAwait(true);

        Assert.Single(session.Events);
        Assert.Equal("gpt-5.5", session.Model);
        Assert.Equal("F:/GitHub/Sample", session.WorkspacePath);
        Assert.Empty(session.Diagnostics);
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-011: Verifies world_state and compacted records are skipped without warnings and
    /// summarized in one aggregate info diagnostic. Fixture: one world_state + one compacted record.
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexNonConversationRecordsSkippedWithInfoDiagnostic()
    {
        var session = await NormalizeAsync([
            "{\"timestamp\":\"2026-07-13T19:45:32.849Z\",\"type\":\"world_state\",\"payload\":{\"full\":true,\"state\":{}}}",
            "{\"timestamp\":\"2026-07-13T19:46:15.453Z\",\"type\":\"compacted\",\"payload\":{\"message\":\"\",\"replacement_history\":[]}}"
        ]).ConfigureAwait(true);

        Assert.Empty(session.Events);
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Severity == "warning");
        var aggregate = Assert.Single(session.Diagnostics, diagnostic => diagnostic.Code == "codex_nonconversation_skipped");
        Assert.Equal("info", aggregate.Severity);
        Assert.Contains("world_state", aggregate.Message, StringComparison.Ordinal);
        Assert.Contains("compacted", aggregate.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-011: Verifies unknown top-level record types still warn but are aggregated to one
    /// diagnostic per distinct type with an occurrence count. Fixture: two unmapped_record + one
    /// another_unknown record.
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexUnknownRecordTypesAggregateOneWarningPerType()
    {
        var session = await NormalizeAsync([
            "{\"type\":\"unmapped_record\",\"payload\":{\"reason\":\"first\"}}",
            "{\"type\":\"unmapped_record\",\"payload\":{\"reason\":\"second\"}}",
            "{\"type\":\"another_unknown\",\"payload\":{}}"
        ]).ConfigureAwait(true);

        Assert.Empty(session.Events);
        var unmapped = Assert.Single(session.Diagnostics, diagnostic => diagnostic.Code == "codex_unknown_record" && diagnostic.Message.Contains("unmapped_record", StringComparison.Ordinal));
        Assert.Equal("warning", unmapped.Severity);
        Assert.Contains("2", unmapped.Message, StringComparison.Ordinal);
        var other = Assert.Single(session.Diagnostics, diagnostic => diagnostic.Code == "codex_unknown_record" && diagnostic.Message.Contains("another_unknown", StringComparison.Ordinal));
        Assert.Equal("warning", other.Severity);
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-SEARCH-001 / TEST-MCP-TRANSCRIPT-011: Verifies top-level
    /// inter_agent_communication_metadata records from real Codex JSONL are skipped as non-conversation
    /// metadata with one aggregate info diagnostic and zero unknown-record warnings.
    /// Fixture shape taken from TruckMate rollout 019f5a3b-a22f-7f01-b440-b187484c35a7
    /// (payload is only trigger_turn).
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexInterAgentMetadataSkippedWithInfoDiagnostic()
    {
        var session = await NormalizeAsync([
            "{\"timestamp\":\"2026-07-18T11:57:31.848Z\",\"type\":\"inter_agent_communication_metadata\",\"payload\":{\"trigger_turn\":false}}",
            "{\"timestamp\":\"2026-07-18T11:57:32.848Z\",\"type\":\"inter_agent_communication_metadata\",\"payload\":{\"trigger_turn\":true}}"
        ]).ConfigureAwait(true);

        Assert.Empty(session.Events);
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Code == "codex_unknown_record");
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Severity == "warning");
        var aggregate = Assert.Single(session.Diagnostics, diagnostic => diagnostic.Code == "codex_nonconversation_skipped");
        Assert.Equal("info", aggregate.Severity);
        Assert.Contains("inter_agent_communication_metadata", aggregate.Message, StringComparison.Ordinal);
        Assert.Contains("2", aggregate.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-SEARCH-001 / TEST-MCP-TRANSCRIPT-011: Verifies tool_search_call and
    /// tool_search_output response_item payloads from real Codex JSONL normalize to paired assistant
    /// tool-call and tool-role events with call_id/name/status metadata and zero unknown-response-item
    /// diagnostics. Fixture shape taken from TruckMate rollout 019f5a3b-a22f-7f01-b440-b187484c35a7:
    /// object arguments.query plus a truncated tools array (real output records are large namespace dumps).
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexToolSearchCallsBecomePairedAssistantAndToolEvents()
    {
        var session = await NormalizeAsync([
            "{\"timestamp\":\"2026-07-16T11:30:57.816Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"tool_search_call\",\"id\":\"tsc-1\",\"call_id\":\"call-search-1\",\"status\":\"completed\",\"execution\":\"client\",\"arguments\":{\"query\":\"PowerShell.Mcp powershell execute command session\",\"limit\":10}}}",
            "{\"timestamp\":\"2026-07-16T11:30:57.839Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"tool_search_output\",\"call_id\":\"call-search-1\",\"status\":\"completed\",\"execution\":\"client\",\"tools\":[{\"type\":\"namespace\",\"name\":\"mcp__powershell\",\"description\":\"Tools in the mcp__powershell namespace.\",\"tools\":[{\"type\":\"function\",\"name\":\"get_current_location\",\"description\":\"Retrieves the current location.\"}]}]}}"
        ]).ConfigureAwait(true);

        Assert.Equal(2, session.Events.Count);
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Code == "codex_unknown_response_item");
        Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Severity == "warning");
        var call = session.Events[0];
        Assert.Equal("assistant", call.Role);
        Assert.Equal("tool_search_call", call.NativeType);
        Assert.Equal("call-search-1", call.Metadata["call_id"]);
        Assert.Equal("completed", call.Metadata["status"]);
        Assert.Equal("tool_search_call", call.Metadata["name"]);
        Assert.Contains("PowerShell.Mcp powershell execute command session", JoinText(call), StringComparison.Ordinal);
        var output = session.Events[1];
        Assert.Equal("tool", output.Role);
        Assert.Equal("tool_search_output", output.NativeType);
        Assert.Equal("call-search-1", output.Metadata["call_id"]);
        Assert.Equal("completed", output.Metadata["status"]);
        Assert.Contains("mcp__powershell", JoinText(output), StringComparison.Ordinal);
        Assert.Contains("get_current_location", JoinText(output), StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-SEARCH-001: Verifies a successful Persist=true ingest of a fixture containing
    /// inter_agent_communication_metadata, tool_search_call, and tool_search_output deletes the
    /// importRecovery envelope and reports persisted=true degraded=false, with zero unknown diagnostics
    /// for those record classes.
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexToolSearchAndInterAgentPersistDeletesImportRecovery()
    {
        var tempWorkspace = Path.Combine(Path.GetTempPath(), "mcp-transcript-codex-search", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempWorkspace);
        try
        {
            var persister = new SucceedingTranscriptPersister();
            var result = await IngestAsync(
                [
                    "{\"timestamp\":\"2026-07-18T11:57:31.848Z\",\"type\":\"inter_agent_communication_metadata\",\"payload\":{\"trigger_turn\":false}}",
                    "{\"timestamp\":\"2026-07-16T11:30:57.816Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"tool_search_call\",\"id\":\"tsc-1\",\"call_id\":\"call-search-1\",\"status\":\"completed\",\"execution\":\"client\",\"arguments\":{\"query\":\"PowerShell.Mcp powershell execute command session\",\"limit\":10}}}",
                    "{\"timestamp\":\"2026-07-16T11:30:57.839Z\",\"type\":\"response_item\",\"payload\":{\"type\":\"tool_search_output\",\"call_id\":\"call-search-1\",\"status\":\"completed\",\"execution\":\"client\",\"tools\":[{\"type\":\"namespace\",\"name\":\"mcp__powershell\",\"tools\":[{\"type\":\"function\",\"name\":\"get_current_location\"}]}]}}"
                ],
                persist: true,
                persister,
                tempWorkspace,
                agent: "Codex").ConfigureAwait(true);

            Assert.True(result.Persisted);
            Assert.False(result.Degraded);
            Assert.Empty(result.ImportRecoveryPaths);
            var receipt = Assert.Single(result.Receipts);
            Assert.Equal("persisted", receipt.Status);
            Assert.True(persister.RecoveryExistedDuringPersist);
            Assert.False(File.Exists(receipt.ImportRecoveryPath), receipt.ImportRecoveryPath);
            var session = Assert.Single(result.Sessions);
            Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Code == "codex_unknown_record");
            Assert.DoesNotContain(session.Diagnostics, diagnostic => diagnostic.Code == "codex_unknown_response_item");
            Assert.DoesNotContain(
                session.Diagnostics,
                diagnostic => diagnostic.Message.Contains("inter_agent_communication_metadata", StringComparison.Ordinal)
                    && diagnostic.Code == "codex_unknown_record");
            Assert.DoesNotContain(
                session.Diagnostics,
                diagnostic => diagnostic.Message.Contains("tool_search_call", StringComparison.Ordinal)
                    && diagnostic.Code == "codex_unknown_response_item");
            Assert.DoesNotContain(
                session.Diagnostics,
                diagnostic => diagnostic.Message.Contains("tool_search_output", StringComparison.Ordinal)
                    && diagnostic.Code == "codex_unknown_response_item");
        }
        finally
        {
            if (Directory.Exists(tempWorkspace))
                Directory.Delete(tempWorkspace, recursive: true);
        }
    }

    /// <summary>
    /// TEST-MCP-TRANSCRIPT-011: Verifies unknown response_item payload types warn once per distinct payload
    /// type instead of per record. Fixture: two ghost_snapshot response_item records.
    /// </summary>
    [Fact]
    public async Task IngestionService_CodexUnknownResponseItemTypesAggregateOneWarningPerType()
    {
        var session = await NormalizeAsync([
            "{\"type\":\"response_item\",\"payload\":{\"type\":\"ghost_snapshot\",\"id\":\"gs-1\"}}",
            "{\"type\":\"response_item\",\"payload\":{\"type\":\"ghost_snapshot\",\"id\":\"gs-2\"}}"
        ]).ConfigureAwait(true);

        Assert.Empty(session.Events);
        var aggregate = Assert.Single(session.Diagnostics, diagnostic => diagnostic.Code == "codex_unknown_response_item");
        Assert.Equal("warning", aggregate.Severity);
        Assert.Contains("ghost_snapshot", aggregate.Message, StringComparison.Ordinal);
        Assert.Contains("2", aggregate.Message, StringComparison.Ordinal);
    }

    private static string JoinText(TranscriptEvent item)
        => string.Join("\n", item.Content.Select(block => block.Text));

    private static async Task<TranscriptSession> NormalizeAsync(string[] recordLines)
    {
        var result = await IngestAsync(recordLines, persist: false, persister: null, workspacePath: null, agent: null).ConfigureAwait(true);
        return Assert.Single(result.Sessions);
    }

    private static async Task<TranscriptIngestionResult> IngestAsync(
        string[] recordLines,
        bool persist,
        ITranscriptSessionPersister? persister,
        string? workspacePath,
        string? agent)
    {
        var tempDirectory = persist
            ? Path.Combine(workspacePath ?? throw new ArgumentNullException(nameof(workspacePath)), "transcripts")
            : Path.Combine(Path.GetTempPath(), "mcp-transcript-codex-coverage", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var createdTempDirectory = !persist;
        try
        {
            var transcriptPath = Path.Combine(tempDirectory, "session.jsonl");
            var lines = new List<string>
            {
                "{\"timestamp\":\"2026-07-03T01:03:25.000Z\",\"type\":\"session_meta\",\"payload\":{\"id\":\"codex-coverage-fixture\"}}"
            };
            lines.AddRange(recordLines);
            await File.WriteAllLinesAsync(transcriptPath, lines, TestContext.Current.CancellationToken).ConfigureAwait(true);
            var service = TranscriptIngestionService.CreateDefault(persister);

            return await service.IngestPathAsync(new TranscriptIngestionRequest(transcriptPath)
            {
                SourceKind = TranscriptSourceKind.Codex,
                Persist = persist,
                Agent = agent,
                WorkspacePath = workspacePath,
                RunId = persist ? "run-codex-search" : null
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
        finally
        {
            if (createdTempDirectory && Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private sealed class SucceedingTranscriptPersister : ITranscriptSessionPersister
    {
        public bool RecoveryExistedDuringPersist { get; private set; }

        public Task<string> PersistAsync(
            TranscriptIngestionRequest request,
            TranscriptSession session,
            TranscriptSessionReceipt receipt,
            CancellationToken cancellationToken = default)
        {
            RecoveryExistedDuringPersist = File.Exists(receipt.ImportRecoveryPath);
            return Task.FromResult("sessionLogId:codex-search-fixture");
        }
    }
}
