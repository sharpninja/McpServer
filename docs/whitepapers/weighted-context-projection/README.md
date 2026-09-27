# Weighted Context Projection: deliverable index

Operator-facing index for the whitepaper on escaping host auto-compaction via SessionLog, weighted projection, MCP memory, and sessionless frontier CLI oneshots.

## Documents

| File | Description |
| --- | --- |
| [whitepaper-weighted-context-projection-v0.1.md](./whitepaper-weighted-context-projection-v0.1.md) | **Design** (**v0.1.25** content; filename retained). Problem, architecture, scoring model, projection algorithm (ordered transition, generation snapshot with `sendFence`, pin/harm precedence, replacement-checked warning retirement, joint admission including depth truncation, token stability including the mixed-pressure split floored once per packing step, live-tool pre-admission before the §6.3 mandatory set), related work with MemGPT scored on the durable-store and hard-ceiling axes, evaluation method, glossary, references. As of v0.1.24 the former addendum is part of this paper: retrospective linking (§15 through §17), continuous link revalidation (§18), per-goal metrics (§19), turn outcome and HV records (§20), baseline storage constraints (§21), linking evaluation hooks (§22), open questions (§23), linking non-claims (§24), and finding maps (Appendix A). The alias from former ADD section numbers is stated once in §13. Traceability-script limits and qualification prerequisites are in the box after Scope; the substantive script statement is §19.4. v0.1.25 qualifies Scope: §21 may cite existing code identifiers as storage constraints, and the paper is not an implementation specification. §19.1 states that the burn-up / earned-value lineage is already in the whitepaper |
| [proposed-implementation-weighted-context-projection-v0.1.md](./proposed-implementation-weighted-context-projection-v0.1.md) | **Proposed implementation** (v0.1.20). Deployment options, code-grounded corrections against `main` @ `e7c43a12`, sealed-projection design, recommendations, roadmap and phase gates, next actions, open questions. Replaces `implementation-recommendations-v0.1.md`. Phase gates follow WP v0.1.25 on the send fence, active markers, live-tool pre-admission including measured pin retention and the unmeasurable-split marker, and the two stability metrics. §1 repeats the traceability-script limits and the qualification prerequisites. Cross-references that used to name the addendum now name WP §19.4, WP §20.4, and WP Appendix A. Open question 7 no longer leaves oversized live-tool admission open |
| [self-eval-round1.md](./self-eval-round1.md) | Round-1 accuracy + actionability self-eval receipt |
| [self-eval-round2.md](./self-eval-round2.md) | Round-2 hostile-pass self-eval receipt |

The file `addendum-retrospective-linking-and-goal-metrics-v0.1.md` is a **stub**, not a primary deliverable. v0.1.20 was merged into the whitepaper at v0.1.24. The stub remains for one release so older links do not 404. Do not edit it as a living specification.

Standing-rules load receipts are deliberately **not** in this folder. See the profile note below.

**Read order.** The whitepaper argues the design, including the linking and goal-metric chapters, and decides nothing operational. It is not an implementation specification. Deployment options, phases, exit gates, and the code-grounded corrections live in the implementation proposal, including two findings that were asserted and then retracted, kept in place so the error stays auditable. Whitepaper §21 is the identifier exception: it cites existing code identifiers as baseline storage constraints (the edge-shape precedent, the Chunks-only embedding column, and explicit tombstone registration). Those names constrain the design. They are not a build procedure. Cross-references use `WP §N` for the whitepaper and `IMPL §N` for the proposal. Former `ADD §N` resolves once, in whitepaper §13. The whitepaper Scope is where **IMPL** is introduced.

## One-paragraph abstract

Long-horizon agent friction is **compaction / context loss**, not whitespace-estimator "savings." The MCP memory layer holds durable cross-session facts. Eleven shipped tools span a typed write path (`memory_remember`), meaning-ranked recall, exploration, promotion, consolidation, revert, and a compatibility CRUD surface. That layer is not a turn ledger. This design **extends** existing MCP `sessionlog_*` with weight / pin / projection metadata, scores each turn by **relative weight for advancing current work**, and assembles a budgeted **`contextProjection`** (expand / summarize / omit) with **re-admit** from the full log. A stored scalar does not by itself persist a relation between turns or pack both ends. That is the linking design in §15 through §18. Plan-level scope and progress (§19) and HV-adjudicated outcome records (§20) are the only progress path that may revise weight. Prefer an outer orchestrator + **sessionless CLI oneshots** (Option C) so model-facing context is owned externally; keep plugin PreCompact as fallback on hook-rich hosts. Do not treat `memory-bench-whitespace` as provider-metered proof. Success = task continuity without operator re-paste.

## Self-eval / profile notes

- Standing rules restored from durable memory only (`STANDING-RULES-FROM-MEMORY.md`, not tracked in this repo). Full 19-file `add-profile` **not** on box; PAYTON-LEGION2 disconnected. **Never publish** profile files publicly. The round-1/round-2 load receipts are intentionally absent from this folder for that reason.
- Two self-eval rounds completed (receipts above, against v0.1.2). Paper version now **v0.1.25** after the hostile full-document review, the Astra re-reviews through receipt `20260926-145009-ct`, the confidence polish, the secondary document pass, the merge of former addendum v0.1.20, and claimed remediation of Astra P3-03 and P3-04. That v0.1.25 pass is pending re-review and is not an AGREE. The self-evals predate those fixes. Tip `a26cbdbe` was DISAGREE 97/100. P3-02 is closed on that receipt. The 23 prior Astra closures stay closed, including P2-07 and P2-11. Parked Perplexity findings P1-02, P2-02, and P2-03 were not edited (P2-02 would reopen Astra P2-07). A documentation review does not establish implemented behavior, measured efficacy, operator approval, or operational HV / MCP audit closure.
- **Operational Perplexity HV is still blocked on API key** after box reseed and is not done. Do not claim that gate was completed. A separate secondary document pass, the GrokCode adjudication of 2026-09-26 (`perplexity-adjudication-20260926`), supplied the Pplx P1-01, P2-01, and P3-01 finding list recorded in Appendix A. That pass is not the operational gate.

## Review checklist (for tomorrow)

- [ ] Approve/reject Option C as primary Phase 2 spike (Claude or Grok CLI oneshot)
- [ ] Approve Phase 1: extend `sessionlog_*` vs new store
- [ ] **Correct the `sessionlog_delete_turn` description only, and keep the "Irreversible" warning on `sessionlog_delete_session`**. `delete_turn` wrongly says child rows are removed when they are tombstoned. The `delete_session` warning stays: no `sessionlog_restore` tool exists, revival is only a side effect of resubmitting the same session, and a deleted turn has no revival path at all, so the call really is irreversible to its caller. Physical-delete blocking also holds only on the tracked save path, and the bulk tombstone/revival paths append no audit rows (proposed implementation §3.2, §3.2.1, §3.2.2). An earlier version of this checklist item had this backwards
- [ ] **Decide whether Option B/C is still exclusive,** given `Microsoft.Agents.AI` is already referenced and QBAgent already drives an `IChatClient` (IMPL §4.2)
- [ ] Confirm success metric: zero operator re-paste after reproject
- [ ] Note the operational Perplexity HV gate is still pending an API key. Whitepaper Appendix A is not that gate
- [ ] Note add-profile full restore when Legion reconnects
- [ ] Problem framing accepts compaction, not estimator deltas, as the primary pain
- [ ] SessionLog vs MCP Memory vs Manager UI boundaries are clear
- [ ] Phase 2 spike acceptance checklist (IMPL §8.1) is usable as pass/fail
- [ ] Recommendations (IMPL §7) map 1:1 to Roadmap phases (IMPL §8)

## Out of scope for v0.1.x

- Learned scorer / production v2 judge
- Eight-CLI matrix certification
- ToS legal opinion; provider-metered A/B
- Production SessionLog migrations
- Invented literature beyond frozen citation list
- Claiming the operational Perplexity HV gate complete while its API key is missing. The secondary document pass in whitepaper Appendix A is not that gate

## Status

Draft. Whitepaper **v0.1.25**. Proposed implementation **v0.1.20** (companion pointer only; no new runtime design). Former addendum **v0.1.20** was merged into the whitepaper on 2026-09-27 (America/Chicago). The addendum file is a stub, not a third living design.

v0.1.25 is claimed remediation of Astra P3-03 and Astra P3-04, pending re-review. It is not an AGREE. P3-03 qualifies Scope and the read order above: §21 may cite existing code identifiers as storage constraints, and this paper is not an implementation specification. P3-04 rewrites §19.1 so the burn-up / earned-value lineage is already in the whitepaper. The WP §12 citation stays deferred to v0.2.

Held Astra closures stay closed, including P2-07, P2-10, P2-11, and P3-02. Parked Perplexity findings P1-02, P2-02, and P2-03 stay parked. P2-02 would reopen Astra P2-07 and was not edited.

Residual Astra P2-12 is still claimed remediation pending re-review. It is not an AGREE. Receipt `20260926-145009-ct` was DISAGREE 97/100 on tip `a26cbdbe` and left that residual open. Commit `2b70dcbe` and PR #60 claim the unmeasurable-split remediation and say the claim is not an AGREE. Merge `ad1941a` has the words "AGREE 98" in the subject line. No Astra receipt on `2b70dcbe` or `ad1941a` was found that closes P2-12. The receipted Astra AGREE 98 remains `20260926-122217-ct` on tip `24416845`, which predates P2-12. This status does not convert the merge subject into a closure.

The operational Perplexity gate remains pending an API key. Awaiting operator Payton Byrd review. Author: Payton Byrd.
