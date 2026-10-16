<!-- SPDX-License-Identifier: Apache-2.0 -->
# Requirements reconciliation before the architecture HV: shared brief

Operator request (2026-10-12, verbatim): "Before doing HV, please do a THOROUGH and holistic reconciliation of the new reqs to the existing reqs, map relationships between new reqs and existing code (especially tests) and project potential problems introduced by the changes."

You are one of five parallel read-only reconciliation reviewers. Your lane and output file are in the prompt that launched you. Read this whole brief first.

## Standing rules (mandatory)

- READ-ONLY everywhere. Your only write is your one results file under `scratchpad/recon/`. Do not edit the architecture, its parts, the plan, requirements, TODOs, memories, or settings.
- Bash may only launch pwsh (`pwsh -NoProfile -Command ...`). Prefer the Read and Grep tools. No Python, no Node, no shell text tools. Git is read-only: `git show`, `git grep`, `git log`, `git diff`, `git ls-tree` only.
- Never print, quote, or copy any apiKey, X-Api-Key, api_key, token, password, or credential value; do not read `~/.creds` or `~/.config/qbrain-test`; the requirement export may contain receipt hashes, which are fine, but no keys.
- No network, no MCP or REST calls, no session-log or TODO writes.
- No tables in your output; bullets and short paragraphs. No em dashes or en dashes.
- Facts carry citations (`file:line`, `path:line@a8d3934b`, or a requirement id with its field); inference is labeled "Inference:".

## Sources (scratchpad root `/tmp/claude-1000/-home-sharpninja-github-McpServer--claude-worktrees-session-20261009/49428485-c8ea-46d7-86c6-58add8faed97/scratchpad`, worktree `/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009`)

- NEW requirements and design (what is proposed):
  - The plan `docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md` in the worktree: requirements catalog at "## Requirements catalog" (21 new FRs, 25 new TRs, 23 TESTs, mappings, "Updated existing requirements (32 records in 27 entries)", 9 new use cases, and "Agent-context requirements (catalog completed in P0)" with 9 FR scope statements), TDD AC allocation, scope catalogs A, B, C. Note: the plan has NOT yet been re-derived from the round-5 architecture; where the plan and the architecture differ, the architecture is the authority and the difference is itself a finding to report.
  - The architecture `docs/architecture/SessionTurn-Review-AgentContext-Architecture.md` (5354 lines, the authority; section 3.1 decisions including the round-5 operator decisions and resolutions, section 3.5 shared components, sections 4 to 6 units with their named test cases, section 8.3 bounded contexts) and `docs/Stateless-Agent-Context-Management.md`.
  - Round-5 review and edit logs in `scratchpad/review-arch-r5/` (`dry-ab.md`, `dry-c.md`, `whatif-ab.md`, `whatif-c.md`, `edit-*.md`) explain what changed and why.
- EXISTING requirements (what is there):
  - The live requirement store of the main workspace, exported read-only today through the plugin: `scratchpad/recon/store/listFr.yaml` (353 FRs with ACs and evidence), `listTr.yaml` (487 TRs), `listTest.yaml` (512 TESTs), `listMappings.yaml` (1037 FR to TR and FR to TEST mappings). Each file is the plugin's YAML response (`payload.result.items`). This is the source of truth.
  - The generated projections `docs/Project/Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`, `Requirements-Matrix.md` in the worktree (may lag the store; a difference is worth one line, not a section).
- EXISTING code and tests: `origin/develop` at `a8d3934b` is the baseline the architecture cites (read with `git -C <worktree> show a8d3934b:<path>` and `git -C <worktree> grep -n <pattern> a8d3934b -- <paths>`). Tests live under `tests/` (unit: QBrainAi.Support.Mcp.Tests, QBrainAi.Client.Tests, QBrainAi.Cqrs.Tests, QBrainAi.Repl.Core.Tests, QBrainAi.McpAgent.Tests, and others; integration: *.IntegrationTests; BDD validation: *.Validation; plugin integration: QBrainAi.PluginIntegration.Tests) and the plugin core tests under `plugins/core/` (Pester and the bats cases the plan ports); `build/Build.Tests` is infrastructure.
- PENDING code changes not yet on develop (context for problem projection): the worktree `/home/sharpninja/github/McpServer/.claude/worktrees/netanalyzers-all` (branch `chore/netanalyzers-all`, committed IDE0130 namespace-to-folder moves and CA rule fixes, plus an uncommitted switch to Meziantou.NET.Sdk with analyzer remediation). It renamed about 4,468 test methods to remove underscores (CA1707) and moved namespaces to match folders (IDE0130). The plan and architecture name hundreds of new test cases with underscores (for example `TurnWrite_ForcedInterleaving_ReviewResetVsComplete_NeverClosedWithoutReview`), and existing TEST records and AC evidence in the store cite test names and paths.

## Output conventions

Markdown, bullets, no tables, no em or en dashes. Sections as your prompt asks. End with "Summary for the coordinator" (under 300 words) and return that summary as your final message.
