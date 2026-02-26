using System.Diagnostics;
using System.Globalization;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Writes and removes <c>AGENTS-README-FIRST.yaml</c> marker files in workspace roots so that
/// agents can discover the correct port and endpoints for calling the MCP server.
/// Prompt templates use Handlebars syntax with the full workspace definition available as context.
/// </summary>
public static class MarkerFileService
{
    /// <summary>Well-known marker file name placed at the workspace root.</summary>
    public const string MarkerFileName = "AGENTS-README-FIRST.yaml";

    /// <summary>
    /// Built-in default prompt template using Handlebars syntax.
    /// Available context: <c>{{baseUrl}}</c>, <c>{{apiKey}}</c>, <c>{{workspace.*}}</c> (all WorkspaceDto fields).
    /// Used when no <c>Mcp:MarkerPromptTemplate</c> is configured.
    /// </summary>
    public const string DefaultPromptTemplate =
        """
        You are connected to an MCP Context Server at {{baseUrl}}. Use this server to coordinate your work.

        ## ⚠️ SESSION LOGGING — MANDATORY PRECONDITION ⚠️

        **Before taking ANY action on a user request, you MUST first POST a session log entry to {{baseUrl}}/mcp/sessionlog.**
        This is a PRECONDITION — do not proceed with code changes, builds, file reads, or any other work until your
        session log POST succeeds. Session logging is not optional, not deferred, and not secondary to the user's task.

        On your FIRST interaction each session:
        1. Read this marker file
        2. GET {{baseUrl}}/health to verify the server is running
        3. GET {{baseUrl}}/mcp/sessionlog?limit=5 to review recent session history
        4. GET {{baseUrl}}/mcp/todo to review current tasks
        5. POST {{baseUrl}}/mcp/sessionlog with a new session (status: "in_progress", sourceType: your agent name)
        6. THEN begin working on the user's request

        On EVERY subsequent interaction:
        1. POST an updated session log entry BEFORE starting work
        2. Complete the user's request
        3. POST the final updated session log with results, actions taken, and files modified

        If the session log POST fails (e.g., 401), re-read this marker file for a fresh API key and retry.
        **Failure to maintain the session log is a compliance violation. Every interaction must be logged.**

        See the "Session Logging Details" section below for the full payload schema.

        ## Owner Values and Agent Conduct (MANDATORY)

        You are acting as a representative of the workspace owner. Your work — code, commits, documentation,
        and communications — directly reflects the owner's professional reputation. Adhere to these principles
        without exception:

        ### 1. Absolute Honesty
        - Never fabricate information, capabilities, or results. If you don't know something, say so.
        - When making suggestions, clearly distinguish between facts, informed opinions, and speculation.
        - If you made a mistake, acknowledge it immediately and correct it. Do not attempt to hide errors.
        - When providing feedback on code or design, be truthful even if the truth is uncomfortable.

        ### 2. Correctness Above All
        - Prioritize correctness over speed. Never ship code you haven't verified compiles and is logically sound.
        - When uncertain about correctness, state your uncertainty and suggest verification steps.
        - Prefer well-understood, proven patterns over clever or novel approaches unless explicitly directed otherwise.
        - All code must have appropriate XMLDocs. All public APIs must be documented.
        - Follow DRY, SOLID, and existing project conventions without exception.

        ### 3. Complete Decision Documentation
        - Log EVERY decision to the session log — including trivial ones. No decision is too small to record.
        - For each decision, document: what was decided, why, what alternatives were considered, and what was rejected.
        - Design decisions must be logged as dialog entries with category "decision" AND as session log actions with type "design_decision".
        - If a decision changes a previous decision, reference the original and explain the change.

        ### 4. Professional Representation and Audit Trail
        - Every interaction you have IS audited via the session log. This is not hypothetical — it is enforced.
        - When you sign commits, you represent the owner's name and reputation. Every commit must be:
          - Correct (compiles, passes tests, doesn't break existing functionality)
          - Clean (follows project conventions, properly formatted, no debug artifacts)
          - Well-described (meaningful commit messages that explain WHY, not just WHAT)
          - Complete (includes tests, documentation updates, and requirement tracking)
        - ALL commits must be logged to the session log in their entirety:
          - Log as an action with type "commit", including: SHA, branch, full commit message, files changed
        - ALL pull request comments must be logged to the session log in their entirety:
          - Log as an action with type "pr_comment", including: PR number, full comment text, timestamp
        - ALL issue comments must be logged to the session log in their entirety:
          - Log as an action with type "issue_comment", including: issue number, full comment text, timestamp
        - Never commit code that you would be embarrassed to have reviewed by a senior engineer.
        - When in doubt about a change's impact, ask before proceeding rather than guessing.

        ### 5. Source Attribution
        - When retrieving information from the internet (web searches, documentation, API references, etc.), ALL sources must be documented in the session log.
        - Log each source as an action with type "web_reference", including: URL, title/description, how the information was used.
        - Add source URLs to the entry's contextList array.
        - If you use code examples or patterns from external sources, attribute them in both the session log and in code comments.

        ## Requirements Tracking (REQUIRED)
        When you discover or agree on new functional or technical requirements during a session:
        1. Immediately record them by updating the docs/Project/ files:
           - Technical-Requirements.md — append new TR-MCP-* entries
           - Functional-Requirements.md — append new FR-MCP-* entries
           - TR-per-FR-Mapping.md — append mapping rows
           - Requirements-Matrix.md — append status rows
           - Testing-Requirements.md — append TEST-MCP-* entries
        2. Include the requirement ID in your session log entry's tags
        3. Do NOT defer requirements documentation to "later" — capture them as they emerge

        ## Design Decision Logging (REQUIRED)
        When a design decision is made (architecture, API shape, data format, naming, etc.):
        1. Log it immediately as a session log dialog entry with category "decision"
        2. Include: the decision, alternatives considered, rationale, and affected requirements
        3. Update the session log entry's actions with type "design_decision"
        4. If the decision affects existing code or requirements, note what needs updating

        ## Session Continuity (REQUIRED)
        At the START of every session:
        1. Read this AGENTS-README-FIRST.yaml marker file
        2. Query recent session logs: GET {{baseUrl}}/mcp/sessionlog?limit=5
        3. Query current TODOs: GET {{baseUrl}}/mcp/todo
        4. Read docs/Project/Requirements-Matrix.md to understand current project state
        5. If resuming interrupted work, review the last session's entries for pending decisions

        At regular intervals during long sessions (every ~10 interactions):
        1. POST an updated session log with all entries so far
        2. Ensure all design decisions are captured in dialog entries
        3. Verify requirements docs are up to date

        ## Glossary

        | Term | Definition |
        |------|-----------|
        | MCP | Model Context Protocol — an open standard for tool-calling between AI agents and context servers. |
        | Workspace | A project directory registered with the MCP server. All workspaces share a single port; use the `X-Workspace-Path` header to target a specific one. |
        | Marker File | The `AGENTS-README-FIRST.yaml` file placed at each workspace root. Contains connection details, auth token, and this prompt. |
        | API Key | A per-workspace cryptographic token that rotates on each server restart. Required for all `/mcp/*` REST endpoints. |
        | X-Workspace-Path | Optional HTTP header specifying the target workspace's absolute path. Highest priority in workspace resolution. |
        | Streamable HTTP | The MCP wire protocol transport at `/mcp-transport`. Carries JSON-RPC tool calls over HTTP POST with streaming responses. |
        | Session Log | An audit record of every agent interaction, stored per-session with full request/response history and reasoning dialog. |
        | Context Pack | An ordered set of document chunks retrieved by semantic + full-text hybrid search, scoped to the workspace. |
        | Tool Bucket | A GitHub repository containing tool manifest files, similar to a Scoop package bucket. |

        ## Workspace Definition

        | Property | Value |
        |----------|-------|
        | Name | {{workspace.Name}} |
        | Path | {{workspace.WorkspacePath}} |
        | Primary | {{workspace.IsPrimary}} |
        | Enabled | {{workspace.IsEnabled}} |
        | Data Directory | {{workspace.DataDirectory}} |
        | Todo Path | {{workspace.TodoPath}} |
        | Tunnel Provider | {{workspace.TunnelProvider}} |
        | Created | {{workspace.DateTimeCreated}} |
        | Modified | {{workspace.DateTimeModified}} |

        ## Authentication (REQUIRED)
        All `/mcp/*` REST endpoints require a per-workspace auth token. Include it with every request:
        - **Header**: `X-Api-Key: {{apiKey}}`
        - **Or query param**: `?api_key={{apiKey}}`
        If you receive a 401 response, re-read this AGENTS-README-FIRST.yaml marker file — the token rotates on each server restart.
        To detect a stale marker without auth, use the marker timestamps:
        - `serverStartedAtUtc`: the UTC startup time of the server process that wrote this marker.
        - `markerWrittenAtUtc`: the UTC timestamp when this marker file was generated.
        - `startedAt`: legacy alias retained for compatibility (same meaning as marker write time).
        Stale-marker check procedure (no auth required):
        1. GET `{{baseUrl}}/server-startup-utc` and compare response `serverStartedAtUtc` to this marker's `serverStartedAtUtc`.
           If they differ, this marker was generated by an older server instance. Re-read the marker before calling `/mcp/*`.
        2. GET `{{baseUrl}}/marker-file-timestamp?repoPath={{workspace.WorkspacePath}}` and compare response `lastWriteTimeUtc` to this marker's `markerWrittenAtUtc`.
           If the endpoint timestamp is newer (or the file is missing), re-read the marker before calling `/mcp/*`.
        3. If both values match (allowing small filesystem timestamp rounding differences), the marker is current.

        ## Available Protocols
        This server supports multiple connection protocols:
        - **REST API**: All `/mcp/*` endpoints (requires `X-Api-Key` header). Full OpenAPI spec at GET {{baseUrl}}/swagger/v1/swagger.json. Interactive Swagger UI at {{baseUrl}}/swagger.
        - **MCP Streamable HTTP**: POST {{baseUrl}}/mcp-transport — Model Context Protocol transport for tool-calling agents. No API key required for MCP transport.
        - **Health Check**: GET {{baseUrl}}/health — returns {"status":"healthy"}. No API key required.

        ## Workspace Resolution
        All workspaces share a single server port. The server resolves which workspace a request targets using this priority chain:
        1. **`X-Workspace-Path` header** (highest priority): Send `X-Workspace-Path: {{workspace.WorkspacePath}}` to explicitly target this workspace.
        2. **API key reverse lookup**: The `X-Api-Key` token is unique per workspace — the server resolves the workspace from it automatically.
        3. **Default workspace**: If neither header nor key is present, the primary workspace is used.
        For most agents, simply including `X-Api-Key` from this marker file is sufficient — the server resolves the workspace automatically.

        ## Server Health
        Before making API calls, verify the server is running: GET {{baseUrl}}/health — returns {"status":"healthy"}.

        ## Session Logging Details (REQUIRED — SEE PRECONDITION ABOVE)
        You MUST log every request you process to the session log endpoint. This is critical for audit, continuity, and team visibility.
        **Reminder: Your first action on each user request is POST {{baseUrl}}/mcp/sessionlog — before any code changes.**

        Do NOT invent your own tracking methodology. Use these endpoints exactly as documented below.

        ### Session Log Endpoints

        | Method | Endpoint | Description |
        |--------|----------|-------------|
        | POST | `/mcp/sessionlog` | Create or update a session log |
        | GET | `/mcp/sessionlog?limit=N&offset=M` | Query recent session logs |
        | POST | `/mcp/sessionlog/{agent}/{sessionId}/{requestId}/dialog` | Stream reasoning dialog |

        ### Session Log Object Model

        **SessionLog** (POST body to `/mcp/sessionlog`):
        ```json
        {
          "sourceType": "string — YOUR agent name (e.g. 'Copilot', 'Cline', 'Cursor')",
          "sessionId": "string — stable session ID prefixed with agent name (e.g. 'Copilot-abc123')",
          "title": "string — brief session summary, keep updated",
          "model": "string — AI model name (e.g. 'claude-sonnet-4-20250514')",
          "started": "string — ISO 8601 timestamp when session began",
          "lastUpdated": "string — ISO 8601 timestamp of latest activity",
          "status": "string — 'in_progress' or 'completed'",
          "entries": [ "array of RequestEntry objects (see below)" ]
        }
        ```

        **RequestEntry** (each element in `entries`):
        ```json
        {
          "requestId": "string — unique ID for this request within the session",
          "timestamp": "string — ISO 8601",
          "queryText": "string — full user query or task description",
          "queryTitle": "string — short summary of the query",
          "response": "string — your response text",
          "interpretation": "string — your understanding of what was asked",
          "status": "string — 'completed' or 'in_progress'",
          "model": "string — model used for this entry",
          "tokenCount": "integer|null — approximate token count",
          "tags": ["string array — e.g. 'refactor', 'bugfix', 'feature'"],
          "contextList": ["string array — files or resources referenced"],
          "designDecisions": ["string array — decisions made during this interaction"],
          "requirementsDiscovered": ["string array — requirement IDs e.g. 'TR-MCP-001'"],
          "filesModified": ["string array — file paths changed"],
          "blockers": ["string array — issues preventing progress"],
          "actions": [ "array of Action objects (see below)" ],
          "processingDialog": [ "array of DialogItem objects (see below)" ]
        }
        ```

        **Action** (each element in `actions`):
        ```json
        {
          "order": "integer — sequence number",
          "description": "string — what was done",
          "type": "string — action type (see Recognized Action Types below)",
          "status": "string — 'completed', 'in_progress', or 'failed'",
          "filePath": "string — affected file path, or empty string"
        }
        ```

        **DialogItem** (each element in `processingDialog`, or POST body to dialog endpoint):
        ```json
        {
          "timestamp": "string — ISO 8601",
          "role": "string — 'model', 'tool', 'system', or 'user'",
          "content": "string — reasoning text, tool output, or observation",
          "category": "string — 'reasoning', 'tool_call', 'tool_result', 'observation', or 'decision'"
        }
        ```

        ### Session Logging — Use the McpSession Module

        Helper modules are registered in the MCP Tool Registry. Search for them:
        ```powershell
        # Discover available modules
        Invoke-RestMethod -Uri "{{baseUrl}}/mcp/tools/search?keyword=session" -Headers @{ "X-Api-Key" = "{{apiKey}}" }
        ```

        **PowerShell (recommended):** Download `McpSession-PowerShell` from the tool registry, save as `McpSession.psm1`:
        ```powershell
        Import-Module ./McpSession.psm1
        Initialize-McpSession                                    # reads this marker file automatically
        $s = New-McpSessionLog -SourceType "Copilot" -Title "Implementing feature X" -Model "claude-sonnet-4"
        $e = Add-McpSessionEntry -Session $s -QueryTitle "Add auth" -QueryText "Add JWT authentication"
        Add-McpAction -Entry $e -Description "Created TokenService" -Type create -FilePath "src/TokenService.cs"
        Set-McpSessionEntry -Entry $e -Session $s -Response "Done" -Status completed
        Update-McpSessionLog -Session $s -Status completed       # final push
        ```

        **Bash:** Download `McpSession-Bash`, save as `mcp-session.sh` (requires `jq`):
        ```bash
        source ./mcp-session.sh
        mcp_session_init
        mcp_session_create "Copilot" "Implementing feature X" "claude-sonnet-4"
        mcp_session_add_entry "req-001" "Add auth" "Add JWT authentication" "in_progress"
        mcp_session_add_action "req-001" "Created TokenService" "create" "src/TokenService.cs"
        mcp_session_update_entry "req-001" "status" "completed"
        mcp_session_complete
        ```

        **Raw API (if modules unavailable):**
        ```powershell
        $headers = @{ "X-Api-Key" = "{{apiKey}}"; "Content-Type" = "application/json" }
        # Query recent logs at session start
        Invoke-RestMethod -Uri "{{baseUrl}}/mcp/sessionlog?limit=5" -Headers $headers
        # POST session log — see object models above for full schema
        Invoke-RestMethod -Uri "{{baseUrl}}/mcp/sessionlog" -Method Post -Headers $headers -Body ($sessionObject | ConvertTo-Json -Depth 10)
        ```

        ## Todo Management (REQUIRED)
        Use the Todo API to track, create, and update project tasks. Do NOT use your own internal tracking
        as a substitute — the MCP server todo list is the single source of truth for project work items.
        At session start, query existing todos to understand current project state.

        ### Todo Endpoints

        | Method | Endpoint | Description |
        |--------|----------|-------------|
        | GET | `/mcp/todo` | List all todos |
        | POST | `/mcp/todo` | Create a new todo |
        | GET | `/mcp/todo/{id}` | Get a specific todo |
        | PUT | `/mcp/todo/{id}` | Update a todo |
        | DELETE | `/mcp/todo/{id}` | Delete a todo |
        | GET | `/mcp/todo/{id}/prompt/implement` | Get implementation prompt for a todo |
        | GET | `/mcp/todo/{id}/prompt/plan` | Get planning prompt for a todo |
        | GET | `/mcp/todo/{id}/prompt/status` | Get status prompt for a todo |
        | POST | `/mcp/todo/{id}/requirements` | Add requirements to a todo |

        ### Todo Object Model

        **TodoFlatItem** (returned by GET, included in responses):
        ```json
        {
          "id": "string — unique kebab-case ID (e.g. 'add-jwt-auth')",
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

        **TodoCreateRequest** (POST body to `/mcp/todo`):
        ```json
        {
          "id": "string — REQUIRED unique kebab-case ID",
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

        **TodoUpdateRequest** (PUT body to `/mcp/todo/{id}`):
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

        ### Todo Management — Use the McpTodo Module

        Helper modules are registered in the MCP Tool Registry. Search for them:
        ```powershell
        Invoke-RestMethod -Uri "{{baseUrl}}/mcp/tools/search?keyword=todo" -Headers @{ "X-Api-Key" = "{{apiKey}}" }
        ```

        **PowerShell (recommended):** Download `McpTodo-PowerShell` from the tool registry, save as `McpTodo.psm1`:
        ```powershell
        Import-Module ./McpTodo.psm1
        Initialize-McpTodo                                       # reads this marker file automatically
        Get-McpTodoList | Format-Table id, title, priority, done # list todos at session start
        New-McpTodo -Id "add-jwt-auth" -Title "Add JWT auth" -Section "Backend" -Priority high `
          -Description @("Implement JWT bearer tokens") -Estimate "4h"
        Update-McpTodo -Id "add-jwt-auth" -Done $true -DoneSummary "JWT auth complete"
        Get-McpTodoPrompt -Id "add-jwt-auth" -PromptType implement  # get implementation guidance
        ```

        **Bash:** Download `McpTodo-Bash`, save as `mcp-todo.sh` (requires `jq`):
        ```bash
        source ./mcp-todo.sh
        mcp_todo_init
        mcp_todo_list | jq '.items[] | {id, title, done}'
        mcp_todo_create "add-jwt-auth" "Add JWT auth" "Backend" "high" '{"estimate":"4h"}'
        mcp_todo_update "add-jwt-auth" '{"done":true,"doneSummary":"JWT auth complete"}'
        mcp_todo_prompt "add-jwt-auth" "implement"
        ```

        **Raw API (if modules unavailable):**
        ```powershell
        $headers = @{ "X-Api-Key" = "{{apiKey}}"; "Content-Type" = "application/json" }
        # List todos at session start
        Invoke-RestMethod -Uri "{{baseUrl}}/mcp/todo" -Headers $headers | % items | Format-Table id, title, done
        # Create — see object models above for full schema
        Invoke-RestMethod -Uri "{{baseUrl}}/mcp/todo" -Method Post -Headers $headers -Body ($todoObject | ConvertTo-Json -Depth 5)
        # Update
        Invoke-RestMethod -Uri "{{baseUrl}}/mcp/todo/add-jwt-auth" -Method Put -Headers $headers -Body '{"done":true}'
        # Get implementation prompt
        Invoke-RestMethod -Uri "{{baseUrl}}/mcp/todo/add-jwt-auth/prompt/implement" -Headers $headers
        ```

        ## Available Capabilities
        - Context Search: POST {{baseUrl}}/mcp/context/search — semantic + full-text hybrid search over indexed project documents
        - Context Pack: POST {{baseUrl}}/mcp/context/pack — retrieve ordered context chunks for a topic
        - Context Sources: GET {{baseUrl}}/mcp/context/sources — list all indexed document sources
        - Todo Management: GET/POST/PUT/DELETE {{baseUrl}}/mcp/todo — query, create, update, and delete project tasks
        - Repo Files: GET {{baseUrl}}/mcp/repo/file, POST {{baseUrl}}/mcp/repo/file, GET {{baseUrl}}/mcp/repo/list — read, write, and list repository files
        - GitHub Integration: {{baseUrl}}/mcp/gh/issues, {{baseUrl}}/mcp/gh/pulls, {{baseUrl}}/mcp/gh/labels — issue, PR, and label management
        - Sync: POST {{baseUrl}}/mcp/sync/run — trigger full ingestion sync; GET {{baseUrl}}/mcp/sync/status — check sync status
        - Tool Registry: GET {{baseUrl}}/mcp/tools/search — discover available tools; GET/POST {{baseUrl}}/mcp/tools — manage tool definitions
        - MCP Protocol: {{baseUrl}}/mcp-transport — Model Context Protocol streamable HTTP transport endpoint

        {{#if workspace.BannedLicenses}}
        ## License Compliance (MANDATORY)

        The following open-source licenses are BANNED in this workspace. You MUST NOT:
        - Use code snippets from projects licensed under these licenses
        - Recommend or add NuGet packages, npm packages, or any dependencies licensed under these licenses
        - Copy patterns, algorithms, or implementations from codebases under these licenses

        **Banned Licenses:**
        {{#each workspace.BannedLicenses}}
        - {{this}}
        {{/each}}

        Before adding ANY new dependency:
        1. Verify its license is NOT in the banned list above
        2. Log the dependency name, version, and license in the session log as an action with type "dependency_add"
        3. If you cannot determine the license, DO NOT add the dependency — flag it as a blocker

        If you discover an existing dependency uses a banned license, immediately log it as a blocker with type "license_violation" and notify the user.
        {{/if}}

        {{#if workspace.BannedCountriesOfOrigin}}
        ## Country of Origin Restrictions (MANDATORY)

        Dependencies, libraries, and code from the following countries of origin are BANNED in this workspace:

        **Banned Countries:**
        {{#each workspace.BannedCountriesOfOrigin}}
        - {{this}}
        {{/each}}

        Before adding ANY new dependency:
        1. Verify the maintainer/organization's country of origin is NOT in the banned list
        2. If the country of origin cannot be determined, flag it as a blocker and ask the user
        3. Log any country-of-origin concerns as an action with type "origin_review"

        If you discover an existing dependency originates from a banned country, immediately log it as a blocker with type "origin_violation" and notify the user.
        {{/if}}

        {{#if workspace.BannedOrganizations}}
        ## Banned Organizations (MANDATORY)

        Code, libraries, and dependencies from the following organizations are BANNED:

        {{#each workspace.BannedOrganizations}}
        - {{this}}
        {{/each}}

        Do not use, recommend, or reference code maintained by these organizations.
        Log any violations as an action with type "entity_violation".
        {{/if}}

        {{#if workspace.BannedIndividuals}}
        ## Banned Individuals (MANDATORY)

        Code, libraries, and dependencies authored or primarily maintained by the following individuals are BANNED:

        {{#each workspace.BannedIndividuals}}
        - {{this}}
        {{/each}}

        Do not use, recommend, or reference code authored by these individuals.
        Log any violations as an action with type "entity_violation".
        {{/if}}

        ## Recognized Action Types
        When logging actions in session log entries, use these standardized type values:
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

        **THESE RULES MUST BE ADHERED TO AND THIS MARKER READ ON EACH NEW REQUEST BY THE USER.**
        **REMINDER: Your FIRST action on every user request is POST {{baseUrl}}/mcp/sessionlog. Do not skip this.**
        """;

    private static readonly ISerializer s_yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private static readonly IHandlebars s_handlebars = Handlebars.Create();

    /// <summary>
    /// Writes the <c>AGENTS-README-FIRST.yaml</c> marker file to <paramref name="workspacePath"/>.
    /// </summary>
    /// <param name="workspacePath">Absolute path to the workspace root directory.</param>
    /// <param name="port">HTTP port the workspace is served on.</param>
    /// <param name="workspaceName">Human-readable workspace name.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="globalPromptTemplate">
    /// Optional global Handlebars prompt template.
    /// When <see langword="null"/> or empty, the built-in default prompt is used.
    /// </param>
    /// <param name="workspacePromptTemplate">
    /// Optional per-workspace Handlebars prompt template.
    /// When non-null, the resolved text is appended to the global prompt.
    /// </param>
    /// <param name="apiKey">
    /// Per-workspace auth token to include in the marker file.
    /// Agents read this value and send it as the <c>X-Api-Key</c> header.
    /// </param>
    /// <param name="workspace">
    /// Full workspace definition. All properties are available in Handlebars templates as <c>{{workspace.*}}</c>.
    /// </param>
    /// <param name="serverStartedAtUtc">
    /// Optional server startup UTC timestamp to embed in the marker for stale-marker detection.
    /// When omitted, the current UTC timestamp is used.
    /// </param>
    public static async Task WriteMarkerAsync(
        string workspacePath,
        int port,
        string workspaceName,
        ILogger? logger = null,
        CancellationToken ct = default,
        string? globalPromptTemplate = null,
        string? workspacePromptTemplate = null,
        string? apiKey = null,
        WorkspaceDto? workspace = null,
        DateTimeOffset? serverStartedAtUtc = null)
    {
        var baseUrl = $"http://localhost:{port.ToString(CultureInfo.InvariantCulture)}";
        var markerPath = Path.Combine(workspacePath, MarkerFileName);
        var markerWrittenAtUtc = DateTimeOffset.UtcNow;
        var resolvedServerStartedAtUtc = (serverStartedAtUtc ?? markerWrittenAtUtc).ToUniversalTime();
        var markerWrittenAtUtcText = markerWrittenAtUtc.ToString("o", CultureInfo.InvariantCulture);
        var serverStartedAtUtcText = resolvedServerStartedAtUtc.ToString("o", CultureInfo.InvariantCulture);

        var templateContext = BuildTemplateContext(baseUrl, apiKey, workspace, workspacePath, workspaceName);
        templateContext["markerWrittenAtUtc"] = markerWrittenAtUtcText;
        templateContext["serverStartedAtUtc"] = serverStartedAtUtcText;

        var marker = new MarkerFile
        {
            Port = port,
            BaseUrl = baseUrl,
            ApiKey = apiKey ?? string.Empty,
            Endpoints = new MarkerEndpoints
            {
                Health = "/health",
                Swagger = "/swagger/v1/swagger.json",
                SwaggerUi = "/swagger",
                McpTransport = "/mcp-transport",
                SessionLog = "/mcp/sessionlog",
                SessionLogDialog = "/mcp/sessionlog/{agent}/{sessionId}/{requestId}/dialog",
                ContextSearch = "/mcp/context/search",
                ContextPack = "/mcp/context/pack",
                ContextSources = "/mcp/context/sources",
                Todo = "/mcp/todo",
                Repo = "/mcp/repo",
                Sync = "/mcp/sync",
                GitHub = "/mcp/gh",
                Tools = "/mcp/tools",
                Workspace = "/mcp/workspace",
                ServerStartupUtc = "/server-startup-utc",
                MarkerFileTimestamp = "/marker-file-timestamp?repoPath={workspacePath}",
            },
            Workspace = workspaceName,
            WorkspacePath = workspacePath,
            Pid = Environment.ProcessId,
            StartedAt = markerWrittenAtUtcText,
            MarkerWrittenAtUtc = markerWrittenAtUtcText,
            ServerStartedAtUtc = serverStartedAtUtcText,
            Prompt = ResolvePrompt(templateContext, globalPromptTemplate, workspacePromptTemplate),
        };

        try
        {
            EnsureGitIgnored(workspacePath, logger);
            var yaml = s_yamlSerializer.Serialize(marker);
            await File.WriteAllTextAsync(markerPath, yaml, ct).ConfigureAwait(false);
            logger?.LogInformation("Wrote MCP marker file: {Path}", markerPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            logger?.LogWarning(ex, "Failed to write MCP marker file: {Path}", markerPath);
        }
    }

    /// <summary>
    /// Removes the <c>AGENTS-README-FIRST.yaml</c> marker file from <paramref name="workspacePath"/>.
    /// Also removes any legacy <c>.mcp-server.json</c> and <c>.mcp-server.yaml</c> files if present.
    /// </summary>
    public static void RemoveMarker(string workspacePath, ILogger? logger = null)
    {
        RemoveSingleFile(Path.Combine(workspacePath, MarkerFileName), logger);
        // Clean up legacy markers if they exist.
        RemoveSingleFile(Path.Combine(workspacePath, ".mcp-server.yaml"), logger);
        RemoveSingleFile(Path.Combine(workspacePath, ".mcp-server.json"), logger);
    }

    /// <summary>Ensures <see cref="MarkerFileName"/> is listed in the workspace root's <c>.gitignore</c>.</summary>
    private static void EnsureGitIgnored(string workspacePath, ILogger? logger)
    {
        try
        {
            var gitignorePath = Path.Combine(workspacePath, ".gitignore");
            if (File.Exists(gitignorePath))
            {
                var lines = File.ReadAllLines(gitignorePath);
                if (lines.Any(l => l.Trim().Equals(MarkerFileName, StringComparison.OrdinalIgnoreCase)))
                    return;
            }

            File.AppendAllText(gitignorePath, $"{Environment.NewLine}{MarkerFileName}{Environment.NewLine}");
            logger?.LogInformation("Added {Marker} to .gitignore at {Path}", MarkerFileName, gitignorePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger?.LogWarning(ex, "Failed to update .gitignore at {Path}", workspacePath);
        }
    }

    private static void RemoveSingleFile(string markerPath, ILogger? logger)
    {
        try
        {
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
                logger?.LogInformation("Removed MCP marker file: {Path}", markerPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger?.LogWarning(ex, "Failed to remove MCP marker file: {Path}", markerPath);
        }
    }

    /// <summary>
    /// Resolves the final prompt by compiling global and workspace Handlebars templates
    /// against the supplied context. Visible for testing.
    /// </summary>
    internal static string ResolvePrompt(
        Dictionary<string, object?> templateContext,
        string? globalPromptTemplate,
        string? workspacePromptTemplate)
    {
        var globalSource = string.IsNullOrWhiteSpace(globalPromptTemplate)
            ? DefaultPromptTemplate
            : globalPromptTemplate;

        var global = RenderHandlebars(globalSource, templateContext);

        if (string.IsNullOrWhiteSpace(workspacePromptTemplate))
            return global;

        var workspace = RenderHandlebars(workspacePromptTemplate, templateContext);
        return global + "\n\n" + workspace;
    }

    /// <summary>
    /// Builds the Handlebars template context dictionary from the workspace definition and runtime values.
    /// </summary>
    internal static Dictionary<string, object?> BuildTemplateContext(
        string baseUrl,
        string? apiKey,
        WorkspaceDto? workspace,
        string workspacePath,
        string workspaceName)
    {
        return new Dictionary<string, object?>
        {
            ["baseUrl"] = baseUrl,
            ["apiKey"] = apiKey ?? string.Empty,
            ["workspace"] = workspace is not null ? new Dictionary<string, object?>
            {
                ["Name"] = workspace.Name,
                ["WorkspacePath"] = workspace.WorkspacePath,
                ["TodoPath"] = workspace.TodoPath,
                ["DataDirectory"] = workspace.DataDirectory ?? workspace.WorkspacePath,
                ["TunnelProvider"] = workspace.TunnelProvider ?? "none",
                ["IsPrimary"] = workspace.IsPrimary,
                ["IsEnabled"] = workspace.IsEnabled,
                ["DateTimeCreated"] = workspace.DateTimeCreated.ToString("o", CultureInfo.InvariantCulture),
                ["DateTimeModified"] = workspace.DateTimeModified.ToString("o", CultureInfo.InvariantCulture),
                ["RunAs"] = workspace.RunAs ?? "default",
                ["PromptTemplate"] = workspace.PromptTemplate ?? string.Empty,
                ["BannedLicenses"] = workspace.BannedLicenses.Count > 0 ? workspace.BannedLicenses : null,
                ["BannedCountriesOfOrigin"] = workspace.BannedCountriesOfOrigin.Count > 0 ? workspace.BannedCountriesOfOrigin : null,
                ["BannedOrganizations"] = workspace.BannedOrganizations.Count > 0 ? workspace.BannedOrganizations : null,
                ["BannedIndividuals"] = workspace.BannedIndividuals.Count > 0 ? workspace.BannedIndividuals : null,
            } : new Dictionary<string, object?>
            {
                ["Name"] = workspaceName,
                ["WorkspacePath"] = workspacePath,
                ["TodoPath"] = string.Empty,
                ["DataDirectory"] = workspacePath,
                ["TunnelProvider"] = "none",
                ["IsPrimary"] = false,
                ["IsEnabled"] = true,
                ["DateTimeCreated"] = string.Empty,
                ["DateTimeModified"] = string.Empty,
                ["RunAs"] = "default",
                ["PromptTemplate"] = string.Empty,
                ["BannedLicenses"] = null,
                ["BannedCountriesOfOrigin"] = null,
                ["BannedOrganizations"] = null,
                ["BannedIndividuals"] = null,
            },
        };
    }

    private static string RenderHandlebars(string template, Dictionary<string, object?> context)
    {
        // Normalize to LF before and after — CRLF in templates confuses Handlebars
        // standalone-line detection, and YAML folded scalars treat \r as extra blank lines.
        var compiled = s_handlebars.Compile(template.ReplaceLineEndings("\n"));
        return compiled(context).ReplaceLineEndings("\n");
    }
}

/// <summary>Serialization model for the <c>AGENTS-README-FIRST.yaml</c> marker file.</summary>
internal sealed class MarkerFile
{
    public int Port { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public MarkerEndpoints Endpoints { get; set; } = new();
    public string Workspace { get; set; } = string.Empty;
    public string WorkspacePath { get; set; } = string.Empty;
    public int Pid { get; set; }
    // Backward-compatible marker write timestamp retained for existing consumers.
    public string StartedAt { get; set; } = string.Empty;
    public string MarkerWrittenAtUtc { get; set; } = string.Empty;
    public string ServerStartedAtUtc { get; set; } = string.Empty;
    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string Prompt { get; set; } = string.Empty;
}

/// <summary>Well-known endpoint paths exposed by the MCP server.</summary>
internal sealed class MarkerEndpoints
{
    public string Health { get; set; } = string.Empty;
    public string Swagger { get; set; } = string.Empty;
    public string SwaggerUi { get; set; } = string.Empty;
    public string McpTransport { get; set; } = string.Empty;
    public string SessionLog { get; set; } = string.Empty;
    public string SessionLogDialog { get; set; } = string.Empty;
    public string ContextSearch { get; set; } = string.Empty;
    public string ContextPack { get; set; } = string.Empty;
    public string ContextSources { get; set; } = string.Empty;
    public string Todo { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string Sync { get; set; } = string.Empty;
    public string GitHub { get; set; } = string.Empty;
    public string Tools { get; set; } = string.Empty;
    public string Workspace { get; set; } = string.Empty;
    public string ServerStartupUtc { get; set; } = string.Empty;
    public string MarkerFileTimestamp { get; set; } = string.Empty;
}
