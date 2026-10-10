# Hostile validator: Bash ruling and hook dedupe
TimestampUtc: 2026-10-09T05:04:12.3769870Z
Validator: Codex CLI, gpt-6-sol, xhigh. Live proof: /home/sharpninja/.codex/sessions/2026/10/08/rollout-2026-10-08T23-58-07-01a11f06-9241-7f03-afd1-7ca98c83e42c.jsonl line 8 turn_context payload model, effort, collaboration_mode.settings.reasoning_effort.
Add-profile: skill read; 19 non-skill profile Markdown files read in full.
Work class: class 2 operator-directed ops. Surface C: N/A. Surface D: N/A.
Reviewer MCP turn: Codex-20261009T050039Z-hv-ruling-fix / req-20261009T050039Z-hv-ruling-fix.
Request JSONL: /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261009T045806Z-ruling-fix-20261009.request.jsonl
Response JSONL: /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261009T045806Z-ruling-fix-20261009.response.jsonl

## Verdict
DISAGREE. Accuracy 91/100, completeness 92/100, confidence 97/100. Core settings edit is semantically correct; the restart explanation, unconditional validator assurance, and Bash writes prevent acceptance. Post-edit one-turn behavior is untested.

## Item findings
- **D1 PASS** Ruling memory and index. Evidence: bash-launcher-ruling.md has frontmatter and exact launcher/read-only/state-change scope; MEMORY.md line 3 links it.
- **D2 PASS** Nine bridge hooks before edit and nine plugin hooks. Evidence: Backup SHA-256 F5FB2179F5818B0A97945C62F96BAA771562E0AC914377A852DF09EDE1C4EB6B; parsed backup has 1 command for each of six lifecycle events and 3 PostToolUse commands, all bridge calls. Plugin hooks.json has corresponding nine script names and matching PostToolUse matchers.
- **D3 PASS** Minimal parsed settings change. Evidence: Current settings has no hooks key; six other top-level keys compare equal after ConvertFrom-Json/ConvertTo-Json; enabledPlugins mcpserver@mcpserver-local is true. ConvertTo-Json changed formatting only; no semantic key/value drift found.
- **D4 PASS** Validator CheckOnly observed valid and non-mutating. Evidence: Transcript lines 1425-1426 set CLAUDE_PLUGIN_ROOT and get status valid, missingHooks []; same-context rerun returned valid and settings SHA-256 remained 955FBE80B644DB992F4FB5490AEE0DF061976A258809D49F3B1E2BA9CA8902C3. This result is environment-dependent, as B2 records.
- **D5 FAIL** Hook snapshot and restart explanation. Evidence: No post-edit prompt proves one turn, which the implementer correctly leaves unverified. Its reason is false against current official Claude Code settings documentation, lines 215-218: hooks edits reload in the running session. Transcript and MCP decision at 04:55:34 assert hooks are snapshotted at session start.
  - Disposition: Curable now: retract the snapshot/restart assertion, cite current documentation, and keep the outcome unverified until an actual fresh-prompt test.
- **D6 PASS** AGENTS.md writer unidentified and commit unchanged. Evidence: Operator transcript line 1377 says 1 fine. 2 fix. 3 i did not edit. Live MCP agents-author turn records unidentified writer; git show confirms f56a67a7 remains the neutral signing-line commit and HEAD remains 9fb9986e32b78e955980c1b0c50ad272fe2034f0.
- **D7 PASS** MCP turns, decision timing, failed write retry. Evidence: Live server has 9fd2 completed (4 decisions/4 dialogs/4 design actions), fix-hook-dupes completed (4/4/4), agents-author completed (2/2/2), and twin f3c0 canceled/annotated (1/1/1). First decisions timestamp 04:55:30-40, before memory 04:55:48 and settings edit 04:56:03. Transcript lines 1431/1441 show foreign path validation error and 1451/1452 successful retry; line 1446 shows rerun left 9fd2 actions at orders 1,2,3 before later intentional actions 4,5. No completed work action was found before its work.
- **D8 PASS** UTC timestamp at start of user-facing messages. Evidence: Transcript assistant text blocks after line 1377 are lines 1390 and 1405; both start with ISO UTC timestamps. Review ended before a later final message.
- **B1 FAIL** Operator Bash ruling followed in tool calls. Evidence: Transcript Bash tool calls 1396, 1430, 1445, 1461 run cat > to create persistent scratch scripts; line 1451 runs sed ... > and brace redirection in Bash. These are state changes outside pwsh after the ruling. Write tool calls 1406 and 1465 also write outside pwsh under the literal state-change wording.
  - Disposition: Historical process breach needs an operator ruling: waive this result or require a new compliant execution/review. Past Bash writes cannot be retroactively moved inside pwsh.
- **B2 FAIL** Validator will not re-add bridge hooks unconditionally. Evidence: The MCP fix-hook-dupes decision says CheckOnly valid means repair will not re-add bridges. With CLAUDE_PLUGIN_ROOT unset, the same CheckOnly returned status missing for three required hooks, while the settings hash stayed unchanged. Validator Test-PluginEnabled lines 70-83 iterates the enabledPlugins object as a list and recognizes the plugin via the environment fallback. The broad assurance exceeds its evidence.
  - Disposition: Curable now: qualify the validator result with the required CLAUDE_PLUGIN_ROOT context and retract the unconditional repair claim. Product validator changes belong to the separately planned A2 work.
- **B3 PASS** Look before settings change. Evidence: Transcript line 1391 inspects settings and plugin hooks; line 1415 creates a backup before ConvertTo-Json write; backup hash equals the original per line 1416.
- **B4 PASS** No Python. Evidence: All post-answer transcript tool inputs were inspected; no Python invocation appears.
- **B5 PASS** Native-object JSON mutation. Evidence: Settings edit at transcript line 1415 parses with ConvertFrom-Json -AsHashtable and serializes with ConvertTo-Json; no raw JSON line replacement was used.
- **B6 PASS** MCP-only session storage. Evidence: Transcript uses plugin repl-invoke client.SessionLog methods through PowerShell; live server query independently confirms persisted turns. No raw session-log storage edit appears.
- **B7 PASS** Turn mapping for three requests. Evidence: Three distinct answer turns are present and the duplicate hook turn is canceled, with cross-reference in response and decision; twin originated at 04:54:16 before settings edit.
- **B8 PASS** Decision and action chronology. Evidence: Live decision dialog times precede relevant memory/settings changes; completed read/edit actions were appended after the observed work. The failed patch and rerun were disclosed in the live turn.
- **B9 PASS** Chat formatting. Evidence: Both observed post-answer user-facing assistant text blocks start with UTC timestamps; no table-style message appears.
- **B10 UNKNOWN** One new Claude prompt now produces exactly one MCP turn. Evidence: No fresh Claude prompt after the settings edit is in the reviewed transcript. Settings inspection and validator output do not test live turn cardinality.
  - Disposition: Use a fresh prompt and query the live MCP turn count; record the result separately.

## Failure ownership
Curable by implementer work now: D5: retract snapshot claim and test with a fresh prompt; B2: qualify environment-dependent validator result and repair assertion.
Operator decision required: B1: operator waiver or new compliant result for historical Bash writes.
Unknown: B10: One new Claude prompt now produces exactly one MCP turn.

## Sources and limits
Official Claude Code settings documentation: https://code.claude.com/docs/en/settings (When edits take effect, lines 215-218). This directly contradicts a general hook snapshot claim. The running host version was not established, and no post-edit prompt test exists.
Settings formatting differences from ConvertTo-Json do not change parsed values. The CheckOnly validator result depends on CLAUDE_PLUGIN_ROOT; with it set, valid; without it, missing three hooks.
Response JSONL is written by the runner and is still growing while this review runs; the path is the durable stream receipt. Head SHA at review: 9fb9986e32b78e955980c1b0c50ad272fe2034f0.

Persistence proof: live client.SessionLog.QueryAsync returned the reviewer turn completed, response exactly equal to the 9463-character Markdown receipt, with 1 decision, 6 actions, 1 dialog item, 2 filesModified, and the full VERDICT JSON marker. Queried 2026-10-09T05:04:57.7033176Z.

=== VERDICT JSON ===
{"overallVerdict":"DISAGREE","accuracy":91,"completeness":92,"confidence":97,"passCount":14,"failCount":3,"unknownCount":1,"failList":["D5: Hook snapshot and restart explanation","B1: Operator Bash ruling followed in tool calls","B2: Validator will not re-add bridge hooks unconditionally"],"unknownList":["B10: One new Claude prompt now produces exactly one MCP turn"],"curableFails":["D5: retract snapshot claim and test with a fresh prompt","B2: qualify environment-dependent validator result and repair assertion"],"operatorDecisionFails":["B1: operator waiver or new compliant result for historical Bash writes"],"headSha":"9fb9986e32b78e955980c1b0c50ad272fe2034f0","receiptPaths":["/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-ruling-fix-20261009T045806Z.md","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-ruling-fix-20261009T045806Z.json","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261009T045806Z-ruling-fix-20261009.request.jsonl","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261009T045806Z-ruling-fix-20261009.response.jsonl"],"reviewerSessionId":"Codex-20261009T050039Z-hv-ruling-fix","reviewerRequestId":"req-20261009T050039Z-hv-ruling-fix"}
