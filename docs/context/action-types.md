# Recognized Action Types

Use these standardized type values when logging actions in session log turns.

- `edit` — file modification
- `create` — new file creation
- `delete` — file deletion
- `design_decision` — architectural or design choice
- `commit` — git commit (include SHA, branch, message, files)
- `pr_comment` — pull request comment (include PR number, full text)
- `issue_comment` — issue comment (include issue number, full text)
- `web_reference` — internet source consulted (include URL, title, usage)
- `dependency_add` — new dependency added (include name, version, license)
- `license_violation` — banned license detected
- `origin_violation` — banned country of origin detected
- `origin_review` — country of origin could not be determined
- `entity_violation` — banned organization or individual detected
- `copilot_invocation` — server-initiated Copilot call
- `policy_change` — workspace policy configuration change

- `tool_execution` — an actual host tool invocation, distinct from file-operation and design-decision actions. Server validation requires a nonnegative order and nonblank description/status. Status vocabulary remains compatible with host lifecycle values (for example pending, completed and failed); type matching is case-insensitive and stored fields are preserved. Keep the separate `tool_call` and `tool_result` dialog records. An execution action never substitutes for an explicit decision or no-decision attestation. (FR-MCP-AUDITACTION-001 / TR-MCP-AUDITACTION-001)
