# Hostile validation receipt — QBrain.AI rebrand plan

- Stamp: 20261005T154611Z
- Subject: `docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md`
- Subject SHA-256 at receipt time: `5ba31fc11d32ba9ce7c0e19f99a30136f4c85d14de706b352d4f12e46431ccaf`
- Claim list: `docs/receipts/hv/20261005T154611Z-qbrain-ai-rebrand-plan.prompt.md`
- Requested model: `gpt-6-astra`
- Requested effort: `xhigh`
- Operator shorthand: `astra-6-xhigh`
- Runner: Cursor cloud agent on this VM
- Required live host: PAYTON-LEGION2

## Verdict

- OverallVerdict: **NOT RUN**
- Accuracy: **not scored**
- Completeness: **not scored**
- AGREE: **no**
- DISAGREE: **no** (a disagreement would require the locked model to score the claims)
- Phase 0 done: **no**

This file is a blocker receipt and a template for the Legion run. It is not a hostile-validation agreement.

## Why the live run did not happen

The cloud VM has no Codex CLI and no Astra entry point. Checked in this session:

- `command -v codex` and `command -v astra` did not resolve a program.
- `/usr/local/bin` has no `codex` binary.
- A filesystem search under `/home`, `/opt`, and `/usr/local` found no Codex or Astra executable. The only `codex` hits were unrelated plugin cache paths named `.codex`.
- `$HOME/.codex` is absent, so there is no rollout jsonl whose `turn_context` can show `gpt-6-astra` / `xhigh`.

Prior agreements that did use `gpt-6-astra` at `xhigh` were Codex sessions on PAYTON-LEGION2, for example `docs/receipts/hv/hostile-validator-setupprompt-overall-20260928T153330Z.md`. This VM cannot repeat that path.

The live Astra run has to be done on Legion. Launch Codex there as `gpt-6-astra` with reasoning effort `xhigh`, give it the claim list above, and write the filled template to a new file under `docs/receipts/hv/`. Keep the request and response jsonl. Until that receipt exists and says OverallVerdict AGREE, Phase 0 of the plan is not done, and Phase 1 stays unapproved.

## Unfilled template (Legion fills this; do not treat it as the verdict)

- TimestampUtc: `<utc>`
- ValidatorIdentity: Codex
- Model: gpt-6-astra
- Effort: xhigh
- Proof: `<rollout jsonl path and turn_context timestamp>`
- SubjectSha256: `<hash computed on Legion>`
- OverallVerdict: `<AGREE or DISAGREE>`
- Accuracy: `<0-100>`
- Completeness: `<0-100>`
- PASS / FAIL / UNKNOWN: `<counts>`
- C0 through C9: `<PASS, FAIL, or UNKNOWN, with the plan heading used>`
