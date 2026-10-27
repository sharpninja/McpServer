# template-sync

The receipt of one prompt-template sync and service restart on the Windows lab host. It was kept at `docs/receipts-template-sync-restart.txt` and was copied here on 2026-10-27 to group every receipt under `docs/receipts`. The copy is byte-identical (SHA-256 in [../receipt-grouping-20261027.json](../receipt-grouping-20261027.json)). The original stays in `docs/` until the operator clears its deletion.

## Contents

- `receipts-template-sync-restart.txt`: two copies of `templates/prompt-templates.yaml` into the service data and install folders, then the service status (Running), the health status (Healthy), the health nonce and the health version `1.4.2+c56f391a04c5858485251560a3bee95c695f5d9b`.

## How it is used

It is an evidence file. Nothing builds from or loads it. `docs/receipts/qbrain-ai-rebrand-phase1-inventory.tsv` lists it by its old path.

## Related

- Effort and TODO: not recorded in this file. Head commit c56f391a, from the health version.
- Parent: [../README.md](../README.md)
- Sibling receipt groups: [../agent-help/README.md](../agent-help/README.md), [../graphrag-global/README.md](../graphrag-global/README.md)
