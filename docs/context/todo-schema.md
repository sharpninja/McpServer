# TODO Management Schema Reference

Load this file when you need to create, update, query, or manage project TODOs.

## Endpoints

- `GET /mcpserver/todo` — list all todos
- `POST /mcpserver/todo` — create a new todo
- `GET /mcpserver/todo/{id}` — get a specific todo
- `PUT /mcpserver/todo/{id}` — update a todo
- `DELETE /mcpserver/todo/{id}` — delete a todo
- `GET /mcpserver/todo/{id}/prompt/implement` — get implementation prompt
- `GET /mcpserver/todo/{id}/prompt/plan` — get planning prompt
- `GET /mcpserver/todo/{id}/prompt/status` — get status prompt
- `POST /mcpserver/todo/{id}/requirements` — add requirements to a todo

## Naming Conventions (Normative)

- TODO IDs for new items must be uppercase kebab-case with exactly 3 segments:
  `<SDLC-PHASE>-<AREA>-###`
- Required regex: `^[A-Z]+-[A-Z0-9]+-\d{3}$`
- Valid examples: `PLAN-NAMINGCONVENTIONS-001`, `MCP-API-042`
- Invalid examples: `plan-api-001`, `MCP-API-42`, `MCPAPI001`
- When creating/updating `dependsOn`, each dependency ID must follow the same TODO ID convention.

## TodoFlatItem (returned by GET)

```json
{
  "id": "string — unique TODO ID in format <PHASE>-<AREA>-### (e.g. 'MCP-AUTH-001')",
  "title": "string — brief title",
  "section": "string — grouping category (e.g. 'Backend', 'Frontend', 'Infrastructure')",
  "priority": "string — 'critical', 'high', 'medium', or 'low'",
  "done": "boolean — whether the task is complete",
  "estimate": "string|null — effort estimate (e.g. '2h', '1d')",
  "note": "string|null — additional context",
  "description": ["string array — detailed description lines"],
  "technicalDetails": ["string array — technical implementation notes"],
  "implementationTasks": [
    { "task": "string — subtask description", "done": "boolean" }
  ],
  "completedDate": "string|null — ISO 8601 when completed",
  "doneSummary": "string|null — summary of what was done",
  "remaining": "string|null — what work remains",
  "priorityNote": "string|null — why this priority",
  "reference": "string|null — link or reference",
  "dependsOn": ["string array — IDs of prerequisite todos"],
  "functionalRequirements": ["string array — FR IDs"],
  "technicalRequirements": ["string array — TR IDs"]
}
```

## TodoCreateRequest (POST body)

```json
{
  "id": "string — REQUIRED TODO ID matching ^[A-Z]+-[A-Z0-9]+-\\d{3}$",
  "title": "string — REQUIRED brief title",
  "section": "string — REQUIRED grouping category",
  "priority": "string — REQUIRED: 'critical', 'high', 'medium', or 'low'",
  "estimate": "string|null",
  "description": ["string array|null"],
  "technicalDetails": ["string array|null"],
  "implementationTasks": [{ "task": "string", "done": false }],
  "note": "string|null",
  "remaining": "string|null",
  "dependsOn": ["string array|null — IDs of prerequisite todos"],
  "functionalRequirements": ["string array|null"],
  "technicalRequirements": ["string array|null"]
}
```

## TodoUpdateRequest (PUT body)

```json
{
  "title": "string|null — only include fields you want to change",
  "priority": "string|null",
  "section": "string|null",
  "done": "boolean|null — set true to mark complete",
  "estimate": "string|null",
  "description": ["string array|null"],
  "technicalDetails": ["string array|null"],
  "implementationTasks": [{ "task": "string", "done": true }],
  "note": "string|null",
  "completedDate": "string|null",
  "doneSummary": "string|null",
  "remaining": "string|null",
  "dependsOn": ["string array|null"],
  "functionalRequirements": ["string array|null"],
  "technicalRequirements": ["string array|null"]
}
```

## McpTodo Module — PowerShell Lifecycle

```powershell
# List todos at session start
Get-McpTodo | Format-Table id, title, priority, done

# Get a specific todo
Get-McpTodo -Id "MCP-AUTH-001"

# Create a new todo
New-McpTodo -Id "MCP-AUTH-001" -Title "Add JWT auth" -Section "Backend" -Priority high `
  -Description @("Implement JWT bearer tokens") -Estimate "4h"

# Update fields
Update-McpTodo -Id "MCP-AUTH-001" -Remaining "Need tests"

# Mark complete
Complete-McpTodo -Id "MCP-AUTH-001" -DoneSummary "JWT auth complete"

# Get implementation guidance
Get-McpTodoPrompt -Id "MCP-AUTH-001" -Type implement

# Add requirements
Add-McpTodoRequirements -Id "MCP-AUTH-001" -FunctionalRequirements @("FR-AUTH-001") `
  -TechnicalRequirements @("TR-AUTH-001")

# Delete
Remove-McpTodo -Id "MCP-AUTH-001"
```

## McpTodo Module — Bash Lifecycle

```bash
source ./mcp-todo.sh
mcp_todo_init
mcp_todo_list | jq '.items[] | {id, title, done}'
mcp_todo_get "MCP-AUTH-001"
mcp_todo_create "MCP-AUTH-001" "Add JWT auth" "Backend" "high" '{"estimate":"4h"}'
mcp_todo_update "MCP-AUTH-001" '{"remaining":"Need tests"}'
mcp_todo_complete "MCP-AUTH-001" "JWT auth complete"
mcp_todo_prompt "MCP-AUTH-001" "implement"
mcp_todo_add_requirements "MCP-AUTH-001" '{"functionalRequirements":["FR-AUTH-001"]}'
mcp_todo_delete "MCP-AUTH-001"
```

## Raw API (for understanding only — use modules)

The modules automatically include the `X-Workspace-Path` header for correct workspace routing. Raw calls will target the wrong workspace.
