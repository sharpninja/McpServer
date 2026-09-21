# Weighted Context Projection — deliverable index

Operator-facing index for the whitepaper on escaping host auto-compaction via SessionLog, weighted projection, MCP memory, and sessionless frontier CLI oneshots.

## Documents

| File | Description |
| --- | --- |
| [whitepaper-weighted-context-projection-v0.1.md](./whitepaper-weighted-context-projection-v0.1.md) | **Design only** (**v0.1.9** content; filename retained). Problem, architecture, scoring model, projection algorithm, related work, evaluation method, glossary, references |
| [proposed-implementation-weighted-context-projection-v0.1.md](./proposed-implementation-weighted-context-projection-v0.1.md) | **Proposed implementation** (v0.1.3). Deployment options, code-grounded corrections against `main` @ `e7c43a12`, sealed-projection design, recommendations, roadmap and phase gates, next actions, open questions. Replaces `implementation-recommendations-v0.1.md` |
| [self-eval-round1.md](./self-eval-round1.md) | Round-1 accuracy + actionability self-eval receipt |
| [self-eval-round2.md](./self-eval-round2.md) | Round-2 hostile-pass self-eval receipt |

Standing-rules load receipts are deliberately **not** in this folder — see the profile note below.

**Read order.** The whitepaper argues the design and decides nothing operational. The implementation proposal is where deployment options, phases, exit gates, and everything grounded in the current code live — including two findings that were asserted and then retracted, kept in place so the error stays auditable. Cross-references use `WP §N` for the whitepaper and `IMPL §N` for the proposal.

## One-paragraph abstract

Long-horizon agent friction is **compaction / context loss**, not whitespace-estimator “savings.” The MCP memory layer holds durable cross-session facts — eleven shipped tools spanning a typed write path (`memory_remember`), meaning-ranked recall, exploration, promotion, consolidation, revert, and a compatibility CRUD surface — but it is not a turn ledger. This design **extends** existing MCP `sessionlog_*` with weight / pin / projection metadata, scores each turn by **relative weight for advancing current work**, and assembles a budgeted **`contextProjection`** (expand / summarize / omit) with **re-admit** from the full log. Prefer an outer orchestrator + **sessionless CLI oneshots** (Option C) so model-facing context is owned externally; keep plugin PreCompact as fallback on hook-rich hosts. Do not treat `memory-bench-whitespace` as provider-metered proof. Success = task continuity without operator re-paste.

## Self-eval / profile notes

- Standing rules restored from durable memory only (`STANDING-RULES-FROM-MEMORY.md`, not tracked in this repo). Full 19-file `add-profile` **not** on box; PAYTON-LEGION2 disconnected. **Never publish** profile files publicly — the round-1/round-2 load receipts are intentionally absent from this folder for that reason.
- Two self-eval rounds completed (receipts above, against v0.1.2). Paper version now **v0.1.9** after five external review passes (PRs #53, #54, and four rounds on PR #55); the self-evals predate those fixes.
- **Perplexity Hostile Validation (HV) is still blocked on API key** after box reseed — **not done**. Do not claim HV was completed.

## Review checklist (for tomorrow)

- [ ] Approve/reject Option C as primary Phase 2 spike (Claude or Grok CLI oneshot)
- [ ] Approve Phase 1: extend `sessionlog_*` vs new store
- [ ] **Correct the `sessionlog_delete_turn` description only, and keep the "Irreversible" warning on `sessionlog_delete_session`** — `delete_turn` wrongly says child rows are removed when they are tombstoned. The `delete_session` warning stays: no `sessionlog_restore` tool exists, revival is only a side effect of resubmitting the same session, and a deleted turn has no revival path at all, so the call really is irreversible to its caller. Physical-delete blocking also holds only on the tracked save path, and the bulk tombstone/revival paths append no audit rows (proposed implementation §3.2, §3.2.1, §3.2.2). An earlier version of this checklist item had this backwards
- [ ] **Decide whether Option B/C is still exclusive,** given `Microsoft.Agents.AI` is already referenced and QBAgent already drives an `IChatClient` (§4.2)
- [ ] Confirm success metric: zero operator re-paste after reproject
- [ ] Note Perplexity HV still pending API key
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
- Claiming Perplexity HV complete while key missing

## Status

Draft — whitepaper **v0.1.9**, proposed implementation **v0.1.3** — 2026-09-20 (America/Chicago). Awaiting operator Payton Byrd review. Author: Payton Byrd.
