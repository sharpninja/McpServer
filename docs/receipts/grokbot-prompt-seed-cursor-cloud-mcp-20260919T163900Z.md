# Grok-bot prompt: stand up and seed MCP on this Cursor Cloud agent VM

Copy everything below the line into Grok Bot. Do not re-litigate PLAN-TXNKEYSERVER-001. Do not commit or push unless the operator later acks.

---

## Mission

Stand up a trusted local MCP Server on **this Cursor Cloud agent environment** and seed it so later agents can use marker + `/health` + requirements/TODO/sessionlog (including `workflow.requirements.generateDocument`).

This is **not** the Linux box at `/opt/mcpserver`. This is **not** Windows Legion. Nuke `UpdateService` is Windows-only. Do **not** claim Legion `UpdateService`. Do **not** touch `/opt/mcpserver` on some other host.

## Where you are

- Cloud agent: https://cursor.com/agents/bc-9b28196c-3625-5d78-a231-15e618daccaa
- Repo: `https://github.com/sharpninja/McpServer`
- Worktree: `/workspace`
- Branch: `develop`
- HEAD (do not reset): `8f30caf98410166bb8cb11958dde5445491d3d61` (`merge(develop): integrate origin memory work with keyserver bypass`)
- OS: Linux x64
- Setup status: `INSTALL_FAILED`
- Failed install installed **.NET SDK 9.0.318** under `/home/ubuntu/.dotnet`. `global.json` requires **SDK 10.0.201** with `rollForward: latestFeature`. `dotnet restore` failed for that reason.
- `pwsh` / `pwsh.exe` are absent.
- `AGENTS-README-FIRST.yaml` is absent.
- `GET http://localhost:7147/health` currently fails (nothing listening).
- Personal Cursor environment; no healthy environment build. Egress is not restricted.

## Dirty tree (do not destroy)

A Class 2 refresh-docs + wrap-up is **uncommitted** on this worktree. Do **not** `git reset`, `stash`, `checkout --`, `clean`, or revert those files. Do **not** commit or push. Tip-only commit is paused for operator ack.

Expected dirty set includes user-facing docs, in-repo wiki copies, ZIP twins, superseded banners, and receipts under `docs/receipts/` including:

- `docs/receipts/implementer-txnkeyserver-box-deploy-20260919T154640Z.md`
- `docs/receipts/hostile-validator-20260919T162808Z.md` + `.json`
- `docs/receipts/hv/20260919T162808Z-txnkeyserver-box-deploy-done-claim.request.jsonl` + `.response.jsonl`
- `docs/receipts/hostile-validator-20260919T161130Z.md` (prior DISAGREE, history)
- `docs/receipts/refresh-docs-20260919T163447Z.md`
- `docs/receipts/refresh-docs-20260919T163531Z.md`
- `docs/receipts/wrap-up-20260919T163500Z.md`

If you must write runtime files (SQLite, logs, publish output), put them under `/tmp/mcpserver-cloud/` or `/workspace/mcp-data/` and keep generated `AGENTS-README-FIRST.yaml` at `/workspace/AGENTS-README-FIRST.yaml` (that file is generated; do not hand-edit YAML as text).

## Product facts already true (do not re-implement)

- Keyserver is QuadBrain/brain-slot only: `TurnTransactionKeyserverScope` on `develop` `8f30caf` / `facbb3a6`.
- First-party adapters (TODO, session-log including QBAgent, requirements ingest, memory, etc.) bypass coordinator/keyserver even when `Mcp:TurnTransactions:Enabled=true`.
- PLAN-TXNKEYSERVER-001 is `Done=true` on the **Linux box** MCP after `/opt/mcpserver` publish/swap. HV AGREE Accuracy 99 Completeness 98 is in the receipts above.
- Repo default `TurnTransactions.Enabled` is `false`. Live box keeps it `true`. For this VM, set **`Enabled: true`** and **`RequiredForMutations: true`** so seed/proof matches the shipped bypass. Do **not** disable the flag as a "fix".
- Do **not** mark any other TODO done. Do **not** edit `docs/Project/TODO.yaml` by hand (projection only).
- Do **not** run Integration / Validation / Review / AiReview suites unless the operator asks.

## What "seed" means here

1. Toolchain so this tree can compile (`dotnet` 10.x matching `global.json`).
2. Local process serving `http://localhost:7147` from `/workspace` at commit `8f30caf` plus the existing dirty docs (runtime config only; do not revert docs).
3. Workspace registered as **primary** at path `/workspace`.
4. Marker `AGENTS-README-FIRST.yaml` written at workspace root with a live API key and HMAC signature block.
5. Storage reachable (SQLite).
6. Requirements store ingested from `docs/Project/` markdown (`Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`). Prove `FR-MCP-173`, `TR-MCP-TXNKEY-001`, `TEST-MCP-221`, FR-MCP-120 carve-out, TEST-MCP-161 retarget exist.
7. TODO store usable. `GET` `PLAN-TXNKEYSERVER-001` is enough; do not flip Done.
8. Optional but useful: `sync_run` / context ingest so `context_search` is not empty.
9. Proof mutations: one TODO create/update, one sessionlog begin/complete, one requirements read, with `TurnTransactions.Enabled=true` and **no** keyserver / subscriber-commit errors.
10. Then `POST /mcpserver/requirements/generateDocument` with `format=wiki` `docType=all` if marker+health+nonce are trusted. Copy output into the in-repo wiki/ZIP twins only if generate succeeds. If it fails, document the exact error; do not invent wiki pages.

## Required setup steps

Use Linux. Prefer `pwsh` if you install it; bash is allowed only to install PowerShell or when `pwsh` is still missing. Build JSON/YAML from objects, not hand-stitched strings. Do not use `python` to mutate YAML if `pwsh` + `plugins/core/lib-ps/yaml-object-mutation.ps1` is available.

### A. Toolchain

1. Install .NET SDK **10.0.201** or a 10.0.x that satisfies `global.json` `rollForward: latestFeature` into `/home/ubuntu/.dotnet` (or a dedicated prefix). Do not leave 9.0.318 as the only SDK.
2. `export DOTNET_ROOT=$HOME/.dotnet` and `export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"`.
3. `dotnet --list-sdks` must show a 10.0.x. `dotnet --version` from `/workspace` must not print `SDK-not-found`.
4. Install PowerShell 7+ (`pwsh`) if reasonably possible. Record if it stays absent.
5. Confirm `curl` works. Do not install Node for JSON/YAML construction.

### B. Config (runtime overlay only)

Do not rewrite committed `appsettings.yaml` in git. Use a **runtime overlay**:

- `ASPNETCORE_ENVIRONMENT=Staging` or `Development`
- `PORT=7147` or `Mcp__Port=7147`
- `Mcp__RepoRoot=/workspace`
- `Mcp__DataDirectory=/tmp/mcpserver-cloud/data` (or `/workspace/mcp-data`)
- `Mcp__Database__Provider=sqlite`
- `Mcp__Database__Sqlite__DataSource=/tmp/mcpserver-cloud/data/mcp.db`
- `Mcp__TodoStorage__Provider=database` (or `sqlite` alias)
- `Mcp__TurnTransactions__Enabled=true`
- `Mcp__TurnTransactions__RequiredForMutations=true`
- `Mcp__BrainSlots__ExecutionEnabled=false` (do not stand up QuadBrain providers)

If you need a workspace list, the committed `Mcp:Workspaces` entry pointing at `E:\github\McpServer` is **wrong** for this VM. Register `/workspace` via the API after start, or use an overlay that lists `/workspace` as primary. Do not commit that overlay.

### C. Build and start

From `/workspace`:

```bash
dotnet publish src/McpServer.Support.Mcp/McpServer.Support.Mcp.csproj \
  -c Staging -r linux-x64 --self-contained false \
  -o /tmp/mcpserver-cloud/publish
```

If restore fails, fix the SDK first, not the project.

Start **detached** (tmux is required on this VM for long-running processes). Example:

```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH="$DOTNET_ROOT:$PATH"
export PORT=7147
export Mcp__RepoRoot=/workspace
export Mcp__DataDirectory=/tmp/mcpserver-cloud/data
export Mcp__TurnTransactions__Enabled=true
export Mcp__TurnTransactions__RequiredForMutations=true
mkdir -p /tmp/mcpserver-cloud/data /tmp/mcpserver-cloud/logs
```

Run the published DLL or project with `--urls http://127.0.0.1:7147 --instance default`. Keep the process in tmux. Do not kill unrelated PIDs. If 7147 is busy, identify the listener before changing ports.

### D. Trust bootstrap

1. Wait until `GET http://127.0.0.1:7147/health` returns HTTP 200.
2. `GET http://127.0.0.1:7147/health?nonce=<fresh-hex>` must echo the same nonce. `status` Healthy. `storage` reachable.
3. Confirm `/workspace/AGENTS-README-FIRST.yaml` exists. If not, `POST /mcpserver/workspace` to register primary workspacePath `/workspace`, then regenerate markers (`POST /mcpserver/workspace/regenerate-markers` or the equivalent documented route).
4. Recompute HMAC-SHA256 from the marker `signature.fields` + `signature.format` using the workspace API key. If signature verification fails: log `MCP_UNTRUSTED`, stop mutating MCP, write a fail receipt. Do not probe extra `/mcpserver/*` writes.
5. Use `X-Api-Key` from the marker for all `/mcpserver/*` calls. Use `X-Workspace-Path: /workspace`.

### E. Seed

After trust:

1. `POST /mcpserver/requirements/ingest` with empty/omitted body so the server reads `docs/Project/` markdown (FR-MCP-173 scope is already in those files).
2. `GET /mcpserver/requirements/fr/FR-MCP-173`, `.../tr/TR-MCP-TXNKEY-001`, `.../test/TEST-MCP-221`. Confirm titles/bodies match QuadBrain-only keyserver + all-adapter bypass. Confirm mapping FR-MCP-173 → TR-MCP-TXNKEY-001 + TEST-MCP-221. Confirm FR-MCP-120 carve-out and TEST-MCP-161 retarget.
3. `GET /mcpserver/todo/PLAN-TXNKEYSERVER-001`. Record Done as the store reports it. Do **not** PUT Done.
4. Optional: MCP `sync_run` or REST context rebuild for `/workspace` docs. Do not ingest secrets.
5. Proof mutations (any agent id in Pascal-Case, e.g. `GrokBotCloud`):
   - create + update a proof TODO (canonical id, e.g. `BOX-CLOUD-PROOF-TXNKEY-001`)
   - sessionlog open + begin + complete with `todoId=PLAN-TXNKEYSERVER-001` or `None` as required by the session-log schema
   - one requirements GET or harmless notes-free read
6. Confirm responses succeed with `TurnTransactions.Enabled=true` and **no** keyserver / subscriber-commit / coordinator rollback errors.
7. If trust still holds: `POST /mcpserver/requirements/generateDocument` `{ "format": "wiki", "docType": "all" }`. If it returns a zip, refresh in-repo wiki twins the same way refresh-docs does (`docs/wiki.yaml` 44 docs; do not delete generated pages). If blocked, document it.

### F. Receipt

Write `docs/receipts/grokbot-cursor-cloud-mcp-seed-<UTC>.md` with:

- SDK versions before/after
- publish path, start command, tmux session name, PIDs
- health JSON (status, version, nonce echo, storage) — no API keys
- marker path + signature verify true/false (do not paste the API key)
- workspace registration result
- ingest counts / FR-173 GET evidence
- PLAN-TXNKEYSERVER-001 Done value as read
- proof HTTP codes
- generateDocument result or exact blocker
- statement: not Legion `UpdateService`; not `/opt/mcpserver` box swap
- `git status --short --untracked-files=all` (dirty tree must still include the refresh-docs files)

Do not echo API keys, tokens, or marker secrets into receipts.

## Stop conditions

- Do not commit, push, force-push, or open a PR.
- Do not reset the dirty refresh-docs tree.
- Do not mark TODOs done.
- Do not run Integration/Validation/Review.
- Do not disable `TurnTransactions.Enabled`.
- If `/health` or HMAC fails: `MCP_UNTRUSTED`, receipt, stop.

## Done when

`/workspace/AGENTS-README-FIRST.yaml` exists, HMAC verifies, `/health?nonce=` echoes, storage reachable, FR-MCP-173 is in the store, and a proof TODO + sessionlog mutation succeeded with transactions enabled and no keyserver errors. Then stop and report the receipt path plus `git status`.
