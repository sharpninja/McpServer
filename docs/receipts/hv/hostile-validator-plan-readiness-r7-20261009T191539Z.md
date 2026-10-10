# Hostile validator plan readiness, round 7

TimestampUtc: 2026-10-09T19:37:14Z. HEAD: `3bdf04ba96ee37c2ea06f181fbe3404e5257295f`.

**DISAGREE.** Accuracy 96/100, completeness 94/100, confidence 0.96. Approval requires 98/98 and every applicable claim to pass. This review scores the plan, live TODO, and entry-point handoff only.

## Validator and mandatory reads

Live Codex CLI rollout `turn_context` at 2026-10-09T19:15:43Z records `model=gpt-6-sol`, `effort=xhigh`, and this worktree; `thread.started` ID `01a12217-b094-7bd1-8402-351e6fcc9eba`. Rollout: `/home/sharpninja/.codex/sessions/2026/10/09/rollout-2026-10-09T14-15-42-01a12217-b094-7bd1-8402-351e6fcc9eba.jsonl`.

Read add-profile SKILL.md and all 19 non-skill profile Markdown files in full; read the main `AGENTS-README-FIRST.yaml` in full; ran `workflow.memory.list` and read all 14 effective MCP memories (12 Global, 2 Workspace). MCP reads used the Codex plugin wrapper with `-ParamsObject`; main-store reads set `MCP_WORKSPACE_PATH=/home/sharpninja/github/McpServer`.

## P1-P10

- **P1 FAIL.** Most inspected source anchors and nine plugin repo states matched HEAD. Plan line 61 mislocates the request-ID assignment; line 414 falsely says the new TEST statements name updated FR ACs. F-R7-01/02.
- **P2 FAIL.** Live main store returned 352 FR, 480 TR, 512 TEST; the 67 planned P0 FR/TR/TEST IDs were absent without collision. Ten mapping additions can merge with existing edges, but their target TEST statements omit the ACs they claim to cover. F-R7-02.
- **P3 FAIL.** D2-D10 and partial QB-AGENT-001 clearance were largely reflected. D1 is contradicted by the Copilot timestamp-only negative case that expects two turns. F-R7-03.
- **P4 FAIL.** Server, client, REPL, MCP, PowerShell and Node cores, nine plugins, and three provider migrations are named. Copilot event identity and declared Node 18/20 compatibility lack a valid implementation path. F-R7-03/04.
- **P5 FAIL.** Copilot stable identity versus unsupported status, and preserving Node support versus raising the runtime floor, remain material decisions. F-R7-03/04.
- **P6 UNKNOWN.** Named build targets, test-project paths, G1-G6 commands, and BDPv4 order exist statically; P0.1 now specifies an SDK 10.0.201 bootstrap. Installed SDK is 10.0.111 and Pester/bats are absent, so gate execution was not verified in review-only scope.
- **P7 FAIL.** Mapped updated ACs lack named TEST statements; Copilot negative test expects two turns; Node 20 score parsing has no compatible test. HV-P0, HV-A, HV-B and 98/98 are otherwise aligned. F-R7-02/03/04.
- **P8 FAIL.** Thirty tasks and phase ordering agree after round-6 edits, but D1/AC003 conflict with Copilot nonce construction and its negative test. F-R7-03.
- **P9 FAIL.** Live TODO description is 520/520 byte-equal to the plan's nonempty lines; all 30 tasks match in order; technicalDetails uses new IDs and marks 010G as legacy. Handoff current-marker and hook-registration claims are stale. F-R7-05.
- **P10 PASS.** Risks match source/provider observations. Nine listed triage report IDs resolve; worktree-only triage group resolves as grouped/failed and main route is absent as disclosed. Plugin defects are scheduled in A9.

## Findings and PASS conditions

### F-R7-01 — implementer edit

- Plan location: current source inventory, line 61.
- Exact defect: `plugin-hook.ps1:863` is cited as the request-ID generator.
- Evidence: `plugins/core/lib-ps/plugin-hook.ps1:865` assigns `req-<timestamp>-prompt-<rand4>`; line 863 is not the assignment.
- PASS condition: correct the anchor to line 865.

### F-R7-02 — implementer edit

- Plan location: mapping additions lines 414-424, TEST statements lines 361-385, P0.4 line 167.
- Exact defect: line 414 says every mapped TEST names its updated FR AC in its P0 statement; those statements omit them.
- Evidence: line 415 maps `TEST-MCP-SESSIONTURN-003` to `FR-MCP-SESSIONLIFE-001-AC006`, but line 365 omits that AC. Line 422 maps `TEST-MCP-SESSIONCLASS-002` to `FR-SUPPORT-013 ac-5`, but line 373 omits it. The ten added rows have the same gap.
- PASS condition: add every updated FR AC from lines 415-424 to the corresponding TEST statement, name its verifying test case, and require RED/green receipts for changed ACs before P0 creates the records and mappings.

### F-R7-03 — operator decision

- Plan location: locked D1 line 29, Copilot binding line 83, `FR-MCP-SESSIONTURN-001-AC003` line 190, A3 RED tests line 569.
- Exact defect: one Copilot prompt can mint two turns when two hook deliveries carry different timestamps.
- Evidence: line 83 admits separate Copilot hooks may receive different timestamps and expects **two turns plus warning** for timestamp-only differences. Lines 29 and 190 require one nonce and one turn per event across every hook host; line 29 says that warning never counts as D1 compliance. A3 only tests two processes with identical recorded payloads.
- PASS condition: use a stable adapter/event identity and test two differing-timestamp deliveries yielding one turn; or declare Copilot turn logging unsupported before mint and align ACs, gates, matrix, and handoff. Operator options: **stable Copilot adapter with one-turn guarantee** or **Copilot turn logging unsupported until stable identity exists**.

### F-R7-04 — operator decision

- Plan location: C10 line 122, score parsing line 557, Node A8/S9.
- Exact defect: required `JSON.parse` reviver `context.source` cannot provide lossless raw score lexemes on a declared supported Node 20 runtime.
- Evidence: `plugins/core/lib-node/package.json` declares `node >=20`; cline, cline-v2, and opencode declare `>=18`. Official Node 20.20.2 (V8 11.3.244.8) returned `undefined` for reviver `context.source`; local Node 26.8.2 returned `0.1` for the same probe. No compatible parser or runtime-floor migration is specified.
- PASS condition: choose the Node support floor. Options: **preserve Node 18/20 using a raw-token lossless parser and version-matrix tests**, or **raise engines and migrate host prerequisites with compatibility receipts**. Amend C10, A8/S9, TEST-MCP-SESSIONREVIEW-008/009, and G6.

### F-R7-05 — implementer edit

- Plan location: entry-point handoff pointer at plan line 3; handoff lines 55 and 80.
- Exact defect: handoff presents old marker state and historical duplicate hook registration as current.
- Evidence: main marker `startedAt` is `2026-10-09T17:45:57.2150865+00:00`, not handoff's `13:58:49Z`. `~/.claude/settings.json` has no hooks key; MEMORY-FACT-003 and corrected plan line 13 say duplicate registration was historical, while handoff line 80 says it is current.
- PASS condition: mark the earlier marker and hook incident historical and state the current inventory.

## Round-6 reattack

- F-R6-01 fixed in plan context; handoff remains stale (F-R7-05).
- F-R6-02 fixed: S7 has `TR-MCP-SESSIONCLASS-004`.
- F-R6-03 partial: ten mappings added, but TEST statements still omit updated ACs (F-R7-02).
- F-R6-04 fixed: C1-C12 disposition and ACK gates added.
- F-R6-05 specified: SDK bootstrap; execution remains unverified under P6.
- F-R6-06 fixed: slice and phase-wide HV completion semantics aligned.
- F-R6-07 fixed: Other with `otherNotes` specified.
- F-R6-08 fixed: TODO technicalDetails uses new IDs and labels 010G legacy.

Earlier-round source, route, provider, plugin, gate, and status findings were rechecked against current plan/source; no other live defect was confirmed.

## Evidence and verification limit

Plan source: 639 lines, 520 nonempty. Live TODO: 520/520 byte-equal description lines and 30/30 ordered tasks, status NOT APPROVED. Nine plugin repositories, migrations, source anchors, and triage reports were inspected. Official Node source: https://nodejs.org/en/blog/announcements/v20-release-announce and https://nodejs.org/dist/v20.20.2/. SDK source: https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json. Codex hook sources: https://learn.chatgpt.com/docs/hooks and https://developers.openai.com/plugins/build/plugins.

P6 remains UNKNOWN because P0 toolchain provisioning and G1-G6 execution would exceed review-only scope. This is a verification limit, not an infrastructure score.

## Reviewer turn and persistence

Session `Codex-20261009T032846Z-plugin-session`, request `req-20261009T191808Z-plan-readiness-r7`. `workflow.sessionlog.updateTurn` returned `code=persisted`, `persisted=true`, `degraded=false`, `queued=false`. Full Markdown and JSON result appendDialog calls both returned code=persisted, persisted=true, degraded=false, queued=false. appendActions and rich updateTurn also returned persisted=true. Read-only workflow.sessionlog.queryHistory showed this session in_progress, turnCount 4, lastUpdated 2026-10-09T19:38:13Z. The final receipt is re-appended below and turn completion is checked afterward.

Runner request: `docs/receipts/hv/20261009T191539Z-plan-readiness-r7.request.jsonl`. Runner response: `docs/receipts/hv/20261009T191539Z-plan-readiness-r7.response.jsonl`.


