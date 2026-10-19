# Catalog of handoff documents in sharpninja/McpServer

- **Catalog date:** 2026-10-16 (America/Chicago)
- **Base:** `origin/develop` at `36783b79` (merge of PR #75). Branch scan covers all 14 open remote branches other than `develop` and `main`.
- **Dates are last-commit dates (committer date, `git log -1` for that path on that ref), not authoring dates.** Many 2026-09-16 and 2026-10-05 values come from repo-wide rewrite commits (rebrand and SPDX header sweeps) and do not say when a document was written. Where a document states its own date, the summary says so.
- **Method:** git trees of `origin/develop` and every open remote branch, read from a bare clone. A path matches if it contains `handoff` or `hand-off` (case-insensitive) or lies under `docs/handoffs/`. Paths ending `.cs`, `.trx` or `.jsonl` are excluded. Headings and status text were read from each blob. Status and outstanding items come from reading each document and cross-checking whether the work landed on `develop`. Where that could not be shown, the status is `unverified`.
- **Status values:** `open` (work remains and is not shown as landed), `done` (work landed or the record is a closed historical record), `superseded` (replaced by a newer document), `stale` (old, removed from develop, or describing state that no longer holds), `unverified` (could not be established from the repo alone).
- **No secrets:** no keys, tokens or nonces are reproduced here. Some listed documents themselves cite hashes and ids; this catalog omits them.

## Totals

- **develop:** 122 matching files = 20 outside `docs/receipts/` + 102 under `docs/receipts/` (37 Markdown receipts and 65 non-Markdown receipt artifacts: JSON, TXT, XML, YAML). Counts follow the stated match rule and exclusions. The earlier chat catalog reported 111 (58 documents and 53 artifacts) with a looser rule, so the totals differ.
- **Open remote branches (non-develop/main) scanned:** 14. **Distinct branch files that differ from or are missing on develop:** 25 (by path and blob; many are the same blob on several branches).

## Outstanding work index

Open and unverified items only, newest first. Each row links to its handoff path on the branch that holds it (item 7 links the copy on `grok/session-lifecycle`, one of eleven old branches). Links resolve while those branches exist.

| # | Last commit | Item | Handoff path | Branch | Status | Outstanding |
|---|---|---|---|---|---|---|
| 1 | 2026-10-16 | PLAN-SESSIONTRIAGEFIX-001: remediation of the session-20261009 triage reports | [`docs/handoffs/PLAN-SESSIONTRIAGEFIX-001-handoff-20261016T125035.md`](https://github.com/sharpninja/McpServer/blob/fix/session-triage-20261013/docs/handoffs/PLAN-SESSIONTRIAGEFIX-001-handoff-20261016T125035.md) | `fix/session-triage-20261013` | open (paused by operator) | All 57 work items committed and every gate green at `5d149ffc`, but no HV round has run and the TODO shows 0 of 65 tasks done. Not started: slice E lane rerun, HV rounds, deploy and post-deploy. Blocked: Windows evidence for slices C and D waits on a WMI permission change on PAYTON-LEGION2. Four requirement-traceability items left open by the operator. Branch is unmerged to develop. |
| 2 | 2026-10-16 | session-20261009 worktree: SessionTurn/Review/AgentContext plan and architecture | [`docs/handoffs/session-20261009-worktree-handoff-20261016T125035.md`](https://github.com/sharpninja/McpServer/blob/worktree-session-20261009/docs/handoffs/session-20261009-worktree-handoff-20261016T125035.md) | `worktree-session-20261009` | open | Merged plan PLAN-SESSIONTURNREVIEW-001 is NOT APPROVED and a code freeze holds. Architecture hostile validation round 2 is DISAGREE 95/94/98 with six implementer fixes AR2-F1..AR2-F6 not applied. Round-5 reconciliation must be presented to the operator before any fix or round 3. Open TODOs: PLAN-SESSIONTURNREVIEW-001 (29 of 30 tasks open), BUG-BUILD-005 and -008 HV passes, PLAN-BUILDWHATIF-001 (0 of 4), MAINT-CIREMOVAL-001 (3 of 6). Branch is 1 commit ahead of develop at last measure and not yet on develop. |
| 3 | 2026-10-16 | Federation fixes (P1 path handling and forward-slash workspace keys; P2 to P4) | [`docs/handoffs/federation-fixes-handoff-20261016T121400.md`](https://github.com/sharpninja/McpServer/blob/develop/docs/handoffs/federation-fixes-handoff-20261016T121400.md) | `develop` | open | Merged as PR #79 (`ebf84590`). P1.4 has never passed its three-suite gate. P1.5 (rekey command and startup guard) is not on develop; a Codex worktree for it is in progress locally and is not on origin (unverified). P2 (IMPL-FEDROUTE-001), P3 (IMPL-FEDWSSYNC-001), P4 (IMPL-FEDBACKFILL-001) not started. Four rebased-tree test failures to attribute and a stale test expectation to update. **Deploy hold:** do not deploy develop until P1.5 lands, because stores holding `C:\...` workspace ids would stop matching. |
| 4 | 2026-10-16 | PLAN-LINUX-RUNAS-REMEDIATION-001 (frozen revision 6) | [`HANDOFF-LINUX-RUNAS-REMEDIATION-001.md`](https://github.com/sharpninja/McpServer/blob/develop/HANDOFF-LINUX-RUNAS-REMEDIATION-001.md) | `develop` | open (frozen, not implementation-ready) | Claude review of revision 6 is DISAGREE (accuracy 95, completeness 92, FORMAL_BLOCKED); nine failures and six unknowns remain. No implementation, worktree or deployment approved. Needs an explicit follow-up scope from the operator. |
| 5 | 2026-10-13 | Round-5 reconciliation lane files (requirements, code, risk) | [`docs/handoffs/session-20261009-recon-20261012/RECONCILIATION.md`](https://github.com/sharpninja/McpServer/blob/develop/docs/handoffs/session-20261009-recon-20261012/RECONCILIATION.md) | `develop` | unverified | Reconciliation found two blockers and thirteen inconsistencies against the round-5 architecture. It predates the domain reorganization and two architecture HV rounds, so many items may be addressed. Not re-checked here. Tracked through the worktree handoff above. |
| 6 | 2026-10-05 | PLAN-PLUGINHANDOFF-001 umbrella and completion plan | [`docs/plans/PLAN-PLUGINHANDOFF-001-completion-20260906.md`](https://github.com/sharpninja/McpServer/blob/develop/docs/plans/PLAN-PLUGINHANDOFF-001-completion-20260906.md) | `develop` | unverified | A 2026-09-11 hostile review (`hv/...rereview2.verdict.md`) recorded AGREE 100/100 that the umbrella TODO was complete for its scope. The completion plan says to keep MCP-WORKSPACEHYGIENE-002 and the umbrella open while the operator keep-open instruction holds. Live TODO state was not checked. |
| 7 | 2026-09-16 | Grok completion program (paused 2026-09-09) | [`docs/handoffs/grok-completion-program-20260909.md`](https://github.com/sharpninja/McpServer/blob/grok/session-lifecycle/docs/handoffs/grok-completion-program-20260909.md) | `11 older branches (see Section 2)` | unverified (stale copy) | Document says the operator paused all work; PLAN-REDDITFEATURES-001 listed as open and not started. Removed from develop on 2026-10-08. Whether the program resumed or closed was not established. |

Not listed because the item is closed, superseded or stale: the 2026-10-13 worktree handoff and 2026-10-09 sessionturn plan (superseded), the txnkeyserver handoff (done), the opensession integration prompt (done), the 503 readiness handoff (done, inferred), and the old May to July handoffs (stale). See the tables below.

## Section 1: handoff documents on origin/develop

### 1a. Outside docs/receipts/

**Folder: `(repo root)`**

| Path | Last commit | Size (bytes) | Heading | Status | Summary | Outstanding items |
|---|---|---|---|---|---|---|
| `HANDOFF-LINUX-RUNAS-REMEDIATION-001.md` | 2026-10-16 `5f5f328d` | 9233 | Handoff: Linux RunAs remediation — frozen revision 6 | open | Frozen revision-6 plan handoff for restoring Linux Agent Helper answers with the CLI running as the `sharpninja` user. Review DISAGREE (95/92). Plan not implementation-ready; no implementation approved. | Nine review failures and six unknowns; needs an explicit follow-up scope. Durable evidence is under `docs/receipts/runas-remediation/PLAN-LINUX-RUNAS-REMEDIATION-001/` (12 files). |

**Folder: `docs`**

| Path | Last commit | Size (bytes) | Heading | Status | Summary | Outstanding items |
|---|---|---|---|---|---|---|
| `docs/Handoff-Ingestion.md` | 2026-10-05 `6de989f3` | 4119 | Handoff Ingestion | done | How handoff ingestion turns a handoff document into an MCP TODO draft: sources, modes, review, provenance. Implemented by `HandoffIngestionService`. | None. |

**Folder: `docs/Project/wiki/azure`**

| Path | Last commit | Size (bytes) | Heading | Status | Summary | Outstanding items |
|---|---|---|---|---|---|---|
| `docs/Project/wiki/azure/Handoff-Ingestion.md` | 2026-09-16 `55fae924` | 4127 | Handoff Ingestion | done | Wiki copy of Handoff-Ingestion.md (azure projection); 8 bytes larger than the docs copy. | None. Near-duplicate. |

**Folder: `docs/Project/wiki/github`**

| Path | Last commit | Size (bytes) | Heading | Status | Summary | Outstanding items |
|---|---|---|---|---|---|---|
| `docs/Project/wiki/github/Handoff-Ingestion.md` | 2026-09-16 `55fae924` | 4127 | Handoff Ingestion | done | Wiki copy of Handoff-Ingestion.md (github projection); identical size to the azure copy. | None. Near-duplicate. |

**Folder: `docs/handoffs`**

| Path | Last commit | Size (bytes) | Heading | Status | Summary | Outstanding items |
|---|---|---|---|---|---|---|
| `docs/handoffs/example.md` | 2026-10-05 `6de989f3` | 1361 | Example Handoff: Document a bounded ingest-to-TODO sample | done | Worked Path-source sample for handoff ingestion (`EXAMPLE-HANDOFF-001`); ingest with `DraftOnly` only. | None. |
| `docs/handoffs/federation-fixes-handoff-20261016T121400.md` | 2026-10-16 `6601c471` | 17748 | Handoff: federation fixes (merged to develop as ebf84590), 2026-10-16T12:14:00 local, updated 12:35 | open | Definitive federation state: P1.1 to P1.3, P1.R and P1.4a to d delivered and merged as PR #79 (`ebf84590`); P1.4 gate never passed; P1.5 and P2 to P4 not started. Decisions D1 to D7 on record. | Baseline at `99a2a840`, stale UseCase test expectation, three migration-test failures, P1.4 gate, P1.5 rekey and startup guard, P2 to P4, deployment sequence. Deploy hold applies. |
| `docs/handoffs/session-20261009-worktree-handoff-20261013T165506.md` | 2026-10-13 `961f596b` | 24632 | Handoff: worktree session-20261009 (branch worktree-session-20261009), 2026-10-13T16:55:06 local | superseded | Worktree handoff for the merged SessionTurn/Review/AgentContext plan, written 2026-10-13T16:55. Superseded by the 2026-10-16T12:50:35 handoff on branch `worktree-session-20261009`; its git, PR and worktree facts are stale (PR #78 has since merged). | Open work carried by the 2026-10-16 handoff (Section 2). |
| `docs/handoffs/sessionturn-review-plan-20261009T022141Z.md` | 2026-10-13 `645b94ed` | 62311 | Handoff: Session-turn logging, request class, and turn reviews plan (APPROVED; amended 2026-10-10) | superseded | Handoff for the session-turn logging, request class and turn reviews plan (marked APPROVED, amended 2026-10-10). Its own banner says it is superseded by the 2026-10-13 worktree handoff; its approval status is stale. | None; history of plan-readiness rounds 1 to 43 only. |

**Folder: `docs/handoffs/session-20261009-recon-20261012`**

| Path | Last commit | Size (bytes) | Heading | Status | Summary | Outstanding items |
|---|---|---|---|---|---|---|
| `docs/handoffs/session-20261009-recon-20261012/BRIEF.md` | 2026-10-13 `645b94ed` | 5132 | Requirements reconciliation before the architecture HV: shared brief | stale | Shared brief for five read-only reconciliation lanes (2026-10-12). Standing rules and scope; not a standalone handoff. | None. |
| `docs/handoffs/session-20261009-recon-20261012/RECONCILIATION.md` | 2026-10-13 `645b94ed` | 14758 | Round-5 reconciliation: new requirements, existing requirements, code, tests, and projected problems | unverified | Round-5 reconciliation of new requirements, existing requirements, code, tests and projected problems. Verdict: architecture not ready for HV; two blockers and thirteen inconsistencies. | Findings never presented to the operator before HV; may be partly addressed since. |
| `docs/handoffs/session-20261009-recon-20261012/code-plugin.md` | 2026-10-13 `645b94ed` | 43312 | Recon lane code-plugin: new and changed requirements mapped to the plugin core, host plugins, plugin tests, an | unverified | Lane report: plugin code impact of the round-5 design. | Feeds RECONCILIATION.md. |
| `docs/handoffs/session-20261009-recon-20261012/code-server.md` | 2026-10-13 `645b94ed` | 56138 | Recon lane code-server: new and changed requirements against existing server, client, REPL, and storage code a | unverified | Lane report: server code impact of the round-5 design. | Feeds RECONCILIATION.md. |
| `docs/handoffs/session-20261009-recon-20261012/req-ab.md` | 2026-10-13 `645b94ed` | 44638 | req-ab: requirements reconciliation for Phases A and B and the cross-cutting requirements | unverified | Lane report: requirements reconciliation for Phases A and B and cross-cutting requirements (overlaps O1.. and contradictions C1..). | Feeds RECONCILIATION.md; same open status. |
| `docs/handoffs/session-20261009-recon-20261012/req-c.md` | 2026-10-13 `645b94ed` | 48586 | req-c: requirements reconciliation for Phase C (stateless agent context) | unverified | Lane report: requirements reconciliation for Phase C (agent context). | Feeds RECONCILIATION.md. |
| `docs/handoffs/session-20261009-recon-20261012/risk.md` | 2026-10-13 `645b94ed` | 49099 | Risk projection of the round-5 design changes (lane: risk) | unverified | Lane report: risk projection of the round-5 design changes with severity and likelihood. | Feeds RECONCILIATION.md. |

**Folder: `docs/plans`**

| Path | Last commit | Size (bytes) | Heading | Status | Summary | Outstanding items |
|---|---|---|---|---|---|---|
| `docs/plans/PLAN-PLUGINHANDOFF-001-completion-20260906.md` | 2026-10-05 `6de989f3` | 89624 | BDPv4 Completion Plan: Entire Active Program | unverified | BDPv4 completion plan for the entire active program (BUG-TRIAGE-139 and unfinished PLUGINHANDOFF phases). Plan-only. | Keeps MCP-WORKSPACEHYGIENE-002 and the umbrella open under the operator keep-open instruction. |
| `docs/plans/PLAN-PLUGINHANDOFF-001.md` | 2026-10-05 `6de989f3` | 68448 | PLAN-PLUGINHANDOFF-001: Plugin core, hostile review, plugin integration, workspace hygiene, handoff, wiki hydr | unverified | Master plan: plugin core, hostile review, plugin integration, workspace hygiene, handoff and wiki hydration. Heading status "third revision ... still draft" as of 2026-08-21. | Later HV AGREE (2026-09-11) claims completion for the TODO; live TODO not checked. |

**Folder: `plugins/core/skills/handoff`**

| Path | Last commit | Size (bytes) | Heading | Status | Summary | Outstanding items |
|---|---|---|---|---|---|---|
| `plugins/core/skills/handoff/SKILL.md` | 2026-09-16 `55fae924` | 1694 | Handoff Ingestion (skill; front matter name `handoff`) | done | Plugin skill `handoff`: ingest, inspect and approve handoff runs through `workflow.handoff.*`. | None. |
| `plugins/core/skills/handoff/invoke.ps1` | 2026-09-16 `55fae924` | 3781 | (script) | done | PowerShell entrypoint for the handoff skill. | None. |

**Folder: `plugins/core/test-fixtures/pester`**

| Path | Last commit | Size (bytes) | Heading | Status | Summary | Outstanding items |
|---|---|---|---|---|---|---|
| `plugins/core/test-fixtures/pester/PluginHandoffSkill.Tests.ps1` | 2026-10-05 `6de989f3` | 5641 | (script) | done | Pester test for the handoff skill. | None. |

### 1b. docs/receipts/ Markdown receipts (grouped)

All are evidence records of the 2026-08 and 2026-09 PLUGINHANDOFF and MCP-HANDOFF work. They are historical; their own work is closed or rolled into PLAN-PLUGINHANDOFF-001 (Section 1a). Status `done` means a closed historical record, `superseded` means a later receipt replaced it.

**MCP-HANDOFF validation and plan receipts (2026-08-16 to 08-17)**

| Path (under `docs/receipts/`) | Last commit | Size (bytes) | Heading | Status | Summary |
|---|---|---|---|---|---|
| `mcp-handoff-001-validation-20260816T185657Z.md` | 2026-08-18 `bf000bb7` | 2317 | MCP-HANDOFF-001 validation receipt 20260816T185657Z | superseded | Validation receipt; TODOs remained open at that time. |
| `mcp-handoffplan-001-codex-not-approved-20260817T035435Z.md` | 2026-08-18 `bf000bb7` | 5594 | MCP-HANDOFFPLAN-001 Codex NOT APPROVED remediations | superseded | Remediations for a Codex NOT APPROVED review of MCP-HANDOFFPLAN-001; product fixes first, later redone test-first (see gate-a receipts). |
| `mcp-handoffplan-001-final-20260816T205400Z.md` | 2026-08-18 `bf000bb7` | 4232 | MCP-HANDOFFPLAN-001 final receipt 20260816T205400Z | superseded | Final receipt before Codex review; hostile AGREE required before done and not yet obtained. |
| `mcp-handoffplan-001-final-20260816T231500Z.md` | 2026-08-18 `bf000bb7` | 4487 | MCP-HANDOFFPLAN-001 receipt for Codex review | superseded | Receipt for Codex review; SQL migration-lock test failure noted; hostile AGREE not obtained that turn. |
| `mcp-handoffplan-001-gate-a-corrected-20260817T063623Z.md` | 2026-08-18 `bf000bb7` | 13111 | GATE A corrected: mutation-backed RED then GREEN | done | Gate A corrected: mutation-backed RED then GREEN with rebuild before GREEN. |
| `mcp-handoffplan-001-gate-a-mutation-red-20260817T041322Z.md` | 2026-08-18 `bf000bb7` | 5583 | GATE A: corrective mutation-backed RED then GREEN | superseded | Gate A mutation-backed RED then GREEN; replaced by the corrected receipt. |
| `mcp-handoffplan-001-gate-b-20260817T090020Z.md` | 2026-08-18 `bf000bb7` | 5699 | GATE B: integration RED then two full greens | done | Gate B: integration RED then two full greens. |
| `mcp-handoffplan-001-scope-corrected-20260817T005200Z.md` | 2026-08-18 `bf000bb7` | 5223 | MCP-HANDOFFPLAN-001 scope-corrected receipt | superseded | Scope-corrected receipt; both TODOs open for independent review. |
| `mcp-handoffplan-001-validation-20260816T230700Z.md` | 2026-08-18 `bf000bb7` | 2071 | MCP-HANDOFFPLAN-001 validation receipt | superseded | Validation receipt; TODOs remained open at that time. |

**PLUGINHANDOFF continuation and hourly dispatch receipts (2026-08-21 to 08-22)**

| Path (under `docs/receipts/`) | Last commit | Size (bytes) | Heading | Status | Summary |
|---|---|---|---|---|---|
| `plan-pluginhandoff-b3-dispatch-20260821T222058Z.md` | 2026-09-16 `55fae924` | 2247 | PLAN-PLUGINHANDOFF-001 continuation dispatch | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-c-red-p1-dispatch-20260821T231931Z.md` | 2026-09-16 `55fae924` | 1968 | PLAN-PLUGINHANDOFF-001 continuation dispatch (C-red-P1) | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-c-red-p4-dispatch-20260822T002007Z.md` | 2026-09-16 `55fae924` | 1734 | PLAN-PLUGINHANDOFF-001 continuation dispatch (C-red-P4 r2 + D1) | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-d4-dispatch-20260822T141100Z.md` | 2026-09-16 `55fae924` | 1201 | PLAN-PLUGINHANDOFF-001 hourly: D4 plus E-red/G-red dispatch | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-hourly-20260822T151129Z.md` | 2026-09-16 `55fae924` | 1260 | PLAN-PLUGINHANDOFF-001 hourly: D4 remainder plus E/G hostile plus F-red | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-p11-d2-dispatch-20260822T022012Z.md` | 2026-09-16 `55fae924` | 1447 | PLAN-PLUGINHANDOFF-001 continuation dispatch (C-red-P11 + D2) | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-p12-d2g-dispatch-20260822T031946Z.md` | 2026-09-16 `55fae924` | 1356 | PLAN-PLUGINHANDOFF-001 continuation dispatch (C-green-P11-P13 + D2-green) | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-p14-d3-dispatch-20260822T042008Z.md` | 2026-09-16 `55fae924` | 1393 | PLAN-PLUGINHANDOFF-001 continuation dispatch (C-green-P14 + D3) | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-p15-dispatch-20260822T052451Z.md` | 2026-09-16 `55fae924` | 2497 | PLAN-PLUGINHANDOFF-001 continuation dispatch (C-red-P15) | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-p15g-dispatch-20260822T061903Z.md` | 2026-09-16 `55fae924` | 2147 | PLAN-PLUGINHANDOFF-001 continuation dispatch (C-green-P15) | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-p16-dispatch-20260822T071924Z.md` | 2026-09-16 `55fae924` | 1755 | PLAN-PLUGINHANDOFF-001 continuation dispatch (C-red-P16) | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-p18-dispatch-20260822T084434Z.md` | 2026-09-16 `55fae924` | 1351 | PLAN-PLUGINHANDOFF-001 hourly: C-green-P16-P18 hostile dispatch | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-p20-dispatch-20260822T131500Z.md` | 2026-09-16 `55fae924` | 1953 | PLAN-PLUGINHANDOFF-001 hourly: C-red-P20 hostile dispatch | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |
| `plan-pluginhandoff-p6-d15-dispatch-20260822T012004Z.md` | 2026-09-16 `55fae924` | 1573 | PLAN-PLUGINHANDOFF-001 continuation dispatch (C-green-P5-P6 + D1.5) | done | Continuation dispatch record: health check, prior hostile AGREE or DISAGREE references and next step. |

**D0 inventories and D4 gate receipts**

| Path (under `docs/receipts/`) | Last commit | Size (bytes) | Heading | Status | Summary |
|---|---|---|---|---|---|
| `d0-handoff-inventory-20260821T222058Z.md` | 2026-09-16 `55fae924` | 16906 | D0 Handoff evidence inventory | done | D0 Handoff evidence inventory (reuse versus new work per plan phase). Two near-duplicate runs. |
| `d0-handoff-inventory-20260821T231931Z.md` | 2026-09-16 `55fae924` | 17436 | D0 Handoff evidence inventory | done | D0 Handoff evidence inventory (reuse versus new work per plan phase). Two near-duplicate runs. |
| `d4-handoff-gate-20260822T141644Z.md` | 2026-09-16 `55fae924` | 3529 | D4 HANDOFFPLAN gate for PLAN-PLUGINHANDOFF-001 | done | D4 HANDOFFPLAN gate receipt with verdict section. |
| `d4-handoff-gate-20260822T143852Z.md` | 2026-09-16 `55fae924` | 3731 | D4 HANDOFFPLAN gate receipts (2026-08-22T14:38:52Z) | unverified | D4 gate receipts; says PLAN-PLUGINHANDOFF-001 stays done:false, HYGIENE-002 open, D5 not started (as of 2026-08-22; later AGREE 2026-09-11). |

**Hostile-review (hv) prompts and verdicts for PLAN-PLUGINHANDOFF-100 (2026-09-11)**

| Path (under `docs/receipts/`) | Last commit | Size (bytes) | Heading | Status | Summary |
|---|---|---|---|---|---|
| `hv/20260911T021504Z-plan-pluginhandoff-100.prompt.md` | 2026-09-16 `55fae924` | 6194 | (no heading) You are Codex CLI gpt-5.6-sol extra-high (xhigh) hostile reviewer. | superseded | Reviewer prompt (or pending copy of it) for the PLAN-PLUGINHANDOFF-100 claim; same content as a dated prompt. |
| `hv/20260911T021504Z-plan-pluginhandoff-100.verdict.md` | 2026-09-16 `55fae924` | 9485 | (no heading) 2026-09-11T02:27:46.214Z - OverallVerdict: **DISAGREE** | superseded | Earlier hostile verdict: DISAGREE. Superseded by the rereview2 AGREE. |
| `hv/20260911T023901Z-plan-pluginhandoff-100-rereview.prompt.md` | 2026-09-16 `55fae924` | 2116 | (no heading) You are Codex CLI gpt-5.6-sol extra-high (xhigh) hostile reviewer. | superseded | Reviewer prompt (or pending copy of it) for the PLAN-PLUGINHANDOFF-100 claim; same content as a dated prompt. |
| `hv/20260911T023901Z-plan-pluginhandoff-100-rereview.verdict.md` | 2026-09-16 `55fae924` | 6108 | (no heading) Reviewed at 2026-09-11T02:45:44.6548429Z. | superseded | Earlier hostile verdict: DISAGREE. Superseded by the rereview2 AGREE. |
| `hv/20260911T024811Z-plan-pluginhandoff-100-rereview2.prompt.md` | 2026-09-16 `55fae924` | 1302 | (no heading) You are Codex CLI gpt-5.6-sol extra-high (xhigh) hostile reviewer. | superseded | Reviewer prompt (or pending copy of it) for the PLAN-PLUGINHANDOFF-100 claim; same content as a dated prompt. |
| `hv/20260911T024811Z-plan-pluginhandoff-100-rereview2.verdict.md` | 2026-09-16 `55fae924` | 2627 | (no heading) === VERDICT JSON === | done | Final re-review: AGREE, accuracy 100, completeness 100 (14 pass, 0 fail). |
| `hv/plan-pluginhandoff-100.prompt.pending.md` | 2026-09-16 `55fae924` | 6194 | (no heading) You are Codex CLI gpt-5.6-sol extra-high (xhigh) hostile reviewer. | superseded | Reviewer prompt (or pending copy of it) for the PLAN-PLUGINHANDOFF-100 claim; same content as a dated prompt. |
| `hv/plan-pluginhandoff-100.rereview.prompt.md` | 2026-09-16 `55fae924` | 2116 | (no heading) You are Codex CLI gpt-5.6-sol extra-high (xhigh) hostile reviewer. | superseded | Reviewer prompt (or pending copy of it) for the PLAN-PLUGINHANDOFF-100 claim; same content as a dated prompt. |
| `hv/plan-pluginhandoff-100.rereview2.prompt.md` | 2026-09-16 `55fae924` | 1302 | (no heading) You are Codex CLI gpt-5.6-sol extra-high (xhigh) hostile reviewer. | superseded | Reviewer prompt (or pending copy of it) for the PLAN-PLUGINHANDOFF-100 claim; same content as a dated prompt. |

**Other**

| Path (under `docs/receipts/`) | Last commit | Size (bytes) | Heading | Status | Summary |
|---|---|---|---|---|---|
| `handoff-usecase-canvas-20260809.md` | 2026-08-10 `bd8a8d9e` | 3843 | Detailed handoff: Use Case Extension + UML canvas (2026-08-09) | unverified | Detailed handoff for the Use Case extension and UML canvas (2026-08-09). Verified-status table lists an elevated UpdateService as blocked. Branch `worktree-session-20261009` has a commit removing it as unrelated. |

### 1c. docs/receipts/ non-Markdown artifacts (grouped by folder)

JSON, TXT, XML and YAML evidence captured by the PLUGINHANDOFF and MCP-HANDOFF work. Each is a machine capture (TODO readbacks, requirement exports, test output). Status is `done` (closed historical evidence) for all.

| Folder or file pattern (under `docs/receipts/`) | Files | Types | Total bytes | Last commit | What it holds |
|---|---|---|---|---|---|
| `_d1-20260822T002007Z` | 1 | 1 xml | 2562 | 2026-09-16 | D1 Pester result XML for the handoff skill. |
| `_d2-20260822T022012Z` | 7 | 5 txt, 2 xml | 23402 | 2026-09-16 | D2 handoff test stdout (C#, durability, migration, plugin sync) and Pester XML. |
| `_d3-20260822T042008Z` | 1 | 1 json | 3193 | 2026-09-16 | D3 handoff requirement mappings. |
| `_hv-a6-20260821T213854Z` | 1 | 1 json | 76019 | 2026-09-16 | TODO PLAN-PLUGINHANDOFF-001 snapshot for A6 hostile review. |
| `_hv-c-green-p1-p3` | 2 | 2 txt | 81724 | 2026-09-16 | Snapshots of the PLAN-PLUGINHANDOFF-001 TODO (and a compile probe) captured around C red/green gates. |
| `_hv-c-green-p11-p13` | 1 | 1 yaml | 81800 | 2026-09-16 | Snapshots of the PLAN-PLUGINHANDOFF-001 TODO (and a compile probe) captured around C red/green gates. |
| `_hv-c-green-p4` | 2 | 2 txt | 81876 | 2026-09-16 | Snapshots of the PLAN-PLUGINHANDOFF-001 TODO (and a compile probe) captured around C red/green gates. |
| `_hv-c-red-p1-reverify` | 2 | 2 txt | 81724 | 2026-09-16 | Snapshots of the PLAN-PLUGINHANDOFF-001 TODO (and a compile probe) captured around C red/green gates. |
| `_hv-c-red-p1` | 2 | 2 txt | 81622 | 2026-09-16 | Snapshots of the PLAN-PLUGINHANDOFF-001 TODO (and a compile probe) captured around C red/green gates. |
| `_hv-c-red-p11` | 1 | 1 json | 564 | 2026-09-16 | Snapshots of the PLAN-PLUGINHANDOFF-001 TODO (and a compile probe) captured around C red/green gates. |
| `_hv-c-red-p4-r2` | 2 | 2 txt | 81724 | 2026-09-16 | Snapshots of the PLAN-PLUGINHANDOFF-001 TODO (and a compile probe) captured around C red/green gates. |
| `_hv-c-red-p4-r3` | 2 | 2 txt | 81876 | 2026-09-16 | Snapshots of the PLAN-PLUGINHANDOFF-001 TODO (and a compile probe) captured around C red/green gates. |
| `_hv-c-red-p4` | 2 | 2 txt | 81724 | 2026-09-16 | Snapshots of the PLAN-PLUGINHANDOFF-001 TODO (and a compile probe) captured around C red/green gates. |
| `_hv-d15-20260822T012452Z` | 4 | 4 json | 29813 | 2026-09-16 | TODO and requirement readbacks, migration lists and git status for D1.5 and D2-green gates. |
| `_hv-d2g-20260822T032428Z` | 10 | 2 txt, 8 json | 128719 | 2026-09-16 | TODO and requirement readbacks, migration lists and git status for D1.5 and D2-green gates. |
| `_hv-d4-20260822T154254Z` | 2 | 2 txt | 11229 | 2026-09-16 | D4 TODO MCP-HANDOFFPLAN-001 readback. |
| `(top level files)` | 6 | 1 txt, 5 json | 589234 | 2026-09-16 | D0 inventory JSON twins, D4 gate JSON, and the 515 KB `mcp-handoff-001-validation` JSON. |
| `_hv-happly-hdone` | 12 | 10 txt, 2 json | 29411 | 2026-08-21 | TODO and FR-HANDOFF-001..007 readbacks from the hostile apply gate. |
| `_hv-hclassify-s3` | 1 | 1 txt | 698 | 2026-08-21 | TODO readback from the classification gate. |
| `_hv-p0a-d0-out` | 4 | 3 txt, 1 json | 24783 | 2026-09-16 | P0-A D0 scans of handoff methods and files. |

Receipt notes: several `todo-plan-pluginhandoff-001` snapshots are byte-size duplicates (81,724 bytes) across `_hv-c-*` folders; `_d1`/`_d2` Pester XML files repeat the same test.

## Section 2: handoff documents on open branches

Only files that differ from, or are absent on, develop are listed (25 distinct path and blob combinations). "On develop?" means the same path exists on develop. Branch sets:

- **Set A (11 old branches, all pre-rebrand snapshots):** `cursor/bdp-draft-v5-1675`, `cursor/merge-wcp-addendum-4d61`, `cursor/session-log-submit-budget-66aa`, `cursor/wcp-hostile-review-remediation-2f4a`, `cursor/wcp-pplx-secondary-remediation-f7ef`, `cursor/wcp-stub-legacy-anchors-98f1`, `cursor/whitepaper-context-projection-v012`, `docs/wcp-retrospective-indexing`, `grok/session-lifecycle`, `memory`, `memory-s1-red`.
- **`cursor/bdpv4-requirements-package-61d0`** is a 12th, older branch (36 matching files); it carries 2026-08-18 versions of several files.

| Branch | Path | On develop? | Last commit | Size (bytes) | Status | Summary | Outstanding items |
|---|---|---|---|---|---|---|---|
| `fix/session-triage-20261013` | `docs/handoffs/PLAN-SESSIONTRIAGEFIX-001-handoff-20261016T125035.md` | no | 2026-10-16 `f66c39f9` | 14211 | open | Handoff for PLAN-SESSIONTRIAGEFIX-001: paused remediation of every triage report from session-20261009. Not on develop. | See outstanding index item 1. |
| `worktree-session-20261009` | `docs/handoffs/session-20261009-worktree-handoff-20261016T125035.md` | no | 2026-10-16 `56c4b800` | 27907 | open | Refreshed worktree handoff for the SessionTurn/Review/AgentContext workstream. **Supersedes** `session-20261009-worktree-handoff-20261013T165506.md`. Notes PR #78 merged on 2026-10-13 and the branch is 1 commit ahead of develop. | See outstanding index item 2. |
| Set A + `cursor/bdpv4-requirements-package-61d0` (12) | `CODEX-HANDOFF.md` | no (removed 2026-10-08) | 2026-06-24 `1281d4db` | 22386 | stale | Codex handoff (top section updated 2026-06-25): plugin wrap-up, stop-gate repair and known incorrect state; also PLAN-TURNTRANSACTIONS-001 notes. Removed from develop 2026-10-08. | Not verified. |
| Set A + `cursor/bdpv4-requirements-package-61d0` (12) | `HANDOFF.md` | no (removed 2026-10-08) | 2026-07-25 `28f56515` | 7630 | stale | Grok handoff of 2026-06-23 for branch `feat/xunit-v3-and-review-hardening` with uncommitted shim and prompt work. Removed from develop 2026-10-08. | Remaining steps listed in the file are from June; not verified. |
| Set A + `cursor/bdpv4-requirements-package-61d0` (12) | `docs/Project/Plugin-Simplification-Handoff.md` | no (removed 2026-10-08) | 2026-06-13 `a9fdd29a` | 11955 | stale | McpServer ecosystem simplification handoff on `feature/sessionlog-lifecycle` (based on `main@95ba268`, 2026-06). Lists DONE phases and a decision required before fan-out. Plugin repos later merged `feature/plugin-core-migration`. Removed from develop 2026-10-08. | Not verified. |
| Set A (11) | `docs/handoffs/grok-completion-program-20260909.md` | no (removed 2026-10-08) | 2026-09-16 `55fae924` | 31380 | unverified | Grok handoff for the paused MCP Server completion program (Codex, 2026-09-09). Continuity only, not authorization to resume. Removed from develop on 2026-10-08 (`3ed6c639`). | Program resumption or closure not established; PLAN-REDDITFEATURES-001 listed open, not started. |
| Set A (11) | `docs/handoffs/handback-overlay-from-quadbrain-qbagent-20260910.md` | no (removed 2026-10-08) | 2026-09-16 `55fae924` | 22917 | stale | Handback to the overlay implementer after a QuadBrain/QBAgent interruption (Grok Build, 2026-09-10). Removed from develop 2026-10-08. | Resume point was Section 8 of the file; not verified as completed. |
| Set A (11) | `docs/handoffs/handoff-qbexec-mcp-tools-compaction-20260910.md` | no (removed 2026-10-08) | 2026-09-16 `55fae924` | 11308 | stale | Continuity handoff for the QuadBrain mcp_* executor and Grok compact knobs (2026-09-10). Removed from develop 2026-10-08. | Resume point was Section 8; not verified. |
| Set A (11) | `docs/handoffs/integrate-session-start-opensession-20260916.md` | no (removed 2026-10-08) | 2026-09-16 `6a72d445` | 6549 | done | Prompt to integrate the session-start OpenSession persist fix into other work. Develop `plugin-hook.ps1` now calls `client.SessionLog.OpenSessionAsync` and has a `persist-failed` path, so the fix landed. Removed from develop 2026-10-08. | None. |
| Set A minus `memory`, `memory-s1-red` (9) | `docs/handoffs/txnkeyserver-all-adapters-20260919.md` | no (removed 2026-10-08) | 2026-09-19 `bedc35cf` | 6874 | superseded | QuadBrain-only keyserver, all-adapter bypass (stop 2026-09-19). Banner: superseded as an active handoff; PLAN-TXNKEYSERVER-001 is Done=true after deploy. Removed from develop 2026-10-08. | None. |
| Set A + `cursor/bdpv4-requirements-package-61d0` (12) | `docs/plans/handoff-agent-parity-plan-2026-05-28.yaml` | no (removed 2026-10-08) | 2026-05-28 `6e744049` | 1018 | stale | Handoff YAML for creating PLAN-AGENTPARITY-001 (status ready-for-trusted-execution, 2026-05-28). Removed from develop 2026-10-08. | Not verified; the 2026-05-29 phase-1 handoff followed it. |
| Set A + `cursor/bdpv4-requirements-package-61d0` (12) | `docs/plans/handoff-mcpserver-503-auth-readiness-2026-06-15.md` | no (removed 2026-10-08) | 2026-06-15 `bf48f255` | 21663 | done | Handoff to fix blanket HTTP 503 on `/mcpserver/*` (PLAN-MCPSERVER503-001). File says the production fix was not yet applied; develop now has `WorkspaceReadinessHealthCheck`, readiness and auth integration tests, and FR-MCP-132/133 and TR-MCP-AUTH-010 in the requirement docs. Inferred from code presence, not run. Removed from develop 2026-10-08. | Confirm with a test run if needed. |
| Set A + `cursor/bdpv4-requirements-package-61d0` (12) | `docs/plans/handoff-parity-v4-phase1-complete-2026-05-29.md` | no (removed 2026-10-08) | 2026-05-28 `6e744049` | 8022 | stale | Clean handoff: v4 Byrd process and agent plugin parity Phase 1 gate complete (PLAN-AGENTPARITY-001). Has a Next Steps section. Removed from develop 2026-10-08. | Next steps not verified. |
| `cursor/bdpv4-requirements-package-61d0` | `docs/receipts/_hv-h5-done-handoff.ps1` | no (removed 2026-09-16) | 2026-08-18 `bf000bb7` | 685 | stale | Script used for the H5 done-handoff test run (2026-08-18). Removed from develop 2026-09-16. | None. |

The plan files `docs/plans/handoff-agent-parity-plan-2026-05-28.yaml` and `docs/plans/handoff-*` May files are the "two May plan files".

### Same path, different content from develop

| Path | Where it differs | Diff (develop vs branch) | Which is current |
|---|---|---|---|
| `docs/handoffs/session-20261009-worktree-handoff-20261013T165506.md` | `worktree-session-20261009` | Branch adds one "Superseded ..." banner line (+2 lines). | Branch version is newer (2026-10-16) but is itself superseded by the 20261016 handoff. |
| `docs/Handoff-Ingestion.md` | Set A (11) | Develop uses the QBrainAi names (`/qbrainai/handoff/*`, `QBrainAiClient`); branches use `/mcpserver/handoff/*` and `McpServerClient`. | Develop is the rebranded current form; branch copies predate the rebrand. |
| docs/Handoff-Ingestion.md, docs/Project/wiki/azure/Handoff-Ingestion.md, docs/Project/wiki/github/Handoff-Ingestion.md | `cursor/bdpv4-requirements-package-61d0` | Older 30-line (about 1.6 KB) version dated 2026-08-18; develop has the 107-line version. | Develop. |
| `docs/handoffs/example.md` | Set A (11) | Section name and public-surface names pre-rebrand (4 lines). | Develop. |
| `docs/plans/PLAN-PLUGINHANDOFF-001.md` | Set A (11) | Rebrand wording differences (about 66 changed lines: QBrainAi/McpServer paths, section name). | Develop (rebranded); content otherwise the same plan. |
| `docs/plans/PLAN-PLUGINHANDOFF-001-completion-20260906.md` | Set A (11) | Rebrand wording differences (about 22 changed lines, test project names and REST route). | Develop. |
| `plugins/core/skills/handoff/SKILL.md` | `cursor/bdpv4-requirements-package-61d0` | Branch lacks the "YAML Mutation Rule" section (+4/-4 lines). | Develop. |
| `plugins/core/test-fixtures/pester/PluginHandoffSkill.Tests.ps1` | Two older variants: `cursor/bdp-draft-v5-1675`, `cursor/session-log-submit-budget-66aa`, `grok/session-lifecycle` (4,952 bytes, 2026-09-16) and the other 8 Set A branches (5,642 bytes, 2026-09-28) | Older versions of the test; develop is 5,641 bytes. | Develop. |

## Supersession and duplicates

- `session-20261009-worktree-handoff-20261016T125035.md` (branch `worktree-session-20261009`) supersedes `session-20261009-worktree-handoff-20261013T165506.md`, which in turn superseded `sessionturn-review-plan-20261009T022141Z.md`. Only the 2026-10-16 file is current.
- `PLAN-SESSIONTRIAGEFIX-001-handoff-20261016T125035.md` supersedes `docs/Project/PLAN-SESSIONTRIAGEFIX-001-Status-20261016T113408.md` for current state (that file is outside the handoff match rule and is not catalogued).
- `federation-fixes-handoff-20261016T121400.md` is the definitive federation handoff; it does not name an older handoff it replaces.
- `txnkeyserver-all-adapters-20260919.md` is marked superseded by its own banner (work done and deployed).
- The three `Handoff-Ingestion.md` copies on develop (docs plus azure and github wiki projections) are near-duplicates; the wiki copies are 4,127 bytes against 4,119 for the docs copy.
- `docs/receipts/hv/plan-pluginhandoff-100.prompt.pending.md`, `.rereview.prompt.md` and `.rereview2.prompt.md` duplicate the dated prompts of the same names (same byte sizes). The first two verdicts are superseded by the rereview2 AGREE.
- `d0-handoff-inventory-20260821T222058Z` and `...T231931Z` (md and json) are two near-identical runs; `mcp-handoffplan-001-final-*`, `gate-a-*` and `not-approved` receipts are earlier attempts later redone (see Section 1b).
- The `_hv-c-*` receipt folders repeat the same 81,724-byte PLAN-PLUGINHANDOFF-001 TODO snapshot.
- Branch copies of old handoffs (Set A) are the same blobs on up to 12 branches; they were removed from develop on 2026-10-08 (`3ed6c639`).
- `docs/handoffs/example.md` and the plugin skill are samples and documentation, not workstream handoffs.
- `docs/receipts/handoff-usecase-canvas-20260809.md` is on develop; branch `worktree-session-20261009` carries a commit removing it as unrelated (status unverified).

## Notes and limits

- Statuses were derived from reading each document and from develop file presence; live TODO and requirement stores were not queried, so claims about TODO completion are `unverified`.
- Local-only work (for example a Codex worktree for P1.5) cannot be seen from origin and is not catalogued as landed.
- This catalog is itself under `docs/handoffs/` and matches the scan rule; it is not counted in the totals above.
