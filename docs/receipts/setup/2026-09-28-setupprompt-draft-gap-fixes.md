# MCP-SETUPPROMPT-001 - Draft critical gap fixes

**Status:** draft text updated (does not mark TODO done; does not re-run Astra HV)  
**Author:** GrokCode on PAYTON-LEGION2  
**When:** 2026-09-28 10:58:33 (America/Chicago / CT)  
**Draft:** docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md  
**Prior SHA256:** 5B518FF2CF4AF0D4F74399F2B186486FEEA430801FB181D8EB92FE812718D766  
**New SHA256:** 7DDF8C82F468E677C1026235D696A26BC6F4F254CADFE4C3061671D8BF65468B  
**Inventory source:** docs/receipts/setup/2026-09-28-setupprompt-install-surface-inventory.md  
**Method:** surgical ASCII-only text edits. Did not commit/push. Did not run UpdateService or mutate the live service. Did not re-run full Astra HV.

---

## Critical gaps addressed (inventory 1-4)

1. **Checklist install-complete** - Operator checklist no longer treats `linux-publish-swap` as install-complete. Install complete = Windows UpdateService **or** `service-other` (operator-owned) only. Configure-before-start is now an explicit checklist gate before Install.
2. **Linux/systemd conflict redirects** - Intake systemd follow-up, `service-systemd` playbook action, and conflict-table rows `WINDOWS_SERVICE_ON_LINUX`, `SYSTEMD_PENDING_VALIDATION`, and `LINUX_PUBLISH_SWAP_PENDING_VALIDATION` now redirect to `service-other` / stop-only. `linux-publish-swap` remains blocked pending validation (no longer a redirect target).
3. **REPL acquisition pinned** - Primary path is `./build.ps1 InstallReplTool` (PackageId `SharpNinja.McpServer.Repl`, command `mcpserver-repl`). Plugin ensure-repl / GitHub-release / Codex setup.sh called out as non-ordinary for dry-runs. Script/dotnet alternate marked secondary/non-default and requires Pack first.
4. **Config-before-start + PS.MCP + Grok skills mirror** - Hard rule and Windows Obtain now spell PowerShell.MCP >= 1.14.0 assert/install/fail-closed. `service-windowsService` and Windows Configure section emphasize Provider / omit-empty connection keys / secret env / RepoRoot+Workspaces rewrite **before** UpdateService start. `agent-grok` adds skills-mirror Obtain/Configure evidence when intake `skillMirror` is set.

---

## Edit inventory (16 surgical replacements)

- checklist-install-complete
- intake-systemd-redirect
- playbook-systemd-redirect
- conflict-windows-service-on-linux
- conflict-systemd-pending
- conflict-linux-publish-swap-pending
- phase1-repl-primary
- windows-obtain-repl
- linux-obtain-repl-note
- hard-rule-psmcp-bootstrap
- windows-obtain-psmcp
- service-windows-config-before-start
- agent-grok-skills-mirror
- windows-configure-before-start
- windows-yaml-workspace-rewrite
- install-section-stage-note

---

## Preserved (intentionally untouched)

- Overall HV AGREE substance: ImagePath strict quoting, fail-closed Verify, PowerShell-only ordinary automation, F01-F09 remediations
- `linux-publish-swap` and `systemd` remain blocked pending validation/inventory
- ASCII-only (non-ASCII count after edit: 0)

---

## Ready for Legion dry-runs A/B?

**Yes** - critical inventory gaps 1-4 are closed in the draft text. Parent may still want a scoped re-HV before claiming prompt AGREE; this receipt does not re-run HV.

---

## Non-actions

- Did not commit or push
- Did not run UpdateService / Restore / Manage Install
- Did not mutate the live service under C:\ProgramData\McpServer
- Did not mark MCP-SETUPPROMPT-001 TODO done
- Did not re-run full Astra HV