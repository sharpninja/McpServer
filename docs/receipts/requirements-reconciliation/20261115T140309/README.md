# Two-machine McpServer requirement review

This is the operator review queue for the SQL timestamp and provenance pass of 2026-11-15. It is a conservative list of entries that have not yet been adjudicated. It does not claim that every entry is impossible to reconcile.

## Review tables

- `unreconcilable-entries-compact.csv`: one row for each of 1,118 divergent requirement or mapping entries. It lists differing fields, both stored SQL timestamps, timestamp quality, which stored entry is later when comparable, and provenance leads. SHA-256: `200E93C6D5C15A8389D626DCBC4CDF208BD8FEA51B728862E4D25EBF3B50A559`.
- `unreconcilable-entries-for-review-v7.csv`: all 3,160 divergent field rows, including both field values as JSON and the same timestamp and provenance columns. SHA-256: `12B7FB0D8B83F88715A86BD96EB0BF80DCC3BD85A7DA0DC4730AF151B90BADC0`.

`FR-MCP-USECASE-016` and `FR-MCP-USECASE-017` are excluded because the operator already directed that both remain deleted on OMARCHY and be deleted on LEGION2. That decision is recorded separately in `operator-decision-usecase-016-017.md`.

## Entry groups

`review-groups-v1.csv` groups all 1,118 entries. It has 1,120 membership rows because two LEGION2-only rekey placeholders are listed both as missing on OMARCHY and as ID-mismatch candidates. The entry groups are:

- `missing-on-legion2`: 62 entries, 280 differing field rows.
- `missing-on-omarchy`: 5 entries, 31 differing field rows.
- `id-mismatch-candidate`: 2 of the five LEGION2-only entries, 12 field rows. Their `-REKEY-CANONICAL-20261115` suffixes point to canonical IDs that are also present. This is a candidate classification, not a verified replacement decision.
- `similar-at-least-98-assurance`: zero complete entries. No whole entry has every differing field equivalent under the conservative check.
- `requires-adjudication`: 1,051 entries, 2,849 differing field rows. These remain in the review queue; some may be resolvable after inspecting explicit decisions.

`review-field-rows-grouped-v1.csv` is the requested field-row view. It assigns all 3,160 differing rows to one primary group: 157 ID-mismatch candidates, 40 exact-rename similarity candidates, 280 missing-on-LEGION2 rows, 19 missing-on-OMARCHY rows, and 2,664 requiring adjudication. The 157 ID-mismatch rows include 145 acceptance-criteria rows where text and satisfaction match but IDs differ, plus 12 rows from two LEGION2-only rekey placeholders; those 12 also count toward the 31 raw presence differences missing on OMARCHY. The 40 similarity candidates match exactly after the mechanical `McpServer` to `QBrainAi` namespace or `/mcpserver` to `/qbrainai` route rename. This establishes narrow textual similarity; no row has yet been certified at 98% assurance that one version is the approved governing value. No probabilistic confidence score was inferred from text distance.

The grouping script is `Group-UnreconciledEntries.ps1`; it passed PowerShell parsing and PSScriptAnalyzer Warning/Error with zero findings, was run with `-WhatIf`, then wrote the CSVs. SHA-256 of `review-groups-v1.csv`: `064282804A4738432F919AC46CDFD6BFB6752D701E5EF57AFF3D92CDB2AD5118`. SHA-256 of `review-field-rows-grouped-v1.csv`: `B07ECE82DA85D3F363473567940683D462904E070D62B89C06F9B23822CA6F7D`.

## SQL method and limits

The scoped `Requirements` tables yielded 1,566 rows on PAYTON-LEGION2 and 1,515 on LAB-OMARCHY. Their SQL-stored `UpdatedAtUtc` values were read directly. The raw receipts are `legion2-sql-timestamps.json` and `omarchy-sql-timestamps.json`. The receipt hashes and exact SQL comparison counts are in `unreconciled-sql-timestamp-summary-v4.json`.

Of the 3,160 divergent field rows, the stored edit time is later on OMARCHY for 2,321 and later on LEGION2 for 21. Another 165 rows have an entry on only one machine, so relative recency cannot be compared. The remaining 653 have indeterminate last-edit order: 495 involve a legacy 1970 UTC placeholder and 158 are mapping rows whose SQL link table stores `CreatedAtUtc` but no `UpdatedAtUtc`.

The JSON reader used `ConvertFrom-Json -DateKind String`. Earlier comparison and table artifacts in this directory without the `v4` SQL summary or `v7` detailed table suffix converted UTC strings through local time and are superseded. For example, the raw SQL edit time for `FR-MCP-190` on LEGION2 is `2026-11-15T17:49:02.4750041Z`, while OMARCHY stores `2026-11-15T00:04:09.1230401Z`.

## Session and transcript provenance

The prior session-log search receipts are `session-provenance-candidates-refined.json` and `bulk-session-candidates.json`. A Codex or Claude session turn within 24 hours appears for 421 detailed field rows. A nearby turn is a lead, not proof of the field's governing decision.

All 1,061 unique divergent IDs were searched as literal terms in Codex and Claude transcripts on both machines. The search receipts are the `*-transcript-hits-v3.json` files. An additional OMARCHY Codex archive scan had zero hits. Time correlation against usable SQL timestamps appears in the `*-transcript-time-correlation-v3.json` files. It found 16 IDs with nearby events on LEGION2, 211 in OMARCHY Codex transcripts, and 439 in OMARCHY Claude transcripts. These sets overlap and must not be added together. Five IDs had no literal transcript hit on any searched machine. Transcript text was not copied into these receipts; they contain ID, path, event time, and role category only.

The Codex plugin's `client.SessionLog.GetAsync` call failed with `Unsupported task result type: System.Threading.Tasks.Task<UnifiedSessionLogDto>`. `QueryAsync` returned summary turns but not full actions or design decisions. The defect was filed as `triage-report-6e527f046d9949f98b215fdafba4cd3d`. Agent Help was also attempted and failed with `The pipe is being closed.` Thus the table's session and transcript columns are provenance leads, and every row remains `UNRECONCILED` pending examination of governing decisions.

No requirement record was changed by this SQL and transcript review pass. The Pester remediation remains gated on requirement reconciliation.
