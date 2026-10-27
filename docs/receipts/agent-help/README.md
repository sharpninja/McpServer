# agent-help

Receipts from the Agent Help work of July 2026: a captured help session, the Agent Help test totals, and two Nuke run logs. They were kept at the top of `docs/` as `docs/receipts-agenthelp-*` and were copied here on 2026-10-27 to group every receipt under `docs/receipts`. The copies are byte-identical (SHA-256 in [../receipt-grouping-20261027.json](../receipt-grouping-20261027.json)). The originals stay in `docs/` until the operator clears their deletion.

## Contents

- `receipts-agenthelp-ask-turn.json`: the dialog of one Agent Help session (`help-20260708185114-...`, 2026-07-08), starting with the stub context pack of 110 documents across 3 topics.
- `receipts-agenthelp-global-graphrag-ask.json`: a completed Agent Help turn (`help-20260708195859-...`, turn-0001). It is a context-guided fallback answer built from workspace and `global:canonical/...` excerpts of the global GraphRAG corpus, with the inbound guard result.
- `receipts-agenthelp-smoke.json`: an Agent Help smoke result: a session id, the health nonce used, and status idle.
- `receipts-agenthelp-tests.txt`: the Agent Help test totals. Repl.Core 9, Support.Mcp.Tests 24, Support.Mcp.IntegrationTests 4: 37 passed, 0 skipped. Then a `build.ps1 Compile` log.
- `receipts-agenthelp-traceability.txt`: a Nuke run log (NUKE 9.0.4 on Windows), recorded for the traceability check.
- `receipts-agenthelp-updateservice.txt`: a Nuke run log (NUKE 9.0.4 on Windows) of the UpdateService deployment after the Agent Help change.

## How it is used

These are evidence files. Nothing builds from or loads them. Later receipts cite them by their old `docs/receipts-agenthelp-*` paths, for example `docs/receipts/qbrain-ai-rebrand-phase1-inventory.tsv` and `docs/receipts/hv/20260916T105737Z-g1-c3-21e5b43f.response.jsonl`.

## Related

- Effort: Agent Help (FR-MCP-HELP-* requirements); the TODO id is not recorded in these files.
- Parent: [../README.md](../README.md)
- Sibling receipt groups: [../graphrag-global/README.md](../graphrag-global/README.md), [../template-sync/README.md](../template-sync/README.md)
