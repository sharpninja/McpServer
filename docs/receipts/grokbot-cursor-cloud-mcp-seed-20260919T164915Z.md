# Cursor Cloud MCP seed receipt 2026-09-19T16:49:15Z

Class: stand up and seed a trusted **local** MCP Server on this Cursor Cloud agent VM (`bc-9b28196c-3625-5d78-a231-15e618daccaa`).

Agent: CursorGrok
Worktree: `/workspace`
Branch: `develop`
HEAD: `8f30caf98410166bb8cb11958dde5445491d3d61` (unchanged; no reset)
This is **not** `/opt/mcpserver` on another host and **not** Windows Legion `UpdateService`.

Prompt saved at: `docs/receipts/grokbot-prompt-seed-cursor-cloud-mcp-20260919T163900Z.md`

## Trust

- Marker path: `/workspace/AGENTS-README-FIRST.yaml` (generated; gitignored)
- HMAC-SHA256 `marker-v1` over `signature.fields` (29 fields): match True
- `GET http://127.0.0.1:7147/health?nonce=seed-20260919T164736Z` echoed the nonce
- Health: `Healthy`, storage `reachable`, version `1.0.0+8f30caf98410166bb8cb11958dde5445491d3d61`
- Marker `workspacePath`: `/workspace`
- Marker `baseUrl`: `http://cursor:7147` (API calls used `http://127.0.0.1:7147`)
- Auth: `X-Api-Key` + `X-Workspace-Path: /workspace` (key values not recorded here)

## Runtime (not product commits)

- SDK: `10.0.401` at `/home/ubuntu/.dotnet` (`global.json` rollForward latestFeature). SDK `9.0.318` remains installed and unused for this publish.
- `pwsh` 7.5.10 at `/home/ubuntu/.local/bin/pwsh`
- Publish: `dotnet publish` Staging `linux-x64` → `/tmp/mcpserver-cloud/publish`
- Process: tmux session `mcpserver-cloud`, pid from marker, `PORT=7147`
- Overlay only (env on pid 6358):
  - `Mcp__RepoRoot=/workspace`
  - `Mcp__DataDirectory=/tmp/mcpserver-cloud/data`
  - `Mcp__Database__Provider=sqlite`
  - `Mcp__TurnTransactions__Enabled=true`
  - `Mcp__TurnTransactions__RequiredForMutations=true`
  - `Mcp__IdentityServer__Enabled=false` (Linux: LocalDB is not supported)
  - `Mcp__Federation__Enabled=false`
- SQLite: `/tmp/mcpserver-cloud/data/mcp.db`
- Primary workspace: `/workspace` (`isPrimary: true`)
- Existing `appsettings.yaml` still lists Windows paths `E:\github\McpServer` and `F:\GitHub\Requirements`. Server init created accidental Linux folders `/workspace/E:\github\McpServer` and `/workspace/F:\GitHub\Requirements` with generated markers. Do not commit those folders or any `appsettings.yaml` rewrite.

## Seed proofs

`POST /mcpserver/requirements/ingest` with empty body failed parse on heading `TR-MCP-AGENT-PARITY-020..027` in `docs/Project/Technical-Requirements.md`. Retry sent the four `docs/Project/` markdown files with that range heading stripped **in the request body only** (repo file not edited). Result 200:

- FR parsed/updated 336
- TR parsed/updated 458
- TEST parsed/updated 490
- mapping parsed/updated 336

Store GETs present:

- `FR-MCP-173` — Keyserver signs QuadBrain transactions only
- `TR-MCP-TXNKEY-001` — Keyserver gate is QuadBrain/brain-slot only
- `TEST-MCP-221` — unit-scope bypass + coordinator still hit for brain-slot
- `FR-MCP-120` — carve-out text names FR-MCP-173 for every non-QuadBrain first-party mutation
- `TEST-MCP-161` — retargeted to QuadBrain/brain-slot coordinator tests; first-party mutations pointed at FR-MCP-173 / TEST-MCP-221

`GET /mcpserver/todo/PLAN-TXNKEYSERVER-001` → 404. This workspace YAML bootstrap imported 54 items and does not contain that id. Done was not written. Box Done remains a box fact, not this VM TODO row.

Proof mutations with turn transactions enabled and not degraded (`GET /mcpserver/turntransactions/status` → `enabled: true`, `degraded: false`):

- `POST/PUT` `TEST-CLOUDSEED-001` (left `done: false`). Sessionlog rejected `TEST-*` as a requirement id.
- `POST/PUT` `SEED-CLOUDMCP-001` (left `done: false`)
- Session `CursorGrok-20260919T164848Z-cloudseed2` / `req-20260919T164848Z-cloud-mcp-seed2` open → begin → complete 200
- No keyserver / subscriber-commit errors on those first-party writes

Optional `sync_run` via `/mcp-transport` (`Accept: application/json, text/event-stream`): Completed, documents 95, chunks 182, error null.

Server projection wrote the two seed TODOs into `docs/Project/TODO.yaml`. That is not a hand edit. No other TODOs marked done.

## generateDocument

- `POST /mcpserver/requirements/generateDocument` `{ "format": "wiki", "docType": "all" }` → 404 (no such REST route)
- Live controller is `GET /mcpserver/requirements/generate?doc=all&format=wiki`
- That GET returned 500 `NotSupportedException`: Linux requirements export cleanup rejects nested stale paths before mutation because the kernel lacks a contained atomic delete: `azure/Architecture`
- Existing refresh-docs wiki copies under `docs/Project/wiki/` were left in place. No invented wiki pages. ZIP twins not regenerated.

## Commit-sync

Still paused. No commit. No push. No PR.

## Stop condition

Marker verifies, health echoes nonce, FR-MCP-173 is in the store, ingest + TODO + sessionlog mutations succeeded with `TurnTransactions` on. Wiki generateDocument remains blocked by the Linux nested-stale cleanup guard.
