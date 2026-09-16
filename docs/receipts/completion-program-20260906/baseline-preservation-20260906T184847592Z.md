# Completion Program Phase 0 Baseline Preservation

Captured at 2026-09-06T18:48:47.592Z by Codex task `01a07803-b2f3-7bf1-acf7-44fbac9c8e72` using PowerShell.Mcp 1.14.0 console `#36788 Disco` with subagent id `sa-3a814463`.

## Scope

- Inventory and preserve the live dirty baseline only.
- No source, index, branch, TODO, requirement, session-log, build, deployment, or remote state was changed.
- The parent session `Codex-20260906T161930Z-plugin-session` and turn `req-20260906T183724Z-prompt-f860` were not opened, reset, updated, or completed.
- Backup contents were selected from Git dirty-path inventories. No recursive workspace dump was taken.
- Credentials and runtime-secret values are intentionally absent from this receipt. The local backup contains original audit artifacts and must be treated as sensitive.

## Root Baseline

- Repository: `F:\GitHub\McpServer`
- Branch: `develop`
- HEAD: `08eaf2a506a0aa2db89766e6a547d9ae1c85f681`
- HEAD tree: `40ae058c5bac675820a6b9dc020c7f7aa06839e9`
- Upstream: `origin/develop` at the same SHA; merge base is the same SHA.
- Baseline dirty paths before this receipt: 3,736 total, comprising 52 unstaged tracked paths, 0 staged paths, and 3,684 untracked paths.
- Archived root files: 3,734 files, 45,698,760 bytes. Two dirty paths are Git worktree pointers and were preserved through separate nested-repository snapshots.
- Root classification: 63 program paths, 5 requirements projections, 22 generated requirements projections, 3,643 validation-evidence paths, 1 runtime test artifact, and 2 nested-repository pointers.
- Status-count correction: the prior count of 396 used default `git status --porcelain`, while the preserved count of 3,736 used `--untracked-files=all`. Default mode collapses untracked directories, so these counts are different representations and do not prove dirty-state growth. Root HEAD did not drift from the expected `08eaf2a5`.

## Worktrees And Gitlinks

- Nine McpServer worktrees were registered.
- Candidate `F:\GitHub\McpServer\.mcpServer\worktrees\bug-triage-139-review` is clean on `codex/bug-triage-139-remediation` at `808ec049d56daf214c391beccd6b9d69bb9867c6`, matching the expected candidate.
- Integration worktree `F:\GitHub\McpServer\.mcpServer\worktrees\bug-triage-139-integrate` is clean at `910a444959969a87099b800b4b6fe12c77c0aece`.
- Clean existing worktrees: `sessionpersist/s1-red` at `0e0c5763`, `sessionsan/s3-red` at `d54f4e32`, `triage/113-tags` at `dfd7097b`, and `triage/tempvol` at `9c7c3ec3`.
- Dirty nested worktree `triage/stale-turns` is at `8ff862efd3a2b4f9e667b86b65ab18a5bb71d7c5` with 26 dirty files: 2 generated projections and 24 validation artifacts.
- Dirty nested worktree `triage/transcript` is at `dddcab83f13d579ca358316fd2b2d5e7dbda9133` with 26 dirty files: 5 requirements projections and 21 validation artifacts.
- Nested wiki clone is clean at `763c83803046a018107e06c9945e508551236d86`.
- `tools/McpServerTools` is indexed as gitlink `8de15e5ad60dabcc3693c02e7d73e4003a4d9b89`, but its working directory is empty and has no `.git` metadata.

## Sibling Plugins

- `mcpserver-codex-plugin`: `main` at `ad147fe7840e3f341454693b4d33f74bc5c5f3d2`, 26 dirty paths.
- `mcpserver-claude-code-plugin`: `main` at `239dad926b71d0540960b0285a3bd9cd8b9b6054`, 31 dirty paths.
- `mcpserver-claude-cowork-plugin`: `main` at `f56bd106fcfcfb68e52bad30717f9b25514c98ef`, 33 dirty paths.
- `mcpserver-copilot-plugin`: `main` at `e0c6a173afa8600289020e2617b84a6a18d33833`, 33 dirty paths.
- `mcpserver-grok-plugin`: `main` at `e3a95067dca7a21880f0247ab5817d8b070b725c`, 33 dirty paths.
- `mcpserver-cline-plugin`: `main` at `5d9ba01949f19b16de6106517ca1292468a9d9df`, 15 dirty paths.
- `mcpserver-cline-v2-plugin`: `main` at `406e6dc43b064c2d3d4428b25750836ac054ed36`, 15 dirty paths.
- `mcpserver-opencode-plugin`: `master` at `5731b2872e17f3eba254312aab24c1b974414a6a`, 15 dirty paths.
- The 201 plugin paths classify as 166 program paths, 30 distribution-metadata paths, and 5 runtime test artifacts. The common `resolve-cache-dir.ps1` SHA-256 is `9924A0F58A5157421E5EF6521B7626211776E517FECB09A0C2D50004E6348793` in core and all eight plugin repositories, confirming that common file is synchronized in the dirty state.

## Preservation Evidence

- Backup root: `C:\Users\kingd\AppData\Local\Temp\McpServer-completion-preservation\20260906T184847592Z`
- Overall manifest: `preservation-manifest.json`
- Overall manifest SHA-256: `5E3AE9DDBE95DF7D2174BB239BE22206362D574D165D2E7C0DCD7E8A2382102A`
- Recovery units: 11 repositories, 3,989 dirty paths, 3,987 archived files, and 49,907,921 source bytes.
- Compressed archives total 11,784,324 bytes.
- Root archive SHA-256: `D6C90A6C0DB000F56B51F35164CE3E1E5B23FE0A3E1E69B7342349D7DBB8265F`
- Nested stale-turns archive SHA-256: `660383D55764574DC3E7A8BAFD6DD5E9DA6E318F0E0EEE63BDE34257124B65C2`
- Nested transcript archive SHA-256: `DC84A9CAEC074F8634FE6451FD0C75959C1532422B4A727AE720ACA9D6658355`
- Plugin archive SHA-256 values: Codex `835504B42FE4710707A782111AF04555A6957DD1A9D88C1F1B272E2E84E226BE`; Claude Code `E5EB24DC08D3D653D514461B564C8CAFC3381C83E7EF543A2D64F5AA507CAF7D`; Claude Cowork `75C6D5973706949860D86ADD98210667A487FF2A0E98D432BF065D997E847B6C`; Copilot `BE73E59E70B23C103196467365DDA2924AB52BAB0735CDC8D06CD320740C91BD`; Grok `AFCEB78A5CCD47BA0B5B913CB2D2675EC978B7DDDAA87DAD2631B6D09F21378D`; Cline `BE485D1E43DCB2B9F5BDC6F6CFB71FF11283C90AAB1BE1730AA81B2E85512877`; Cline v2 `3547C512860C7E4335A0E56A16FAEA7A295BD515B0F295D3A2B7D516BD17F627`; OpenCode `5D10DE879637C7F98C3806C8B3D9F4801DBC6117C7E7DEBAEEBE71F8DCCBAABF`.
- Verification result: 0 archive-hash failures, 0 per-entry hash failures, 0 files changed during capture, and 0 repositories whose status changed during capture.
- Independent re-verification at `2026-09-06T18:54:49.746Z` recalculated all 11 ZIP SHA-256 values from disk: 11 matched their repository manifests and 0 mismatched. The overall preservation manifest remained `5E3AE9DDBE95DF7D2174BB239BE22206362D574D165D2E7C0DCD7E8A2382102A`.
- Every repository snapshot includes before/after status, unstaged and index patches, a HEAD-inclusive patch, index-stage metadata, untracked-path inventory, per-entry hashes, and archive hashes.

## Classification Boundary

- Root source, tests, build logic, plugin core, Handoff code, requirements projections, and generated wiki projections are completion-program candidates pending later review.
- Validation receipts are preserved evidence, not production implementation. Runtime `testResults.xml` files are preserved operational artifacts, not source.
- The two dirty triage worktrees are separate in-progress efforts. Their files and Git state must not be folded into the completion branch without explicit reconciliation.
- Sibling plugin changes are program candidates because their common resolver exactly matches core; repo-specific manifests, versions, package metadata, and tests still require independent reconciliation before any sync or commit.

## Open Risks And Blockers

- `git submodule status --recursive` fails with `fatal: no submodule mapping found in .gitmodules for path '.worktrees/session-persist'`. Gitlink and worktree inventories were captured directly instead.
- The empty `tools/McpServerTools` gitlink has no local repository metadata, so its checked-out content could not be preserved or validated.
- The root remains intentionally dirty. Since capture, expanded status gained this receipt and the concurrent `linux-preflight-20260906T185128Z.md` receipt; neither path was included in the original backup.
- Drive `F:` had 1.76 GiB free during capture. No build was run, so capacity for later full validation remains unproven.
- The backup is local temporary storage, not a commit or remote copy. Its hashes prove captured integrity but do not make it durable against deletion of the temp directory.
- No production implementation, merge, build, test, deployment, plugin synchronization, TODO mutation, requirements mutation, or completion claim was attempted in this bounded task.

## Correction Provenance

- Corrected at `2026-09-06T18:54:49.746Z` after review identified an invalid comparison between default and expanded untracked status modes.
- Original receipt SHA-256 before correction: `F6721330E682BF98E0B37D8B047BBF9DB6E71CFF2A4B313A45C1857FFBCC0E86`.
- Re-running both modes against the same current worktree produced 397 default entries: 52 tracked and 345 collapsed untracked entries. Expanded mode produced 3,738 entries: 52 tracked and 3,686 untracked files.
- Comparing current expanded status to the captured expanded status found exactly two additions and no removals: this baseline receipt and `linux-preflight-20260906T185128Z.md`.
- Both added files are in the newly untracked `docs/receipts/completion-program-20260906/` directory, represented as one entry in default mode. Removing that one collapsed directory entry reconciles the current default count of 397 to the earlier default count of 396. The captured expanded count remains 3,736 after excluding the two post-capture receipts.
- The earlier claim of 3,340 new dirty paths is withdrawn. The evidence shows representation expansion plus two post-capture receipt files, not that amount of repository drift.
- No backup archive, repository manifest, patch, status capture, or overall preservation manifest was changed by this correction.
- Post-correction verification at `2026-09-06T18:56:53.365Z` rehashed all 3,987 captured source files across 11 repositories: 0 content differences, 0 removed status entries, and 0 unexpected added status entries. The only added status paths are the two named receipts.
