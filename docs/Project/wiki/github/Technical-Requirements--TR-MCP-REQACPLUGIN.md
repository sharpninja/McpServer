# TR-MCP-REQACPLUGIN

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-REQACPLUGIN-001

**Plugin-side schema + shaper changes for AcceptanceCriteria** — Bash plugins gain _repl_emit_acceptance_criteria_block helper and per-method emit/hydrate calls in _repl_requirements_typed_params for createFr/createTr/createTest/updateFr/updateTr/updateTest. TS plugins gain shared AcceptanceCriterion JSON schemas and typedParams pass-through for the same six methods plus per-kind/mixed batch records items.
**Covered by:** FR: FR-MCP-REQACPLUGIN-001; TEST: TEST-MCP-REQACPLUGIN-BASH, TEST-MCP-REQACPLUGIN-LIVE, TEST-MCP-REQACPLUGIN-TS
**Status:** pending
Scope: layer-1+

## TR-MCP-REQACPLUGIN-002

**Plugin-side AcceptanceCriteria capture verification** — Bash and TypeScript plugin requirement mutation dispatchers SHALL reject successful-looking create/update responses that explicitly show an empty acceptanceCriteria list when the caller supplied a non-null criteria array, while preserving backward compatibility for responses that omit the field.
**Covered by:** FR: FR-MCP-REQACPLUGIN-002; TEST: TEST-MCP-REQACPLUGIN-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [x] Bash plugins enforce the check after workflow and typed successful create/update responses through a shared helper. (evidence: Codex, Claude Code, Claude Cowork, Copilot, and Grok direct sourced shell assertions passed.)
- [x] TypeScript plugins enforce the same check after workflow and typed successful create/update responses through shared response inspection. (evidence: Cline, Cline v2, and OpenCode focused Jest tests passed.)
- [x] The guard is scoped to FR/TR/TEST create/update mutations with caller-supplied acceptanceCriteria and keeps no-criteria mutations compatible. (evidence: Focused shell no-AC assertions, focused Jest files, and npm builds passed.)

