# TEST-MCP-REQACPLUGIN

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-REQACPLUGIN

### TEST-MCP-REQACPLUGIN-001

Validate the Bash plugin family emits and hydrates acceptanceCriteria on FR/TR/TEST requirement create/update commands and exposes copyAcceptanceCriteriaFromTodo.


### TEST-MCP-REQACPLUGIN-002

Plugin regression tests prove caller-supplied acceptanceCriteria is not silently lost when requirement create/update responses explicitly report an empty criteria list.

**Acceptance Criteria:**
- [x] Direct sourced shell assertions pass for all five Bash plugin repos: criteria-only update emits caller criteria, no-criteria create omits criteria, and explicit empty response returns requirements_acceptance_criteria_not_captured. (evidence: Focused shell assertions passed for Codex, Claude Code, Claude Cowork, Copilot, and Grok.)
- [x] Focused Jest tests pass for Cline, Cline v2, and OpenCode covering criteria-only update forwarding and explicit empty-response failure. (evidence: Cline and Cline v2 requirements.test.ts passed; OpenCode complex-tools.test.ts passed with coverage disabled for focused scope after full file tests passed.)
- [x] TypeScript plugin builds pass after the guard is added. (evidence: npm run build passed for Cline, Cline v2, and OpenCode.)
