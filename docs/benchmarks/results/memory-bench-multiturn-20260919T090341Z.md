# Memory bench multi-turn token summary

v2 is the real efficiency bench. v1 (`memory-prompt-pack-v1`) remains smoke/regression.
Primary metric: tokens used. A job succeeds only if every required query turn passes.
Failed jobs appear in the artifact with pass=false and are excluded from the success-gated token means. Do not claim an efficiency win from failed without_memory runs.

## Success-gated token means (highlighted)

**SUCCESS-GATED mean tokens_total:** with_memory=131.8 without_memory=243.2
**SUCCESS-GATED median tokens_total:** with_memory=94 without_memory=168
**Paired-success mean tokens_total** (jobs that passed both conditions): with_memory=131.8 without_memory=243.2
with_memory won on tokens among paired successes: yes

## Success rates

jobs_n=5 with_memory=5/5 (1) without_memory=5/5 (1) paired_successes=5

## Per-job tokens

| plugin | job_id | condition | pass | tokens_in | tokens_out | tokens_total | token_source | failed_turns |
|---|---|---|---|---|---|---|---|---|
| grok | JOB-DEC-001 | with_memory | true | 69 | 25 | 94 | estimator |  |
| grok | JOB-DEC-001 | without_memory | true | 120 | 48 | 168 | estimator |  |
| grok | JOB-FACT-001 | with_memory | true | 61 | 32 | 93 | estimator |  |
| grok | JOB-FACT-001 | without_memory | true | 100 | 33 | 133 | estimator |  |
| grok | JOB-MULTI-001 | with_memory | true | 133 | 42 | 175 | estimator |  |
| grok | JOB-MULTI-001 | without_memory | true | 374 | 76 | 450 | estimator |  |
| grok | JOB-NEG-001 | with_memory | true | 138 | 67 | 205 | estimator |  |
| grok | JOB-NEG-001 | without_memory | true | 232 | 71 | 303 | estimator |  |
| grok | JOB-PREF-001 | with_memory | true | 65 | 27 | 92 | estimator |  |
| grok | JOB-PREF-001 | without_memory | true | 118 | 44 | 162 | estimator |  |

## Per-turn tokens

| plugin | job_id | turn_id | role | condition | required | pass | tokens_in | tokens_out | tokens_total | token_source |
|---|---|---|---|---|---|---|---|---|---|---|
| grok | JOB-PREF-001 | EST-001 | establish | without_memory | false | true | 39 | 30 | 69 | estimator |
| grok | JOB-PREF-001 | QRY-001 | query | without_memory | true | true | 79 | 14 | 93 | estimator |
| grok | JOB-PREF-001 | EST-001 | establish | with_memory | false | true | 42 | 15 | 57 | estimator |
| grok | JOB-PREF-001 | QRY-001 | query | with_memory | true | true | 23 | 12 | 35 | estimator |
| grok | JOB-DEC-001 | EST-001 | establish | without_memory | false | true | 39 | 30 | 69 | estimator |
| grok | JOB-DEC-001 | QRY-001 | query | without_memory | true | true | 81 | 18 | 99 | estimator |
| grok | JOB-DEC-001 | EST-001 | establish | with_memory | false | true | 42 | 13 | 55 | estimator |
| grok | JOB-DEC-001 | QRY-001 | query | with_memory | true | true | 27 | 12 | 39 | estimator |
| grok | JOB-FACT-001 | EST-001 | establish | without_memory | false | true | 37 | 17 | 54 | estimator |
| grok | JOB-FACT-001 | QRY-001 | query | without_memory | true | true | 63 | 16 | 79 | estimator |
| grok | JOB-FACT-001 | EST-001 | establish | with_memory | false | true | 40 | 18 | 58 | estimator |
| grok | JOB-FACT-001 | QRY-001 | query | with_memory | true | true | 21 | 14 | 35 | estimator |
| grok | JOB-MULTI-001 | EST-001 | establish | without_memory | false | true | 27 | 18 | 45 | estimator |
| grok | JOB-MULTI-001 | EST-002 | establish | without_memory | false | true | 73 | 20 | 93 | estimator |
| grok | JOB-MULTI-001 | EST-003 | establish | without_memory | false | true | 121 | 14 | 135 | estimator |
| grok | JOB-MULTI-001 | QRY-001 | query | without_memory | true | true | 153 | 24 | 177 | estimator |
| grok | JOB-MULTI-001 | EST-001 | establish | with_memory | false | true | 30 | 8 | 38 | estimator |
| grok | JOB-MULTI-001 | EST-002 | establish | with_memory | false | true | 30 | 5 | 35 | estimator |
| grok | JOB-MULTI-001 | EST-003 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| grok | JOB-MULTI-001 | QRY-001 | query | with_memory | true | true | 43 | 22 | 65 | estimator |
| grok | JOB-NEG-001 | EST-001 | establish | without_memory | false | true | 37 | 33 | 70 | estimator |
| grok | JOB-NEG-001 | QRY-001 | query | without_memory | true | true | 82 | 19 | 101 | estimator |
| grok | JOB-NEG-001 | QRY-002 | query | without_memory | true | true | 113 | 19 | 132 | estimator |
| grok | JOB-NEG-001 | EST-001 | establish | with_memory | false | true | 48 | 24 | 72 | estimator |
| grok | JOB-NEG-001 | QRY-001 | query | with_memory | true | true | 45 | 20 | 65 | estimator |
| grok | JOB-NEG-001 | QRY-002 | query | with_memory | true | true | 45 | 23 | 68 | estimator |

## Live subject

- plugin: grok
- mode: live
- pack_role: v2 real efficiency bench
- subject: cursor-grok-4.6-high-fast (Cursor cloud subscription context)
- XAI_API_KEY: not used
- adapter: MemoryBenchSubscriptionGrokMultiTurnAdapter (not MemoryBenchGrokMultiTurnAdapter fixtures)
- token_source: estimator (`memory-bench-whitespace` / `1.0.0`) because the host did not report usage
- h7a-value-gate.json: left `agree=false` (policy placeholder; this run is evidence, not hostile AGREE)
- Evidence note: all 5 jobs passed under both conditions. Success-gated mean tokens_total was 131.8 with_memory vs 243.2 without_memory (with_memory won among paired successes). H7a stays false because plugin rollout still needs a hostile/operator AGREE receipt, not only this subscription-context run.
