# PLAN-TXNKEYSERVER-001 Linux box deploy receipt

- Written: 2026-09-19T15:47:00Z UTC (America/Chicago)
- Host: Grok Bot Linux box MCP (**not** Windows Legion)
- Repo: `sharpninja/McpServer` `develop` @ `8f30caf98410166bb8cb11958dde5445491d3d61`
- Worktree: `/tmp/mcpserver-develop-handoff` (`git pull --ff-only origin develop` → Already up to date, exit 0)

## Health version

| | Value |
|---|---|
| **Old** | `1.0.0+c9551bc83bd3ab9c705541c491d920258f3ae853` |
| **New** | `1.0.0+8f30caf98410166bb8cb11958dde5445491d3d61` |
| Live | Healthy (self + upstream; storage reachable) |

## TurnTransactionKeyserverScope present

- **yes** — live binary match count **2**
- Staged publish binary also count **2** before swap
- Pre-deploy live binary: **absent** (only log text under `/opt/mcpserver/logs`)

## Deploy (Linux equivalent of Nuke UpdateService)

| Step | Command / action | Exit |
|---|---|---|
| Publish | `dotnet publish ... -r linux-x64 --self-contained -p:PublishSingleFile=true -o /tmp/mcpserver-publish-stage` | **0** |
| Backup | `cp -a /opt/mcpserver /tmp/mcpserver-opt-backup-20260919154411` (+ etc appsettings/env) | **0** |
| Stop | SIGTERM supervisor `891013` + child `903524`; port 7147 free | **0** |
| Swap | `cp -a` stage → `/opt/mcpserver`; restore `appsettings.yaml` + `templates/`; keep `/var/lib/mcpserver` | **0** |
| Config | Added `Mcp.TurnTransactions.Enabled: true` + `RequiredForMutations: true` (was missing; defaults false) | **0** |
| Restart | `nohup /home/box/.local/bin/mcpserver-supervise` → PIDs supervisor `1223090` / child `1223094` | **0** |
| API key | Rotated on restart; synced workspace apikey from `AGENTS-README-FIRST.yaml` | **0** |

Single-file binary ~200MB (prior ~199MB).

## Live proof (`TurnTransactions.Enabled=true`)

| Check | HTTP | Notes |
|---|---|---|
| TODO create `BOX-DEPLOY-PROOF-TXNKEYSERVER-001` | **201** | success=True; failureKind=none; note=deploy-proof create 2026-09-19T15:45:21Z |
| TODO update note | **200** | success=True; note=deploy-proof update 2026-09-19T15:45:22Z; keyserver bypass exercised |
| Sessionlog open | **200** | agent=GrokBotDeploy; sessionId=GrokBotDeploy-20260919T154537Z-txnkeyserver-box; created=True |
| Sessionlog begin `req-20260919T154605Z-deploy-proof` | **201** | turnId=5; status=in_progress; todoId=PLAN-TXNKEYSERVER-001 (linkage only) |
| Sessionlog complete | **200** | turnId=5; status=completed |
| Requirements FR list | **200** | 305 items |
| Requirements effective | **200** | ok |
| Requirements PUT `FR-MCP-120` notes | **200** | notes=box-deploy-proof 2026-09-19T15:46Z |

Artifacts: `/tmp/txnkeyserver-proof/`. No keyserver / Subscriber-commit failures in proof responses.

## Explicitly NOT done

- Did **not** mark `PLAN-TXNKEYSERVER-001` done (hostile AGREE still required).
- Did **not** force-push or touch Legion.
- Did **not** run Integration / Validation / Review / AiReview suites.
- Did **not** disable `TurnTransactions`.

## Blockers

- None for deploy + live proof.
- Sessionlog requires canonical `PLAN-*`/`ISSUE-*` todoId for turns; proof TODO `BOX-DEPLOY-PROOF-TXNKEYSERVER-001` is valid for TODO API but not for sessionlog turn linkage.

## Receipt path

`/workspace/deliverables/txnkeyserver-box-deploy-20260919T154640Z.md`
