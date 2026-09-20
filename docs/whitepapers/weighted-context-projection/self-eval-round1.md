# Self-eval Round 1 — Weighted Context Projection whitepaper

**When:** 2026-09-20 ~04:16–04:20 CT (America/Chicago)  
**Paper:** `whitepaper-weighted-context-projection-v0.1.md` → content bumped to **v0.1.1**  
**Profile load source:** `/home/box/.claude/profile/STANDING-RULES-FROM-MEMORY.md` (mirrored at `/workspace/operator-profile/profile/STANDING-RULES-FROM-MEMORY.md`). Receipt: `profile-load-round1.txt`. Full 19-file add-profile **not** on box; PAYTON-LEGION2 disconnected. Profile content **not** published.

Standing rules applied: accuracy-first with receipts; no invented metrics; label assumptions vs facts; success = task continuity without operator re-paste; Hostile AGREE needs ≥98% accuracy AND completeness before done claims; never publish add-profile publicly.

---

## Accuracy issues found / fixed

| Finding | Fix |
| --- | --- |
| Glossary **HV** wrongly expanded as “High-visibility / high-value” | Corrected to **Hostile Validation** (with explicit negation of the wrong expansion) |
| SessionLog read as possibly greenfield (“proposed / extend”) | Clarified MCP already has **`sessionlog_*` APIs**; design **extends** that ledger with weight/pin/projection metadata |
| Plugin hooks understated / incomplete host matrix | Stated Claude / Grok / Copilot are hook-richer than Codex / Cline / OpenCode; hooks inject/react, do **not** own full transcript |
| `memory_remember` used without alias clarity | Prefer shipped verbs `remember` / `recall` / `explore` / `consolidate` / `promote`; label `memory_remember` as bridge/API alias |
| Document id used WHITEPAPER-… slug; README linked wrong casing | Document field now points at filename `whitepaper-weighted-context-projection-v0.1.md`; version **v0.1.1** |
| Author field missing (audience only) | Added **Author: Payton Byrd** |
| Option C / MAF / literature / IDs MCP-MEMORY-002 + PLAN-MANAGER-MEMORY-UI-001 | Already largely correct; left intact; strengthened subscription fair-use wording slightly |

**Not invented:** No new papers; arXiv / ACL / Haystack / Praison / MAF links unchanged. Estimator bench remains `memory-bench-whitespace` (whitespace/estimator multiturn bench); still **not** provider-metered proof.

## Actionability issues found / fixed

| Finding | Fix |
| --- | --- |
| No operator “do this tomorrow” checklist | Added **§12 Immediate next actions (operator review)** with approve/reject Option C, SessionLog extend-vs-new, success metric, Perplexity HV pending API key, add-profile restore note, ID confirmation |
| Phase 1 exit criteria vague | Concrete schema field list matching §4.2; simulator inputs/outputs specified; prefer extend `sessionlog_*` |
| Section numbering | Recommendations→13, Roadmap→14, Glossary→15, References→16 |

## Residual risks

- Perplexity HV **not** run (API key missing post-reseed)—do not claim HV complete.
- Full 19-file profile still absent until Legion reconnects; standing-rules-only load may miss prefs not restored to memory.
- Phase 2 CLI flag names for “fresh / no-resume” remain product-specific **(assumption until spike)**—not asserted as measured.
- Hostile AGREE ≥98% accuracy+completeness **not** claimed after Round 1 alone; Round 2 required.

## Verdict

**Improved — still needs Round 2.** Accuracy blockers (HV glossary, SessionLog greenfield implication, hook matrix) fixed; actionability checklist and Phase 1 concreteness added. Soft language, Phase 2 acceptance checklist, Out-of-scope block, and Recommendations↔Roadmap 1:1 mapping still need a second hostile pass before any done claim.
