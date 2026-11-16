# Operator decision: retired use case FRs

Recorded on 2026-11-15. The operator directed that `FR-MCP-USECASE-016` and
`FR-MCP-USECASE-017` remain deleted on LAB-OMARCHY and be deleted on
PAYTON-LEGION2. This decision settles these two FR records and their linked
mappings in the reconciliation. It does not decide the disposition of their
linked TR or TEST records, which were not deleted.

The earlier snapshots and `decision-ledger.json` represent the state before
this correction. Their rows for these two FRs and mappings are superseded by
this explicit operator decision and the live verification below. The original
receipts are retained for the audit trail.

## Mutation receipts

- LEGION2 `workflow.requirements.deleteFr` for `FR-MCP-USECASE-016` returned
  `deleted: true`, request `req-20261115T205115Z-51f4`.
- LEGION2 `workflow.requirements.deleteFr` for `FR-MCP-USECASE-017` returned
  `deleted: true`, request `req-20261115T205115Z-51d6`.
- OMARCHY received no mutation for these records.

## Live readback

After the deletions, `workflow.requirements.getFr` returned `not_found` for
both IDs on both hosts. `workflow.requirements.listMappings` returned zero
mappings for each ID on both hosts. This readback verifies the settled state
without relying on response timestamps or an older snapshot.

The decision was also appended to the active Codex session turn
`req-20261115T194019Z-prompt-4167` in session
`Codex-20261115T143649Z-plugin-session-rekey-canonical-20261115`.
