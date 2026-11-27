# TR-MCP-COMP

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-COMP-001

**Workspace Compliance Ban Lists** — `WorkspaceDto`, `WorkspaceCreateRequest`, and `WorkspaceUpdateRequest` include four `List<string>` properties: `BannedLicenses`, `BannedCountriesOfOrigin`, `BannedOrganizations`, `BannedIndividuals`. `MarkerFileService.BuildTemplateContext` exposes these as Handlebars context (null when empty). `DefaultPromptTemplate` uses `{{#if}}` / `{{#each}}` blocks to conditionally render compliance sections. Recognized action types: `license_violation`, `origin_violation`, `origin_review`, `entity_violation`, `dependency_add`.
**Covered by:** `IWorkspaceService.cs`, `MarkerFileService.cs`
**Status:** pending
Scope: layer-1+

## TR-MCP-COMP-002

**Agent Values Prompt Sections** — `DefaultPromptTemplate` includes five mandatory non-configurable sections: (1) Absolute Honesty, (2) Correctness Above All, (3) Complete Decision Documentation, (4) Professional Representation and Audit Trail, (5) Source Attribution. Each section specifies required session log action types (`commit`, `pr_comment`, `issue_comment`, `web_reference`, `design_decision`).
**Covered by:** `MarkerFileService.DefaultPromptTemplate`
**Status:** pending
Scope: layer-1+

## TR-MCP-COMP-003

**Session Continuity Protocol** — The `default-marker-prompt` template (YAML) includes Requirements Tracking, Design Decision Logging, and Session Continuity sections. Agents must: read marker file at session start, query recent session logs, query TODOs, read Requirements-Matrix.md, post updated session logs every ~10 interactions, and capture requirements/decisions as they emerge.
**Covered by:** `templates/prompt-templates.yaml` (`default-marker-prompt`), `PromptTemplateService`
**Status:** pending
Scope: layer-1+

