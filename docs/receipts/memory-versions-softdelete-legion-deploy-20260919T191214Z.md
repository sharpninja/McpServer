# PR #51 MemoryVersions deployment on PAYTON-LEGION2

- Observed local time: 2026-09-19 14:12 -05:00. Work class: operator-directed deployment and diagnostics.
- Target and actual HEAD: `09275d0c0b3468135f61c5383252f0fdeebfc3d3` on `origin/develop`. Main checkout fast-forwarded; deployment built from a clean detached worktree at that SHA. Pre-existing main checkout changes were preserved.
- Nuke `UpdateService`: exit 0; transcript `docs/receipts/memory-versions-pr51-updateservice-20260919T185545Z.log` contains `NUKE_EXIT_CODE=0`. No manual binary deployment. Nuke bumped `GitVersion.yml` to 1.4.39 in the deployment worktree; it remains staged, not committed.
- Live service after controlled token-rotation restart: `Healthy`, version `1.4.39+09275d0c0b3468135f61c5383252f0fdeebfc3d3`; old PID 53472, new PID 89852.
- SQL Server `dbo.__EFMigrationsHistory` contains `20260918223000_AddMemoryVersionAndEdgeStorage`, `20260919060000_AddMemoryIndexStorage`, and `20260919193000_AddMemoryVersionSoftDeleteColumns`.
- `dbo.MemoryVersions`, `dbo.MemoryEdges`, and `dbo.MemoryIndexes` each have `IsDeleted`, `DeletedAtUtc`, `DeletedBy`, and `DeleteReason` (12 of 12 checked).
- Plugin/REPL `remember`: HTTP 201 created `MEMORY-FACT-003`. `list`: one matching item, `MEMORY-FACT-003`, before and after rotation.
- Plugin/REPL `recall`: HTTP 200, but the result omits `items` and ranking fields. Read-only direct live endpoint diagnostic returned HTTP 200, `rankingMode=hybrid`, and the `MEMORY-FACT-003` hit (five hits before rotation, two after with a more specific query). Client source `MemoryClient.RecallAsync` uses `MemorySurfaceResult`, a DTO without `items`. Triage `triage-report-a6fb8ae08ce348799d0db61ae2e0734a`. Server recall works; plugin usable-hit proof remains incomplete.
- Redaction: one authenticated post-deploy request generated two `X-Api-Key` header-value occurrences in a 5,227-byte new service-log segment: two `[REDACTED]`, zero unredacted, and zero appearances of the then-current token.
- API key rotated: yes. Service restart exit 0 generated a different workspace marker token at local `2026-09-19 14:06:12 -05:00`; old token got HTTP 401 and new token HTTP 200. No key value or digest is in this receipt. Plugin status is available; native plugin memory list and REPL memory list both succeeded after restart. The known Codex session cache, current-turn cache, and `~/.codex/config.toml` had no embedded API key field; session cache refreshed at local `14:06:47`. No running Director process or Director-named repository was found in the targeted checks, so an offline Director credential source was not verified.
- PR #51 schema stop gate: cleared by remember 201 and 12-column SQL proof. Full plugin recall presentation and exhaustive offline consumer proof remain open. No push performed; preserve unrelated dirty work and request operator Ack if commit-sync applies.
- Independent hostile validation requested at `docs/receipts/hv/20260919T191500Z-pr51-memory-deploy.request.jsonl`; complete Grok CLI event stream at the matching `.response.jsonl` (432,434 bytes). CLI exited with stream result `is_error=true`, `stop_reason=cancelled`, after a tool result stated `User cancelled the execution for tool use_tool`. No final verdict, scores, or reviewer session-log receipt was returned. Hostile gate is incomplete and no tracked done-state or push is authorized by this review.

## Follow-up, 2026-09-19 14:38 -05:00

- Re-fetched `origin/develop`; both checkout HEAD and remote tip remain `09275d0c0b3468135f61c5383252f0fdeebfc3d3` (`merge-base --is-ancestor` exit 0). Live `/health`: `Healthy`, `1.4.39+09275d0c0b3468135f61c5383252f0fdeebfc3d3`, storage `reachable`.
- Authenticated native plugin memory list found `MEMORY-FACT-003` after rotation. Installed `director.exe` 1.0.0.0 performed `director todo list --workspace F:\GitHub\McpServer` with exit 0 after rotation; Director config contains only `defaultBaseUrl` and the Director README says workspace API credentials come from the marker. No separately embedded Director API key was found in that config.
- Plugin/REPL recall usable-hit proof remains incomplete on the deployed 09275d0c client. Draft PR #52 (`512f9e495a5bfd899ac820aa276549e50f3fcf8f`, https://github.com/sharpninja/McpServer/pull/52) has a proposed client result fix and focused test receipts in its PR description; it is OPEN/DRAFT and has no CI check result. Do not report the live plugin recall gate as green from its unmerged tests.
- No code deployment or push repeated in this follow-up. The existing staged GitVersion.yml and untracked receipts remain uncommitted; the attached prompt requires operator Ack before a push when commit-sync applies.


## Live recheck, 2026-09-19T15:08:43-05:00

- `F:\GitHub\McpServer` develop and `origin/develop` both resolve to `09275d0c0b3468135f61c5383252f0fdeebfc3d3`; no reset, checkout, deploy, or push was repeated. The prior deployment worktree is dirty and cannot check out `develop` because the main checkout owns that branch.
- Live `/health`: `Healthy`, `1.4.39+09275d0c0b3468135f61c5383252f0fdeebfc3d3`, storage `reachable`; service PID `89852`. Prior Nuke transcript still exists and ends `NUKE_SOURCE_HEAD=09275d0c0b3468135f61c5383252f0fdeebfc3d3`, `NUKE_EXIT_CODE=0`.
- Fresh SQL Server query: migration history returned all three required IDs (`20260918223000_AddMemoryVersionAndEdgeStorage`, `20260919060000_AddMemoryIndexStorage`, `20260919193000_AddMemoryVersionSoftDeleteColumns`); information schema returned all 12 soft-delete columns across MemoryVersions, MemoryEdges, and MemoryIndexes.
- Fresh REPL `workflow.memory.remember`: HTTP 201, `MEMORY-FACT-004`; `workflow.memory.list` found exactly one matching smoke item. REPL remove succeeded; native list after removal returned zero matching items. Existing `MEMORY-FACT-003` still returned by plugin memory get/list.
- Fresh REPL `workflow.memory.recall`: HTTP 200 but no items in the client result. Read-only direct live endpoint diagnostic: HTTP 200, `rankingMode=hybrid`, two hits including `MEMORY-FACT-003`. This proves the service recall path, but the plugin/REPL usable-hit criterion remains open. PR #52 is OPEN/DRAFT at `0054d987d68392867e90fa711f71c10f11c44430`; no code from that PR has been deployed here.
- Current-key search in the active daily log: zero occurrences. After the documented 14:06:12 local rotation/restart, 78 of 79 log lines naming `X-Api-Key` contain `[REDACTED]`; the remaining line contains no header-value syntax. The 15:06-15:07 authenticated smoke requests produced redacted header lines. Historical pre-rotation log content was not treated as current protection evidence.
- Prior key rotation and consumer checks remain documented above. No key value was output or written here. No push performed; the pasted prompt requires operator Ack before a commit/push under commit-sync. Hostile review remains incomplete; no tracked done-state was changed.


## Live recheck, 2026-09-19T15:37:00-05:00

- `git fetch origin --prune` exit 0; `origin/develop` remains `09275d0c0b3468135f61c5383252f0fdeebfc3d3`. PR #52 is still OPEN/DRAFT at `0054d987d68392867e90fa711f71c10f11c44430` and unmerged.
- `/health` returned `Healthy`, version `1.4.39+09275d0c0b3468135f61c5383252f0fdeebfc3d3`, storage `reachable`; Windows `McpServer` service is running.
- Native plugin `memory_get` returned `MEMORY-FACT-003` after the earlier key rotation. This recheck did not repeat `remember`, SQL proof, or deployment; their receipts remain above.
- Plugin/REPL usable-hit recall remains blocked by the deployed client result contract; PR #52 proposes a fix but has not been merged or deployed. No push, merge, or deploy was performed in this recheck.


## Draft PR #52 boundary, 2026-09-19T15:40-05:00

- `gh pr view 52` reports OPEN/DRAFT at `0054d987d68392867e90fa711f71c10f11c44430`, changing seven client/REPL/test files (907 insertions, 30 deletions). It is a separate client fix, not part of merged PR #51.
- `gh pr checks 52` reports Build & Test and Validate failed after 3s and 2s respectively; run metadata shows zero executed steps in each failed job, so this does not establish a code failure or a green gate. The PR was not reviewed, merged, or deployed during this recheck.
- Local deploy worktree has Nuke's staged `GitVersion.yml` next-version change from 1.4.38 to 1.4.39 plus untracked receipts. The attached prompt's Ack condition and incomplete plugin recall/HV gate still prevent a completion or push claim.


## Pasted-prompt follow-through, 2026-09-19T21:13:04Z

- Queried `origin/develop` with `git ls-remote`; local and remote remain `09275d0c0b3468135f61c5383252f0fdeebfc3d3`. Live `/health` returned `Healthy`, version `1.4.39+09275d0c0b3468135f61c5383252f0fdeebfc3d3`, storage `reachable`. Native `memory_get` returned `MEMORY-FACT-003`.
- Draft PR #52 remains OPEN/DRAFT at `0054d987d68392867e90fa711f71c10f11c44430`. GitHub Build & Test and Validate jobs failed with zero executed steps. In an isolated detached review worktree at that SHA, focused client tests passed 13/13 and REPL workflow tests passed 5/5; complete affected project suites passed 298/298 and 852/852. Support memory client/REPL focused tests passed 3/3. These tests do not prove the deployed plugin presents recall hits. No PR #52 merge or deploy occurred.
- Security incident: while locating the health endpoint, a marker-file search exposed the then-current workspace API key in a PowerShell.Mcp tool output. The cached output file was deleted, but the tool transcript cannot be assumed erased. Direct `Restart-Service McpServer` was denied because the console has no service-control elevation; key re-rotation and old-key rejection proof are pending operator UAC availability. No key value or digest is written in this receipt.
- Stop gate remains open: live plugin/REPL usable-hit recall proof, new rotation proof, and hostile validation are incomplete. No tracked done-state change, commit, or push.
