# graphrag-global

The implementation receipt for PLAN-GRAPHRAG-GLOBAL-001 (2026-07-08), the host-global GraphRAG corpus. It was kept at `docs/receipts-graphrag-global-scope.txt` and was copied here on 2026-10-27 to group every receipt under `docs/receipts`. The copy is byte-identical (SHA-256 in [../receipt-grouping-20261027.json](../receipt-grouping-20261027.json)). The original stays in `docs/` until the operator clears its deletion.

## Contents

- `receipts-graphrag-global-scope.txt`: what the plan implemented and the test command used. It lists:
  - the `GraphRagStorageScope` enum;
  - the `GraphRagOptions` global settings;
  - global scope resolution under the data folder;
  - the startup seeder that copies canonical docs;
  - Agent Help preferring the global corpus;
  - the `?scope=global` controller and MCP tool parameter.

## How it is used

It is an evidence file. Nothing builds from or loads it. TODO readbacks and session-life HV streams under `docs/receipts` cite it by its old path.

## Related

- Plan: PLAN-GRAPHRAG-GLOBAL-001; requirement TR-MCP-GRAPHRAG-GLOBAL-001 (cited in `src/QBrainAi.GraphRag/Options/GraphRagOptions.cs`).
- Parent: [../README.md](../README.md)
- Sibling receipt groups: [../agent-help/README.md](../agent-help/README.md), [../template-sync/README.md](../template-sync/README.md)
