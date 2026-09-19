# Memory plugin benchmarks

## Pilot: Grok first

Integration tests and the required CI bench run on the **Grok** plugin/agent lane first.
Validate **performance and value** (primary metric: **tokens used**) under `with_memory` vs `without_memory` before implementing other agent plugins.

Other plugins (`claude-code`, `claude-cowork`, `cline`, `cline-v2`, `codex`, `copilot`, `opencode`) are **unblocked after H7a AGREE**. Default CI stays Grok; `./build.ps1 BenchMemory -Plugin all` runs all eight recorded/stub adapters.

## Primary metric: tokens used

| Field | Meaning |
|---|---|
| `tokens_in` | Prompt / system / injection / tool-result tokens |
| `tokens_out` | Completion tokens |
| `tokens_total` | `tokens_in + tokens_out` |
| `token_source` | `host` \| `estimator` \| `recorded` |

Pass/fail rubrics are correctness/safety gates only.

v2 job success requires every required query turn to pass. Primary comparison is macro mean/median `tokens_total` **only over successful jobs**. Failed jobs stay in the artifact with `pass=false` and are excluded from those means. Do not claim an efficiency win from failed `without_memory` runs.

## Pack

- `memory-prompt-pack-v1.yaml` — smoke/regression only (PREF/DEC/FACT/PROC/MULTI/NEG/CONF/SAFE). Single-turn seeded cells. Not the efficiency claim.
- `memory-prompt-pack-v2-multiturn.yaml` — **real efficiency + correctness bench**. Multi-turn jobs (establish, then query without restating facts). Paired `without_memory` (conversation-history baseline) vs `with_memory` (compact REQUIRED MEMORIES / memory_recall; no establish-transcript replay).
- Results under `results/` (token summary tables first). v2 artifacts are `memory-bench-multiturn-<UTC>.json|.md`.
- Token-primary v2 results on this branch: `results/memory-bench-multiturn-20260919T090341Z.md` (paired JSON + `live/grok-subscription-cloud-agent-v2-multiturn.json`). v1 `memory-bench-20260919T084351Z.*` is smoke/regression only.

## Conditions

| Condition | Memories | Injection | memory_* tools |
|---|---|---|---|
| `without_memory` | empty / suppressed | off | unavailable / stubbed empty |
| `with_memory` | seeded | on if supported | available |

## Run

Default plugin is **grok**. `-Plugin all` is explicit and post-H7a.

```powershell
# Default: Grok only (pilot), stub mode, both conditions
# Runs v1 smoke/regression tests plus v2 multi-turn harness tests (FullyQualifiedName~MemoryBench)
./build.ps1 BenchMemory
./build.ps1 BenchMemory -Plugin grok

# After H7a AGREE only
./build.ps1 BenchMemory -Plugin all
```

CI prints **token means by plugin × condition** as the first metrics block.
`Memory:Bench:Gate=true` (Nuke `-MemoryBenchGate`) fails the target on required correctness misses; default is report-only.

without_memory vs with_memory are pack conditions, not extra CLI flags: every run executes both.

## Grok pilot validation path

- Adapter: `MemoryBenchGrokAdapter` (`recorded-fixture` entrypoint; stub/recorded fixtures, no cloud).
- Unit + fixture integration: `tests/McpServer.Support.Mcp.Tests/Memory/MemoryBench*.cs` and `MemoryIntegrationTests.cs`.
- Remember→recall/injection is asserted on the Grok lane with recorded fixtures. Live Grok is opt-in via `XAI_API_KEY` and tags `mode=live`; CI does not require a real XAI key.
- After H7a AGREE (`docs/benchmarks/h7a-value-gate.json` cites `docs/receipts/hostile-validator-20260919T081530Z.md`), the same recorded-fixture adapters exist for the other seven plugins. Live keys (`ANTHROPIC_API_KEY`, `CLINE_API_KEY`, `OPENAI_API_KEY`, `COPILOT_GITHUB_TOKEN`, `OPENCODE_API_KEY`) are opt-in only.
- Success-gated multi-turn (`IMemoryBenchMultiTurnAdapter`) is registered per plugin. Failed jobs stay in the artifact and are excluded from token means.
- No live Perplexity. No Python product path. Token estimator id `memory-bench-whitespace` / version `1.0.0` when the host does not report usage.

## Traceability

FR-MCP-MEMORY-018, TR-MCP-MEMORY-BENCH-002, TEST-MCP-MEMORY-018, UC-MEMORY-009.
Hostile **H7a** (Grok value gate), then optional **H7b**.
