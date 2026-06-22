---
title: REPL + Repair Endpoint Redeploy - All Units Green
date: 2026-06-22
status: SUCCESS - unblocks waiting sessions
version: McpServer + sharpninja.mcpserver.repl 6.1.3-local.20260622.4
---

# REPL Redeploy + Repair Endpoint Complete (2026-06-22)

## Summary
- Full unit test suite (Nuke `./build.ps1 Test`) **PASSED** with zero failures and zero skips.
- Repair endpoint for purging bad placeholder FRs added and wired.
- Backfill guards prevent recreation of junk IDs (word tokens, wildcards like FR-SOCIAL-*, sentence fragments).
- REPL global tool redeployed to 6.1.3-local.20260622.4 (includes repair + prior fixes for listFr, state, identity).

This resolves the post-redeploy regressions around polluted requirements catalog (BUG-1 primary).

## Unit Test Results (all projects)
- McpServer.Support.Mcp.Tests: 1302 passed, 0 failed, 0 skipped
- McpServer.Client.Tests: 211 passed, 0 failed, 0 skipped
- McpServer.Repl.Core.Tests: 742 passed, 0 failed, 0 skipped
- McpServer.Cqrs.Tests: 33 passed
- McpServer.McpAgent.Tests: 62 passed
- McpServer.Launcher.Tests: 20 passed
- Build.Tests: 72 passed
- McpServer.QBAgent.Tests: 48 passed
- Others (Cqrs etc.): green

**Byrd gate satisfied: entire executed unit scope 100% green (no skips).**

## Repair Endpoint
- REST: `POST /mcpserver/requirements/fr/repair` → `{ "purged": <int> }`
- Client: `RequirementsClient.RepairFrPlaceholdersAsync()`
- REPL: `workflow.requirements.repairPlaceholders` (no params)
- Purge removes only backfilled placeholders with non-canonical IDs.
- `EfTodoService` + `NormalizeRequirementLinks` now reject invalid IDs before Ensure/insert (regexes aligned for multi-segment canonical: FR-AREA-SUB-###).

## REPL Redeploy
Command used:
```
pwsh.exe -NoProfile -ExecutionPolicy Bypass -Command "./build.ps1 InstallReplTool --package-version 6.1.3-local.20260622.4"
```

Result:
- Packed: local-packages/SharpNinja.McpServer.Repl.6.1.3-local.20260622.4.nupkg
- Global tool updated: 6.1.3-local.20260622.3 → 6.1.3-local.20260622.4
- Verified: `mcpserver-repl --version` → 6.1.3-local.20260622.4+82cc3e2d2d5d5bb98e80c940940ac5881662a7a3

## What to Tell Waiting Sessions / Agents
- Pull latest marker if needed, but REPL is the key update.
- Use `workflow.requirements.repairPlaceholders` (or direct REST) to clean polluted workspaces.
- `listFr` area filters + shape now correct (prior fixes carried).
- All prior append/state/identity issues addressed in REPL core.
- Run full tests locally with `./build.ps1 Test` before further changes.
- Next: validate on a workspace that had the 30+ bad placeholders; expect purged count >0 and clean subsequent lists/generates.

Everything was blocked on green tests + this redeploy. Now unblocked.

---
Generated after successful full unit run + redeploy. Feed this file to Codex/other waiting sessions.