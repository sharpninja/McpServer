# Hostile Validator Receipt

TimestampUtc: 2026-08-18T02:03:39Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: class 2 (user-directed analysis of Integral2u/SharpMind as a potential QuadBrain backend). Not product implementation. No plan-step done claim.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md).
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.93.0; .version 1.93.0)
Marker: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml
Marker signature: Test-MarkerSignature True (pwsh, 2026-08-18T01:59:59Z window)
Health (this review): nonce 98627f54e6a74971bb2e2382b69c0d2c echoed exactly; status Healthy; version 1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e; storage=unreachable (live at this review). Sessionlog query still returned live rows.
SessionId: GrokCode-20260818T015054Z-plugin-session
RequestId: req-20260818T015644Z-prompt-f210
planFile: None
todoId: None
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass independently re-checked GitHub SharpMind, local QuadBrain slot code, git porcelain, MCP sessionlog, and MCP TODO lists. The implementer receipt was not trusted.

This review did not implement product features. This review wrote only this receipt pair and the MCP review turn.

Accuracy rating: 90/100. Architecture, SHA, PR #4, provider kinds, and no-product-code claims re-verified. The implementer 0-hit search list is false.
Completeness rating: 88/100. Surfaces A-D evaluated. Did not clone SharpMind or re-run every possible HTTP synonym (TcpListener, Listen). Did not prove historical health.storage at implementer time.

## Classification

Class 2. Operator-directed analysis of a third-party repo as a potential backend. Surface C is N/A. Surface D applies only to a false plan-done implication. Byrd v4 is not applied to this analysis action.

## Claims reviewed

### A Requested

A1-architecture. SharpMind is a managed C# GGUF inference/training library plus SharpMind.CUI, not an HTTP model server.
Verdict: PASS
Evidence: github get_commit sha=master -> e0338f2225d79bc2a345cd27be9666701cb7f467. README at that SHA: "ships as a set of composable libraries plus a terminal chat application (SharpMind.CUI)". SharpMind.CUI.csproj OutputType Exe, TargetFramework net10.0. IChatSession is an in-process ChatSession API, not HTTP. Code search Microsoft.AspNetCore / WebApplication / HttpListener / Kestrel / HttpServer / MapGet / MapPost / chat/completions / Microsoft.AspNet: 0 hits.

A1-codesearch-zero. GitHub code search for Microsoft.AspNetCore / WebApplication / HttpListener / chat/completions / tool_calls returned 0 hits.
Verdict: FAIL
Evidence: github search_code repo:Integral2u/SharpMind query=tool_calls total_count=2 (SharpMind.Tokenization/Serialisation/MistralConverter.cs [TOOL_CALLS]; SharpMind.Inference/Chat/PromptFormatters/JinjaTemplateFormatter.cs comment "tool_calls / tool call blocks are intentionally skipped"). Implementer receipt also claimed 0 hits for /v1/; search_code "/v1/" total_count=1 (SharpMind.Core/AgentTools/WeatherTool.cs open-meteo client URLs). Those hits are not an HTTP model server, but the zero-hit statement is false.

A2. QuadBrain brain slots only accept ProviderKind OpenAI or OpenAICompatible, and OpenAiCompatibleBrainSlotChatClient POSTs {endpoint}/chat/completions.
Verdict: PASS
Evidence: src/McpServer.Support.Mcp/Services/BrainSlotValidation.cs ProviderKinds HashSet is only OpenAI and OpenAICompatible; NormalizeProviderKind throws otherwise. BrainSlotChatClientFactory.cs lines 27-31 and 114-115: OpenAiCompatibleBrainSlotChatClient POSTs new Uri(baseEndpoint, "chat/completions"). docs/QUADBRAIN.md: ProviderKind OpenAI | OpenAICompatible; inbound QuadBrain endpoint is also POST {baseUrl}/v1/chat/completions.

A3. Therefore SharpMind cannot be used as a QuadBrain brain-slot backend without a new OpenAI-compatible HTTP wrapper or a new IBrainSlotChatClient / ProviderKind.
Verdict: PASS
Evidence: IBrainSlotChatClient exists (BrainSlotInterfaces.cs). Factory only constructs OpenAiCompatibleBrainSlotChatClient or ExtensionsAiBrainSlotChatClient (OpenAI SDK). SharpMind master exposes in-process ChatSession, not chat/completions. No SharpMind/Integral2u string in src except the analysis receipt.

A4. Open PR #4 documents unmerged correctness bugs (wrong RoPE convention, BPE merge loss, GGUF tensor mis-map) with Qwen examples that are incoherent on current master.
Verdict: PASS
Evidence: github pull_request_read PR 4: state=open, merged=false, title "Fix rotary convention, BPE merge loss and GGUF tensor mis-mapping", body includes before "The capital of France is" -> "a city in the north of the of the of the of" and after "The capital of France is Paris." Current master RoPE.cs still has only adjacent pairing and the Llama-3.2 comment quoted by the PR. Open PRs also 3, 6, 7 (author MBrekhof).

A5. README validated models are 135M-1.5B; that is not best-of-breed for Logic / ArbiterOfTruth / CuriosityEngine.
Verdict: PASS
Evidence: README compatibility matrix names SmolLM 135M through qwen2.5-1.5b-instruct and DeepSeek-R1-Distill-Qwen-1.5B. README says "Runs clean" is not a quality claim. QuadBrain docs/QUADBRAIN.md roles Logic, CuriosityEngine, ArbiterOfTruth require reasoning/research/reconciliation. Operator profile requires best-of-breed per function. 135M-1.5B is not that class. llama3-small is listed without a parameter count; that does not enlarge the named validated range.

A6. Recommendation is do-not-adopt-now; optional future sidecar after PRs 4/6/7 and quality proof. Implementer did not change product code except writing the analysis receipt.
Verdict: PASS (recommendation and no product-code change). Receipt body is scored under B.
Evidence: Implementer session dialog 2026-08-18T01:56:00Z decision text matches do-not-adopt-now plus sidecar-after-PRs. git status --porcelain on BrainSlotChatClientFactory.cs, BrainSlotValidation.cs, docs/QUADBRAIN.md: clean. LastWriteTimeUtc of those files is 2026-06-28 / 2026-07-20, not the analysis window. Only new analysis-window file: ?? docs/receipts/sharpmind-quadbrain-analysis-20260818T015700Z.md (2383 bytes, 40 lines, LastWriteTimeUtc 2026-08-18T01:56:15Z). Workspace src has many older dirty files (for example McpServerClient.cs 2026-08-16T18:08:41Z); those are outside this analysis.

A7. Implementer session GrokCode-20260818T015054Z-plugin-session / req-20260818T015124Z-prompt-3816. Marker signature True. Health nonce 74ce7fc7044942f2b86e6bfc39906a77 echoed.
Verdict: PASS
Evidence: sessionlog_query text=GrokCode-20260818T015054Z-plugin-session returns that session. Turn req-20260818T015124Z-prompt-3816 exists (status canceled: superseded by this review turn). Action 1 records nonce 74ce7fc7044942f2b86e6bfc39906a77 and Test-MarkerSignature True. This review independently got Test-MarkerSignature True and a live nonce echo. Historical health.storage=reachable at implementer time is not replayable; this review sees storage=unreachable while sessionlog still reads.

A8. Implementer did not claim a plan step complete, did not mark any MCP TODO done, did not implement integration.
Verdict: PASS
Evidence: Implementer turn planFile=None todoId=None. sessionlog_query from=2026-08-18T01:50:00Z text=todo returns only these analysis/hostile turns. todo_list done=true: 0 items with CompletedDate after 2026-08-18T01:00Z. Open PLAN-QUADBRAIN-* and PLAN-QBCODE-* remain Done=false. No SharpMind ProviderKind or product integration files.

### B Workspace rules

B1-honesty. Accuracy-first / do not fabricate evidence.
Verdict: FAIL
Rule: AGENTS.md honesty; profile accuracy-first-verify-sources; bring-the-receipts.
Violation: Implementer stated GitHub code search returned 0 hits for tool_calls (parent claim and session dialog) and for /v1/ (receipt + session dialog). Live search_code disproves both. The architectural conclusion can still be true; the cited search result is not.

B2-receipts. Durable receipt must carry machine-verifiable evidence.
Verdict: FAIL
Rule: Always bring the receipts; hostile durable artifact.
Violation: docs/receipts/sharpmind-quadbrain-analysis-20260818T015700Z.md is 2383 bytes / 40 lines. It lists sources and "Claims this analysis will make" then ends. No command output, no SHA re-check transcript, no PR body excerpt, no product git porcelain. The real analysis lives only in the canceled session turn, not in the named receipt.

B3-MCP-only storage. Never edit TODO/session/requirements files directly.
Verdict: PASS
Evidence: docs/Project/TODO.yaml LastWriteTimeUtc 2026-07-10. No SharpMind writes under docs/Project in the analysis window. Session/TODO mutations observed only through MCP query results.

B4-lab PowerShell / no Python.
Verdict: PASS
Evidence: This review used pwsh.exe / PowerShell.MCP only. Implementer session actions cite GitHub MCP and file reads; no python/py invocation found. python.exe exists on the lab machine; that is not evidence the implementer used it.

B5-look-before-delete.
Verdict: PASS
Evidence: No delete actions in implementer turn. No product files removed in the analysis window.

B6-Byrd v4.
Verdict: N/A (not FAIL)
Rule: Byrd v4 applies only to project implementation. This is class 2 analysis.

### C Requirements

C1. FR/TR/TEST/AC for this work.
Verdict: N/A (not FAIL)
Rule: hostile-ops-vs-requirements. Class 2 operator analysis of a third-party repo is not project requirement work. Implementer shipped no product behavior.

### D Plan

D1. Implementer claimed an active plan step complete, or falsely implied a product plan DoD was met.
Verdict: PASS (no such claim)
Evidence: Active plan path none. Implementer turn planFile=None. Open PLAN-QUADBRAIN-001 and children remain Done=false. Session decision says do not add a SharpMind ProviderKind without a later approved plan.

## Explicit FAIL list

1. A1-codesearch-zero: tool_calls is not 0 hits (2 hits). Implementer receipt /v1/ 0-hit claim is also false (1 hit).
2. B1-honesty: overstated zero-hit searches presented as observation.
3. B2-receipts: named durable receipt is a 40-line stub without evidence.

## UNKNOWN / unevaluated

- Implementer-time health.storage field cannot be replayed. Current /health storage=unreachable. Sessionlog still served the implementer session, so this is noted, not used as a FAIL.
- Whether llama3-small exceeds 1.5B is unknown from the README table. Not required to fail A5.
- No local clone of SharpMind; GitHub API + code search used instead.

## Session-log persistence proof

Native MCP tools (mcpserver__sessionlog_*), agent GrokCode, workspace F:\GitHub\McpServer:

- Recovered hook-opened review turn instead of duplicating a second session.
- sessionlog_open + sessionlog_begin_turn on GrokCode-20260818T015054Z-plugin-session / req-20260818T015644Z-prompt-f210
- sessionlog_dialog + complete_turn with actions/decisions
- Persistence proved by sessionlog_query after complete (see follow-up query in the JSON twin and the completed-turn query in this review)

## Files written by this review

- docs/receipts/hostile-validator-20260818T020339Z.md
- docs/receipts/hostile-validator-20260818T020339Z.json
