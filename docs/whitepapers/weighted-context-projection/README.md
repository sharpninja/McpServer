# Weighted Context Projection — deliverable index

Operator-facing index for the whitepaper on escaping host auto-compaction via SessionLog, weighted projection, MCP memory, and sessionless frontier CLI oneshots.

## Documents

| File | Description |
| --- | --- |
| [whitepaper-weighted-context-projection-v0.1.md](./whitepaper-weighted-context-projection-v0.1.md) | **Design only** (**v0.1.23** content; filename retained). Problem, architecture, scoring model, projection algorithm (ordered transition, generation snapshot with `sendFence`, pin/harm precedence, replacement-checked warning retirement, joint admission including depth truncation, token stability including the mixed-pressure split floored once per packing step, live-tool pre-admission before the §6.3 mandatory set), related work with MemGPT scored on the durable-store and hard-ceiling axes, evaluation method, glossary, references. Traceability-script limits and qualification prerequisites are in the box after Scope; the substantive script statement is the addendum. Scope names the addendum as **ADD** |
| [proposed-implementation-weighted-context-projection-v0.1.md](./proposed-implementation-weighted-context-projection-v0.1.md) | **Proposed implementation** (v0.1.18). Deployment options, code-grounded corrections against `main` @ `e7c43a12`, sealed-projection design, recommendations, roadmap and phase gates, next actions, open questions. Replaces `implementation-recommendations-v0.1.md`. Phase gates follow WP v0.1.23 on the send fence, active markers, live-tool pre-admission including measured pin retention and the unmeasurable-split marker, and the two stability metrics. §1 repeats the traceability-script limits and the qualification prerequisites. Open question 7 no longer leaves oversized live-tool admission open |
| [addendum-retrospective-linking-and-goal-metrics-v0.1.md](./addendum-retrospective-linking-and-goal-metrics-v0.1.md) | **Draft addendum** (v0.1.20). Incorporates PR #56, the PR #58 pin exception, Astra re-reviews through receipt `20260926-145009-ct` on tip `a26cbdbe`, and the secondary document pass (finding maps in §12). v0.1.19 claimed P2-12 and P3-02. That receipt closed P3-02 and left residual P2-12 open. v0.1.20 claims that residual pending re-review. It is not an AGREE. The 23 prior Astra closures stay closed, including P2-07 and P2-11. Not operator-approved. Proposes work beyond v0.1.x: retrospective link index over sealed turns, continuous link revalidation, per-goal scope/progress metrics, and turn outcome records adjudicated by Hostile Validation. Read after the other two |
| [self-eval-round1.md](./self-eval-round1.md) | Round-1 accuracy + actionability self-eval receipt |
| [self-eval-round2.md](./self-eval-round2.md) | Round-2 hostile-pass self-eval receipt |

Standing-rules load receipts are deliberately **not** in this folder — see the profile note below.

**Read order.** The whitepaper argues the design and decides nothing operational. The implementation proposal is where deployment options, phases, exit gates, and everything grounded in the current code live — including two findings that were asserted and then retracted, kept in place so the error stays auditable. Cross-references use `WP §N` for the whitepaper, `IMPL §N` for the proposal, and `ADD §N` for the addendum. The whitepaper Scope is where **ADD** and **IMPL** are introduced.

## One-paragraph abstract

Long-horizon agent friction is **compaction / context loss**, not whitespace-estimator “savings.” The MCP memory layer holds durable cross-session facts — eleven shipped tools spanning a typed write path (`memory_remember`), meaning-ranked recall, exploration, promotion, consolidation, revert, and a compatibility CRUD surface — but it is not a turn ledger. This design **extends** existing MCP `sessionlog_*` with weight / pin / projection metadata, scores each turn by **relative weight for advancing current work**, and assembles a budgeted **`contextProjection`** (expand / summarize / omit) with **re-admit** from the full log. Prefer an outer orchestrator + **sessionless CLI oneshots** (Option C) so model-facing context is owned externally; keep plugin PreCompact as fallback on hook-rich hosts. Do not treat `memory-bench-whitespace` as provider-metered proof. Success = task continuity without operator re-paste.

## Self-eval / profile notes

- Standing rules restored from durable memory only (`STANDING-RULES-FROM-MEMORY.md`, not tracked in this repo). Full 19-file `add-profile` **not** on box; PAYTON-LEGION2 disconnected. **Never publish** profile files publicly — the round-1/round-2 load receipts are intentionally absent from this folder for that reason.
- Two self-eval rounds completed (receipts above, against v0.1.2). Paper version now **v0.1.23** after the hostile full-document review, the Astra re-reviews through receipt `20260926-145009-ct`, the v0.1.17 confidence polish, and the v0.1.18 secondary document pass; the self-evals predate those fixes. Those later waves are claimed remediation pending re-review. They are not an AGREE. Tip `a26cbdbe` was DISAGREE 97/100. P3-02 is closed on that receipt. The 23 prior Astra closures stay closed, including P2-07 and P2-11. Parked Perplexity findings P1-02, P2-02, and P2-03 were not edited (P2-02 would reopen Astra P2-07). A documentation review does not establish implemented behavior, measured efficacy, operator approval, or operational HV / MCP audit closure.
- **Operational Perplexity HV is still blocked on API key** after box reseed and is not done. Do not claim that gate was completed. A separate secondary document pass, the GrokCode adjudication of 2026-09-26 (`perplexity-adjudication-20260926`), supplied the Pplx P1-01, P2-01, and P3-01 finding list recorded in the addendum. That pass is not the operational gate.

## Review checklist (for tomorrow)

- [ ] Approve/reject Option C as primary Phase 2 spike (Claude or Grok CLI oneshot)
- [ ] Approve Phase 1: extend `sessionlog_*` vs new store
- [ ] **Correct the `sessionlog_delete_turn` description only, and keep the "Irreversible" warning on `sessionlog_delete_session`** — `delete_turn` wrongly says child rows are removed when they are tombstoned. The `delete_session` warning stays: no `sessionlog_restore` tool exists, revival is only a side effect of resubmitting the same session, and a deleted turn has no revival path at all, so the call really is irreversible to its caller. Physical-delete blocking also holds only on the tracked save path, and the bulk tombstone/revival paths append no audit rows (proposed implementation §3.2, §3.2.1, §3.2.2). An earlier version of this checklist item had this backwards
- [ ] **Decide whether Option B/C is still exclusive,** given `Microsoft.Agents.AI` is already referenced and QBAgent already drives an `IChatClient` (§4.2)
- [ ] Confirm success metric: zero operator re-paste after reproject
- [ ] Note the operational Perplexity HV gate is still pending an API key. The ADD §12 secondary document pass is not that gate
- [ ] Note add-profile full restore when Legion reconnects
- [ ] Problem framing accepts compaction—not estimator deltas—as the primary pain
- [ ] SessionLog vs MCP Memory vs Manager UI (PLAN-MANAGER-MEMORY-UI-001) boundaries are clear
- [ ] Phase 2 spike acceptance checklist (§14.1) is usable as pass/fail
- [ ] Recommendations (§13) map 1:1 to Roadmap phases (§14)

## Out of scope for v0.1.x

- Learned scorer / production v2 judge
- Eight-CLI matrix certification
- ToS legal opinion; provider-metered A/B
- Production SessionLog migrations
- Invented literature beyond frozen citation list
- Claiming the operational Perplexity HV gate complete while its API key is missing. The secondary document pass in the addendum is not that gate

## Status

Draft. Whitepaper **v0.1.23**, proposed implementation **v0.1.18**, addendum **v0.1.20**. Whitepaper, implementation, and addendum revised 2026-09-26 for the hostile-review contract, the Astra re-review through receipt `20260926-145009-ct` (DISAGREE 97/100 on tip `a26cbdbe`), the confidence polish, and the secondary document pass (GrokCode); America/Chicago. v0.1.20 claims remediation of residual Astra P2-12 pending re-review. It is not an AGREE. P3-02 is closed. The 23 prior Astra closures stay closed, including P2-07 and P2-11. The operational Perplexity gate remains pending an API key. Awaiting operator Payton Byrd review. Author: Payton Byrd.
