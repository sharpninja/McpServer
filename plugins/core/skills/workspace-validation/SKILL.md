---
name: workspace-validation
description: Run read-only MCP workspace hygiene validation. Never auto-repair or delete records.
---

# Workspace Validation

Use the shared workspace-validation surfaces. Validation is read-only. Do not repair, delete, or mutate records from findings.

```yaml
method: workspace.validate
method: workflow.workspace.validate
```

Native MCP tool: `workspace_validate`.
Director: `validate-workspace`.
REST: `POST /mcpserver/workspace-validation/validate`.
