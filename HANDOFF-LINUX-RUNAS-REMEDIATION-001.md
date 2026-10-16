# Handoff: Linux RunAs remediation — frozen revision 6

## Task and authorization

- Canonical TODO: **PLAN-LINUX-RUNAS-REMEDIATION-001**.
- Priority: **high**. Section: **AgentExecution**. Status: **not done**.
- Operator instruction: “stop iterating. Write plan as-is to new high todo and create a handoff in repo root specific to this plan.”
- Act-mode approval applies to this capture and handoff only. It does **not** approve the remediation architecture, implementation, worktree creation, or deployment.
- Planning iteration has stopped. Do not automatically launch another review or begin implementation when reading this handoff.

## Exact preserved plan

The TODO's `description[0]` contains the complete revision-6 plan, not a summary, but the database string sanitizer replaces em dashes with ASCII hyphens. The retry verified this is the only readable-text difference. The TODO also stores the exact original UTF-8 bytes as gzip/Base64 in its `technicalDetails` entry beginning `planUtf8GzipBase64: `. Decoding that entry was verified byte-for-byte against the original; no plan revision was made. Its unchanged, directly readable durable copy is:

`/home/sharpninja/github/McpServer/docs/receipts/runas-remediation/PLAN-LINUX-RUNAS-REMEDIATION-001/runas-plan-r6.md`

SHA256:

```text
60F4EF7EDD9A4836C59E0AD966625D9333BC26D1B2278CD68EA6097249AA5ACD
```

The plan intentionally retains its original temporary paths, original snapshot dates, and unresolved contradictions. Those references were not rewritten, because the operator requested preservation **as-is**. Read the durable files below instead of relying on `/tmp` surviving.

## Goal and non-negotiable operator constraints

Restore completed, real Agent Helper answers with the actual CLI running as `sharpninja`, while MCP remains `mcpserver` with its existing hardening. Grok is the configured default, not a reason to bypass the strategy pattern or create a Grok-only pipeline.

- Use a separate implementation worktree. Proposed path: `/home/sharpninja/github/McpServer-runas-remediation`; proposed branch: `remediation/linux-agent-runas`. This worktree was **not created** for this capture.
- Follow strict BDPv4: acceptance tests must pass against mocks, and their assertions must detect deliberately incorrect mock behavior, **before real implementation**. Retain meaningful real-SUT red, green, refactor, and complete current/prior regression evidence with zero failures and zero skips.
- Explicitly track every new/updated FR, TR, AC, and UC and map each AC to named TDD tests and specific phases.
- Run development HV **only at completion of major phases**, not at individual increments or mock/red gates. The frozen plan groups development into P1–P5. Planning review is separately governed by operator authorization.
- Preserve all existing strategy selections/aliases, identity failure semantics, full session contracts, and unrelated production configuration/data.
- Review polling, if later authorized, must be no more frequent than two minutes. Brief updates may report actual elapsed time and qualified ETA.
- Preserve complete tool/reviewer output; UI-level collapsing is a client concern, not model-level shortening.

## Review state: not implementation-ready

Claude's completed revision-6 review is **DISAGREE**:

- Accuracy: **95/100**.
- Completeness: **92/100**.
- Formal gate: **FORMAL_BLOCKED**.
- Nine failures and six unknowns remain. The minimum approval threshold is 98 for both scores.
- Claude confirmed the prior revision's nine findings were addressed and the phase-end-only HV cadence follows the operator directive. That is **not** approval of the whole plan.
- The reviewer-reported timestamp is retained in the verdict; it must not be treated as independently verified wall-clock evidence.

The unresolved findings are:

1. Existing canonical requirement mappings would be dropped, and related HELP/TRIAGEHELP records lack explicit disposition.
2. FR/TR/TEST links are asymmetric; HELP-011 criteria lack corresponding TEST coverage.
3. Some normative clauses and section-8 tests have no owning AC, including typed errors/cancellation, direct-client identity guards, REST/federation policy validation, protocol spoofing, UTF-8/backpressure, and pending-job/query timeouts.
4. AC ownership does not consistently map each test to its exact increment, phase, project, and class; some unit tests are mislabeled final verification.
5. TDD-ledger/HV-cadence validator tests have no defined implementation home, and early-phase evidence is not explicitly revalidated after the validator exists.
6. I8 forward-port/rename work has no explicit acceptance criterion.
7. The earlier structural coverage proof predates the final plan hash and does not validate enough of the linkage/ownership rules. Do **not** treat that proof as acceptance evidence.
8. UC draft flows are prose strings rather than separate ordered basic, alternative, and exception steps with actor/AC references.
9. The seven Validation test projects are not individually enumerated.

Unknowns retained by the review include formal reviewer audit/durable receipt compliance, canonical UC identity/linkage, Grok runtime closure, primary workspace RunAs, exact canonical capture equivalence/directive evidence, and whether the world-readable live configuration contains secrets.

Read the **full verdict**, not only this summary, before any separately authorized follow-up. Partial revision-7 investigation occurred after revision-6 review; no revision-7 plan was finalized or reviewed. It does not amend this frozen revision-6 TODO.

## Durable evidence

All paths below are under:

`/home/sharpninja/github/McpServer/docs/receipts/runas-remediation/PLAN-LINUX-RUNAS-REMEDIATION-001/`

- `runas-plan-r6.md` — exact plan; 20 proposed records, 74 scoped ACs, nine draft UCs.
- `runas-plan-r6-catalog.json` — complete proposed FR/TR/TEST/AC catalog.
- `runas-plan-r6-usecases.json` — draft UC catalog; draft labels are **not** canonical numeric UseCaseIds.
- `runas-canonical-effective-r6.json` — historical effective-layer requirement readback used in planning, not current authoritative state.
- `runas-plan-hv-r6.request.jsonl` — complete captured review request.
- `runas-plan-hv-r6.direct.response.jsonl` — complete captured Claude machine stream, not a summary.
- `runas-plan-hv-r6.verdict.md` — full extracted verdict.
- `preservation-manifest.json` — copy provenance, byte lengths, and SHA256 digests.
- `todo-create-response.yaml` — supported-plugin TODO creation response.
- `todo-retry-update-response.yaml` — successful retry retaining the lossless gzip/Base64 original.
- `todo-readback.yaml` — full supported-plugin readback, including the exact encoded original.
- `todo-verification.json` — capture/readback verification written after creation.

Preserving full reviewer streams here does not retroactively establish Claude's own required session-log persistence or formal approval. Parent audit and reviewer audit are distinct.

## Repository/provenance context

- Canonical workspace: `/home/sharpninja/github/McpServer`.
- Observed `develop` HEAD during capture: `f56dcf70e7e84f2d286e1ca9e0e6680133f09a82`; current source has QBrainAi names.
- The frozen plan proposes historical base `c195837ac888b6f02724221de6a040e63b3e6b47` and a later reviewed current-line forward-port. This is a proposal, not authority to overwrite newer commits.
- Prior task work is recorded in stash `3ba6fc5413d5bb9fa68d7c312229512da0d86832`, with untracked parent recorded as `ad290006`. Do not pop, drop, reset, or blanket-copy it.
- Existing recovery and Claude worktrees were left untouched. Pre-existing untracked `/home/sharpninja/github/McpServer/.claude/worktrees/` content is unrelated to this capture.
- No production code, unit/integration tests, live configuration, service, or database schema was changed for this capture. No build, test suite, new HV, or deployment is claimed.

## Resumption boundary

The operator requested a high-priority backlog record and durable handoff, **not** more iteration. The capture task is complete once TODO priority/content and preserved artifact digests are verified.

Retry verification on October 9, 2026 confirmed high priority, `done: false`, exact lossless plan readback, the known-only readable normalization, and matching digests for all seven preserved artifacts. Earlier failed verification was retained in the conversation; the current verification JSON records the successful retry. The deployed session validator rejects this longer TODO identifier, although TODO CRUD accepts it. Session turns therefore use `todoId: None` and retain the actual identifier in descriptive audit text; this does not alter or rename the TODO.

Any future agent must first obtain an explicit follow-up scope, read this handoff and the full unchanged plan/verdict, use the required agent-specific MCP plugin, and distinguish preservation approval from implementation approval. Recheck canonical requirements, UCs, current workspace settings, repository HEAD, worktree collisions, and prerequisite gates before changing state. Do not mark the remediation TODO done merely because the handoff was saved.