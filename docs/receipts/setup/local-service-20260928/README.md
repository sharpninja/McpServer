# local-service-20260928

Receipts of the 2026-09-28 local service installation on LAB-OMARCHY, made during the dry run of the frontier agent setup prompt (MCP-SETUPPROMPT-001). They were kept in `docs/setup/` beside the setup choices manifest and implementation plan. They were copied here on 2026-10-27 to group every receipt under `docs/receipts`. The copies are byte-identical (SHA-256 in [../../receipt-grouping-20261027.json](../../receipt-grouping-20261027.json)). The originals stay in `docs/setup/` until the operator clears their deletion, because `docs/setup/setup-choices.manifest` and `docs/setup/setup-implementation.plan` name them there.

## Contents

- `bdpv4-binding-receipt.json`: the BDPv4 process binding.
  - Workspace memory MEMORY-DEVPROCESS-001 version 3, taken from `docs/Development-Process-draft-v4.md` at commit 6546a59f, assigned by MEMORY-PROCESS-001 version 2.
  - Hashes and exact-match checks; server-enforced transitions false.
- `bdpv4-service-receipt.json`: the service check at 23:14Z, before the binding receipt. The workspace token was rotated; main PID 611688 runs `/opt/mcpserver/app/QBrainAi.Support.Mcp` at product version `1.4.39+6546a59f`; Codex trusted bootstrap passed, and its TODO query and memory list returned results.
- `cline-test-results.json`: a Cline plugin test run. 5 suites and 32 tests passed, none failed; it cites MCP-PLUGIN-TRIAGE-001.
- `final-service-receipt.json`: the final service check.
  - The workspace token was rotated.
  - Main PID 580731 runs `/opt/mcpserver/app/QBrainAi.Support.Mcp`.
  - Configuration and binary SHA-256, product version `1.4.39+6546a59f`.
  - Codex trusted bootstrap passed; its TODO query and memory list returned results.

## How it is used

These are evidence files for the setup dry run. Nothing builds from or loads them. The setup-prompt and service-update HV streams under `docs/receipts/hv` cite them by their old `docs/setup/` paths.

## Related

- TODO: MCP-SETUPPROMPT-001 (done). Setup prompt: `docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md`.
- The scripts that produced these receipts stay in `docs/setup/` (for example `final-service-verification.ps1`).
- Parent: [../README.md](../README.md)
