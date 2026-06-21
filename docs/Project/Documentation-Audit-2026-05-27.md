# Documentation Audit - 2026-05-27

## Scope

DOC-AUDIT-001 required a thorough documentation audit across MCP Server documentation surfaces, with special attention to stale or contradictory guidance around Byrd process, MCP TODO/session-log usage, Nuke deployment, plugin usage, federation topology, and requirements traceability.

Audited surfaces:

- Root operator docs: `README.md`, `AGENTS.md`, `AGENTS-README-FIRST.yaml`
- Agent and REPL docs: `docs/AGENT-PLUGIN-AVAILABILITY.md`, `docs/REPL-AGENT-GUIDE.md`, `docs/REPL-MIGRATION-GUIDE.md`, `docs/REPL-USER-GUIDE.md`, `docs/context/*.md`
- Requirements docs and generated exports: `docs/Project/*.md`, `docs/Project/wiki/azure/*`, `docs/Project/wiki/github/*`, `docs/requirements/requirements-wiki-documents.zip`
- Marker and template source: `templates/prompt-templates.yaml`
- Deployment and pipeline docs: `docs/MCP-SERVER.md`, `docs/USER-GUIDE.md`, `docs/AZURE-PIPELINES.md`, `.github/workflows/build.yml`, `azure-pipelines.yml`, `scripts/Publish-RequirementsWiki.ps1`
- Marketing and user-facing docs under `docs/Marketing/`

## Findings

1. Agent STDIO protocol wording was stale in multiple agent-facing surfaces.
   `README.md`, `docs/AGENT-PLUGIN-AVAILABILITY.md`, `docs/REPL-AGENT-GUIDE.md`, and `templates/prompt-templates.yaml` still described formatted YAML envelopes for direct `mcpserver-repl --agent-stdio` callers. Current plugin guidance requires one single-line JSON request envelope per stdin line, with formatted YAML and `type: batch` envelopes rejected.

2. Plugin availability docs were missing the current acquisition rule and Grok plugin surface.
   `docs/AGENT-PLUGIN-AVAILABILITY.md` described local roots as the practical path, but the marker now requires agents to acquire plugins through the MCP Server tool registry before treating local root hints as fallback verification. The same doc also omitted `mcpserver-grok-plugin`.

3. Requirements traceability for documentation guidance guards was incomplete.
   FR-MCP-064 and TR-MCP-DOC-001 existed, but there was no TEST requirement tied to executable checks for agent-facing docs, marker templates, pipeline references, and generated wiki output parity.

4. TR-MCP-DOC-001 status conflicted across requirements surfaces.
   `docs/Project/Technical-Requirements.md` still showed TR-MCP-DOC-001 as planned while `docs/Project/Requirements-Matrix.md` already marked it complete. The audit resolved the source document to the matrix state and added the new guard coverage.

5. Requirements wiki export parity needed an executable guard.
   Azure and GitHub wiki folders had the expected generated files, with Azure-only `.order` and GitHub-only `_Sidebar.md` / `_Footer.md`, but this was not guarded by a test before this audit.

6. Deployment guidance was current for live operator docs.
   `README.md` and `docs/USER-GUIDE.md` direct service redeployments through the Nuke `UpdateService` target. Historical session plans still mention direct `scripts/Update-McpService.ps1`; those are retained as historical artifacts rather than live instructions.

## Changes Made

- Updated direct STDIO guidance to require single-line JSON request envelopes in:
  - `README.md`
  - `docs/AGENT-PLUGIN-AVAILABILITY.md`
  - `docs/REPL-AGENT-GUIDE.md`
  - `templates/prompt-templates.yaml`
- Added plugin registry acquisition guidance and Grok plugin coverage to `docs/AGENT-PLUGIN-AVAILABILITY.md` and `docs/REPL-AGENT-GUIDE.md`.
- Added `tests/Build.Tests/DocumentationGuidanceTests.cs` to guard:
  - single-line JSON stdio guidance
  - current pipeline file references
  - generated Azure/GitHub requirements wiki file parity
- Added TEST-MCP-147 through the MCP requirements workflow and mapped it to FR-MCP-064 / TR-MCP-DOC-001.
- Updated source requirements docs and regenerated requirements wiki outputs plus `docs/requirements/requirements-wiki-documents.zip`.

## Validation

- Red gate: `dotnet test tests\Build.Tests\Build.Tests.csproj -c Debug --filter FullyQualifiedName~DocumentationGuidanceTests --no-restore` initially failed on stale STDIO guidance.
- Green gate: the same focused test command passed with 3 passed, 0 failed, 0 skipped.
- Requirements workflow created `TEST-MCP-147` and mapped it to `FR-MCP-064` / `TR-MCP-DOC-001`.
- Requirements wiki export was regenerated with `workflow.requirements.generateDocument` using `format: wiki` and `docType: all`.
- ZIP artifact was refreshed from the generated export payload at `docs/requirements/requirements-wiki-documents.zip`.

## Follow-On Plan

No separate follow-on TODO is required for DOC-AUDIT-001. The audit identified a bounded set of contradictory guidance and implemented the fixes in this slice. Future documentation changes should keep `DocumentationGuidanceTests` in the Build.Tests gate so stale stdio, plugin acquisition, pipeline, and generated wiki parity guidance fails before merge.
