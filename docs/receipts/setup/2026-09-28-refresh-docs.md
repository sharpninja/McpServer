# refresh-docs receipt 2026-09-28 (CT)

Class: operator-directed `/refresh-docs` then `/wrap-up` through commit-sync pause.
Agent: GrokCode on PAYTON-LEGION2
Branch: `develop` @ `d18ba00b` (ahead of origin/develop by 1; local setup draft commit not pushed)
Trust: marker `AGENTS-README-FIRST.yaml` signature verified via plugin Status; `/health` nonce echoed Healthy (`1.4.39+c59185ad0bcc518fd673a2f8b418d30739765f19`, storage reachable).
REPL: `MCP_REPL_EXECUTABLE=C:\Users\kingd\.dotnet\tools\mcpserver-repl.exe` (not on default PATH in this non-interactive shell).

## Context

- MCP-SETUPPROMPT-001 already marked done; HV overall **AGREE** Accuracy 99 Completeness 99 (`docs/receipts/hv/hostile-validator-setupprompt-overall-20260928T153330Z.md`); post-dry-run receipt present.
- Draft candidate: `docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md` (committed in `d18ba00b`).

## Step 1 - Docs refreshed from code/facts

- `README.md` - Git HEAD tip `d18ba00b`; new section **Frontier agent setup prompt (draft)** with draft + receipt pointers.
- `docs/README.md` - index link to setup draft.
- `docs/USER-GUIDE.md` - section 1 pointer to draft + HV AGREE note.
- `docs/FAQ.md` - Q/A for frontier-agent setup prompt.
- `docs/MCP-SERVER.md` - draft pointer section.
- No FR/TR/TEST invented by hand.

## Step 2 - Prune

- Deleted untracked local draft backups under `docs/setup/` (`*.bak-pre-*`, `*.w06bak`). Not wiki sources.
- Flagged (kept): historical superseded drafts already bannered elsewhere (`docs/Development-Process-draft-v3.md`, UseCase v1/v2 plans, etc.) - no new deletes of published project docs.
- Flagged (not deleted): large pre-existing dirty tree under `tests/**` and memory-bench result JSON/MD - outside refresh-docs scope; leave for commit-sync ack.

## Step 3 - wiki.yaml

- Schema `mcp-wiki-export/v1` remains.
- Document count **44 -> 45**.
- Added `setup-frontier-agent-prompt` -> `docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md` target `Setup/Frontier-Agent-Setup-Prompt-Draft.md` platforms github+azure.
- Navigation: under User Guides after `docs-index`.
- YAML parse OK; all 45 ids present in navigation; setup source on disk.

## Step 4 - Export

- `workflow.requirements.generateDocument` format=wiki docType=all **success**.
- Wrote `contentBase64` ZIP (1,278,835 bytes) to:
  - `docs/requirements/requirements-wiki-documents.zip`
  - `docs/Project/requirements-wiki-documents.zip` (twin)
- sha256 `BDE2C397C406B41F3DCF9CF66B85A09FA61A39320D08B4552827793EBB41EB0A`
- entryCount **101** (was 99); includes `github/Setup/Frontier-Agent-Setup-Prompt-Draft.md` and azure twin.
- Extracted/synced into `docs/Project/wiki/{github,azure}/`; **0** project-doc wiki deletes (`git diff --diff-filter=D` empty for those paths).

## Validation this refresh

- See wrap-up Phase B for `git diff --check` / build-test evidence.

## Commit / push

- **Not done** (refresh-docs + wrap-up commit-sync pause). Awaiting parent/user ack.
