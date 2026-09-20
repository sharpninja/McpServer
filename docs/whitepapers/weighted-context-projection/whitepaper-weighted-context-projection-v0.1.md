# Weighted Context Projection for Long-Horizon Agent Work: Escaping Host Auto-Compaction via SessionLog, Memory, and Sessionless Frontier CLIs

**Document:** whitepaper-weighted-context-projection-v0.1.md  
**Version:** v0.1.2  
**Status:** Draft for operator review (post Round-2 self-eval)  
**Audience:** Operator Payton Byrd  
**Author:** Payton Byrd  
**Date:** 2026-09-20 (America/Chicago)  
**Authoring context:** Engineering design note derived from MCP memory workstreams, plugin-hook limits, and literature on long-horizon agent context management. Not a product claim sheet.

---

## Executive summary

Long-horizon agent sessions fail more often from **context loss under host auto-compaction** than from raw model capability limits **(operational observation; not a measured benchmark claim)**. Plugin hooks can inject memories and react to compact events, but they do not own the host transcript. Cross-session MCP memory (remember / recall / explore / consolidate / promote) is necessary and already shipping, yet it is the wrong primary store for *turn-by-turn task state*. Whitespace-token estimator benches (including `memory-bench-whitespace`) are **not** provider-metered savings proof and must not be rehabilitated as such.

This whitepaper proposes **Weighted Context Projection**:

1. An append-only **SessionLog** holds the durable transcript (turns are never permanently deleted).
2. Each turn receives a **relative weight** for how much it advances **current work toward completion**.
3. A live **`contextProjection`** under a hard token budget expands pinned / high-weight turns, summarizes mid-weight turns, and omits low-weight turns while keeping them recoverable.
4. Reweighting can **re-admit** a previously omitted turn from SessionLog into the projection.
5. MCP memory verbs (`remember` / `recall` / `explore` / `consolidate` / `promote`) bridge durable cross-session facts so the projection stays small; `memory_remember` is the bridge/API alias for the shipped `remember` verb.
6. Prefer an **outer orchestrator** that assembles the projection and invokes frontier models via **sessionless CLI oneshots**, so host auto-compaction is unnecessary for the *model-facing* context—when we own assembly.

The design goal is operational continuity after reproject (or after a host compact fallback), not token-savings theater. Success is: the agent continues the task without the operator re-pasting lost constraints.

---

## 1. Problem

### 1.1 The real friction is compaction / context loss

Operator workflows that span dozens to hundreds of turns accumulate tool traces, dead ends, corrected plans, and quiet constraints (“never push to main”, machine paths, standing prefs). Host environments eventually compact. Compaction is typically **lossy and irreversible from the model’s point of view**: the full turn text leaves the live window; a summary may or may not preserve what mattered for *task progress*.

That is the failure mode this paper addresses. It is not “we estimated fewer whitespace tokens.”

### 1.2 What already shipped (and what it is not)

- **MCP-MEMORY-002** shipped `remember` / `recall` / `explore` / `consolidate` / `promote`, plus REQUIRED MEMORIES injection across eight plugins **(per MCP-MEMORY-002 ship scope)**. That layer is for durable, cross-session facts and standing instructions.
- **PLAN-MANAGER-MEMORY-UI-001** (Manager Memory UI) is a separate Viewer CRUD surface. It is not SessionLog and not a projection engine.
- Plugin hooks: Claude / Grok / Copilot are hook-richer than Codex / Cline / OpenCode today. Supported injection and reaction points include (where the host exposes them) `UserPromptSubmit`, `PreCompact`, and `PostCompact`. Hooks inject or react; they do **not** own the full host transcript and do **not** prevent the host from compacting.

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

**Principle:** SessionLog is the append-only ledger. MCP already ships **`sessionlog_*` APIs** for session/turn logging; this design **extends** that ledger with weight / pin / projection metadata—it is not a wholly greenfield store. Memory is the cross-session digest. Projection is the ephemeral, budgeted view assembled for the model. UI is for humans.

---

## 3. Design principles

1. **Never permanently delete turns from SessionLog.** Omission from the projection is not deletion.
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
+------------------+     append      +-------------+
|  AgentInvoker    | --------------->| SessionLog  |
|  (CLI / hooks)   |                 | (append-only)|
+--------+---------+                 +------+------+
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
| MemoryBridge     | ----> MCP remember (alias memory_remember) / recall / REQUIRED MEMORIES
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
| `weight` | Relative score for current-work progress ∈ [0, 1] or ranked mass |
| `pin` | Boolean / pin class (operator, system, acceptance) |
| `projectionGeneration` | Monotonic id of last scorer/projector pass |
| `projectionState` | `expanded` \| `summarized` \| `omitted` \| `stubbed` |
| `summaryText` | Mid-tier stub when not expanded |
| `tokenEstimate` | Local estimate for *budgeting only* (not billing proof) |
| `reAdmitCount` | How often this turn was pulled back from omit |

Projection object:

| Field | Intent |
| --- | --- |
| `budgetTokens` | Hard cap for model-facing assembly |
| `generation` | Matches scorer pass |
| `segments[]` | Ordered expanded / summary / stub markers with `turnId` refs |
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

- Periodic or per-turn judge prompt: given goal + recent projection + candidate turn stubs, redistribute mass under budget.
- Still write results back to SessionLog; never delete.
- Keep hysteresis and pin overrides.

### 5.3 Reweight triggers

Preferred: **after each turn**. Acceptable: at compact gates / budget pressure / operator “reproject” command. Continuous reweighting is the intended mechanism that makes host auto-compaction *unnecessary for model-facing context* when assembly is owned externally **(design intent until Phase 2 demonstrates it)**.

---

## 6. Projection algorithm

Informal algorithm under hard budget `B`:

1. Load SessionLog turns for `sessionId` plus standing memories from MemoryBridge.
2. Ensure pins are marked `expanded` (fail closed: if pins alone exceed `B`, surface error / force memory promotion / ask operator—do not silently drop pins).
3. Score or refresh weights (respect hysteresis).
4. Allocate remaining budget:
   - Sort non-pinned turns by weight descending (stable tie-break: recency, then `turnId`).
   - Expand while residual budget allows full payloads.
   - For the next band, attach `summaryText` stubs.
   - Omit the rest; record `omittedTurnIds`.
5. Emit `contextProjection` with `generation++`.
6. On later reweight: if an omitted turn’s weight rises enough, **re-admit** full payload (or a richer summary) and demote something else.

**Recoverability invariant:** For every omitted turn, SessionLog still has the bytes (or blob). Projection never claims deletion.

**Budget adherence:** Local token estimates may drive packing. They are engineering controls, not published savings metrics.

---

## 7. Runtime deployment options

Honest comparison of where this can live.

### Option A — Stay inside IDE plugins

**What works:** Hook injection of REQUIRED MEMORIES; PreCompact / PostCompact reactions; prompt prefixes; limited transcript glimpses depending on host APIs.

**What fails:** Plugins do not own the full host transcript. The host can still auto-compact. Projection can *mitigate* loss but cannot guarantee that model-facing context equals SessionLog-derived projection.

**Verdict:** Necessary interim hardening; not sufficient for the end state.

### Option B — Microsoft Agent Framework with client-managed history

MAF supports client-managed chat history patterns (`ChatMessageStore` / `ChatHistoryProvider`). On invoke, the framework can obtain an exact history list from the provider (modulo system messages, tools, and `AIContextProvider` contributions). That is materially stronger control than opaque host threads.

**Caveat:** Foundry persistent threads (or any service-owned thread store) weaken control: the service may retain or reshape history outside the projector. Prefer client-managed stores when the goal is weighted projection.

**Verdict:** Strong fit for API-shaped agents where message arrays are first-class. Weaker fit when the operational preference is frontier **CLI subscriptions** rather than per-token API burn.

### Option C — Outer orchestrator (extend QBAgent) + sessionless frontier CLI oneshots

**Shape:**

1. SessionLog is source of truth.
2. ContextProjector builds a prompt blob under budget.
3. AgentInvoker calls Claude / Grok / Codex / etc. as a **sessionless oneshot** using that CLI’s fresh-session / no-resume flags **(assumption: exact flag names are CLI-specific and must be verified in the Phase 2 spike; do not invent flags here)**.
4. Capture the full turn (including tool traces if observed) back into SessionLog; reweight; repeat.

**Why sessionless:** Resuming a vendor CLI session reintroduces vendor-owned transcript and their compaction. Fresh oneshots preserve *our* projection as the model-facing context.

**Trade-offs (engineering-honest):**

- Control is **prompt-blob**, not a full structured `messages[]` API (unless the chosen CLI exposes one).
- Tool loops: either let the CLI own inner tools for that oneshot, or keep tools in the orchestrator and pass results in the next projection. Both are viable; split-brain tooling is the failure mode to avoid.
- Fair-use / rate limits of CLI **subscriptions** still apply even for sessionless oneshots. This is not infinite capacity and is not a claim of uncapped API throughput.
- Prompt-cache friendliness: keep a **stable prefix** (system + standing memories) and a **variable tail** (projection segments). Do not reshuffle the prefix every turn.
- Host UI may still compact its *display* transcript; that is UX, not model-facing truth, if invocation bypasses the host model path.

**Verdict:** Best match for escaping host auto-compaction on the *model-facing* path while remaining compatible with CLI subscription usage (fair-use still applies). **Primary recommendation for Phase 2 spike** (pending operator approve/reject in §12).

---

## 8. Related work

Map each cited line of work to **shared ideas** vs **what it misses** relative to task-progress relative weights + re-admit from a full log.

### 8.1 Research

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

**HiGMem** ([arXiv:2604.18349](https://arxiv.org/abs/2604.18349))  
Shares: event summaries with expand-to-underlying-turns; hierarchical recoverability.  
Misses: retrieval-for-QA framing more than continuous *task-progress* reweight under an always-on agent budget; less emphasis on escaping host compact via outer oneshot invocation.

**G-Long** ([arXiv:2606.13115](https://arxiv.org/abs/2606.13115))  
Shares: graph structure + attention-aware importance scoring for long-term dialogue memory.  
Misses: importance from summarizer attention ≠ relative weight for unfinished engineering work; no SessionLog/CLI assembly story.

**Adaptive context compression** ([arXiv:2603.29193](https://arxiv.org/abs/2603.29193))  
Shares: turn importance scores; retain / summarize / drop bands; dynamic budget.  
Misses: importance mix (similarity, recency, dependency) is not explicitly *progress-toward-completion*; durable re-admit + MCP memory bridge + host-escape runtime need productization beyond the paper’s conversational benchmarks.

### 8.2 Industry

**Haystack `SummarizationCompactor`** ([docs](https://docs.haystack.deepset.ai/docs/next/summarization-compactor))  
Shares: progressive summarization under compaction hooks; preserve system + recent steps; replace older turns with summaries.  
Misses: one-way compact pressure; no weighted re-admit from full log as a primary loop; not task-progress relative mass with hysteresis.

**PraisonAI intelligent / context compaction** ([docs](https://praison.ai/docs/features/intelligent-conversation-compaction))  
Shares: structured summaries (topic, goals, decisions, actions); preserve recent; tool-call pairing.  
Misses: same core gap—compaction as reduction, not continuous projection with re-admit and SessionLog authority.

---

## 9. Comparison table

| Approach | Durable full turns | Task-progress weights | Re-admit | Hard budget projection | Cross-session memory bridge | Escapes host compact |
| --- | --- | --- | --- | --- | --- | --- |
| Host auto-compact only | No | No | No | Host-defined | No | N/A (is the problem) |
| Plugin hooks + MCP memory | Partial | No | Weak | No | Yes (002) | No |
| MemGPT-style paging | Partial | Indirect | Via recall | Soft | Yes | App-dependent |
| Rolling recursive summary | No (lossy) | No | No | Soft | Optional | App-dependent |
| Selective Context | N/A | Self-info ≠ progress | No | Yes | No | App-dependent |
| PACE | Design-dependent | Next-step utility (close) | Glimpse-like | Adaptive | Design-dependent | App-dependent |
| HiGMem | Turn layer yes | Event/retrieval | Expand turns | Retrieval budget | Profile optional | App-dependent |
| G-Long | Graph triplets | Attention importance | Graph expand | Retrieval | No SessionLog | App-dependent |
| Adaptive compression (2603.29193) | Usually in-memory | Relevance/recency/deps | Limited | Yes | Limited | App-dependent |
| Haystack / Praison compactors | Often replaced | Structured summary heuristics | No | Threshold-based | Optional | If you own agent runtime |
| **This design (WCP)** | **SessionLog yes (design)** | **Explicit (design)** | **Yes (design)** | **Yes (design)** | **MCP MemoryBridge (design)** | **Option C oneshot (design target; not yet measured)** |

---

## 10. Evaluation plan

Measure what matters. Do **not** lead with whitespace estimator deltas.

### 10.1 Primary success metrics

1. **Post-reproject (or post-compact fallback) task continuation** without operator re-paste of constraints, paths, or acceptance criteria.
2. **Weight stability / hysteresis:** rate of projectionState flips per turn; thrash budget.
3. **Projection token budget adherence:** fraction of assemblies ≤ `budgetTokens` (local estimator OK for this engineering metric only).
4. **Re-admit usefulness:** when a turn is re-admitted, did the subsequent action correctly use it (human or rubric check)?
5. **Pin integrity:** zero silent pin drops.

### 10.2 Explicit non-metrics (for now)

- Estimator-only “token savings %” as a published claim.
- Provider bill deltas inferred from whitespace benches.
- Vanity context-window fill charts without task outcomes.

### 10.3 Protocol sketch

- Record real operator sessions into SessionLog (offline corpus).
- Phase 1 simulator: replay scorer + projector; operators score blind continuations (full context vs projection vs host-compact summary).
- Phase 2 online spike: one CLI oneshot path; log continuation failures and re-paste events.
- Compare against PreCompact-only fallback on the same tasks.

---

## 11. Risks and open questions

1. **Thrashing:** scores oscillate; projection churns; prompt cache dies. Mitigation: hysteresis, pin classes, cooldown on re-admit.
2. **Bad scorer drops quiet constraints:** low-verbosity “never X” turns get omitted. Mitigation: constraint detector → pin or MemoryBridge; fail tests in eval protocol.
3. **CLI Terms of Service / fair use:** sessionless high-frequency oneshots may trip rate or ToS limits. Mitigation: backoff, batching, respect vendor rules; do not pretend subscriptions are uncapped APIs.
4. **Tool-loop ownership:** split between orchestrator and CLI causes missing traces in SessionLog. Mitigation: pick one owner per spike; log everything observable.
5. **Host UI compact still happens:** operators may think context is gone when only the IDE view compacted. Mitigation: UX clarity that SessionLog + projection are authoritative on Option C.
6. **Re-score cost every turn:** LLM judge v2 can be expensive. Mitigation: v1 rules by default; judge on schedule or on budget pressure.
7. **Evaluation honesty:** easy to overfit summaries that *look* complete. Mitigation: adversarial hidden constraints in replay tests.
8. **Prompt-blob vs messages[] fidelity:** some tool schemas and multimodal parts may not round-trip cleanly through CLI stdin prompts.

---

## 12. Immediate next actions (operator review)

Concrete checklist for tomorrow—no need to re-derive the design:

- [ ] **Approve or reject Option C** as the primary Phase 2 spike target (Claude or Grok CLI sessionless oneshot; prompt-blob control, not full `messages[]` unless that CLI exposes it).
- [ ] **Approve Phase 1 SessionLog path:** extend existing MCP `sessionlog_*` with weight / pin / projection metadata **vs** stand up a new store (default recommendation: extend).
- [ ] **Confirm success metric:** zero operator re-paste of constraints / paths / acceptance criteria after reproject (or compact fallback)—not estimator token deltas; not provider-metered savings claims from `memory-bench-whitespace`.
- [ ] **Note:** Perplexity HV still **pending API key** after box reseed—do not treat HV as done.
- [ ] **Note:** Full 19-file `add-profile` restore still needed when PAYTON-LEGION2 reconnects; standing rules currently restored from durable memory only. Never publish profile files publicly.
- [ ] **Confirm IDs:** MCP-MEMORY-002 (memory verbs), PLAN-MANAGER-MEMORY-UI-001 (Manager UI)—no invented plan-name variants.
- [ ] **Skim related-work table** for fairness (especially PACE proximity + re-admit gap); literature list is frozen for v0.1.x—no invented papers.

---

## 13. Recommendations

Mapped **1:1 to Roadmap phases** in §14. Cross-cutting constraints listed after.

| Rec | Maps to | Action |
| --- | --- | --- |
| **R0** | **Phase 0** | Treat compaction / context loss as the primary problem; complete operator review of this whitepaper; run Hostile Validation when unblocked (Perplexity HV still pending API key). Keep MCP-MEMORY-002 as cross-session companion, not the turn ledger. |
| **R1** | **Phase 1** | Extend MCP `sessionlog_*` with weight / pin / projection metadata (prefer extend over new store); ship offline projection simulator with the schema fields and I/O in §14 Phase 1 exit criteria. |
| **R2** | **Phase 2** | Spike Option C (outer orchestrator + one Claude or Grok sessionless CLI oneshot) early—before over-investing in in-host projection theater. Meet the Phase 2 spike acceptance checklist in §14.1. |
| **R3** | **Phase 3** | Keep Option A PreCompact fallback for hook-rich hosts (Claude / Grok / Copilot) during transition; measure re-paste rate vs baseline. |
| **R4** | **Phase 4** | Scorer honesty: ship v1 rules with hysteresis before any learned / LLM judge (v2). |

**Cross-cutting (all phases):**

- Do **not** rehabilitate `memory-bench-whitespace` (whitespace/estimator multiturn bench) as provider-metered savings proof.
- Success = task continuity without operator re-paste after reproject / compact fallback—not estimator deltas.
- Prefer MAF client-managed history (**Option B**) when the runtime is API-native rather than CLI-subscription-native; Option B is a parallel path, not a substitute for the Phase 2 CLI spike unless the operator redirects.

---

## 14. Roadmap

| Phase | Deliverable | Exit criteria |
| --- | --- | --- |
| **0** | This whitepaper + Hostile Validation (HV) alignment | Operator review of open questions; HV still pending where blocked on API keys |
| **1** | SessionLog schema extension (weight/pin/projection) + offline projection simulator on recorded sessions | **Schema fields present:** `turnId`, `sessionId`, `payload`, `weight`, `pin`, `projectionGeneration`, `projectionState`, `summaryText`, `tokenEstimate`, `reAdmitCount`; projection object fields `budgetTokens`, `generation`, `segments[]`, `omittedTurnIds[]`, `standingMemoryIds[]`. **Simulator I/O:** inputs = recorded SessionLog turns + budget `B` + pin set; outputs = `contextProjection` JSON + budget-adherence report + omit/re-admit trace. Replay diffs vs full-context baseline; no production CLI dependency yet. Prefer extend `sessionlog_*` over a new store unless operator rejects. |
| **2** | Outer-orchestrator spike (extend QBAgent) with **one** CLI (Claude or Grok) sessionless oneshot | End-to-end: log → score → project → oneshot → append → reweight; one re-admit demo; continuation without operator re-paste; **no** estimator-savings claim |
| **3** | PreCompact fallback for hook-rich hosts (Claude / Grok / Copilot) | Inject projection / memory on compact gate; measure re-paste rate vs baseline |
| **4** | Scorer v1 rules → v2 LLM judge with hysteresis | Thrash metrics under threshold; quiet-constraint eval suite green |

### 14.1 Phase 2 spike acceptance checklist (pass/fail)

All bullets must be **pass** before calling the Phase 2 spike done. Fail any → not done.

- [ ] **PASS/FAIL — Fresh CLI flags:** Sessionless oneshot uses verified fresh-session / no-resume flags for the chosen CLI (Claude **or** Grok); flag names documented from that CLI’s real help/docs—not invented.
- [ ] **PASS/FAIL — Projection under budget:** Assembled prompt / projection is ≤ configured `budgetTokens` (local estimator OK for this engineering gate only).
- [ ] **PASS/FAIL — SessionLog append:** Full oneshot turn (observable request + response + tool traces) appends to SessionLog via extended `sessionlog_*` (or agreed interim path)—no silent drop.
- [ ] **PASS/FAIL — Reweight:** At least one scorer/projector pass updates `weight` / `projectionState` after append.
- [ ] **PASS/FAIL — Re-admit demo:** One previously omitted turn is re-admitted into a later projection and used by a subsequent oneshot (receipt: turnIds + generations).
- [ ] **PASS/FAIL — No estimator savings claim:** Spike write-up does **not** claim provider-metered token savings or cite `memory-bench-whitespace` deltas as cost proof.
- [ ] **PASS/FAIL — Continuation:** Operator (or rubric) confirms task continues without re-pasting standing constraints after reproject.

### 14.2 Out of scope for v0.1.x

Explicitly **not** attempted in this whitepaper revision or the Phase 0–2 decision window:

- Learned / trained scorer weights or production v2 judge prompts
- Full eight-CLI matrix (Codex / Cline / OpenCode / Copilot / …) oneshot certification
- Legal opinion on vendor CLI Terms of Service for high-frequency sessionless oneshots
- Provider-metered A/B cost studies or published “token savings %”
- New SessionLog implementation code or production migrations (Phase 1 may prototype schema offline only)
- Inventing additional literature beyond the frozen citation list in §16
- Claiming Perplexity Hostile Validation complete while API key remains missing post-reseed
- Publishing operator `add-profile` / standing-rules profile files publicly

---

## 15. Appendix: glossary

| Term | Meaning |
| --- | --- |
| **SessionLog** | Append-only durable transcript of turns for a session; extends MCP `sessionlog_*` with weight/pin/projection metadata |
| **Turn** | Full request + intermediates + response unit |
| **Weight** | Relative importance for advancing current work toward completion |
| **Pin** | Hard retain-in-projection marker |
| **contextProjection** | Budgeted model-facing assembly derived from SessionLog + memory |
| **Re-admit** | Restoring an omitted turn into the projection from SessionLog |
| **MemoryBridge** | Path between turn stream and MCP durable memory (`remember` / `recall` / `explore` / `consolidate` / `promote`; `memory_remember` = bridge/API alias) |
| **Sessionless oneshot** | Fresh CLI invocation without vendor session resume |
| **Host auto-compaction** | IDE/runtime irreversible (to the model) context reduction |
| **Hysteresis** | Resistance to rapid weight/state flipping |
| **HV** | Hostile Validation (accuracy + completeness gate; not “high-visibility / high-value”) |

---

## 16. References

1. Packer, C., et al. *MemGPT: Towards LLMs as Operating Systems.* arXiv:2310.08560. https://arxiv.org/abs/2310.08560  
2. Li, Y., et al. *Compressing Context to Enhance Inference Efficiency of Large Language Models* (Selective Context). arXiv:2310.06201. https://arxiv.org/abs/2310.06201  
3. Wang, Q., et al. *Recursively Summarizing Enables Long-Term Dialogue Memory in Large Language Models.* arXiv:2308.15022. https://arxiv.org/abs/2308.15022  
4. Xu, W., et al. *A-MEM: Agentic Memory for LLM Agents.* arXiv:2502.12110. https://arxiv.org/abs/2502.12110  
5. Wei, L., et al. *PACE: Predictive Adaptive Context Extraction for Long-Horizon LLM Agents.* ACL 2026. https://aclanthology.org/2026.acl-long.1252/  
6. Cao, S., He, J., Tan, F. *HiGMem: A Hierarchical and LLM-Guided Memory System for Long-Term Conversational Agents.* arXiv:2604.18349. https://arxiv.org/abs/2604.18349  
7. *G-Long: Graph-Enhanced Memory Management for Efficient Long-Term Dialogue Agents.* arXiv:2606.13115. https://arxiv.org/abs/2606.13115  
8. *Developing Adaptive Context Compression Techniques for Large Language Models (LLMs) in Long-Running Interactions.* arXiv:2603.29193. https://arxiv.org/abs/2603.29193  
9. deepset Haystack. *SummarizationCompactor* documentation. https://docs.haystack.deepset.ai/docs/next/summarization-compactor  
10. PraisonAI. *Intelligent Conversation Compaction* documentation. https://praison.ai/docs/features/intelligent-conversation-compaction  
11. Microsoft Agent Framework. *Chat history storage patterns* (client-managed `ChatMessageStore` / `ChatHistoryProvider`). https://devblogs.microsoft.com/agent-framework/chat-history-storage-patterns-in-microsoft-agent-framework/

---

## Document control

| Version | Date (CT) | Notes |
| --- | --- | --- |
| v0.1 | 2026-09-20 | Initial whitepaper for operator review tomorrow |
| v0.1.1 | 2026-09-20 | Round-1 self-eval: HV = Hostile Validation; SessionLog extends `sessionlog_*`; hook matrix honesty; Immediate next actions; Phase 1 exit criteria concreteness; memory verb / alias clarity |
| v0.1.2 | 2026-09-20 | Round-2 hostile pass: remove soft overclaims; label assumptions; Phase 2 spike acceptance checklist; Out of scope for v0.1.x; Recommendations↔Roadmap 1:1 |

**Non-claims:** This document does not assert measured token-cost reductions, benchmark wins against PACE/HiGMem/G-Long, ToS clearance for high-frequency CLI oneshots, or completed Perplexity HV. Those require separate empirical, legal/ops, and API-key-unblocked work. Design-target rows in §9 are not empirical results.
