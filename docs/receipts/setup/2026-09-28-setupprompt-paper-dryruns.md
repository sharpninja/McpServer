# MCP-SETUPPROMPT-001 - Paper intake dry-runs A/B

**Status:** paper dry-runs complete (does not mark TODO done; does not re-run Astra HV)  
**Author:** GrokCode on PAYTON-LEGION2  
**When:** 2026-09-28 11:11:03 (America/Chicago / CT)  
**Draft:** docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md  
**Draft SHA256 before dry-run (verified):** 7DDF8C82F468E677C1026235D696A26BC6F4F254CADFE4C3061671D8BF65468B  
**Draft SHA256 after surgical clarity fixes:** 9933BAB3B92198E90BA23588CE9F459E191684D0BAB3140F1C0A8C10448910C0  
**Inventory:** docs/receipts/setup/2026-09-28-setupprompt-install-surface-inventory.md  
**Gap-fixes prior:** docs/receipts/setup/2026-09-28-setupprompt-draft-gap-fixes.md  
**Method:** Paper intake only. Read files, hashed draft, validated manifests on paper, simulated Phase 4 ImagePath/StartName/listener checks read-only. Did **not** run UpdateService, sc.exe, service start/stop, live ProgramData config writes, InstallReplTool mutation, or Claude hook install mutation. Did not re-HV.

---

## Dry-run A (paper) — PASS (intake walk)

**Intake:** GrokCode + windowsService (ProgramData / McpServer.Support.Mcp.exe / 7147 / LocalSystem) + sqlserver secret via env name `Mcp__Database__SqlServer__ConnectionString` only + REPL via InstallReplTool path only.

**Artifacts:**
- `docs/receipts/setup/dry-runs/2026-09-28-paper-A/setup-choices.manifest`
- `docs/receipts/setup/dry-runs/2026-09-28-paper-A/setup-implementation.plan`

**Draft contradiction walk:** none for this choice set. Maps to `agent-grok` + `storage-mssql` + `service-windowsService` + `process-deferred`; config-before-start before UpdateService; REPL primary pinned; no conflict-table refuse.

**Live read-only sim (not live AGREE):**
- Service Running; StartName LocalSystem; listener on 7147 owned by service PID; exe + `.mcpservice-deployment.json` (generatedBy=`build/Build.UpdateService.cs`) + `appsettings.yaml` present — PASS
- ImagePath live unquoted (`...\McpServer.Support.Mcp.exe --urls http://+:7147`) fails draft strict quoted regex — FAIL vs Phase 4 (live drift; source `GetServiceImagePath` returns quoted form; healing would require UpdateService — **not run**)
- Secret env name not observed in Process/User/Machine scopes; provider provenance incomplete on paper — VERIFY_EVIDENCE_UNAVAILABLE for full DB identity
- Trust/nonce + Grok smoke — not executed (paper)

**A result:** **PASS** for paper intake/playbook walk. Live verify sim notes recorded; does not claim live setup success.

---

## Dry-run B (paper) — PASS (intake walk)

**Intake:** Grok + Claude `agent-multi` + same WindowsService + SQLite at `C:\ProgramData\McpServer\mcp.db` + `SQLITE_MULTI_AGENT` ack + dual smoke + Claude hook install **expected evidence only**.

**Artifacts:**
- `docs/receipts/setup/dry-runs/2026-09-28-paper-B/setup-choices.manifest`
- `docs/receipts/setup/dry-runs/2026-09-28-paper-B/setup-implementation.plan`

**Draft contradiction walk:** none refuse-level. `SQLITE_MULTI_AGENT` caution path matches conflict table (continue with unsupportedFlags risk note). Dual-smoke and Claude hook evidence requirements spelled in plan. Same ImagePath live-sim FAIL as A. `mcp.db` not present (not created on paper).

**Expected Claude hook evidence (documented, not installed):**
- `install-claude-mcp-hooks.ps1` (+ `-VerifyOnly`)
- `~/.claude/settings.json` UserPromptSubmit / Stop / PostToolUse MCP hooks
- Claude Code restart after install
- `claude-hook-validation` skill pass
- Path surfaces exist on lab (script + settings.json); content not validated on paper

**B result:** **PASS** for paper intake/playbook walk with caution ack. Live verify sim notes recorded.

---

## Gaps found

### Documentation clarity (fixed surgically this pass)

1. **Phase 2 vs Phase 3 numbering** — Install section numbered before Configure, but Windows must configure-before-start first. Added explicit execution-order paragraph under Phase 2.
2. **Non-goals omissions** — MSIX packaging, Keycloak helper, SyncAgentPlugins were inventory non-goals but missing from draft Non-goals. Added three bullets.
3. **Claude smoke evidence** — Smoke line under-specified for dual-agent dry-runs. Expanded to require host-matched Claude helpers (not Grok repl-invoke) + hook `-VerifyOnly` evidence and restart note.

### Observed live drift (not a draft contradiction; not mutated)

4. **ImagePath quoting** — Draft Phase 4 + `WindowsServiceHelper.GetServiceImagePath` require fully quoted UpdateService form. Live WMI PathName is unquoted. Paper verify sim FAILs. Fix path is elevated UpdateService (out of scope for this paper dry-run).
5. **Secret env presence** — `Mcp__Database__SqlServer__ConnectionString` not present in Process/User/Machine env scopes during paper inspect (value never read). Service-account binding not probed further.

### Deferred / accepted (no draft edit)

6. Inventory high items still open beyond this pass: Codex bash `setup.sh` vs PowerShell-only ordinary rule; Docker SDK pin if service-other Docker used. Not blocking A/B paper intake.
7. Parent still owns scoped re-HV; this receipt does not re-HV.

---

## Draft SHA after

- **SHA256:** `9933BAB3B92198E90BA23588CE9F459E191684D0BAB3140F1C0A8C10448910C0`
- **Non-ASCII count after edits:** 0
- **Surgical fixes applied:** phase2-execution-order-clarity; non-goals-msix-keycloak-sync; claude-smoke-hook-evidence-clarity

---

## Receipt paths

| Kind | Path |
|------|------|
| Deliverable | docs/receipts/setup/2026-09-28-setupprompt-paper-dryruns.md |
| Paper A manifest | docs/receipts/setup/dry-runs/2026-09-28-paper-A/setup-choices.manifest |
| Paper A plan | docs/receipts/setup/dry-runs/2026-09-28-paper-A/setup-implementation.plan |
| Paper B manifest | docs/receipts/setup/dry-runs/2026-09-28-paper-B/setup-choices.manifest |
| Paper B plan | docs/receipts/setup/dry-runs/2026-09-28-paper-B/setup-implementation.plan |
| Draft | docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md |

---

## Non-actions

- Did not run UpdateService / Restore / Manage Install / sc.exe
- Did not start/stop the service
- Did not write live ProgramData config
- Did not run InstallReplTool (mutation)
- Did not run Claude hook installer
- Did not read or print secret values
- Did not commit/push
- Did not mark MCP-SETUPPROMPT-001 TODO done
- Did not re-run Astra HV