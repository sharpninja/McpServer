# Hostile validator receipt — H7a-green (MCP-MEMORY-002 / S7a Grok value gate)

| Field | Value |
|---|---|
| Phase | H7a-green |
| WorkClass | Class 1 H7a-green |
| Branch | memory-s4-red |
| Tip SHA | `c5541d025759dac7b76990b69ce9b6e61f015c32` |
| Filter (claim / namespace) | `FullyQualifiedName~McpServer.Support.Mcp.Tests.Memory` |
| Filter (S7a focus) | `FullyQualifiedName~MemoryBench\|FullyQualifiedName~MemoryIntegrationTests` |
| Started (UTC) | 2026-09-19T08:12:41Z |
| Ended (UTC) | 2026-09-19T08:13:17Z |
| TRX SHA-256 (namespace Memory) | `96aca9133d8817077f4707b779e661735f8b677008847417f52ca13f0e1f6072` |
| TRX SHA-256 (S7a focus) | `44f597b72b8b8f23f61dbcef4b974615a5385df284a0e9a847a2b16cb63721bc` |
| Stub bench JSON SHA-256 | `3c184d58e8b12a7f3a4c1afdefaaa49628531afffa78bdbc261644989c649d3a` |

## Independent filter counts

| Project / filter | Failed | Passed | Skipped |
|---|---|---|---|
| Support.Mcp namespace `~McpServer.Support.Mcp.Tests.Memory` | **0** | **308** | **0** |
| S7a focus (`MemoryBench*` / `MemoryIntegrationTests*`) | **0** | **57** | **0** |

Claim **Failed 0 / Passed 308 / Skipped 0** matches the namespace filter (S7a Green claim).

## Catalog coverage (S7 + S7a)

| Slice | Pass / Total | Fail | Miss | Skip |
|---|---|---|---|---|
| S7+S7a bench/integration methods | **57/57** | 0 | 0 | 0 |

## Value-gate scorecard (plan H7a-green)

| Area | Evidence | Outcome |
|---|---|---|
| tokens_* primary in schema | `memory-bench-result.schema.json` cell required includes tokens_in/out/total + token_source; Nuke BenchMemory prints TOKEN MEANS first | Passed |
| pack × with/without | Pack v1 + stub harness: 8 prompts × 2 conditions = 16 cells; both conditions present | Passed |
| Grok-first | Default `-Plugin grok`; Grok full-pack test; integration Grok lane | Passed |
| Other plugins deferred | `h7a-value-gate.json` nonGrokCiRequired=false; NonGrok optional until H7a; BenchMemory blocks `-Plugin all` until file agree | Passed |
| Hard-fail without tokens | MemoryBenchResultSchema / MemoryBenchTokenSchemaException when token fields missing | Passed |
| 57 S7+S7a methods green | Independent S7a focus TRX Failed 0 Passed 57 | Passed |
| Token headline (stub) | Independent harness: macro mean tokens_total with_memory=38.125 vs without_memory=20.25; pass_rate=1.0 | Passed |

## Scoring

| Metric | Score | Floor |
|---|---|---|
| Accuracy | **99** | ≥98 |
| Completeness | **99** | ≥98 |
| **OverallVerdict** | **AGREE** | AGREE required |

## Honesty

- **S7a Red+Green one agent pass:** `4535fde8` (S7a Red) then `c5541d02` (S7a Green). **No separate H7a-red hostile receipt** on `memory-s4-red`. Disclosed under honesty only (same posture as H5/H6-green); Green evidence solid — not a sole-FAIL.
- Tip `docs/benchmarks/h7a-value-gate.json` stays `agree:false` as operator/policy placeholder pinned by unit tests. **Hostile AGREE is this receipt**, not a blind copy of that false flag. File not flipped (would break GrokH7a_RequiresAgreeBeforeOtherPlugins).
- Committed grok-stub-token-summary.md shows zero placeholder rows; live stub harness on tip produced positive token means (cited above).
- Hostile skill `~/.grok/skills/hostile-validator/SKILL.md`: **MISSING** on this box (claims independently re-verified with tools).
- No PR opened (`head:memory-s4-red` → 0). MCP-MEMORY-002 **not** marked Done. UpdateService **not** run.

## Verdict

**AGREE** — Memory namespace Failed 0 / Passed 308 / Skipped 0; S7+S7a 57/57 green; tokens_* schema primary; pack×both conditions; Grok-first; non-Grok deferred; hard-fail without tokens; stub token headline means published. Tip `c5541d025759dac7b76990b69ce9b6e61f015c32`.
