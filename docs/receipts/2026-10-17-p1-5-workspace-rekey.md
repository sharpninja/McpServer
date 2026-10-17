# P1.5 workspace path rekey validation receipt

Recorded 2026-10-17 on PAYTON-LEGION2. Agent: Codex. Exact host model identifier and effort are unavailable in the exposed session metadata; they are not inferred.

Branch: feature/p1-5-workspace-rekey-20261016.
Base: 36783b79d061303bfb3e17abe685640a953491af (develop, PR #75).
Scope: this worktree only. No merge, rebase, deployment, service operation, live database rekey, or force push.

## Behavior and requirements

FR-MCP-PATHNORM-001, TR-MCP-PATHNORM-001, and TEST-MCP-PATHNORM-001 were recorded through MCP before implementation. Their projections and the plan retain pending acceptance state.

- Policy A delegates Windows lexical normalization to P1.4, preserving case and rejecting convergent old keys before updates.
- Apply requires a fresh, verified provider backup before transactional changes. Failures roll back; repeated apply changes zero rows.
- CLI dry-run requires an explicitly selected copy. A real CLI test verifies both synthetic configured and copy database files remain byte-identical, and the output contains mappings/counts without sentinel secrets.
- Startup performs raw validation before migrations/backfills or canonical EF queries. The PR #81 regression first proves the canonical EF predicate misses a legacy row, then verifies startup refusal before migration history writes, and finally proves the predicate finds it after explicit rekey.
- Automatic triage, handoff replay, and federation keys are recomputed from persisted inputs. Manual or unreproducible triage keys are retained and counted.
- Token-file migration retains a verified original backup and unchanged protected payloads. A post-commit token/report failure is explicitly reported for retry.
- Xml and Pkcs are narrowly pinned at 10.0.13. NuGet audit and warnings-as-errors were not disabled or suppressed.

Operator instructions: [workspace-path-rekey.md](../workspace-path-rekey.md).
Plan: [2026-10-07-federation-fixes.md](../plans/2026-10-07-federation-fixes.md).

## Chronological test evidence

Tests and literal/negative-control fixtures preceded production implementation, following this task's explicit red-first instruction.

- Initial restore was blocked by NU1902/NU1903. This is dependency evidence, not a red test result.
- After the narrow package repair, p15-red.trx: 20 total, 6 passed, 14 failed, 0 skipped. Failures exposed the absent implementation.
- Expanded p15-red-expanded.trx: 27 total, 6 passed, 21 failed, 0 skipped.
- Intermediate binding: 29 total, 22 passed, 7 failed, 0 skipped; subsequently 29 passed, 0 failed, 0 skipped.
- Additional red regressions exposed case-insensitive SQL updates, missing PostgreSQL apply-copy option handling, and the fresh-database guard creating a file. Each was corrected before the final runs.
- The first full unit gate exposed obsolete backslash query fixtures, the new physical-path allowlist entries, and two historical migration fixtures needing rekey before canonical queries; it also exposed two host skips. Fixtures now follow canonical storage semantics, and native-path cases assert the active host's documented behavior with no skips.
- Final focused run: 36 passed, 0 failed, 0 skipped, exit 0.
- Final Nuke gate: Restore, Compile, Test succeeded, exit 0, completed 2026-10-17T08:26:10 local. All 4,647 tests passed, 0 failed, 0 skipped.

Final commands (PowerShell, worktree current directory):

```powershell
dotnet test tests\QBrainAi.Support.Mcp.Tests\QBrainAi.Support.Mcp.Tests.csproj -c Debug --filter 'FullyQualifiedName~WorkspacePathRekeyTests|FullyQualifiedName~RemoveTodoRecordsMigrationTests' --logger 'trx;LogFileName=p15-final-focused.trx' --results-directory '.mcp-p1-5-tmp\test-results'
.\build.ps1 Test --configuration Debug --test-run-id p15-20261017T082100
```

Final Nuke project results:

- QBrainAi.Client.Tests: 306/306 passed.
- QBrainAi.Cqrs.Tests: 33/33 passed.
- QBrainAi.Launcher.Tests: 20/20 passed.
- QBrainAi.McpAgent.Tests: 65/65 passed.
- QBrainAi.QBAgent.Tests: 96/96 passed.
- QBrainAi.Repl.Core.Tests: 868/868 passed.
- QBrainAi.Support.Mcp.Tests: 3,259/3,259 passed.

The Nuke unit scope excludes integration/BDD suites by repository convention. No source changes followed this final gate; only this receipt and source-control delivery followed.

## Limits and acceptance status

Real SQL Server and PostgreSQL native backup/constraint rehearsals have not run. Their copy-selection logic is unit-tested; SQLite is exercised with disposable databases, historical schema, constraints, rollback, and the real CLI. SQL Server/PostgreSQL copy rehearsals remain an operator validation step under the deployment hold.

No independent hostile-review verdict is claimed. Draft PR delivery does not mark the plan, requirements, deployment gate, or rehearsal gate complete.

The preexisting plan/test work was retained and extended. Test-generated benchmark/schema edits were copied into .mcp-p1-5-tmp/generated-gate-artifacts, hash-verified, and only those five initially clean files were restored. Temporary evidence and session caches are retained locally and excluded from the commit.

MCP session: Codex-20261017T124336Z-plugin-session.
MCP request: req-20261017T124349Z-prompt-6632.

## Local evidence hashes

These files remain local; this receipt records their SHA-256 values without committing raw logs or marker material.

- .mcp-p1-5-tmp/test-results/p15-red.trx: SHA256 628DB56E440BC8F7434DEFF5E26EBD830B52C01C4A57A7434CA41570A79EDEE3
- .mcp-p1-5-tmp/test-results/p15-red-expanded.trx: SHA256 A95C3116AB6FA52943A6662C8FFAE9EE8EE132478DC8CF64AFD81ECB737900B2
- .mcp-p1-5-tmp/test-results/p15-final-focused.trx: SHA256 4ED3EF46F663C5EB84C59FFE46E0EABF0017C96E1B467FB07761376CB8BA6C8A
- .mcp-p1-5-tmp/nuke-unit-gate-verified.log: SHA256 C7A68BC4445F59D9A0A6876FD5C8E827C77A96F558F85C0C967C565D92BF4871
- TestResults\p15-20261017T082100\unit\QBrainAi.Client.Tests\QBrainAi.Client.Tests.trx: SHA256 B587E34BBFDF5460998ABFBD14C9EDA1466B85F78E272D900CDAD7F876D3CE06
- TestResults\p15-20261017T082100\unit\QBrainAi.Cqrs.Tests\QBrainAi.Cqrs.Tests.trx: SHA256 52EC64450DE66999E769336405C83B272F4D91E623624546089DF7600B739385
- TestResults\p15-20261017T082100\unit\QBrainAi.Launcher.Tests\QBrainAi.Launcher.Tests.trx: SHA256 2025859638D0FCED3E7D903051B2DD66F4A9A3104676941EE66057B18E3E864F
- TestResults\p15-20261017T082100\unit\QBrainAi.McpAgent.Tests\QBrainAi.McpAgent.Tests.trx: SHA256 82083B9436C8479C1F0E362171CE6EDBC64B9322012C4CDD6B34856CEB4B448C
- TestResults\p15-20261017T082100\unit\QBrainAi.QBAgent.Tests\QBrainAi.QBAgent.Tests.trx: SHA256 2F76B92437A52D088DFB2FAEA51AD2DB4CB609F9A8F608AFAA2782E35988E4FD
- TestResults\p15-20261017T082100\unit\QBrainAi.Repl.Core.Tests\QBrainAi.Repl.Core.Tests.trx: SHA256 7E798D6F4038D156B38D8DEBA89DFB4FC9E8014EA46BBC333DCD7DBFE9EF4F0B
- TestResults\p15-20261017T082100\unit\QBrainAi.Support.Mcp.Tests\QBrainAi.Support.Mcp.Tests.trx: SHA256 6E03DD8FBBB6DF625232CA6AFD540B8402FC550848C306627D1BB2F468D2B756