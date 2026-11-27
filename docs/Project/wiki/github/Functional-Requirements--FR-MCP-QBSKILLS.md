# FR-MCP-QBSKILLS

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-QBSKILLS-001 Agent Skills registry and progressive disclosure

QBAgent provides an agentskills.io-compatible skill registry/loader. Discovery loads only each skill name+description; Activation loads the full SKILL.md on demand; Execution follows the instructions using the baseline tools. SKILL.md frontmatter requires name and description (optional license/version/allowed-tools).
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Discovery returns only name+description for every available skill; load returns the full SKILL.md body.
- [ ] A SKILL.md missing name or description is rejected by the manifest parser.

## FR-MCP-QBSKILLS-002 Skill tool (list_skills / load_skill)

QBAgent exposes external tools list_skills and load_skill(name) so the model can discover and activate skills at runtime under progressive disclosure.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] list_skills returns the discovery list; load_skill returns the named skill body.

## FR-MCP-QBSKILLS-003 Vendor dotnet/skills and author workspace skills

The repo vendors dotnet/skills (agentskills.io) and authors workspace skills in SKILL.md format: byrd-tdd-process, mcp-session-logging, mcp-todo, mcp-requirements-traceability, and usage skills for the git/bash/edit tools. Discovery scans both vendored and workspace skill roots.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Discovery includes both vendored dotnet/skills and authored workspace skills.
- [ ] Authored workspace SKILL.md files pass frontmatter validation.

