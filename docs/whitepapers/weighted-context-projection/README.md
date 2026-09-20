# Weighted Context Projection — deliverable index

Operator-facing index for the whitepaper on escaping host auto-compaction via SessionLog, weighted projection, MCP memory, and sessionless frontier CLI oneshots.

## Documents

| File | Description |
| --- | --- |
| [whitepaper-weighted-context-projection-v0.1.md](./whitepaper-weighted-context-projection-v0.1.md) | Full technical whitepaper (**v0.1.4** content; filename retained) |
| [implementation-recommendations-v0.1.md](./implementation-recommendations-v0.1.md) | Advisory: design read against `main` @ `e7c43a12` — corrections, existing capability, phasing |
| [self-eval-round1.md](./self-eval-round1.md) | Round-1 accuracy + actionability self-eval receipt |
| [self-eval-round2.md](./self-eval-round2.md) | Round-2 hostile-pass self-eval receipt |

Standing-rules load receipts are deliberately **not** in this folder — see the profile note below.

## One-paragraph abstract

Long-horizon agent friction is **compaction / context loss**, not whitespace-estimator “savings.” MCP-MEMORY-002 holds durable cross-session facts (shipped tool surface: `memory_add` / `memory_get` / `memory_list` / `memory_update` / `memory_remove`); it is not a turn ledger. This design **extends** existing MCP `sessionlog_*` with weight / pin / projection metadata, scores each turn by **relative weight for advancing current work**, and assembles a budgeted **`contextProjection`** (expand / summarize / omit) with **re-admit** from the full log. Prefer an outer orchestrator + **sessionless CLI oneshots** (Option C) so model-facing context is owned externally; keep plugin PreCompact as fallback on hook-rich hosts. Do not treat `memory-bench-whitespace` as provider-metered proof. Success = task continuity without operator re-paste.

## Self-eval / profile notes

- Standing rules restored from durable memory only (`STANDING-RULES-FROM-MEMORY.md`, not tracked in this repo). Full 19-file `add-profile` **not** on box; PAYTON-LEGION2 disconnected. **Never publish** profile files publicly — the round-1/round-2 load receipts are intentionally absent from this folder for that reason.
- Two self-eval rounds completed (receipts above, against v0.1.2). Paper version now **v0.1.4** after the two external review passes (PRs #53, #54); the self-evals predate those fixes.
- **Perplexity Hostile Validation (HV) is still blocked on API key** after box reseed — **not done**. Do not claim HV was completed.

## Review checklist (for tomorrow)

- [ ] Approve/reject Option C as primary Phase 2 spike (Claude or Grok CLI oneshot)
- [ ] Approve Phase 1: extend `sessionlog_*` vs new store
- [ ] **Correct the `sessionlog_*` delete tool descriptions** — they say "Irreversible" but `McpDbContext` blocks physical deletes and mirrors mutations to the append-only audit ledger (implementation recommendations §3.2)
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

Draft **v0.1.4** — 2026-09-20 (America/Chicago). Awaiting operator Payton Byrd review. Author: Payton Byrd.
