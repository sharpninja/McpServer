# Weighted Context Projection for Long-Horizon Agent Work: Escaping Host Auto-Compaction via SessionLog, Memory, and Sessionless Frontier CLIs

**Document:** whitepaper-weighted-context-projection-v0.1.md  
**Version:** v0.1.6  
**Status:** Draft for operator review (design only; implementation proposal split out)  
**Audience:** Operator Payton Byrd  
**Author:** Payton Byrd  
**Date:** 2026-09-20 (America/Chicago)  
**Authoring context:** Engineering design note derived from MCP memory workstreams, plugin-hook limits, and literature on long-horizon agent context management. Not a product claim sheet.

**Scope.** This document argues a design. It deliberately contains no deployment choice, no roadmap, no phase gates, and no code-level specifics. Those live in the companion proposal, `proposed-implementation-weighted-context-projection-v0.1.md`, referred to below as **IMPL**. A bare `§N` refers to a section of this paper; `IMPL §N` refers to that companion.

---

## Executive summary

Long-horizon agent sessions fail more often from **context loss under host auto-compaction** than from raw model capability limits **(operational observation; not a measured benchmark claim)**. Plugin hooks can inject memories and react to compact events, but they do not own the host transcript. Cross-session MCP memory (remember / recall / explore / consolidate / promote) is necessary and already shipping, yet it is the wrong primary store for *turn-by-turn task state*. Whitespace-token estimator benches (including `memory-bench-whitespace`) are **not** provider-metered savings proof and must not be rehabilitated as such.

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
5. **Pins beat scores.** Explicit operator or agent pins (e.g., acceptance criteria, blocked-on facts) stay expanded until unpinned.
6. **Memory bridges the long tail.** Standing facts leave the projection via MCP `remember` (bridge/API alias: `memory_remember`) rather than forever occupying mid-tier summary slots.
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
| `weight` | Independent scalar score for current-work progress ∈ [0, 1]. Not a normalized distribution: weights do not sum to 1 across a session, so `Δweight` hysteresis thresholds (§5.2) and the density packing in §6 compare absolute values |
| `pin` | Boolean / pin class (operator, system, acceptance) |
| `projectionGeneration` | Monotonic id of last scorer/projector pass |
| `projectionState` | `expanded` \| `summarized` \| `omitted`. A "stub" is the `summaryText` carried by a `summarized` turn; there is no separate `stubbed` state |
| `summaryText` | Mid-tier stub when not expanded |
| `tokenEstimate` | Local estimate for *budgeting only* (not billing proof) |
| `reAdmitCount` | How often this turn was pulled back from omit |
| `lastReAdmitGeneration` | `projectionGeneration` at which this turn was last re-admitted; the field the re-admit cooldown in §10.1 reads |

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
- Hysteresis: require Δweight above a threshold before changing `projectionState`, to avoid thrashing.

**v2 — LLM / learned judge:**

- Periodic or per-turn judge prompt: given goal + recent projection + candidate turn stubs, re-score candidate turns under budget. The judge emits independent `∈ [0, 1]` scores per turn (see §4.2)—it does **not** redistribute a fixed normalized mass, because normalization would make one turn's score depend on unrelated turns and break `Δweight` hysteresis.
- Still write results back to SessionLog; never delete.
- Keep hysteresis and pin overrides.

### 5.3 Reweight triggers

Preferred: **after each turn**. Acceptable: at compact gates / budget pressure / operator “reproject” command. Continuous reweighting is the intended mechanism that makes host auto-compaction *unnecessary for model-facing context* when assembly is owned externally **(design intent until Phase 2 demonstrates it)**.

---

## 6. Projection algorithm

Informal algorithm under hard budget `B`:

1. Load SessionLog turns for `sessionId` plus standing memories from MemoryBridge.
2. **Charge the fixed prefix against the budget first.** Compute `B_turns = B - tokens(system prefix) - tokens(standing memories)`, where the standing memories are those listed in `standingMemoryIds[]`. Standing memories are model-facing and therefore consume `budgetTokens` exactly as turns do; IMPL §6's stable-prefix guidance makes them cheap to cache, not free to send. Fail closed: if `B_turns <= 0`, surface error / demote or consolidate standing memories via `consolidate` / ask operator—do not silently overflow `B`.
3. Ensure pins are marked `expanded` (fail closed: if pins alone exceed `B_turns`, surface error / force memory promotion / ask operator—do not silently drop pins).
4. Score or refresh weights (respect hysteresis).
5. Allocate the remaining `B_turns` by **value density**, not raw weight:
   - Compute `density = weight / max(tokenEstimate, 1)` for each non-pinned turn.
   - Sort by `density` descending (stable tie-break: `weight` descending, then recency, then `turnId`).
   - Expand while residual budget allows full payloads.
   - For the next band, attach `summaryText` stubs, charged at the stub's own `tokenEstimate` rather than the full payload's.
   - Omit the rest; record `omittedTurnIds`.

   **Why density and not raw weight:** sorting by weight alone lets a single high-weight, high-token payload (e.g. a 40k-token tool dump scored 0.9) consume the budget ahead of many smaller turns of nearly equal weight—including exactly the low-verbosity constraint turns §10.2 exists to protect. Density packing is the standard greedy approximation for a hard-capped budget; raw weight survives as the tie-break so that equal-cost turns still order by task relevance.
6. Emit `contextProjection` with `generation++`.
7. On later reweight: if an omitted turn’s weight rises enough, **re-admit** full payload (or a richer summary) and demote something else, subject to the §10.1 cooldown.

**Budget invariant:** `tokens(system prefix) + tokens(standing memories) + tokens(expanded) + tokens(stubs) ≤ B`. The §9.1 budget-adherence metric and the IMPL §8 Phase 1 adherence report both measure this full sum, not the turn portion alone.

**Recoverability invariant:** For every omitted turn, SessionLog still has the bytes (or blob). Projection never claims deletion.

This is **an existing enforced property of the durable store, not an aspiration of this design.** Deletes of durable records are converted to tombstones rather than executed; physical deletion is rejected outright rather than merely discouraged; and every mutation is mirrored into an append-only audit ledger carrying before-and-after snapshots, so original writes and additive changes stay reconstitutable even for a tombstoned record. Projection therefore *inherits* recoverability instead of having to implement it.

Two consequences the design must respect:

- **Re-admit of an omitted turn is safe by construction** (step 7 above), because omitted turns are never deleted. Re-admit of a *tombstoned* turn is a different matter: recovery reads must bypass the default visibility filters, and no restore operation is exposed on the session-log tool surface today. Any design that depends on reviving deleted turns is depending on unbuilt work.
- **The enforcement is not visible in the data model's type definitions,** which makes it easy to audit incorrectly and conclude the ledger is mutable. An earlier review of this paper did exactly that.

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
2. **Weight stability / hysteresis:** `projectionState` flip rate, defined as (state changes observed in a projection pass) / (turns present in that pass), averaged over the session. Provisional thrash budget: **≤ 0.10**, i.e. under one state change per ten turns per pass. Operator-adjustable, but a number must be fixed before Phase 4 can pass or fail (IMPL §8).
3. **Projection token budget adherence:** fraction of assemblies ≤ `budgetTokens` (local estimator OK for this engineering metric only).
4. **Re-admit usefulness:** when a turn is re-admitted, did the subsequent action correctly use it (human or rubric check)?
5. **Pin integrity:** zero silent pin drops.

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

1. **Thrashing:** scores oscillate; projection churns; prompt cache dies. Mitigation: hysteresis, pin classes, cooldown on re-admit (enforced against `lastReAdmitGeneration`; see §4.2).
2. **Bad scorer drops quiet constraints:** low-verbosity “never X” turns get omitted. Mitigation: constraint detector → pin or MemoryBridge; fail tests in eval protocol.
3. **CLI Terms of Service / fair use:** sessionless high-frequency oneshots may trip rate or ToS limits. Mitigation: backoff, batching, respect vendor rules; do not pretend subscriptions are uncapped APIs.
4. **Tool-loop ownership:** split between orchestrator and CLI causes missing traces in SessionLog. Mitigation: pick one owner per spike; log everything observable.
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
| **Weight** | Independent scalar ∈ [0, 1] for advancing current work toward completion; not a normalized distribution (see §4.2) |
| **Pin** | Hard retain-in-projection marker |
| **contextProjection** | Budgeted model-facing assembly derived from SessionLog + memory |
| **Re-admit** | Restoring an omitted turn into the projection from SessionLog |
| **MemoryBridge** | Path between turn stream and MCP durable memory (`remember` / `recall` / `explore` / `consolidate` / `promote`; `memory_remember` = bridge/API alias) |
| **Sessionless oneshot** | Fresh CLI invocation without vendor session resume |
| **Host auto-compaction** | IDE/runtime irreversible (to the model) context reduction |
| **Hysteresis** | Resistance to rapid weight/state flipping |
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
| v0.1.3 | 2026-09-20 | Round-3 external review: correct MAF API attribution (`ChatMessageStore` → `AgentSession` / `ChatHistoryProvider`); §6 allocates by value density instead of raw weight; retire dead `stubbed` state and add `lastReAdmitGeneration` so the §10.1 re-admit cooldown is implementable before Phase 1 freezes the schema |
| v0.1.4 | 2026-09-20 | Round-3 follow-up: charge system prefix + standing memories against `B` with a stated budget invariant; scope the IMPL §8.1 tool-trace gate to the declared tool owner and add a declaration gate; fix the thrash threshold at ≤ 0.10 flips/turn/pass; resolve `weight` as an independent scalar (not normalized mass); record HiGMem's Findings-of-ACL-2026 venue and 2603.29193's preprint provenance; complete refs 5, 7, 8; repair the §4.1 diagram alignment |
| v0.1.5 | 2026-09-20 | Separate design from implementation, and stop describing shipped work by ticket number. §3 and §6 now state recoverability as an *existing enforced property* of the durable store—tombstones instead of executed deletes, physical deletion rejected, mutations mirrored to an append-only snapshot ledger—so projection inherits the invariant rather than building it; the classes and methods providing it are delegated to the companion implementation note instead of named here. Records the two design-relevant consequences: re-admit of an *omitted* turn is safe by construction, while re-admit of a *tombstoned* turn depends on unbuilt restore capability. §1.2 describes the shipped capabilities directly instead of citing internal work-item identifiers (`MCP-MEMORY-002`, `PLAN-MANAGER-MEMORY-UI-001`), which meant nothing to an external reader |
| v0.1.6 | 2026-09-20 | **Split design from proposed implementation.** Runtime deployment options, immediate next actions, recommendations, and the roadmap with its phase gates moved to `proposed-implementation-weighted-context-projection-v0.1.md`, together with the code-grounded findings that were previously a third document; remaining sections renumbered (old §8–§11 → §7–§10, old §15–§16 → §11–§12). Executive summary now names the memory **tools** (`memory_remember` / `memory_recall` / `memory_explore` / `memory_consolidate` / `memory_promote`) rather than describing `memory_remember` as an alias of a bare `remember` verb, which had the relationship backwards |

> **Note on numbering:** rows above v0.1.6 describe changes using **current** section numbers, not the numbers in force at the time, so that every reference in this table still resolves.

**Non-claims:** This document does not assert measured token-cost reductions, benchmark wins against PACE/HiGMem/G-Long, ToS clearance for high-frequency CLI oneshots, or completed Perplexity HV. Those require separate empirical, legal/ops, and API-key-unblocked work. Design-target rows in §8 are not empirical results.
