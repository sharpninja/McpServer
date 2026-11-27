# FR-MCP-REQACPLUGIN

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-REQACPLUGIN-001 Plugins expose AcceptanceCriteria on requirements surface

Every MCP server plugin (bash and TypeScript families) lets its agent set and read structured acceptanceCriteria ({id,text,isSatisfied,evidence}) on FR/TR/TEST create/update via the plugin command surface, including hydration on partial updates.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Bash family typed-params builder emits acceptanceCriteria block on create+update for FR/TR/TEST
- [ ] TypeScript family tool inputSchema declares acceptanceCriteria + typedParams forwards it
- [ ] Plugin tests cover both create and partial-update hydration paths

## FR-MCP-REQACPLUGIN-002 Plugins fail on proven AcceptanceCriteria loss

Every MCP server plugin that accepts caller-supplied requirement acceptanceCriteria must either preserve the supplied criteria or fail loudly when the mutation response proves the criteria were dropped.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Criteria-only requirement updates preserve the caller-supplied acceptanceCriteria block instead of replacing it with an empty or stale hydrated list. (evidence: Direct sourced shell assertions passed for Codex, Claude Code, Claude Cowork, Copilot, and Grok; Jest criteria-only update tests passed for Cline, Cline v2, and OpenCode.)
- [x] If a caller supplies acceptanceCriteria and a successful mutation response explicitly returns acceptanceCriteria empty, the plugin reports requirements_acceptance_criteria_not_captured instead of success. (evidence: Direct sourced shell assertions and focused Jest tests exercise the explicit empty-response case.)
- [x] Requirement create/update calls without acceptanceCriteria continue to work without injecting an empty criteria list. (evidence: Shell no-AC create assertions passed and existing TypeScript focused tests remain green.)

