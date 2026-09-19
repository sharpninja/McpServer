# Hostile / operator receipt — H7b-green (MCP-MEMORY-002 / S7b remaining seven plugins)

| Field | Value |
|---|---|
| Phase | H7b-green |
| WorkClass | Class 1 H7b-green follow-on |
| Branch | cursor/memory-s7b-h7b-c219 |
| Base | develop @ `63f2e745` (merge PR #49) |
| Filter (namespace) | `FullyQualifiedName~McpServer.Support.Mcp.Tests.Memory` |
| Filter (S7+S7a+S7b focus) | `FullyQualifiedName~MemoryBench\|FullyQualifiedName~MemoryPluginSync\|FullyQualifiedName~MemoryIntegrationTests` |
| Started (UTC) | 2026-09-19T09:16:00Z |
| Ended (UTC) | 2026-09-19T09:20:00Z |
| H7a receipt cited | `docs/receipts/hostile-validator-20260919T081530Z.md` |
| H7a value gate | `docs/benchmarks/h7a-value-gate.json` agree:true |
| v1 stub JSON SHA-256 | `ee4f295e0c9124f23d9172899fd357e1c9c06feea60dade6ec0e92dc0cb6850b` |
| v2 stub JSON SHA-256 | `97f3e7f0be72d9e839ed3e5042d467e3f96a1c717ef2121672903f3dc94907c9` |

## Independent filter counts

| Project / filter | Failed | Passed | Skipped |
|---|---|---|---|
| Support.Mcp namespace `~McpServer.Support.Mcp.Tests.Memory` | **0** | **337** | **0** |
| S7+S7a+S7b focus (`MemoryBench*` / `MemoryPluginSync*` / `MemoryIntegrationTests*`) | **0** | **91** | **0** |

## Value-gate / H7b scorecard

| Area | Evidence | Outcome |
|---|---|---|
| H7a AGREE unblocks -Plugin all | `h7a-value-gate.json` agree:true cites H7a-green receipt `docs/receipts/hostile-validator-20260919T081530Z.md` | Passed |
| Eight recorded/stub adapters | Grok plus claude-code, claude-cowork, cline, cline-v2, codex, copilot, opencode | Passed |
| v1 pack × both conditions | 8 plugins × 8 prompts × 2 = 128 cells; pass_rate=1.0 | Passed |
| Tokens primary (v1 stub) | Headline macro mean tokens_total with_memory=38.125 without_memory=20.25 | Passed |
| Success-gated multiturn | 8 plugins × 5 jobs × 2 = 40/40 jobs pass both conditions; SUCCESS-GATED mean tokens_total with_memory=118.6 without_memory=189.6 | Passed |
| No live cloud keys | Stub/recorded only; live keys fail-closed | Passed |
| Host skill/descriptor | `plugins/core/hosts/{plugin}/SKILL.md` + `memory-descriptor.json` for all seven | Passed |
| Descriptor-load bats | 8/8 ok on claude-code, claude-cowork, codex, copilot `tests/memory.bats` | Passed |
| Sibling plugin PRs | cursor[bot] 403 on push/fork (token scoped to McpServer only) | Gap (honest) |

## Token headline (stub -Plugin all)

- v1 smoke: with_memory mean tokens_total **38.125** vs without_memory **20.25**; pass_rate **1.0** on every plugin.
- v2 success-gated: with_memory mean **118.6** vs without_memory **189.6**; 40/40 paired successes; with_memory won on tokens.

Artifacts: `docs/benchmarks/results/memory-bench-20260919T091800Z.{json,md}` and `docs/benchmarks/results/memory-bench-multiturn-20260919T091800Z.{json,md}`.

## Honesty

- **H7b-red+green one agent pass** on `cursor/memory-s7b-h7b-c219`. No separate H7b-red hostile receipt. Disclosed (same posture as H7a-green).
- Sibling plugin repos confirmed: `mcpserver-claude-code-plugin`, `mcpserver-claude-cowork-plugin`, `mcpserver-cline-plugin`, `mcpserver-cline-v2-plugin`, `mcpserver-codex-plugin`, `mcpserver-copilot-plugin`, `mcpserver-opencode-plugin`. Push and fork returned **403 Resource not accessible by integration**. Canonical payloads live in McpServer `plugins/core/hosts/*` and `plugins/core/sync/apply-memory-s7b-hosts.ps1`. Operator with org write access must apply and open one PR per plugin to `main` (opencode default branch is `master`).
- Node plugin jest suites were not installed in this environment (`node_modules` absent). Descriptor/skill load was proven by JSON/skill parse plus bats on the PowerShell hosts.
- MCP session/TODO store unavailable: `AGENTS-README-FIRST.yaml` missing; `mcpserver-box` failed live tool discovery. MCP-MEMORY-002 was **not** marked Done.
- Default CI plugin remains grok (`nonGrokCiRequired=false`). `-Plugin all` is unblocked.

## Verdict

**AGREE** — H7a file flipped to agree:true with receipt citation; eight-plugin stub/recorded BenchMemory path is green (128 v1 cells, 40/40 v2 jobs); tokens remain primary; seven host skill/descriptor payloads landed in McpServer. Sibling plugin-repo PRs are blocked by token scope and must be opened by an operator with write access.
