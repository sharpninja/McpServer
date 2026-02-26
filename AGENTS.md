# Agent Instructions

## ⚠️ PRIORITY ORDER — NON-NEGOTIABLE ⚠️

**Speed is never more important than following workspace procedures.**

Before doing ANY work on ANY user request, you MUST complete these steps in order:

1. **Read `AGENTS-README-FIRST.yaml`** in the repo root for the current API key and endpoints
2. **GET `/health`** to verify the MCP server is running
3. **POST `/mcp/sessionlog`** with your session entry — do NOT proceed until this succeeds
4. **GET `/mcp/sessionlog?limit=5`** to review recent session history for context
5. **GET `/mcp/todo`** to check current tasks
6. **THEN** begin working on the user's request

On EVERY subsequent user message:
1. POST an updated session log entry BEFORE starting work
2. Complete the user's request
3. POST the final session log with results, actions taken, and files modified

**If you skip any of these steps, STOP and go back and do them before continuing.**
Session logging is not optional, not deferred, and not secondary to the task.
Failure to maintain the session log is a compliance violation.