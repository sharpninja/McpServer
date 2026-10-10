# Memory bench multi-turn token summary

v2 is the real efficiency bench. v1 (`memory-prompt-pack-v1`) remains smoke/regression.
Primary metric: tokens used. A job succeeds only if every required query turn passes.
Failed jobs appear in the artifact with pass=false and are excluded from the success-gated token means. Do not claim an efficiency win from failed without_memory runs.

## Success-gated token means (highlighted)

**SUCCESS-GATED mean tokens_total:** with_memory=118.6 without_memory=189.6
**SUCCESS-GATED median tokens_total:** with_memory=91 without_memory=129
**Paired-success mean tokens_total** (jobs that passed both conditions): with_memory=118.6 without_memory=189.6
with_memory won on tokens among paired successes: yes

## Success rates

jobs_n=40 with_memory=40/40 (1) without_memory=40/40 (1) paired_successes=40

## Per-job tokens

| plugin | job_id | condition | pass | tokens_in | tokens_out | tokens_total | token_source | failed_turns |
|---|---|---|---|---|---|---|---|---|
| claude-code | JOB-DEC-001 | with_memory | true | 69 | 22 | 91 | estimator |  |
| claude-code | JOB-DEC-001 | without_memory | true | 102 | 24 | 126 | estimator |  |
| claude-code | JOB-FACT-001 | with_memory | true | 61 | 19 | 80 | estimator |  |
| claude-code | JOB-FACT-001 | without_memory | true | 95 | 23 | 118 | estimator |  |
| claude-code | JOB-MULTI-001 | with_memory | true | 133 | 35 | 168 | estimator |  |
| claude-code | JOB-MULTI-001 | without_memory | true | 311 | 33 | 344 | estimator |  |
| claude-code | JOB-NEG-001 | with_memory | true | 138 | 36 | 174 | estimator |  |
| claude-code | JOB-NEG-001 | without_memory | true | 190 | 41 | 231 | estimator |  |
| claude-code | JOB-PREF-001 | with_memory | true | 65 | 15 | 80 | estimator |  |
| claude-code | JOB-PREF-001 | without_memory | true | 103 | 26 | 129 | estimator |  |
| claude-cowork | JOB-DEC-001 | with_memory | true | 69 | 22 | 91 | estimator |  |
| claude-cowork | JOB-DEC-001 | without_memory | true | 102 | 24 | 126 | estimator |  |
| claude-cowork | JOB-FACT-001 | with_memory | true | 61 | 19 | 80 | estimator |  |
| claude-cowork | JOB-FACT-001 | without_memory | true | 95 | 23 | 118 | estimator |  |
| claude-cowork | JOB-MULTI-001 | with_memory | true | 133 | 35 | 168 | estimator |  |
| claude-cowork | JOB-MULTI-001 | without_memory | true | 311 | 33 | 344 | estimator |  |
| claude-cowork | JOB-NEG-001 | with_memory | true | 138 | 36 | 174 | estimator |  |
| claude-cowork | JOB-NEG-001 | without_memory | true | 190 | 41 | 231 | estimator |  |
| claude-cowork | JOB-PREF-001 | with_memory | true | 65 | 15 | 80 | estimator |  |
| claude-cowork | JOB-PREF-001 | without_memory | true | 103 | 26 | 129 | estimator |  |
| cline | JOB-DEC-001 | with_memory | true | 69 | 22 | 91 | estimator |  |
| cline | JOB-DEC-001 | without_memory | true | 102 | 24 | 126 | estimator |  |
| cline | JOB-FACT-001 | with_memory | true | 61 | 19 | 80 | estimator |  |
| cline | JOB-FACT-001 | without_memory | true | 95 | 23 | 118 | estimator |  |
| cline | JOB-MULTI-001 | with_memory | true | 133 | 35 | 168 | estimator |  |
| cline | JOB-MULTI-001 | without_memory | true | 311 | 33 | 344 | estimator |  |
| cline | JOB-NEG-001 | with_memory | true | 138 | 36 | 174 | estimator |  |
| cline | JOB-NEG-001 | without_memory | true | 190 | 41 | 231 | estimator |  |
| cline | JOB-PREF-001 | with_memory | true | 65 | 15 | 80 | estimator |  |
| cline | JOB-PREF-001 | without_memory | true | 103 | 26 | 129 | estimator |  |
| cline-v2 | JOB-DEC-001 | with_memory | true | 69 | 22 | 91 | estimator |  |
| cline-v2 | JOB-DEC-001 | without_memory | true | 102 | 24 | 126 | estimator |  |
| cline-v2 | JOB-FACT-001 | with_memory | true | 61 | 19 | 80 | estimator |  |
| cline-v2 | JOB-FACT-001 | without_memory | true | 95 | 23 | 118 | estimator |  |
| cline-v2 | JOB-MULTI-001 | with_memory | true | 133 | 35 | 168 | estimator |  |
| cline-v2 | JOB-MULTI-001 | without_memory | true | 311 | 33 | 344 | estimator |  |
| cline-v2 | JOB-NEG-001 | with_memory | true | 138 | 36 | 174 | estimator |  |
| cline-v2 | JOB-NEG-001 | without_memory | true | 190 | 41 | 231 | estimator |  |
| cline-v2 | JOB-PREF-001 | with_memory | true | 65 | 15 | 80 | estimator |  |
| cline-v2 | JOB-PREF-001 | without_memory | true | 103 | 26 | 129 | estimator |  |
| codex | JOB-DEC-001 | with_memory | true | 69 | 22 | 91 | estimator |  |
| codex | JOB-DEC-001 | without_memory | true | 102 | 24 | 126 | estimator |  |
| codex | JOB-FACT-001 | with_memory | true | 61 | 19 | 80 | estimator |  |
| codex | JOB-FACT-001 | without_memory | true | 95 | 23 | 118 | estimator |  |
| codex | JOB-MULTI-001 | with_memory | true | 133 | 35 | 168 | estimator |  |
| codex | JOB-MULTI-001 | without_memory | true | 311 | 33 | 344 | estimator |  |
| codex | JOB-NEG-001 | with_memory | true | 138 | 36 | 174 | estimator |  |
| codex | JOB-NEG-001 | without_memory | true | 190 | 41 | 231 | estimator |  |
| codex | JOB-PREF-001 | with_memory | true | 65 | 15 | 80 | estimator |  |
| codex | JOB-PREF-001 | without_memory | true | 103 | 26 | 129 | estimator |  |
| copilot | JOB-DEC-001 | with_memory | true | 69 | 22 | 91 | estimator |  |
| copilot | JOB-DEC-001 | without_memory | true | 102 | 24 | 126 | estimator |  |
| copilot | JOB-FACT-001 | with_memory | true | 61 | 19 | 80 | estimator |  |
| copilot | JOB-FACT-001 | without_memory | true | 95 | 23 | 118 | estimator |  |
| copilot | JOB-MULTI-001 | with_memory | true | 133 | 35 | 168 | estimator |  |
| copilot | JOB-MULTI-001 | without_memory | true | 311 | 33 | 344 | estimator |  |
| copilot | JOB-NEG-001 | with_memory | true | 138 | 36 | 174 | estimator |  |
| copilot | JOB-NEG-001 | without_memory | true | 190 | 41 | 231 | estimator |  |
| copilot | JOB-PREF-001 | with_memory | true | 65 | 15 | 80 | estimator |  |
| copilot | JOB-PREF-001 | without_memory | true | 103 | 26 | 129 | estimator |  |
| grok | JOB-DEC-001 | with_memory | true | 69 | 22 | 91 | estimator |  |
| grok | JOB-DEC-001 | without_memory | true | 102 | 24 | 126 | estimator |  |
| grok | JOB-FACT-001 | with_memory | true | 61 | 19 | 80 | estimator |  |
| grok | JOB-FACT-001 | without_memory | true | 95 | 23 | 118 | estimator |  |
| grok | JOB-MULTI-001 | with_memory | true | 133 | 35 | 168 | estimator |  |
| grok | JOB-MULTI-001 | without_memory | true | 311 | 33 | 344 | estimator |  |
| grok | JOB-NEG-001 | with_memory | true | 138 | 36 | 174 | estimator |  |
| grok | JOB-NEG-001 | without_memory | true | 190 | 41 | 231 | estimator |  |
| grok | JOB-PREF-001 | with_memory | true | 65 | 15 | 80 | estimator |  |
| grok | JOB-PREF-001 | without_memory | true | 103 | 26 | 129 | estimator |  |
| opencode | JOB-DEC-001 | with_memory | true | 69 | 22 | 91 | estimator |  |
| opencode | JOB-DEC-001 | without_memory | true | 102 | 24 | 126 | estimator |  |
| opencode | JOB-FACT-001 | with_memory | true | 61 | 19 | 80 | estimator |  |
| opencode | JOB-FACT-001 | without_memory | true | 95 | 23 | 118 | estimator |  |
| opencode | JOB-MULTI-001 | with_memory | true | 133 | 35 | 168 | estimator |  |
| opencode | JOB-MULTI-001 | without_memory | true | 311 | 33 | 344 | estimator |  |
| opencode | JOB-NEG-001 | with_memory | true | 138 | 36 | 174 | estimator |  |
| opencode | JOB-NEG-001 | without_memory | true | 190 | 41 | 231 | estimator |  |
| opencode | JOB-PREF-001 | with_memory | true | 65 | 15 | 80 | estimator |  |
| opencode | JOB-PREF-001 | without_memory | true | 103 | 26 | 129 | estimator |  |

## Per-turn tokens

| plugin | job_id | turn_id | role | condition | required | pass | tokens_in | tokens_out | tokens_total | token_source |
|---|---|---|---|---|---|---|---|---|---|---|
| grok | JOB-PREF-001 | EST-001 | establish | without_memory | false | true | 39 | 15 | 54 | estimator |
| grok | JOB-PREF-001 | QRY-001 | query | without_memory | true | true | 64 | 11 | 75 | estimator |
| grok | JOB-PREF-001 | EST-001 | establish | with_memory | false | true | 42 | 9 | 51 | estimator |
| grok | JOB-PREF-001 | QRY-001 | query | with_memory | true | true | 23 | 6 | 29 | estimator |
| grok | JOB-DEC-001 | EST-001 | establish | without_memory | false | true | 39 | 12 | 51 | estimator |
| grok | JOB-DEC-001 | QRY-001 | query | without_memory | true | true | 63 | 12 | 75 | estimator |
| grok | JOB-DEC-001 | EST-001 | establish | with_memory | false | true | 42 | 11 | 53 | estimator |
| grok | JOB-DEC-001 | QRY-001 | query | with_memory | true | true | 27 | 11 | 38 | estimator |
| grok | JOB-FACT-001 | EST-001 | establish | without_memory | false | true | 37 | 12 | 49 | estimator |
| grok | JOB-FACT-001 | QRY-001 | query | without_memory | true | true | 58 | 11 | 69 | estimator |
| grok | JOB-FACT-001 | EST-001 | establish | with_memory | false | true | 40 | 11 | 51 | estimator |
| grok | JOB-FACT-001 | QRY-001 | query | with_memory | true | true | 21 | 8 | 29 | estimator |
| grok | JOB-MULTI-001 | EST-001 | establish | without_memory | false | true | 27 | 7 | 34 | estimator |
| grok | JOB-MULTI-001 | EST-002 | establish | without_memory | false | true | 62 | 8 | 70 | estimator |
| grok | JOB-MULTI-001 | EST-003 | establish | without_memory | false | true | 98 | 8 | 106 | estimator |
| grok | JOB-MULTI-001 | QRY-001 | query | without_memory | true | true | 124 | 10 | 134 | estimator |
| grok | JOB-MULTI-001 | EST-001 | establish | with_memory | false | true | 30 | 6 | 36 | estimator |
| grok | JOB-MULTI-001 | EST-002 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| grok | JOB-MULTI-001 | EST-003 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| grok | JOB-MULTI-001 | QRY-001 | query | with_memory | true | true | 43 | 15 | 58 | estimator |
| grok | JOB-NEG-001 | EST-001 | establish | without_memory | false | true | 37 | 15 | 52 | estimator |
| grok | JOB-NEG-001 | QRY-001 | query | without_memory | true | true | 64 | 13 | 77 | estimator |
| grok | JOB-NEG-001 | QRY-002 | query | without_memory | true | true | 89 | 13 | 102 | estimator |
| grok | JOB-NEG-001 | EST-001 | establish | with_memory | false | true | 48 | 10 | 58 | estimator |
| grok | JOB-NEG-001 | QRY-001 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| grok | JOB-NEG-001 | QRY-002 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| claude-code | JOB-PREF-001 | EST-001 | establish | without_memory | false | true | 39 | 15 | 54 | estimator |
| claude-code | JOB-PREF-001 | QRY-001 | query | without_memory | true | true | 64 | 11 | 75 | estimator |
| claude-code | JOB-PREF-001 | EST-001 | establish | with_memory | false | true | 42 | 9 | 51 | estimator |
| claude-code | JOB-PREF-001 | QRY-001 | query | with_memory | true | true | 23 | 6 | 29 | estimator |
| claude-code | JOB-DEC-001 | EST-001 | establish | without_memory | false | true | 39 | 12 | 51 | estimator |
| claude-code | JOB-DEC-001 | QRY-001 | query | without_memory | true | true | 63 | 12 | 75 | estimator |
| claude-code | JOB-DEC-001 | EST-001 | establish | with_memory | false | true | 42 | 11 | 53 | estimator |
| claude-code | JOB-DEC-001 | QRY-001 | query | with_memory | true | true | 27 | 11 | 38 | estimator |
| claude-code | JOB-FACT-001 | EST-001 | establish | without_memory | false | true | 37 | 12 | 49 | estimator |
| claude-code | JOB-FACT-001 | QRY-001 | query | without_memory | true | true | 58 | 11 | 69 | estimator |
| claude-code | JOB-FACT-001 | EST-001 | establish | with_memory | false | true | 40 | 11 | 51 | estimator |
| claude-code | JOB-FACT-001 | QRY-001 | query | with_memory | true | true | 21 | 8 | 29 | estimator |
| claude-code | JOB-MULTI-001 | EST-001 | establish | without_memory | false | true | 27 | 7 | 34 | estimator |
| claude-code | JOB-MULTI-001 | EST-002 | establish | without_memory | false | true | 62 | 8 | 70 | estimator |
| claude-code | JOB-MULTI-001 | EST-003 | establish | without_memory | false | true | 98 | 8 | 106 | estimator |
| claude-code | JOB-MULTI-001 | QRY-001 | query | without_memory | true | true | 124 | 10 | 134 | estimator |
| claude-code | JOB-MULTI-001 | EST-001 | establish | with_memory | false | true | 30 | 6 | 36 | estimator |
| claude-code | JOB-MULTI-001 | EST-002 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| claude-code | JOB-MULTI-001 | EST-003 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| claude-code | JOB-MULTI-001 | QRY-001 | query | with_memory | true | true | 43 | 15 | 58 | estimator |
| claude-code | JOB-NEG-001 | EST-001 | establish | without_memory | false | true | 37 | 15 | 52 | estimator |
| claude-code | JOB-NEG-001 | QRY-001 | query | without_memory | true | true | 64 | 13 | 77 | estimator |
| claude-code | JOB-NEG-001 | QRY-002 | query | without_memory | true | true | 89 | 13 | 102 | estimator |
| claude-code | JOB-NEG-001 | EST-001 | establish | with_memory | false | true | 48 | 10 | 58 | estimator |
| claude-code | JOB-NEG-001 | QRY-001 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| claude-code | JOB-NEG-001 | QRY-002 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| claude-cowork | JOB-PREF-001 | EST-001 | establish | without_memory | false | true | 39 | 15 | 54 | estimator |
| claude-cowork | JOB-PREF-001 | QRY-001 | query | without_memory | true | true | 64 | 11 | 75 | estimator |
| claude-cowork | JOB-PREF-001 | EST-001 | establish | with_memory | false | true | 42 | 9 | 51 | estimator |
| claude-cowork | JOB-PREF-001 | QRY-001 | query | with_memory | true | true | 23 | 6 | 29 | estimator |
| claude-cowork | JOB-DEC-001 | EST-001 | establish | without_memory | false | true | 39 | 12 | 51 | estimator |
| claude-cowork | JOB-DEC-001 | QRY-001 | query | without_memory | true | true | 63 | 12 | 75 | estimator |
| claude-cowork | JOB-DEC-001 | EST-001 | establish | with_memory | false | true | 42 | 11 | 53 | estimator |
| claude-cowork | JOB-DEC-001 | QRY-001 | query | with_memory | true | true | 27 | 11 | 38 | estimator |
| claude-cowork | JOB-FACT-001 | EST-001 | establish | without_memory | false | true | 37 | 12 | 49 | estimator |
| claude-cowork | JOB-FACT-001 | QRY-001 | query | without_memory | true | true | 58 | 11 | 69 | estimator |
| claude-cowork | JOB-FACT-001 | EST-001 | establish | with_memory | false | true | 40 | 11 | 51 | estimator |
| claude-cowork | JOB-FACT-001 | QRY-001 | query | with_memory | true | true | 21 | 8 | 29 | estimator |
| claude-cowork | JOB-MULTI-001 | EST-001 | establish | without_memory | false | true | 27 | 7 | 34 | estimator |
| claude-cowork | JOB-MULTI-001 | EST-002 | establish | without_memory | false | true | 62 | 8 | 70 | estimator |
| claude-cowork | JOB-MULTI-001 | EST-003 | establish | without_memory | false | true | 98 | 8 | 106 | estimator |
| claude-cowork | JOB-MULTI-001 | QRY-001 | query | without_memory | true | true | 124 | 10 | 134 | estimator |
| claude-cowork | JOB-MULTI-001 | EST-001 | establish | with_memory | false | true | 30 | 6 | 36 | estimator |
| claude-cowork | JOB-MULTI-001 | EST-002 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| claude-cowork | JOB-MULTI-001 | EST-003 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| claude-cowork | JOB-MULTI-001 | QRY-001 | query | with_memory | true | true | 43 | 15 | 58 | estimator |
| claude-cowork | JOB-NEG-001 | EST-001 | establish | without_memory | false | true | 37 | 15 | 52 | estimator |
| claude-cowork | JOB-NEG-001 | QRY-001 | query | without_memory | true | true | 64 | 13 | 77 | estimator |
| claude-cowork | JOB-NEG-001 | QRY-002 | query | without_memory | true | true | 89 | 13 | 102 | estimator |
| claude-cowork | JOB-NEG-001 | EST-001 | establish | with_memory | false | true | 48 | 10 | 58 | estimator |
| claude-cowork | JOB-NEG-001 | QRY-001 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| claude-cowork | JOB-NEG-001 | QRY-002 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| cline | JOB-PREF-001 | EST-001 | establish | without_memory | false | true | 39 | 15 | 54 | estimator |
| cline | JOB-PREF-001 | QRY-001 | query | without_memory | true | true | 64 | 11 | 75 | estimator |
| cline | JOB-PREF-001 | EST-001 | establish | with_memory | false | true | 42 | 9 | 51 | estimator |
| cline | JOB-PREF-001 | QRY-001 | query | with_memory | true | true | 23 | 6 | 29 | estimator |
| cline | JOB-DEC-001 | EST-001 | establish | without_memory | false | true | 39 | 12 | 51 | estimator |
| cline | JOB-DEC-001 | QRY-001 | query | without_memory | true | true | 63 | 12 | 75 | estimator |
| cline | JOB-DEC-001 | EST-001 | establish | with_memory | false | true | 42 | 11 | 53 | estimator |
| cline | JOB-DEC-001 | QRY-001 | query | with_memory | true | true | 27 | 11 | 38 | estimator |
| cline | JOB-FACT-001 | EST-001 | establish | without_memory | false | true | 37 | 12 | 49 | estimator |
| cline | JOB-FACT-001 | QRY-001 | query | without_memory | true | true | 58 | 11 | 69 | estimator |
| cline | JOB-FACT-001 | EST-001 | establish | with_memory | false | true | 40 | 11 | 51 | estimator |
| cline | JOB-FACT-001 | QRY-001 | query | with_memory | true | true | 21 | 8 | 29 | estimator |
| cline | JOB-MULTI-001 | EST-001 | establish | without_memory | false | true | 27 | 7 | 34 | estimator |
| cline | JOB-MULTI-001 | EST-002 | establish | without_memory | false | true | 62 | 8 | 70 | estimator |
| cline | JOB-MULTI-001 | EST-003 | establish | without_memory | false | true | 98 | 8 | 106 | estimator |
| cline | JOB-MULTI-001 | QRY-001 | query | without_memory | true | true | 124 | 10 | 134 | estimator |
| cline | JOB-MULTI-001 | EST-001 | establish | with_memory | false | true | 30 | 6 | 36 | estimator |
| cline | JOB-MULTI-001 | EST-002 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| cline | JOB-MULTI-001 | EST-003 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| cline | JOB-MULTI-001 | QRY-001 | query | with_memory | true | true | 43 | 15 | 58 | estimator |
| cline | JOB-NEG-001 | EST-001 | establish | without_memory | false | true | 37 | 15 | 52 | estimator |
| cline | JOB-NEG-001 | QRY-001 | query | without_memory | true | true | 64 | 13 | 77 | estimator |
| cline | JOB-NEG-001 | QRY-002 | query | without_memory | true | true | 89 | 13 | 102 | estimator |
| cline | JOB-NEG-001 | EST-001 | establish | with_memory | false | true | 48 | 10 | 58 | estimator |
| cline | JOB-NEG-001 | QRY-001 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| cline | JOB-NEG-001 | QRY-002 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| cline-v2 | JOB-PREF-001 | EST-001 | establish | without_memory | false | true | 39 | 15 | 54 | estimator |
| cline-v2 | JOB-PREF-001 | QRY-001 | query | without_memory | true | true | 64 | 11 | 75 | estimator |
| cline-v2 | JOB-PREF-001 | EST-001 | establish | with_memory | false | true | 42 | 9 | 51 | estimator |
| cline-v2 | JOB-PREF-001 | QRY-001 | query | with_memory | true | true | 23 | 6 | 29 | estimator |
| cline-v2 | JOB-DEC-001 | EST-001 | establish | without_memory | false | true | 39 | 12 | 51 | estimator |
| cline-v2 | JOB-DEC-001 | QRY-001 | query | without_memory | true | true | 63 | 12 | 75 | estimator |
| cline-v2 | JOB-DEC-001 | EST-001 | establish | with_memory | false | true | 42 | 11 | 53 | estimator |
| cline-v2 | JOB-DEC-001 | QRY-001 | query | with_memory | true | true | 27 | 11 | 38 | estimator |
| cline-v2 | JOB-FACT-001 | EST-001 | establish | without_memory | false | true | 37 | 12 | 49 | estimator |
| cline-v2 | JOB-FACT-001 | QRY-001 | query | without_memory | true | true | 58 | 11 | 69 | estimator |
| cline-v2 | JOB-FACT-001 | EST-001 | establish | with_memory | false | true | 40 | 11 | 51 | estimator |
| cline-v2 | JOB-FACT-001 | QRY-001 | query | with_memory | true | true | 21 | 8 | 29 | estimator |
| cline-v2 | JOB-MULTI-001 | EST-001 | establish | without_memory | false | true | 27 | 7 | 34 | estimator |
| cline-v2 | JOB-MULTI-001 | EST-002 | establish | without_memory | false | true | 62 | 8 | 70 | estimator |
| cline-v2 | JOB-MULTI-001 | EST-003 | establish | without_memory | false | true | 98 | 8 | 106 | estimator |
| cline-v2 | JOB-MULTI-001 | QRY-001 | query | without_memory | true | true | 124 | 10 | 134 | estimator |
| cline-v2 | JOB-MULTI-001 | EST-001 | establish | with_memory | false | true | 30 | 6 | 36 | estimator |
| cline-v2 | JOB-MULTI-001 | EST-002 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| cline-v2 | JOB-MULTI-001 | EST-003 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| cline-v2 | JOB-MULTI-001 | QRY-001 | query | with_memory | true | true | 43 | 15 | 58 | estimator |
| cline-v2 | JOB-NEG-001 | EST-001 | establish | without_memory | false | true | 37 | 15 | 52 | estimator |
| cline-v2 | JOB-NEG-001 | QRY-001 | query | without_memory | true | true | 64 | 13 | 77 | estimator |
| cline-v2 | JOB-NEG-001 | QRY-002 | query | without_memory | true | true | 89 | 13 | 102 | estimator |
| cline-v2 | JOB-NEG-001 | EST-001 | establish | with_memory | false | true | 48 | 10 | 58 | estimator |
| cline-v2 | JOB-NEG-001 | QRY-001 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| cline-v2 | JOB-NEG-001 | QRY-002 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| codex | JOB-PREF-001 | EST-001 | establish | without_memory | false | true | 39 | 15 | 54 | estimator |
| codex | JOB-PREF-001 | QRY-001 | query | without_memory | true | true | 64 | 11 | 75 | estimator |
| codex | JOB-PREF-001 | EST-001 | establish | with_memory | false | true | 42 | 9 | 51 | estimator |
| codex | JOB-PREF-001 | QRY-001 | query | with_memory | true | true | 23 | 6 | 29 | estimator |
| codex | JOB-DEC-001 | EST-001 | establish | without_memory | false | true | 39 | 12 | 51 | estimator |
| codex | JOB-DEC-001 | QRY-001 | query | without_memory | true | true | 63 | 12 | 75 | estimator |
| codex | JOB-DEC-001 | EST-001 | establish | with_memory | false | true | 42 | 11 | 53 | estimator |
| codex | JOB-DEC-001 | QRY-001 | query | with_memory | true | true | 27 | 11 | 38 | estimator |
| codex | JOB-FACT-001 | EST-001 | establish | without_memory | false | true | 37 | 12 | 49 | estimator |
| codex | JOB-FACT-001 | QRY-001 | query | without_memory | true | true | 58 | 11 | 69 | estimator |
| codex | JOB-FACT-001 | EST-001 | establish | with_memory | false | true | 40 | 11 | 51 | estimator |
| codex | JOB-FACT-001 | QRY-001 | query | with_memory | true | true | 21 | 8 | 29 | estimator |
| codex | JOB-MULTI-001 | EST-001 | establish | without_memory | false | true | 27 | 7 | 34 | estimator |
| codex | JOB-MULTI-001 | EST-002 | establish | without_memory | false | true | 62 | 8 | 70 | estimator |
| codex | JOB-MULTI-001 | EST-003 | establish | without_memory | false | true | 98 | 8 | 106 | estimator |
| codex | JOB-MULTI-001 | QRY-001 | query | without_memory | true | true | 124 | 10 | 134 | estimator |
| codex | JOB-MULTI-001 | EST-001 | establish | with_memory | false | true | 30 | 6 | 36 | estimator |
| codex | JOB-MULTI-001 | EST-002 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| codex | JOB-MULTI-001 | EST-003 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| codex | JOB-MULTI-001 | QRY-001 | query | with_memory | true | true | 43 | 15 | 58 | estimator |
| codex | JOB-NEG-001 | EST-001 | establish | without_memory | false | true | 37 | 15 | 52 | estimator |
| codex | JOB-NEG-001 | QRY-001 | query | without_memory | true | true | 64 | 13 | 77 | estimator |
| codex | JOB-NEG-001 | QRY-002 | query | without_memory | true | true | 89 | 13 | 102 | estimator |
| codex | JOB-NEG-001 | EST-001 | establish | with_memory | false | true | 48 | 10 | 58 | estimator |
| codex | JOB-NEG-001 | QRY-001 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| codex | JOB-NEG-001 | QRY-002 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| copilot | JOB-PREF-001 | EST-001 | establish | without_memory | false | true | 39 | 15 | 54 | estimator |
| copilot | JOB-PREF-001 | QRY-001 | query | without_memory | true | true | 64 | 11 | 75 | estimator |
| copilot | JOB-PREF-001 | EST-001 | establish | with_memory | false | true | 42 | 9 | 51 | estimator |
| copilot | JOB-PREF-001 | QRY-001 | query | with_memory | true | true | 23 | 6 | 29 | estimator |
| copilot | JOB-DEC-001 | EST-001 | establish | without_memory | false | true | 39 | 12 | 51 | estimator |
| copilot | JOB-DEC-001 | QRY-001 | query | without_memory | true | true | 63 | 12 | 75 | estimator |
| copilot | JOB-DEC-001 | EST-001 | establish | with_memory | false | true | 42 | 11 | 53 | estimator |
| copilot | JOB-DEC-001 | QRY-001 | query | with_memory | true | true | 27 | 11 | 38 | estimator |
| copilot | JOB-FACT-001 | EST-001 | establish | without_memory | false | true | 37 | 12 | 49 | estimator |
| copilot | JOB-FACT-001 | QRY-001 | query | without_memory | true | true | 58 | 11 | 69 | estimator |
| copilot | JOB-FACT-001 | EST-001 | establish | with_memory | false | true | 40 | 11 | 51 | estimator |
| copilot | JOB-FACT-001 | QRY-001 | query | with_memory | true | true | 21 | 8 | 29 | estimator |
| copilot | JOB-MULTI-001 | EST-001 | establish | without_memory | false | true | 27 | 7 | 34 | estimator |
| copilot | JOB-MULTI-001 | EST-002 | establish | without_memory | false | true | 62 | 8 | 70 | estimator |
| copilot | JOB-MULTI-001 | EST-003 | establish | without_memory | false | true | 98 | 8 | 106 | estimator |
| copilot | JOB-MULTI-001 | QRY-001 | query | without_memory | true | true | 124 | 10 | 134 | estimator |
| copilot | JOB-MULTI-001 | EST-001 | establish | with_memory | false | true | 30 | 6 | 36 | estimator |
| copilot | JOB-MULTI-001 | EST-002 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| copilot | JOB-MULTI-001 | EST-003 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| copilot | JOB-MULTI-001 | QRY-001 | query | with_memory | true | true | 43 | 15 | 58 | estimator |
| copilot | JOB-NEG-001 | EST-001 | establish | without_memory | false | true | 37 | 15 | 52 | estimator |
| copilot | JOB-NEG-001 | QRY-001 | query | without_memory | true | true | 64 | 13 | 77 | estimator |
| copilot | JOB-NEG-001 | QRY-002 | query | without_memory | true | true | 89 | 13 | 102 | estimator |
| copilot | JOB-NEG-001 | EST-001 | establish | with_memory | false | true | 48 | 10 | 58 | estimator |
| copilot | JOB-NEG-001 | QRY-001 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| copilot | JOB-NEG-001 | QRY-002 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| opencode | JOB-PREF-001 | EST-001 | establish | without_memory | false | true | 39 | 15 | 54 | estimator |
| opencode | JOB-PREF-001 | QRY-001 | query | without_memory | true | true | 64 | 11 | 75 | estimator |
| opencode | JOB-PREF-001 | EST-001 | establish | with_memory | false | true | 42 | 9 | 51 | estimator |
| opencode | JOB-PREF-001 | QRY-001 | query | with_memory | true | true | 23 | 6 | 29 | estimator |
| opencode | JOB-DEC-001 | EST-001 | establish | without_memory | false | true | 39 | 12 | 51 | estimator |
| opencode | JOB-DEC-001 | QRY-001 | query | without_memory | true | true | 63 | 12 | 75 | estimator |
| opencode | JOB-DEC-001 | EST-001 | establish | with_memory | false | true | 42 | 11 | 53 | estimator |
| opencode | JOB-DEC-001 | QRY-001 | query | with_memory | true | true | 27 | 11 | 38 | estimator |
| opencode | JOB-FACT-001 | EST-001 | establish | without_memory | false | true | 37 | 12 | 49 | estimator |
| opencode | JOB-FACT-001 | QRY-001 | query | without_memory | true | true | 58 | 11 | 69 | estimator |
| opencode | JOB-FACT-001 | EST-001 | establish | with_memory | false | true | 40 | 11 | 51 | estimator |
| opencode | JOB-FACT-001 | QRY-001 | query | with_memory | true | true | 21 | 8 | 29 | estimator |
| opencode | JOB-MULTI-001 | EST-001 | establish | without_memory | false | true | 27 | 7 | 34 | estimator |
| opencode | JOB-MULTI-001 | EST-002 | establish | without_memory | false | true | 62 | 8 | 70 | estimator |
| opencode | JOB-MULTI-001 | EST-003 | establish | without_memory | false | true | 98 | 8 | 106 | estimator |
| opencode | JOB-MULTI-001 | QRY-001 | query | without_memory | true | true | 124 | 10 | 134 | estimator |
| opencode | JOB-MULTI-001 | EST-001 | establish | with_memory | false | true | 30 | 6 | 36 | estimator |
| opencode | JOB-MULTI-001 | EST-002 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| opencode | JOB-MULTI-001 | EST-003 | establish | with_memory | false | true | 30 | 7 | 37 | estimator |
| opencode | JOB-MULTI-001 | QRY-001 | query | with_memory | true | true | 43 | 15 | 58 | estimator |
| opencode | JOB-NEG-001 | EST-001 | establish | without_memory | false | true | 37 | 15 | 52 | estimator |
| opencode | JOB-NEG-001 | QRY-001 | query | without_memory | true | true | 64 | 13 | 77 | estimator |
| opencode | JOB-NEG-001 | QRY-002 | query | without_memory | true | true | 89 | 13 | 102 | estimator |
| opencode | JOB-NEG-001 | EST-001 | establish | with_memory | false | true | 48 | 10 | 58 | estimator |
| opencode | JOB-NEG-001 | QRY-001 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
| opencode | JOB-NEG-001 | QRY-002 | query | with_memory | true | true | 45 | 13 | 58 | estimator |
