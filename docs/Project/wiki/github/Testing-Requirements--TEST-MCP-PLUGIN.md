# TEST-MCP-PLUGIN

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-PLUGIN

### TEST-MCP-PLUGIN-011

Validates TR-MCP-PLUGIN-011. mcpserver-claude-code-plugin/tests/StopGateHardening.Tests.ps1: 'does not block a completed code-edit turn whose only audit signal is commits' seeds current-turn.yaml (status=completed, codeEdits=1, all audit counters 0 except auditCommits=2) and asserts the stop-gate output does not contain a block/'audit is incomplete'; 'no-ops instead of blocking for a task-notification phantom turn' seeds status=in_progress, queryTitle=<task-notification>, zero work, and asserts no block and no completeTurn in the repl log. Red before the fix (audit-incomplete block; completeTurn-did-not-mark-completed block), green after. Pester harness uses MCP_CACHE_DIR_OVERRIDE + MCP_PLUGIN_REPL_LOG/RESPONSE mocks.


### TEST-MCP-PLUGIN-012

Validates TR-MCP-PLUGIN-012. mcpserver-claude-code-plugin/tests/CurrentTurnSessionRebind.Tests.ps1 dot-sources ..\lib\repl-invoke.ps1, seeds session-state.yaml (sessionId B) and current-turn.yaml (sessionId A, in_progress), calls Assert-ReplCurrentTurnFresh, and asserts it returns true and current-turn.yaml sessionId is now B. Red before the fix (stale-sessionId reject returned false), green after replacing the two sessionId hard-rejects with an active-session re-bind. ReplFailsafe.Tests.ps1 remain green (AC2).


### TEST-MCP-PLUGIN-013

HookTurnDedupe.Tests.ps1: the UserPromptSubmit hook reuses the open turn on a duplicate prompt (returns turn-already-open, does not call beginTurn) and opens a new turn for a differing prompt. Hermetic via a self-contained marker plus the forced host identity. Validates TR-MCP-PLUGIN-013 / BUG-TRIAGE-077.
