# Memory plugin benchmarks

## Pilot: Grok first

Integration tests and the required CI bench run on the **Grok** plugin/agent lane first.
Validate **performance and value** (primary metric: **tokens used**) under `with_memory` vs `without_memory` before implementing other agent plugins.

Other plugins (`claude-code`, `claude-cowork`, `cline`, `cline-v2`, `codex`, `copilot`, `opencode`) are **opt-in after H7a AGREE**.

## Primary metric: tokens used

| Field | Meaning |
|---|---|
| `tokens_in` | Prompt / system / injection / tool-result tokens |
| `tokens_out` | Completion tokens |
| `tokens_total` | `tokens_in + tokens_out` |
| `token_source` | `host` \| `estimator` \| `recorded` |

Pass/fail rubrics are correctness/safety gates only.

## Pack

- `memory-prompt-pack-v1.yaml` — PREF/DEC/FACT/PROC/MULTI/NEG/CONF/SAFE
- Results under `results/` (token summary tables first)

## Conditions

| Condition | Memories | Injection | memory_* tools |
|---|---|---|---|
| `without_memory` | empty / suppressed | off | unavailable / stubbed empty |
| `with_memory` | seeded | on if supported | available |

## Run

```powershell
# Default: Grok only (pilot)
./build.ps1 BenchMemory
./build.ps1 BenchMemory -Plugin grok

# After H7a AGREE only
./build.ps1 BenchMemory -Plugin all
```

## Traceability

FR-MCP-MEMORY-018, TR-MCP-MEMORY-BENCH-002, TEST-MCP-MEMORY-018, UC-MEMORY-009.
Hostile **H7a** (Grok value gate), then optional **H7b**.
