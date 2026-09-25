# Weighted Context Projection for Long-Horizon Agent Work: Escaping Host Auto-Compaction via SessionLog, Memory, and Sessionless Frontier CLIs

**Document:** whitepaper-weighted-context-projection-v0.1.md  
**Version:** v0.1.15  
**Status:** Draft for operator review (design only; implementation proposal split out)  
**Audience:** Operator Payton Byrd  
**Author:** Payton Byrd  
**Date:** 2026-09-20 (revised 2026-09-25, America/Chicago)  
**Authoring context:** Engineering design note derived from MCP memory workstreams, plugin-hook limits, and literature on long-horizon agent context management. Not a product claim sheet.

**Scope.** This document argues a design. It deliberately contains no deployment choice, no roadmap, no phase gates, and no code-level specifics. Those live in the companion proposal, `proposed-implementation-weighted-context-projection-v0.1.md`, referred to below as **IMPL**. A bare `§N` refers to a section of this paper; `IMPL §N` refers to that companion.

---

## Executive summary

Long-horizon agent sessions fail more often from **context loss under host auto-compaction** than from raw model capability limits **(operational observation; not a measured benchmark claim)**. Plugin hooks can inject memories and react to compact events, but they do not own the host transcript. Cross-session MCP memory (the `memory_*` tools — remember / recall / explore / consolidate / promote, named in full in point 5 below) is necessary and already shipping, yet it is the wrong primary store for *turn-by-turn task state*. Whitespace-token estimator benches (including `memory-bench-whitespace`) are **not** provider-metered savings proof and must not be rehabilitated as such.

This whitepaper proposes **Weighted Context Projection**:

1. An append-only **SessionLog** holds the durable transcript (turns are never permanently deleted).
2. Each turn receives a **relative weight** for how much it advances **current work toward completion**.
3. A live **`contextProjection`** under a hard token budget expands pinned / high-weight turns, summarizes mid-weight turns, and omits low-weight turns while keeping them recoverable.
4. Reweighting can **re-admit** a previously omitted turn from SessionLog into the projection.
5. MCP memory tools (`memory_remember` / `memory_recall` / `memory_explore` / `memory_consolidate` / `memory_promote`) bridge durable cross-session facts so the projection stays small. Where this paper uses the shorter verb forms, they are shorthand for those tool names.
6. Prefer an **outer orchestrator** that assembles the projection and invokes frontier models via **sessionless CLI oneshots**, so host auto-compaction is unnecessary for the *model-facing* context—when we own assembly. The deployment alternatives and their trade-offs are analyzed in IMPL §6.

The design goal is operational continuity after reproject (or after a host compact fallback), not token-savings theater. Success is: the agent continues the task without the operator re-pasting lost constraints.

---

## 1. Problem

### 1.1 The real friction is compaction / context loss

Operator workflows that span dozens to hundreds of turns accumulate tool traces, dead ends, corrected plans, and quiet constraints (“never push to main”, machine paths, standing prefs). Host environments eventually compact. Compaction is typically **lossy and irreversible from the model’s point of view**: the full turn text leaves the live window; a summary may or may not preserve what mattered for *task progress*.

That is the failure mode this paper addresses. It is not “we estimated fewer whitespace tokens.”

### 1.2 What already shipped (and what it is not)

Three capabilities already exist. What matters for this paper is that none of them is the projection engine it proposes.

**A durable cross-session memory layer.** Workspace-scoped tools persist facts, decisions, preferences, procedures, and entities across sessions. A typed write path carries title, type, tags, confidence, and provenance, alongside a thinner compatibility CRUD surface for plain records; retrieval, exploration, promotion between layers, consolidation of near-duplicates, and revert are all present. Each of the eight official host plugins injects the required-memory block at host-supported request boundaries. This layer holds standing truth that should outlive any one session. It does not answer which turns of the *current, unfinished* task still deserve space in the live window.

**An operator-facing memory management surface.** A separate Viewer provides human inspect-and-edit CRUD over those memory records. It is a management view over the memory layer—not SessionLog, and not a projection engine. It is orthogonal to the scoring and packing this paper describes.

**Plugin hooks, unevenly distributed.** Claude / Grok / Copilot expose more hook points than Codex / Cline / OpenCode today. Injection and reaction points include `UserPromptSubmit`, `PreCompact`, and `PostCompact`, where the host exposes them. Hooks inject and react; they do **not** own the full host transcript and do **not** prevent the host from compacting. That asymmetry is why IMPL §6 treats plugin-only deployment as interim hardening rather than an end state.

### 1.3 Estimator benches are not savings proof

The multiturn estimator bench (`memory-bench-whitespace`) measures proxy token counts under controlled whitespace / message-shape assumptions. It is useful for relative plumbing checks. It is **not** provider-metered billing evidence. This whitepaper deliberately refuses to cite estimator deltas as token-savings proof. Any later cost claim must use provider-reported usage or an equivalent metered path.

### 1.4 Design gap

Memory alone stores facts. Compaction alone discards history. Neither continuously answers: *given the current unfinished work, which past turns still deserve tokens in the live window—and which can be stubbed until needed again?*

---

## 2. Background: MCP memory vs SessionLog vs UI

| Layer | Durability | Granularity | Primary job | Compaction relationship |
| --- | --- | --- | --- | --- |
| MCP Memory (002) | Cross-session | Facts / notes / promoted items | Standing prefs, constraints, machine truths | Injected into prompts; shrinks need for long projection |
| SessionLog (extend existing `sessionlog_*`) | Session-durable, append-only | Full turns (request + intermediates + response) | Truth store for reweight / re-admit | Survives host compact; projection is derived |
| Manager Memory UI | Operator-facing CRUD | Memory records | Human inspect / edit | Orthogonal to projection algorithm |
| Host transcript | Ephemeral / host-owned | Whatever the IDE keeps | Live chat UX | Subject to auto-compact; not authoritative |

**Principle:** SessionLog is the append-only ledger—and append-only is an enforced property of the existing durable store, not an aspiration of this design (§6). MCP already ships **`sessionlog_*` APIs** for session/turn logging; this design **extends** that ledger with weight / pin / projection metadata—it is not a wholly greenfield store. Memory is the cross-session digest. Projection is the ephemeral, budgeted view assembled for the model. UI is for humans.

---

## 3. Design principles

1. **Never permanently delete turns from SessionLog.** Omission from the projection is not deletion. The durable store already enforces this, so the invariant is inherited rather than built (§6).
2. **Weight relative to current work progress**, not raw self-information, recency alone, or “interestingness.”
3. **Hard token budget** on the live projection. Soft targets without enforcement invite drift.
4. **Re-admit is first-class.** If a quiet constraint suddenly matters, the scorer must be allowed to pull the full turn back from SessionLog.
5. **Pins beat scores, and they beat stubbing.** Explicit operator, system, or acceptance pins stay expanded until unpinned. An `invalidates` edge or an introduced-harm verdict attaches a marker beside the expanded pin. It does not stub or omit the pin (§6.2). Unpinned targets of those events are marked stubs.
6. **Memory bridges the long tail.** Standing facts leave the projection via the MCP memory write path (`memory_remember`) rather than forever occupying mid-tier summary slots.
7. **Own assembly when possible.** If the orchestrator builds the prompt, host auto-compaction becomes optional for *model-facing* context—not for host UI chrome.
8. **No fake metrics.** Do not claim savings from estimators; measure continuation quality and budget adherence.

---

## 4. Architecture

### 4.1 Components

```
Operator / Host UX
       |
       v
+------------------+     append      +---------------+
|  AgentInvoker    | --------------->| SessionLog    |
|  (CLI / hooks)   |                 | (append-only) |
+--------+---------+                 +------+--------+
         |                                  |
         | projection                       | turns + metadata
         v                                  v
+------------------+  weights/pins   +------------------+
| ContextProjector | <---------------| TurnValueScorer  |
+--------+---------+                 +------------------+
         |
         | standing facts
         v
+------------------+
| MemoryBridge     | ----> MCP memory_remember / memory_recall / REQUIRED MEMORIES
+------------------+
         |
         v
+------------------+
| CompactFallback  |  (PreCompact hooks when host still owns transcript)
+------------------+
```

- **SessionLog:** Durable transcript built on existing MCP `sessionlog_*` APIs, extended with weight / pin / projection metadata. Stores full turn payloads plus that metadata.
- **TurnValueScorer:** Assigns relative weights (and optional rationales) for advancing *current* work.
- **ContextProjector:** Builds `contextProjection` under budget: expand / summarize / omit.
- **MemoryBridge:** Promotes durable facts out of the turn stream into MCP memory.
- **AgentInvoker:** Invokes frontier agents (prefer sessionless CLI oneshot); otherwise injects into host.
- **CompactFallback:** When stuck inside a host that will compact anyway, use PreCompact to dump / reweight / inject a projection stub.

### 4.2 Core data fields

Per turn (illustrative schema; Phase 1 formalizes):

| Field | Intent |
| --- | --- |
| `turnId` | Stable identity in SessionLog |
| `sessionId` | Session scope |
| `payload` | Full request + intermediates + response (or content-addressed blob ref) |
| `weight` | Independent scalar relevance score ∈ [0, 1] for unfinished work. Not a normalized distribution: weights do not sum to 1 across a session, so `Δweight` hysteresis thresholds (§6.1) and the density packing in §6 compare absolute values. Not the progress count; the only progress quantity that may revise this score is ADD §6.1 |
| `pin` | Boolean / pin class (operator, system, acceptance) |
| `projectionGeneration` | Monotonic id of last scorer/projector pass |
| `projectionState` | `expanded` \| `summarized` \| `omitted`. A "stub" is the `summaryText` carried by a `summarized` turn; there is no separate `stubbed` state. `invalidated` and `introduced-harm` are annotations on `expanded` or `summarized`, not extra states (§6.2) |
| `summaryText` | Mid-tier stub when not expanded |
| `tokenEstimate` | Local estimate of the turn's **own rendered payload**, for *budgeting only* (not billing proof). Two distinctions matter. It is not a provider-reported usage count for the call that produced the turn, which includes prompt and history. And it is **not one fixed number per turn**: the same turn renders to different sizes under a `messages[]` assembly than under a prompt blob, and under different tokenizers, so the estimate is per renderer and target model (§10 risk 8; IMPL §3.4) |
| `reAdmitCount` | How often this turn was pulled back from omit |
| `lastReAdmitGeneration` | `projectionGeneration` at which this turn was last re-admitted; the field the re-admit cooldown in §6.1 reads |

Projection object:

| Field | Intent |
| --- | --- |
| `budgetTokens` | Hard cap for model-facing assembly |
| `generation` | Matches scorer pass |
| `segments[]` | Ordered `expanded` / `summarized` segment markers with `turnId` refs |
| `omittedTurnIds[]` | Recoverable via SessionLog |
| `standingMemoryIds[]` | Injected via MemoryBridge |

---

## 5. Scoring model

### 5.1 What “relative weight” means

Weight answers: **If we are trying to finish the current unit of work, how much does this turn still matter in the live window?**

That question is relevance. It is not a progress count. This paper does not define a second numeric "progress." When the retrospective-linking addendum is applied, the only progress quantity that may revise `weight` is the authoritative plan-level series in ADD §6.1, and only by the revision rules in ADD §6.5. A progress floor applies only while ADD §6.5 still calls the contribution active. A finished contribution keeps its audit credit and loses the floor on the next snapshot, without a verdict that calls the credit false. Pending intent, an actor's claim, and the "progress narrative" label below are not that quantity and MUST NOT be treated as inputs to packing.

Examples of high weight:

- Current plan and acceptance criteria still open.
- Unresolved blocker with exact error text needed for the next fix.
- Operator constraint that has not been externalized to memory yet.
- Latest tool result that the next action depends on.

Examples of low weight (omit or stub):

- Explored-and-abandoned dead ends whose conclusions are captured elsewhere.
- Verbose successful tool dumps superseded by a later concise status.
- Social / meta chatter with no residual task dependency.

Mid weight: progress narrative worth a summary (“tried A, failed for reason R, switched to B”).

### 5.2 Scorer v1 (rules) vs v2 (judge)

**v1 — deterministic / heuristic rules (Phase 4 start):**

- Boost: pinned, open TODOs, files still in active edit set, last N turns, turns cited by current plan, turns containing unmet constraints.
- Penalize: turns fully superseded by a later turn’s explicit “supersedes turnId”, pure ack turns, huge tool payloads already summarized into memory.
- Hysteresis applies only to discretionary score-driven `projectionState` changes. Require `|Δweight|` of at least the §6.1 threshold before such a change. Hysteresis MUST NOT block a mandatory override (pin, budget demotion, invalidation, introduced-harm).

**v2 — LLM / learned judge:**

- Periodic or per-turn judge prompt: given goal + recent projection + candidate turn stubs, re-score candidate turns under budget. The judge emits one `∈ [0, 1]` score per turn (see §4.2). Those scores are not a normalized distribution: normalization would make one turn's score depend on unrelated turns and break `Δweight` hysteresis. Independent outputs do not mean the judge ignores the other stubs in the same prompt. Noticing a relation among those inputs is contextual inference. Writing the scalar does not persist that relation and does not co-admit the turns. Persistence and co-admission are ADD §2 and ADD §3.1.
- Still write results back to SessionLog; never delete.
- Keep the §6.1 split: hysteresis on discretionary score changes, mandatory overrides above them.

### 5.3 Reweight triggers

Preferred: **after each turn**. Acceptable: at compact gates / budget pressure / operator “reproject” command. Continuous reweighting is the intended mechanism that makes host auto-compaction *unnecessary for model-facing context* when assembly is owned externally **(design intent until Phase 2 demonstrates it)**.

---

## 6. Projection algorithm

Informal algorithm under hard budget `B`. The authoritative order is §6.1. This list is that procedure with the packing rule written out. Link admission and introduced-harm (steps 3 and 4 of §6.1) run before the density packing in step 5 here.

1. Load SessionLog turns for `sessionId` plus standing memories from MemoryBridge.
2. **Charge the fixed prefix against the budget first.** Compute `B_turns = B - tokens(system prefix) - tokens(standing memories)`, where the standing memories are those listed in `standingMemoryIds[]`. Standing memories are model-facing and therefore consume `budgetTokens` exactly as turns do; IMPL §6's stable-prefix guidance makes them cheap to cache, not free to send. Fail closed: if `B_turns <= 0`, surface error / demote or consolidate standing memories via `consolidate` / ask operator—do not silently overflow `B`.
3. Mark active pins `expanded` (§6.2). Fail closed: if pins alone, including their mandatory markers, exceed `B_turns`, surface error, force memory promotion, or ask the operator. Do not silently drop pins, and do not treat hysteresis as a reason to drop them.
4. Refresh weights. The refresh may change `weight`. It MUST NOT by itself change `projectionState`. State changes follow §6.1.
5. Allocate the remaining `B_turns` by **value density**, not raw weight, over turns not already forced by a mandatory override:
   - Compute `density = weight / max(tokenEstimate, 1)` for each non-pinned turn.
   - Sort by `density` descending (stable tie-break: `weight` descending, then recency, then `turnId`).
   - **Demote** unpinned turns whose payloads no longer fit, lowest density first. A demotion caused by a new pin, a mandatory marker, a joint link endpoint, or the live result is a budget demotion even when weight is unchanged (§6.1).
   - **Do not expand** an omitted or summarized turn only because residual capacity grew (a pin was removed, a neighbor was demoted, or a render shrank). Expansion requires a discretionary score change that clears hysteresis and cooldown, or a mandatory link or harm rule that already named that turn. Unused residual budget stays unused.
   - For a turn already authorized to be summarized, attach `summaryText`, charged at the stub's own `tokenEstimate`.
   - Omit the rest; record `omittedTurnIds`. Introduced-harm and invalidation markers are not omitted to free that room (§6.3).

   **Why density and not raw weight:** sorting by weight alone lets a single high-weight, high-token payload (e.g. a 40k-token tool dump scored 0.9) consume the budget ahead of many smaller turns of nearly equal weight—including exactly the low-verbosity constraint turns §10.2 exists to protect. Density packing is the standard greedy approximation for a hard-capped budget; raw weight survives as the tie-break so that equal-cost turns still order by task relevance.
6. Emit `contextProjection` with `generation++`.
7. On a later pass, re-admit an omitted turn only through §6.1. A discretionary re-admit requires the weight rise to clear hysteresis and requires cooldown to allow it. A mandatory override does not wait on either. Demotion of some other turn, when the budget requires it, is a budget demotion under §6.1 even if that other turn's weight did not change.

### 6.1 Ordered transition and re-admit cooldown

One procedure produces the next `projectionState`. It separates **mandatory overrides** from **discretionary score changes**. This section is the cooldown contract previously cited as §10.1. There is no §10.1. References to a cooldown spec resolve here.

**Generation snapshot.** Generation G freezes a snapshot before judgments, admission, or packing. The frozen inputs are: endpoint seal sequences, the tombstone set for every turn id in the turn set, effective edge transitions, scorer weights, pins, the authoritative progress vector (ADD §6.1), active warning ids (ADD §7.4), the turn set, and the rendered payload versions this call would send. Every decision in G reads that snapshot. Appends during G (new transitions, spills, weight writes, reseals, superseding verdicts, tombstones) are durable immediately.

**Commit point.** G is abortable until transport handoff. The final read of tombstones, seals, evidence watermarks, and superseding verdicts, and the handoff of the assembled bytes, are one critical section under `sendFence`. A mutation of those inputs takes the same fence. It cannot commit after the final read and before handoff. Exactly one of handoff or mutating commit wins. If the mutation wins, do not hand off; abort G and retry on a new snapshot. If handoff wins, the mutation has not committed and is visible at the next generation. After handoff, the generation is published and an in-flight call is not rewritten. Each tool-loop model call is its own generation and its own fence. Packing MUST NOT mix edges, weights, or deletion visibility from after the fenced epoch with payloads from before it.

**Required traces.** Tombstone-before-send: both endpoints are live at freeze, one is tombstoned before the fence, the handoff is refused, and the linked content is not issued. Reseal-before-send: same refusal. Concurrent supersession: a verdict that wins the fence aborts the handoff; a verdict that loses the fence waits for the next generation. Interrupted generation: the process stops before handoff, nothing is published, and a cursor already appended (ADD §5.4) remains. After-read-before-handoff: the test injects a tombstone after the final read is attempted and before handoff. The published call MUST NOT contain that tombstoned endpoint. Either the tombstone waits until after handoff, or the handoff is cancelled. A committed tombstone followed by a handoff of the pre-tombstone bytes is non-conforming.

Mandatory overrides MUST be applied when their trigger is true even if `weight` is unchanged, even if `|Δweight|` is below the hysteresis threshold, and even if the turn is inside re-admit cooldown. Discretionary score changes MUST NOT be applied unless hysteresis and cooldown both allow them.

**Hysteresis threshold.** A discretionary `projectionState` change requires `|Δweight| ≥ 0.05`, where `Δweight` is the current weight minus `weightAtLastStateTransition`. That field is the weight stored when `projectionState` last changed, including a mandatory change. It is not the previous generation's weight. The threshold is absolute, on the independent scalar in §4.2. Provisional, operator-adjustable, and MUST be fixed before Phase 4 can pass or fail (IMPL §8). The edge confidence band and its clock are ADD §5.3, not this threshold.

**Order inside a generation.** Weights are refreshed first (§6 step 4). Then, in this order:

1. **Prefix.** Compute `B_turns` (§6 step 2). If `B_turns ≤ 0`, fail closed. No turn-state change substitutes for that failure.
2. **Pins.** Every turn with an active pin is `expanded` (§6.2). If those pins, with mandatory markers, exceed `B_turns`, fail closed.
3. **Link admission and invalidation** (ADD §3.1), for edges that are still eligible on this snapshot (ADD §5.2). A suspended edge does not admit. This step runs before discretionary packing. The final assembly, not only the admit bit, must satisfy ADD §3.1. A target that is present only because of the edge is dropped with its source when the pair does not fit, except an `invalidates` pair, which fails closed under §6.2 rather than dropping a pin or a required marker. A target that is independently retained (pin, score, live result, or another edge in the closure) stays. If its required source does not fit, `invalidates` fails closed, and every other type attaches the dependency-gap marker instead of omitting the source in silence. An unpinned `invalidates` target already in the assembly is forced to the marked stub. A pinned target stays `expanded` and receives the invalidation marker. Depth, cycles, dedup, and cap charging are ADD §3.1. Depth truncation uses that same final rendering. A required source past `Dep` is not a silent omission and is not an audit-only `depth-capped` record. `invalidates` fails closed. Every other type attaches the dependency-gap marker on the retained endpoint, including an endpoint that entered the closure only as a source. Sending that endpoint with the further source absent and with no marker is non-conforming.
4. **Introduced-harm** (§6.2, ADD §7.4). An unpinned introduced-harm turn is forced to the marked stub. A pinned introduced-harm turn stays `expanded` and receives the harm marker.
5. **Discretionary score changes.** Apply a score-driven expand, summarize, or omit only when `|Δweight|` meets the threshold, cooldown allows the change, and the turn is not held by steps 2-4.
6. **Pack.** Spend residual budget by value density (§6 step 5) on turns that steps 2-4 have not already forced. Packing MAY demote an unpinned turn whose weight did not change when a new pin, a mandatory marker, a joint link endpoint, or a live result consumes budget. That demotion is a **budget demotion**, it is allowed during cooldown, and the trace records `budget-demotion`. Packing MUST NOT expand a turn solely because capacity became available. Expansion is a discretionary score change (step 5) or a mandatory admission already decided in steps 3-4. Residual budget that no authorized expansion uses stays unused.
7. **Emit** `contextProjection` and increment `generation`.

**Unchanged-weight demotion under new pins.** When a pin is added, or an active pin's rendered size grows, packing MUST demote unpinned content as far as required even if those weights are unchanged and even if those turns are inside cooldown. Pins are not the demotion candidates. If every unpinned turn has been omitted or stubbed and the assembly is still over `B`, fail closed.

**Invalidation during cooldown.** An `invalidates` admission, a reseal that voids a judgment (ADD §5.2), a tombstone, an evidence change that voids a judgment (ADD §5.2), or an introduced-harm verdict MUST still be applied while cooldown is open. Cooldown blocks only discretionary score-driven flips: omitting or summarizing a turn because its weight fell, or expanding it because its weight rose. It does not block steps 1-4 or a budget demotion.

**Re-admit cooldown.** When a turn is re-admitted to `expanded` by a discretionary score change, set `lastReAdmitGeneration` to the current `projectionGeneration`. Until `projectionGeneration ≥ lastReAdmitGeneration + C`, the projector MUST NOT omit or summarize that turn solely because its weight fell or failed hysteresis. Provisional **`C = 2`** generations. Operator-adjustable, and MUST be fixed before Phase 4 can pass or fail. Cooldown does not grow `B` and does not override steps 1-4 or a budget demotion. A budget demotion during cooldown MUST be recorded and MUST NOT reset `lastReAdmitGeneration`. A mandatory stub of an unpinned turn (invalidation or introduced-harm) ends the expanded re-admit early, MUST be recorded as a mandatory override, and is not a cooldown violation.

Edge admission uses the same constant `C` for discretionary confidence flips (ADD §5.3). Mandatory edge invalidation does not wait on `C`.

The projection trace names two layers. Evaluators of §9.1 read these fields and no other source.

Turn-layer causes, one primary cause per turn whose projection state or tail membership changed: `score` (discretionary), `pin`, `invalidation`, `introduced-harm`, `budget-demotion`, `warning-retirement`, `link-admit`, `link-withdraw`, `link-degrade`, `link-reconfirm`. Link admission, withdrawal, degradation, and reconfirmation are mandatory at the turn layer even when the edge-layer decision was a discretionary confidence flip.

Edge-layer causes, one primary cause per admission-bit change: `edge-score` (discretionary confidence), `edge-invalidation`, `edge-suspend`, `edge-reconfirm`, `edge-degrade`, `edge-budget`. `edge-score` maps to turn cause `link-admit` or `link-withdraw`. `edge-degrade` and `edge-budget` map to `link-degrade`. `edge-suspend` and `edge-invalidation` map to `link-withdraw`. `edge-reconfirm` maps to `link-reconfirm`.

Each changed turn stores `turnCause` and `originCause`. `turnCause` is the immediate turn-layer cause above. `originCause` is `edge-score` when that turn-layer cause came from a discretionary confidence flip. It is the mandatory edge cause when the turn-layer cause came from `edge-invalidation`, `edge-suspend`, `edge-reconfirm`, `edge-degrade`, or `edge-budget`. When the change did not come from an edge, `originCause` equals `turnCause`.

The state-flip numerator (metric 2) counts only a `projectionState` change whose `turnCause` is `score`. Its denominator stays the previous active set. A rendered rewrite that leaves `projectionState` and tail membership unchanged does not enter that numerator. The token-replacement numerator (metric 3) counts absent previous-tail tokens whose `originCause` is `score`, `edge-score`, or `summary-rewrite`. Mandatory safety withdrawals, pin changes, marker removal, and phase-boundary flushes stay outside that numerator and are reported in `mandatory-token-count`. The edge-flip numerator counts only `edge-score`. A trace that keeps `turnCause` and drops `originCause` is non-conforming. IMPL §8 Phase 1 records state causes and rendered-change events.

**Rendered-change events.** A state or membership cause is not the only trace event. Whenever a rendered segment in the variable tail differs from the previous published snapshot, including when `projectionState` and tail membership stay the same, the trace MUST record one `rendered-change` event for that turn. Discretionary rewriting of a retained summary without that event is prohibited. The event binds `previousRenderId` and `currentRenderId` (the renderer and tokenizer versions of the sent segment) and lists the removed tokens. Each removed span stores `{spanId, tokenCount, originCause}`. Evaluators MUST use those spans. They MUST NOT reuse an earlier state-transition cause to explain the new text. A missing event for a changed render is non-conforming, and those tokens MUST NOT be treated as mandatory or as outside the metric.

**Mixed spans in one retained turn.** One primary `turnCause` MUST NOT cover every removed token. Spans are partitioned. A discretionary summary rewrite is `originCause` `summary-rewrite` and counts in the metric 3 numerator. A mandatory marker that leaves the same segment is a separate span with the mandatory origin (`warning-retirement` or the marker's own cause) and counts only in `mandatory-token-count`. If any removed span is discretionary, the generation is not `mandatory-only`.

### 6.2 Pins, invalidation, and introduced-harm

Precedence, highest first. A lower rule MUST NOT override a higher one. ADD §3.1 and ADD §7.4 restate this rule; they do not define a second one.

1. **Budget fail-closed.** If the mandatory set exceeds `B`, do not drop a member of that set to make the invariant look true. Surface an error, force memory promotion, or ask the operator (§6 step 2 and step 3).
2. **Active pins stay expanded.** An operator, system, or acceptance pin stays `expanded` until unpinned. An `invalidates` edge that names the turn does not stub it. An introduced-harm verdict on the turn does not stub it, omit it, or suspend the pin. Principle 5 is this rule.
3. **Markers are mandatory on those pins.** A pinned `invalidates` target MUST carry the invalidation marker beside the expanded payload: the result is obsolete, and which edge says so. A pinned introduced-harm turn MUST carry the harm marker beside the expanded payload: the approach, that HV judged it introduced a regression, the checkable referent (ADD §7.1), and an explicit statement that the payload is not the recommended next action. The marker is charged against `B`. If the pin plus its marker does not fit, rule 1 applies.
4. **Unpinned mandatory stubs.** An unpinned `invalidates` target that is already in the assembly, and an unpinned introduced-harm turn, MUST be `summarized` with the same marker. They MUST NOT stay `expanded` and MUST NOT be silently `omitted` (omission would leave the failed approach available to retry with no marker).
5. **Discretionary scores sit underneath.** Weight changes, hysteresis, and cooldown (§6.1) do not move a turn that rules 1-4 have fixed.

`introduced-harm` and `invalidated` are annotations, not values of `projectionState`. A revealed regression (ADD §6.2) is not introduced-harm and does not force rule 3 or rule 4.

**Warning retirement.** Rules 3 and 4 and the mandatory set in §6.3 apply to markers whose `warningState` is `active` (ADD §7.4). `repaired` and plan retirement do not need a replacement warning. Consolidation and standing-memory transfer retire a marker only when this sent assembly contains an adequate replacement. Adequate means the sent text, not merely an id, states the prohibited approach or obsolete result, whether the meaning is harm or invalidation, a checkable referent, and that the content is not the next action. An id-only replacement is non-conforming and does not retire the individual marker. If that replacement leaves the prefix or loses any of the four fields, the individual marker is active again before send. The verdict row stays. Retirement does not unpin, and it does not relabel a true introduced-harm verdict as false. Active markers, including a reactivated marker, stay spill-protected. A history of resolved regressions that would not fit in `B` as raw markers MUST still be able to send after true retirements, while any marker that is still active remains in the mandatory set. Pins stay expanded.

### 6.3 Tool-loop budget

The budget invariant below applies at **every model invocation** a deployment issues, not only when `contextProjection` is first assembled. That includes every call in a tool loop: the initial prompt and every later call that carries a new tool result, a continuation, or a reprojected tail.

Live tool results and in-progress dialog are charged at their own `tokenEstimate` for the active renderer (IMPL §5.1). They are not free because the turn is unfinished.

When that live content cannot fit in `B`:

1. **Spill unmarked content first.** Omit or stub unpinned turns that are not active introduced-harm warnings and not active invalidation targets, lowest density first. A spill is a budget demotion (§6.1). It is allowed during cooldown. Record it. This step MUST NOT omit an active introduced-harm marker, an active invalidation marker, or an active dependency-gap marker (ADD §3.1). Retired markers are not in this protection.
2. **Shrink marked stubs to the marker.** An unpinned active introduced-harm or invalidation stub may lose residual payload facts. It MUST keep the marker: the approach or the obsolete result, the verdict or the edge, the checkable referent, and that the payload is not the next action. The active marker stays while the call is issued.
3. **Fail closed.** If the mandatory set still cannot fit, do not invoke the model. The mandatory set is the system prefix, standing memories, active pins with their markers, the live tool result or user turn this call exists to deliver, and every active introduced-harm, invalidation, or dependency-gap marker §6.2 and ADD §7.4 require in this assembly. A warning retired only by a replacement is active for this call when that replacement is absent or inadequate in this assembly. Reactivation happens before spill and before this check. Surface an error or ask the operator. Do not drop pins, do not truncate pinned content, do not omit those active markers, and do not send an over-budget prompt. Do not fail closed in order to keep a retired marker.
4. **Ownership.** The component that issues the call is the component that enforces this (risk 4). A deployment that claims the invariant MUST be able to measure each outgoing prompt and refuse the call. A CLI-owned inner loop whose intermediate model calls the orchestrator cannot measure is not a conforming claim of this invariant. The Phase 2 declaration (IMPL §8.1) records that gap as a fail for the budget gate, not as a silent pass.

**Budget invariant:** `tokens(system prefix) + tokens(standing memories) + tokens(expanded) + tokens(stubs) + tokens(mandatory markers) + tokens(live tool results on this call) ≤ B`, at every issued model invocation. The §9.1 budget-adherence metric and the IMPL §8 Phase 1 adherence report both measure this full sum, not the turn portion alone.

**Recoverability invariant:** For every omitted turn, SessionLog still has the bytes (or blob). Projection never claims deletion.

This is **an existing property of the durable store rather than an aspiration of this design**, but the enforcement is narrower than earlier revisions of this paper claimed, and the boundary has to be stated to be useful. On the **tracked save path**, deletes of durable records are converted to tombstones rather than executed, and an attempt to physically delete one throws rather than proceeding. Writes and field-level updates on that path are mirrored into an append-only audit ledger carrying before-and-after snapshots, which is what makes original writes and additive changes reconstitutable.

What that does **not** amount to is a database-level prohibition. The guard inspects tracked change entries, so set-based deletes and raw `DELETE` statements never reach it; today's recoverability rests on the service layer not issuing them, protected by a test asserting that the session-log service source contains no bulk-delete call, rather than on the database refusing them. A second limit: the ledger covers the tracked path, not the bulk tombstone and revival operations, which update rows directly — so deletion *events* go unrecorded even though those operations only flip deletion metadata and never touch stored content.

So the invariant the projection may safely inherit is: **content survives deletion on every path the current services use, enforced by convention and a guard test above the database, not by the schema.** That is enough for this design, which needs omitted turns to remain readable and never claims a complete deletion history. It is not enough to describe recoverability as structurally guaranteed, and a projection built on a future code path that bypasses the tracked save would not inherit it at all. IMPL §3.2.1 records both mechanisms.

Two consequences the design must respect:

- **Re-admit of an omitted turn is safe by construction** (step 7 above), because omitted turns are never deleted. Re-admit of a *tombstoned* turn is a different matter: recovery reads must bypass the default visibility filters, and no restore operation is exposed on the session-log tool surface today. Any design that depends on reviving deleted turns is depending on unbuilt work.
- **The enforcement is not visible in the data model's type definitions,** which makes it easy to audit incorrectly and conclude the ledger is mutable. An earlier review of this paper did exactly that, and a later one then overstated the correction in the opposite direction. Both errors came from reading one part of the write path and generalizing.

The specific classes, methods, guard behavior, and ledger contract that provide these guarantees are identified in the companion implementation note. This paper states the property; that note states the code.

**Budget adherence:** Local token estimates may drive packing. They are engineering controls, not published savings metrics.

---

## 7. Related work

Map each cited line of work to **shared ideas** vs **what it misses** relative to task-progress relative weights + re-admit from a full log.

### 7.1 Research

**MemGPT** ([arXiv:2310.08560](https://arxiv.org/abs/2310.08560))  
Shares: tiered memory; explicit main context vs recall store; paging.  
Misses: weights are not framed as *relative task-progress toward completion*; recursive / summary tiers remain lossy without a first-class re-admit contract tied to an append-only session ledger for operator agent work.

**Selective Context** ([arXiv:2310.06201](https://arxiv.org/abs/2310.06201))  
Shares: prune low-value tokens to fit budgets.  
Misses: self-information / efficiency pruning ≠ task-progress weights; no durable SessionLog re-admit loop for agent tooling traces.

**Recursively Summarizing** ([arXiv:2308.15022](https://arxiv.org/abs/2308.15022))  
Shares: rolling summary as memory under length pressure.  
Misses: summaries are one-way compressible history; weak story for re-expanding original turns when weights change.

**A-MEM** ([arXiv:2502.12110](https://arxiv.org/abs/2502.12110))  
Shares: agentic memory as linked notes; evolution of memory structure.  
Misses: note graphs are not the same as weighted projection over a full append-only turn log with hard live-context budgets.

**PACE — Predictive Adaptive Context Extraction** ([ACL 2026](https://aclanthology.org/2026.acl-long.1252/))  
Shares: next-step utility; multi-granularity context; adaptive pressure for long-horizon agents—treated here as the closest research cousin on *predictive relevance for the next action* **(author judgment, not a citation rank claim)**.  
Misses: not specified as an operator SessionLog + MemoryBridge + sessionless CLI deployment; re-admit semantics should be made explicit for IDE/CLI product constraints.

**HiGMem** ([arXiv:2604.18349](https://arxiv.org/abs/2604.18349)) — *accepted to Findings of ACL 2026; camera-ready, code public*  
Shares: event summaries with expand-to-underlying-turns; hierarchical recoverability. Strongest empirical result among the memory-system citations here: best F1 on four of five LoCoMo10 question categories, and adversarial F1 from 0.54 to 0.78 over A-MEM while retrieving an order of magnitude fewer turns.  
Misses: retrieval-for-QA framing more than continuous *task-progress* reweight under an always-on agent budget; less emphasis on escaping host compact via outer oneshot invocation.

**G-Long** ([arXiv:2606.13115](https://arxiv.org/abs/2606.13115))  
Shares: graph structure + attention-aware importance scoring for long-term dialogue memory.  
Misses: importance from summarizer attention ≠ relative weight for unfinished engineering work; no SessionLog/CLI assembly story.

**Adaptive context compression** ([arXiv:2603.29193](https://arxiv.org/abs/2603.29193))  
**Provenance caveat:** two-author v1 preprint, no accepted venue, no comments/page-count field, and cross-listed under cs.CV rather than cs.CL. Weight it accordingly against the peer-reviewed entries above; it is retained for idea coverage (banded compression under a dynamic budget), not as evidence.  
Shares: turn importance scores; retain / summarize / drop bands; dynamic budget.  
Misses: importance mix (similarity, recency, dependency) is not explicitly *progress-toward-completion*; durable re-admit + MCP memory bridge + host-escape runtime need productization beyond the paper’s conversational benchmarks.

### 7.2 Industry

**Haystack `SummarizationCompactor`** ([docs](https://docs.haystack.deepset.ai/docs/next/summarization-compactor))  
Shares: progressive summarization under compaction hooks; preserve system + recent steps; replace older turns with summaries.  
Misses: one-way compact pressure; no weighted re-admit from full log as a primary loop; not task-progress relative mass with hysteresis.

**PraisonAI intelligent / context compaction** ([docs](https://praison.ai/docs/features/intelligent-conversation-compaction))  
Shares: structured summaries (topic, goals, decisions, actions); preserve recent; tool-call pairing.  
Misses: same core gap—compaction as reduction, not continuous projection with re-admit and SessionLog authority.

---

## 8. Comparison table

| Approach | Durable full turns | Task-progress weights | Re-admit | Hard budget projection | Cross-session memory bridge | Escapes host compact |
| --- | --- | --- | --- | --- | --- | --- |
| Host auto-compact only | No | No | No | Host-defined | No | N/A (is the problem) |
| Plugin hooks + MCP memory | Partial | No | Weak | No | Yes (002) | No |
| MemGPT-style paging | Partial | Indirect | Via recall | Soft | Yes | App-dependent |
| Rolling recursive summary | No (lossy) | No | No | Soft | Optional | App-dependent |
| Selective Context | N/A | Self-info ≠ progress | No | Yes | No | App-dependent |
| PACE | Design-dependent | Next-step utility (close) | Glimpse-like | Adaptive | Design-dependent | App-dependent |
| HiGMem (Findings of ACL 2026) | Turn layer yes | Event/retrieval | Expand turns | Retrieval budget | Profile optional | App-dependent |
| G-Long | Graph triplets | Attention importance | Graph expand | Retrieval | No SessionLog | App-dependent |
| Adaptive compression (2603.29193 — *v1 preprint, no venue*) | Usually in-memory | Relevance/recency/deps | Limited | Yes | Limited | App-dependent |
| Haystack / Praison compactors | Often replaced | Structured summary heuristics | No | Threshold-based | Optional | If you own agent runtime |
| **This design (WCP)** | **SessionLog yes (design)** | **Explicit (design)** | **Yes (design)** | **Yes (design)** | **MCP MemoryBridge (design)** | **Option C oneshot (design target; not yet measured)** |

---

## 9. Evaluation plan

Measure what matters. Do **not** lead with whitespace estimator deltas.

### 9.1 Primary success metrics

1. **Post-reproject (or post-compact fallback) task continuation** without operator re-paste of constraints, paths, or acceptance criteria.
2. **State-flip rate.** Denominator: turns that were `expanded` or `summarized` on the previous published snapshot (the previous active set), not every turn loaded for the session. If the denominator is 0, do not compute a rate. Record `empty-denominator`. The gate passes for this metric. Numerator: turns in that set whose `projectionState` changed for discretionary turn cause `score` (§6.1). Every other turn-layer cause is mandatory, including `pin`, `invalidation`, `introduced-harm`, `budget-demotion`, `warning-retirement`, `link-admit`, `link-withdraw`, `link-degrade`, and `link-reconfirm`. Those are counted in `mandatory-transition-count` and are outside the numerator. An edge-layer `edge-score` decision is still mandatory at the turn layer once it is recorded as `link-admit` or `link-withdraw`. A generation whose causes are only mandatory is `mandatory-only` and is outside the mean. Stable omissions are outside the denominator. Window: one published generation. Session figure: the mean of counted per-generation rates. A generation is a phase-boundary flush only when its snapshot records `phaseBoundary: true` and a `phaseId`. Evaluators MUST exclude that generation and MUST NOT exclude any other generation by judgment. Provisional bar: **≤ 0.10** when the denominator is at least 1. Operator-adjustable, and the number MUST be fixed before Phase 4 can pass or fail (IMPL §8). Two evaluators of one trace use these fields and no others.
3. **Active-context token replacement.** Separate from the flip rate. A pass can replace the entire variable tail while flipping few states, or flip many tiny turns while leaving the tokens in place. Window: one published generation. Denominator: tokens in the previous generation's variable tail (expanded payloads, stubs, and link-admitted segments). If that token count is 0, do not compute a rate. Record `empty-tail`. The gate passes for this metric. The stable prefix and standing memories are outside both sides, so a large cached prefix cannot dilute the ratio. Numerator: tokens of that previous tail that are absent from the new tail and whose rendered-change span `originCause` is `score`, `edge-score`, or `summary-rewrite` (§6.1). This includes tokens inside a turn that stays in the tail with the same `projectionState`. The denominator is unchanged: previous variable-tail tokens. Tokens whose `originCause` is a mandatory safety withdrawal, a pin change, or another non-discretionary cause stay outside the numerator and are reported in `mandatory-token-count`. A generation is `mandatory-only` for this metric only when no absent previous-tail token has `originCause` `score`, `edge-score`, or `summary-rewrite`. Treating the generation as `mandatory-only` because `turnCause` is `link-withdraw` while `originCause` is `edge-score` is non-conforming. Exclude a generation from the mean and from the maximum when it is `mandatory-only` or when `phaseBoundary` is true with a `phaseId`. A mandatory pin or marker insertion that is the only cause does not fail the bar. Session figures: the per-generation mean and the per-generation maximum of the counted generations. Provisional bar: **≤ 0.25** on each counted generation. Operator-adjustable, and the number MUST be fixed before Phase 4 can pass or fail. Both bars are required. Passing the flip rate does not pass this metric. Edge-flip counting is ADD §9 and uses the same empty-denominator, mandatory-cause, and phase-boundary rules. The projector sets `phaseBoundary` true if and only if the plan phase id on this snapshot differs from the plan phase id on the previous published snapshot, and the trace stores both ids. Evaluators exclude that generation if and only if the flag is true. If every generation in the session is excluded, do not compute a session mean. Record `empty-aggregate`. The stability gate passes for that metric. A missing mean is not a fail and is not a silent skip. The same `empty-aggregate` rule applies to metric 2 and to the edge-flip rate. Mixed-cause expected results are stated in ADD §9 and are normative for these metrics.
4. **Projection token budget adherence:** fraction of issued model invocations, including tool-loop continuations (§6.3), whose measured assembly is ≤ `budgetTokens` (local estimator OK for this engineering metric only). An invocation that was refused by fail-closed is recorded as a refusal, not as adherence.
5. **Re-admit usefulness:** when a turn is re-admitted, did the subsequent action correctly use it (human or rubric check)? Report score-admitted and link-admitted slices separately (ADD §9).
6. **Pin integrity:** zero silent pin drops, including under invalidation, introduced-harm, budget pressure, and tool-loop spill (§6.2, §6.3).

### 9.2 Explicit non-metrics (for now)

- Estimator-only “token savings %” as a published claim.
- Provider bill deltas inferred from whitespace benches.
- Vanity context-window fill charts without task outcomes.

### 9.3 Protocol sketch

- Record real operator sessions into SessionLog (offline corpus).
- Phase 1 simulator: replay scorer + projector; operators score blind continuations (full context vs projection vs host-compact summary).
- Phase 2 online spike: one CLI oneshot path; log continuation failures and re-paste events.
- Compare against PreCompact-only fallback on the same tasks.

---

## 10. Risks and open questions

1. **Thrashing:** scores oscillate; projection churns; prompt cache dies. Mitigation: discretionary hysteresis and re-admit cooldown (§6.1, enforced against `lastReAdmitGeneration`), which do not block mandatory overrides. Stability is the pair of metrics in §9.1 (state-flip rate and active-context token replacement), not the flip rate alone.
2. **Bad scorer drops quiet constraints:** low-verbosity “never X” turns get omitted. Mitigation: constraint detector → pin or MemoryBridge; fail tests in eval protocol.
3. **CLI Terms of Service / fair use:** sessionless high-frequency oneshots may trip rate or ToS limits. Mitigation: backoff, batching, respect vendor rules; do not pretend subscriptions are uncapped APIs.
4. **Tool-loop ownership:** split between orchestrator and CLI causes missing traces in SessionLog, and a CLI-owned inner loop can issue model calls the orchestrator cannot budget. Mitigation: one declared owner per spike; log everything observable; claim the §6.3 invariant only for calls the owner can measure and refuse.
5. **Host UI compact still happens:** operators may think context is gone when only the IDE view compacted. Mitigation: UX clarity that SessionLog + projection are authoritative under the outer-orchestrator deployment (IMPL §6, Option C).
6. **Re-score cost every turn:** LLM judge v2 can be expensive. Mitigation: v1 rules by default; judge on schedule or on budget pressure.
7. **Evaluation honesty:** easy to overfit summaries that *look* complete. Mitigation: adversarial hidden constraints in replay tests.
8. **Prompt-blob vs messages[] fidelity:** some tool schemas and multimodal parts may not round-trip cleanly through CLI stdin prompts.

---

## 11. Appendix: glossary

| Term | Meaning |
| --- | --- |
| **SessionLog** | Append-only durable transcript of turns for a session; extends MCP `sessionlog_*` with weight/pin/projection metadata |
| **Turn** | Full request + intermediates + response unit |
| **Weight** | Independent scalar ∈ [0, 1] for whether a turn still matters to unfinished work; not a normalized distribution and not the progress count (see §4.2, §5.1, ADD §6.1) |
| **Pin** | Hard retain-in-projection marker |
| **contextProjection** | Budgeted model-facing assembly derived from SessionLog + memory |
| **Re-admit** | Restoring an omitted turn into the projection from SessionLog |
| **MemoryBridge** | Path between turn stream and MCP durable memory. Binds to the shipped tools — `memory_remember` for provenance-carrying writes, `memory_recall` for meaning-ranked reads, with `memory_explore` / `memory_consolidate` / `memory_promote` alongside. The bare verb forms used in this paper's prose are shorthand for those tool names, not separate tools; `memory_add` is the thinner compatibility surface, not the canonical write path |
| **Sessionless oneshot** | Fresh CLI invocation without vendor session resume |
| **Host auto-compaction** | IDE/runtime irreversible (to the model) context reduction |
| **Hysteresis** | Resistance to discretionary score-driven `projectionState` flips (§6.1). Does not block pins, budget demotion, invalidation, or introduced-harm |
| **HV** | Hostile Validation (accuracy + completeness gate; not “high-visibility / high-value”) |

---

## 12. References

1. Packer, C., et al. *MemGPT: Towards LLMs as Operating Systems.* arXiv:2310.08560. https://arxiv.org/abs/2310.08560  
2. Li, Y., et al. *Compressing Context to Enhance Inference Efficiency of Large Language Models* (Selective Context). arXiv:2310.06201. https://arxiv.org/abs/2310.06201  
3. Wang, Q., et al. *Recursively Summarizing Enables Long-Term Dialogue Memory in Large Language Models.* arXiv:2308.15022. https://arxiv.org/abs/2308.15022  
4. Xu, W., et al. *A-MEM: Agentic Memory for LLM Agents.* arXiv:2502.12110. https://arxiv.org/abs/2502.12110  
5. Wei, L., et al. *PACE: Predictive Adaptive Context Extraction for Long-Horizon LLM Agents.* In *Proceedings of the 64th Annual Meeting of the Association for Computational Linguistics (Volume 1: Long Papers)*, pp. 27184–27199. ACL, 2026. https://aclanthology.org/2026.acl-long.1252/  
6. Cao, S., He, J., Tan, F. *HiGMem: A Hierarchical and LLM-Guided Memory System for Long-Term Conversational Agents.* Findings of the Association for Computational Linguistics: ACL 2026. arXiv:2604.18349. https://arxiv.org/abs/2604.18349  
7. Choi, M., Jang, Y., Youn, S., Ko, Y. *G-Long: Graph-Enhanced Memory Management for Efficient Long-Term Dialogue Agents.* arXiv:2606.13115. https://arxiv.org/abs/2606.13115  
8. Fofadiya, P., Tiwari, S. *Developing Adaptive Context Compression Techniques for Large Language Models (LLMs) in Long-Running Interactions.* arXiv:2603.29193 (v1 preprint; no accepted venue). https://arxiv.org/abs/2603.29193  
9. deepset Haystack. *SummarizationCompactor* documentation. https://docs.haystack.deepset.ai/docs/next/summarization-compactor  
10. PraisonAI. *Intelligent Conversation Compaction* documentation. https://praison.ai/docs/features/intelligent-conversation-compaction  
11. Microsoft Agent Framework. *Chat history storage patterns* (client-managed `AgentSession` / `ChatHistoryProvider`). https://devblogs.microsoft.com/agent-framework/chat-history-storage-patterns-in-microsoft-agent-framework/

---

## Document control

| Version | Date (CT) | Notes |
| --- | --- | --- |
| v0.1 | 2026-09-20 | Initial whitepaper for operator review tomorrow |
| v0.1.1 | 2026-09-20 | Round-1 self-eval: HV = Hostile Validation; SessionLog extends `sessionlog_*`; hook matrix honesty; Immediate next actions; Phase 1 exit criteria concreteness; memory verb / alias clarity |
| v0.1.2 | 2026-09-20 | Round-2 hostile pass: remove soft overclaims; label assumptions; Phase 2 spike acceptance checklist; Out of scope for v0.1.x; Recommendations↔Roadmap 1:1 |
| v0.1.3 | 2026-09-20 | Round-3 external review: correct MAF API attribution (`ChatMessageStore` → `AgentSession` / `ChatHistoryProvider`); §6 allocates by value density instead of raw weight; retire dead `stubbed` state and add `lastReAdmitGeneration` so the re-admit cooldown (now §6.1) is implementable before Phase 1 freezes the schema |
| v0.1.4 | 2026-09-20 | Round-3 follow-up: charge system prefix + standing memories against `B` with a stated budget invariant; scope the IMPL §8.1 tool-trace gate to the declared tool owner and add a declaration gate; fix the thrash threshold at ≤ 0.10 flips/turn/pass; resolve `weight` as an independent scalar (not normalized mass); record HiGMem's Findings-of-ACL-2026 venue and 2603.29193's preprint provenance; complete refs 5, 7, 8; repair the §4.1 diagram alignment |
| v0.1.5 | 2026-09-20 | Separate design from implementation, and stop describing shipped work by ticket number. §3 and §6 now state recoverability as an *existing enforced property* of the durable store—tombstones instead of executed deletes, physical deletion rejected, mutations mirrored to an append-only snapshot ledger—so projection inherits the invariant rather than building it; the classes and methods providing it are delegated to the companion implementation note instead of named here. Records the two design-relevant consequences: re-admit of an *omitted* turn is safe by construction, while re-admit of a *tombstoned* turn depends on unbuilt restore capability. §1.2 describes the shipped capabilities directly instead of citing internal work-item identifiers (`MCP-MEMORY-002`, `PLAN-MANAGER-MEMORY-UI-001`), which meant nothing to an external reader |
| v0.1.6 | 2026-09-20 | **Split design from proposed implementation.** Runtime deployment options, immediate next actions, recommendations, and the roadmap with its phase gates moved to `proposed-implementation-weighted-context-projection-v0.1.md`, together with the code-grounded findings that were previously a third document; remaining sections renumbered (old §8–§11 → §7–§10, old §15–§16 → §11–§12). Executive summary now names the memory **tools** (`memory_remember` / `memory_recall` / `memory_explore` / `memory_consolidate` / `memory_promote`) rather than describing `memory_remember` as an alias of a bare `remember` verb, which had the relationship backwards |
| v0.1.7 | 2026-09-20 | Round-4 external review. Narrows the §6 recoverability paragraph: the audit ledger covers the tracked save path, not the bulk tombstone and revival operations, so content recoverability holds but a complete deletion history is not claimable. Defines `tokenEstimate` in §4.2 as an estimate of the turn's own rendered payload, explicitly not a provider usage count for the producing call |
| v0.1.8 | 2026-09-20 | Round-5 external review. Fixes the two remaining inverted memory-alias definitions that the v0.1.6 changelog wrongly implied were already handled — Principle 6 and the MemoryBridge glossary entry still presented a bare `remember` verb as canonical with `memory_remember` as its alias; both now bind to the shipped `memory_*` tools and mark the short verb forms as prose shorthand. Redefines `tokenEstimate` in §4.2 as per-renderer and per-model rather than one fixed number per turn, since a turn renders to different sizes under a `messages[]` assembly than under a prompt blob (§10 risk 8) |
| v0.1.9 | 2026-09-20 | Round-5 follow-up. §6's recoverability paragraph narrowed a second time: physical-delete blocking is a guard over tracked change-tracker entries on the `SaveChanges` path, which set-based and raw-SQL deletes bypass, with no database-level trigger or constraint behind it. The inheritable invariant is now stated as content surviving deletion on the paths the current services use, upheld above the database by that guard plus a source-text test, rather than physical deletion being "rejected outright" as a structural property. IMPL §3.2.2 records the mechanism |
| v0.1.15 | 2026-09-25 | Astra re-review (round 6). §6.1 requires a `rendered-change` event when a sent segment changes even if `projectionState` and tail membership stay the same. Each removed span names `previousRenderId`, `currentRenderId`, and `originCause`. Metric 3 counts `summary-rewrite`. Metric 2 still counts only a state change whose cause is `score`. The finding map is ADD §12 round 6, claimed remediation pending an independent Astra review, not an AGREE |
| v0.1.14 | 2026-09-25 | Astra re-review (round 5). §6.1 stores `turnCause` and `originCause`. Metric 3 counts absent tail tokens whose origin is `score` or `edge-score`. Metric 2 still counts only `turnCause` `score` against the previous active set. The finding map is ADD §12 round 5, claimed remediation pending an independent Astra review, not an AGREE |
| v0.1.13 | 2026-09-25 | Astra re-review (round 4). §6.1 `sendFence` covers final validation and transport handoff, and depth truncation uses the dependency-gap or fail-closed rendering. §6.1 names turn-layer and edge-layer causes, including link admission, withdrawal, degradation, and reconfirmation. §6.2 and §6.3 require an adequate replacement before warning retirement and reactivate the marker if that replacement leaves the assembly. §9.1 maps those causes into the stability numerators, records `empty-aggregate` when no generation is counted, and sets `phaseBoundary` from the plan phase id. The finding map is ADD §12 round 4, claimed remediation rather than a verified closure |
| v0.1.12 | 2026-09-25 | Astra re-review (round 3). §5.1 floors expire when the contribution is no longer active. §5.2 independent scores are not an inference ban. §6.1 snapshot includes tombstones, send is the commit point, and `Δweight` uses `weightAtLastStateTransition`. §6.1 step 3 follows ADD §3.1 final-assembly rules, including the dependency-gap marker. §6.2 and §6.3 keep only active warnings in the mandatory set. §9.1 states empty denominators, mandatory-cause exclusion, and the `phaseBoundary` flag. The finding map is ADD §12 round 3 |
| v0.1.11 | 2026-09-25 | Astra re-review (round 2). P1-10: §6.3 spills unmarked unpinned content first, shrinks harm and invalidation stubs to the marker, and fails closed with those markers in the mandatory set. P1-11: §6.1 step 3 joint admission; the required endpoints are co-present or the edge admits neither, and `invalidates` fails closed rather than dropping a pin or marker (ADD §3.1). P1-12: §6.1 generation snapshot freezes seals, edges, weights, pins, the progress vector, and live content before judgments; each tool-loop call is its own generation. The capacity-only expansion ban is a packing subdefect, not receipt id P2-06. The finding-to-section map for this round is ADD §12. Round 3 supersedes the partial closures |
| v0.1.10 | 2026-09-25 | Hostile review remediation (full-document Codex review). P1-01: §6.1 ordered transition, mandatory overrides versus discretionary scores, link admission before packing, unchanged-weight budget demotion, invalidation during cooldown. P1-02: §6.2 pin precedence over introduced-harm and invalidation stubbing; markers on expanded pins; unpinned turns are marked stubs; no fourth `projectionState`. P1-04: §5.1 weight is relevance; only ADD §6.1 progress may revise it. P1-09: §6.3 budget at every issued model call, spill then fail-closed. P2-02: §9.1 state-flip denominator is the previous active set; active-context token replacement is a separate bar. P3-01: cooldown contract is §6.1; dangling §10.1 references retargeted. Remaining findings P1-03, P1-05, P1-06, P1-07, P1-08, P2-01, P2-03, P2-04, P2-05 are closed in the addendum; the finding-to-section map is ADD §12. Round 2 (v0.1.11 / ADD v0.1.8) supersedes the addendum closures for P1-03, P1-04, P1-05, P1-06, P1-07, and P1-08 |

> **Note on numbering:** rows above v0.1.6 describe changes using **current** section numbers, not the numbers in force at the time, so that every reference in this table still resolves.

**Non-claims:** This document does not assert measured token-cost reductions, benchmark wins against PACE/HiGMem/G-Long, ToS clearance for high-frequency CLI oneshots, or completed Perplexity HV. Those require separate empirical, legal/ops, and API-key-unblocked work. Design-target rows in §8 are not empirical results.
