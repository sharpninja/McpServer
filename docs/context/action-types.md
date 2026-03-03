# Recognized Action Types

Use these standardized type values when logging actions in session log entries.

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
