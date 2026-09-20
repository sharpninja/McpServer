# Self-eval Round 2 — Weighted Context Projection whitepaper

**When:** 2026-09-20 ~04:17–04:20 CT (America/Chicago)  
**Paper:** `whitepaper-weighted-context-projection-v0.1.md` → content bumped to **v0.1.2**  
**Profile load source:** `/home/box/.claude/profile/STANDING-RULES-FROM-MEMORY.md` (mirrored at `/workspace/operator-profile/profile/STANDING-RULES-FROM-MEMORY.md`). Receipt: `profile-load-round2.txt` (appended same pattern as round1). Full 19-file add-profile **not** on box; PAYTON-LEGION2 disconnected. Profile content **not** published.

Standing rules re-applied under hostile eyes: accuracy-first with receipts; no invented metrics; label assumptions vs facts; success = no operator re-paste; Hostile AGREE ≥98% accuracy AND completeness before done claims; never publish add-profile.

---

## Accuracy / overclaim issues found / fixed

| Finding | Fix |
| --- | --- |
| Exec summary “fail less often from raw model stupidity” | Reframed as operational observation (not measured benchmark); removed casual dig |
| CLI “force no-resume / fresh-session flags” sounded factual | Marked **(assumption)** — exact flags CLI-specific; verify in Phase 2 |
| Continuous reweight “is what makes compact unnecessary” | Relabeled **design intent until Phase 2 demonstrates** |
| Comparison table WCP row read as empirical | Marked cells **(design)** / Option C **design target; not yet measured** |
| PACE “closest research cousin” | Labeled **(author judgment, not a citation rank claim)** |
| Option C “preserving subscription economics” | Softened to fair-use-compatible wording; verdict tied to §12 approve/reject |
| “eight plugins” | Anchored to **MCP-MEMORY-002 ship scope** |
| Non-claims omitted HV / design-vs-data | Extended: no completed Perplexity HV; §9 design-target ≠ empirical |

Round-1 accuracy fixes (HV glossary, `sessionlog_*` extension, hook matrix, memory verb alias, IDs, author) **verified still present**.

## Actionability / completeness issues found / fixed

| Finding | Fix |
| --- | --- |
| No Phase 2 pass/fail gate | Added **§14.1 Phase 2 spike acceptance checklist** (fresh flags, budget, SessionLog append, reweight, re-admit demo, no estimator claim, continuation) |
| Out-of-scope only thin in README | Added **§14.2 Out of scope for v0.1.x** in the paper (learned scorer, eight-CLI matrix, ToS opinion, provider-metered A/B, migrations, invented papers, fake HV done, public profile publish) |
| Recommendations not 1:1 with Roadmap | Rewrote §13 as **R0–R4 ↔ Phase 0–4** table + cross-cutting constraints; Option B called out as parallel path |
| Soft weasel / done-adjacent language | Removed or labeled; no done claim for HV or spike |

## Residual risks

- Perplexity HV **still blocked** on API key after reseed — **not done**.
- Full 19-file profile restore still pending Legion reconnect.
- Exact Claude/Grok fresh-session flag names remain unverified until spike.
- Hostile AGREE ≥98% on the *implemented system* is out of scope for a design whitepaper; for **this document’s** accuracy+completeness after two rounds: Round-2 pass addresses the operator-requested self-eval gates. Implementation remains unproven.

## Verdict

**Improved and ready for operator review as draft v0.1.2.** Round-2 hostile pass removed remaining soft overclaims, added Phase 2 acceptance + Out of scope, and aligned Recommendations 1:1 with Roadmap. **Do not** treat as Hostile Validation complete; **do not** claim Perplexity HV was run; **do not** claim estimator savings.
