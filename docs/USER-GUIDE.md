# MCP Server User Documentation

This guide is for operators and AI-agent users running `McpServer.Support.Mcp`.

## 1) Installation and prerequisites

### Supported host environment

- Windows 10/11 or Windows Server
- .NET SDK 9.x for local development
- PowerShell 7+
- `gh` CLI for GitHub issue and PR workflows
- Network access to the configured MCP port (default `7147`)

### Prerequisite checks

```powershell
dotnet --version
gh auth status
Invoke-RestMethod http://localhost:7147/health
```

### Install/run options

#### Development run (HTTP + MCP transport)

```powershell
dotnet run --project src\McpServer.Support.Mcp -- --instance default
```

#### STDIO transport

```powershell
dotnet run --project src\McpServer.Support.Mcp -- --transport stdio --instance default
```

#### Windows service deployment

```powershell
gsudo pwsh -NonInteractive -File .\scripts\Update-McpService.ps1 -SkipVersionBump
Get-Service McpServer
```

### Verify startup

- `GET /health` returns healthy status
- `GET /swagger` loads the API UI
- `GET /swagger/v1/swagger.json` returns OpenAPI metadata
- marker file `AGENTS-README-FIRST.yaml` exists at workspace root

## 2) Configuration reference (appsettings + marker file)

### appsettings keys (root)

- `DataFolder`
- `Embedding:*` and `VectorIndex:*`
- `Mcp:Port`, `Mcp:DataSource`, `Mcp:DataDirectory`
- `Mcp:RepoRoot`, `Mcp:RepoAllowlist`
- `Mcp:TodoFilePath`, `Mcp:TodoStorage:*`
- `Mcp:GraphRag:*`
- `Mcp:ToolRegistry:*`
- `Mcp:Tunnel:*`
- `Mcp:Workspaces`
- `Mcp:Instances:{name}:*`
- `VoiceConversation:DefaultExecutionStrategy` (`hosted-agentframework` or `copilot-cli`)
- `VoiceConversation:ModelApiKeyEnvironmentVariableName`
- `GET|PATCH /mcpserver/configuration` (PATCH requires admin role)

### Configuration precedence

1. `PORT` environment variable
2. `Mcp:Instances:{name}:Port`
3. `Mcp:Port`
4. default `7147`

### Marker file (`AGENTS-README-FIRST.yaml`)

Key fields:

- `baseUrl`, `port`
- `apiKey`
- endpoint map (`health`, `swagger`, `todo`, `sessionLog`, etc.)
- `workspacePath`, `workspace`
- `serverStartedAtUtc`, `markerWrittenAtUtc`

Example use:

```powershell
$marker = Get-Content .\AGENTS-README-FIRST.yaml -Raw
$apiKey = ([regex]::Match($marker, 'apiKey:\s*(\S+)')).Groups[1].Value
Invoke-RestMethod -Uri "http://localhost:7147/mcpserver/todo" -Headers @{ "X-Api-Key" = $apiKey }
```

### PowerShell helper modules (McpSession.psm1 + McpTodo.psm1)

```powershell
$marker = Get-Content .\AGENTS-README-FIRST.yaml -Raw
$apiKey = ([regex]::Match($marker, 'apiKey:\s*(\S+)')).Groups[1].Value
$headers = @{ "X-Api-Key" = $apiKey }

Invoke-RestMethod -Uri "http://localhost:7147/mcpserver/tools/search?keyword=mcp-session-module" -Headers $headers
Invoke-RestMethod -Uri "http://localhost:7147/mcpserver/tools/search?keyword=mcp-todo-module" -Headers $headers

Import-Module .\tools\powershell\McpSession.psm1
Import-Module .\tools\powershell\McpTodo.psm1

Initialize-McpSession -Agent "Codex" -Model "gpt-5.3-codex"
Initialize-McpTodo
```

Sample session logging flow:

```powershell
$session = New-McpSessionLog -SourceType "Codex" -Title "MCP docs update" -Model "gpt-5.3-codex"
$turn = Add-McpSessionTurn -Session $session -QueryTitle "Update docs" -QueryText "Create user docs" -Status in_progress
Add-McpAction -Turn $turn -Description "Updated docs\\USER-GUIDE.md" -Type edit -FilePath "docs/USER-GUIDE.md"
Set-McpSessionTurn -Session $session -Turn $turn -Response "Docs complete" -Status completed
Update-McpSessionLog -Session $session
```

Sample TODO progress flow:

```powershell
$todo = Get-McpTodo -Id "MCP-USERDOCS-001"
$tasks = @(
  @{ task = "Write Installation & Prerequisites guide"; done = $true },
  @{ task = "Write Configuration reference (appsettings + marker file)"; done = $true }
)
Update-McpTodo -Id $todo.id -ImplementationTasks $tasks
```

## 3) REST API reference (all controllers)

Base URL: `http://<host>:7147`

Authentication:

- include `X-Api-Key` for `/mcpserver/*`
- include `X-Workspace-Path` for explicit workspace targeting
- OpenAPI: `GET /swagger/v1/swagger.json`

### AuthConfig controller (`/auth/*`)

- `GET /auth/config`
- `POST /auth/device`
- `POST /auth/token`
- `GET /auth/ui/{path}`
- `POST /auth/ui/{path}`

### AgentPool controller (`/mcpserver/agent-pool/*`)

- `GET /mcpserver/agent-pool/agents`
- `POST /mcpserver/agent-pool/agents/{agentName}/start|stop|connect|recycle`
- `POST /mcpserver/agent-pool/connect`
- `GET /mcpserver/agent-pool/queue`
- `POST /mcpserver/agent-pool/queue/one-shot`
- `POST /mcpserver/agent-pool/queue/resolve`
- `POST /mcpserver/agent-pool/queue/{jobId}/cancel|move-up|move-down`
- `DELETE /mcpserver/agent-pool/queue/{jobId}`
- `GET /mcpserver/agent-pool/notifications`
- `GET /mcpserver/agent-pool/jobs/{jobId}/stream`

### Agent controller (`/mcpserver/agents*`)

- `GET /mcpserver/agents`
- `GET|POST|DELETE /mcpserver/agents/{agentId}`
- `POST /mcpserver/agents/{agentId}/ban|unban|launch|stop`
- `GET|POST /mcpserver/agents/{agentId}/events`
- `GET /mcpserver/agents/{agentId}/process-status`
- `GET /mcpserver/agents/running`
- `GET|POST /mcpserver/agents/definitions`
- `GET|DELETE /mcpserver/agents/definitions/{agentType}`
- `POST /mcpserver/agents/definitions/seed`
- `GET /mcpserver/agents/validate`

### Configuration controller (`/mcpserver/configuration`)

- `GET /mcpserver/configuration`
- `PATCH /mcpserver/configuration`

### Context controller (`/mcpserver/context/*`)

- `POST /mcpserver/context/search`
- `POST /mcpserver/context/pack`
- `GET /mcpserver/context/sources`
- `POST /mcpserver/context/rebuild-index`
- `POST /mcpserver/context/ingest-website`
- `POST /mcpserver/context/ingest-website/stream`

Search request example:

```json
{
  "query": "workspace routing",
  "limit": 10,
  "sourceType": "repo"
}
```

Response example:

```json
{
  "chunks": [
    {
      "sourceKey": "docs/context/api-capabilities.md",
      "score": 0.91,
      "text": "Workspace resolution priority..."
    }
  ]
}
```

### Desktop controller (`/mcpserver/desktop/*`)

- `POST /mcpserver/desktop/launch` — requires normal workspace authentication plus the
  privileged `X-Desktop-Launch-Token` header, and the target executable must match
  `Mcp:DesktopLaunch:AllowedExecutables` while `Mcp:DesktopLaunch:Enabled` is `true`.

### Diagnostic controller (`/mcpserver/diagnostic/*`)

- `GET /mcpserver/diagnostic/execution-path`
- `GET /mcpserver/diagnostic/appsettings-path`

### EventStream controller (`/mcpserver/events`)

- `GET /mcpserver/events`

### GitHub controller (`/mcpserver/gh/*`)

- `GET|POST /mcpserver/gh/issues`
- `GET|PUT /mcpserver/gh/issues/{number}`
- `POST /mcpserver/gh/issues/{number}/close|reopen|sync`
- `POST /mcpserver/gh/issues/{id}/comments`
- `POST /mcpserver/gh/issues/sync/from-github`
- `POST /mcpserver/gh/issues/sync/to-github`
- `GET /mcpserver/gh/labels`
- `GET /mcpserver/gh/pulls`
- `POST /mcpserver/gh/pulls/{id}/comments`
- `GET /mcpserver/gh/auth/status`
- `PUT|DELETE /mcpserver/gh/auth/token`
- `GET /mcpserver/gh/oauth/config`
- `GET /mcpserver/gh/oauth/authorize-url`
- `GET /mcpserver/gh/actions/runs`
- `GET /mcpserver/gh/actions/runs/{runId}`
- `POST /mcpserver/gh/actions/runs/{runId}/rerun|cancel`

### GraphRag controller (`/mcpserver/graphrag/*`)

- `GET /mcpserver/graphrag/status`
- `POST /mcpserver/graphrag/index`
- `POST /mcpserver/graphrag/query`

### PromptTemplate controller (`/mcpserver/templates*`)

- `GET|POST /mcpserver/templates`
- `GET|PUT|DELETE /mcpserver/templates/{id}`
- `POST /mcpserver/templates/{id}/resolve`
- `POST /mcpserver/templates/{id}/test`
- `POST /mcpserver/templates/test`

### Repo controller (`/mcpserver/repo/*`)

- `GET /mcpserver/repo/file`
- `POST /mcpserver/repo/file`
- `GET /mcpserver/repo/list`

### Requirements controller (`/mcpserver/requirements/*`)

- `GET /mcpserver/requirements/generate`
- `GET|POST /mcpserver/requirements/fr`
- `GET|PUT|DELETE /mcpserver/requirements/fr/{id}`
- `GET|POST /mcpserver/requirements/tr`
- `GET|PUT|DELETE /mcpserver/requirements/tr/{id}`
- `GET|POST /mcpserver/requirements/test`
- `GET|PUT|DELETE /mcpserver/requirements/test/{id}`
- `GET /mcpserver/requirements/mapping`
- `GET|PUT|DELETE /mcpserver/requirements/mapping/{frId}`
- `POST /mcpserver/requirements/ingest`

### SessionLog controller (`/mcpserver/sessionlog*`)

- `GET /mcpserver/sessionlog`
- `POST /mcpserver/sessionlog`
- `POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog`

### Todo controller (`/mcpserver/todo*`)

- `GET|POST /mcpserver/todo`
- `GET|PUT|DELETE /mcpserver/todo/{id}`
- `POST /mcpserver/todo/{id}/move`
- `POST /mcpserver/todo/{id}/requirements`
- `GET /mcpserver/todo/{id}/prompt/implement|plan|status`
- `POST /mcpserver/todo/{id}/prompt/implement/queue`
- `POST /mcpserver/todo/{id}/prompt/plan/queue`
- `POST /mcpserver/todo/{id}/prompt/status/queue`

Update request example:

```json
{
  "implementationTasks": [
    { "task": "Write Installation & Prerequisites guide", "done": true },
    { "task": "Write Configuration reference (appsettings + marker file)", "done": true }
  ]
}
```

### ToolRegistry controller (`/mcpserver/tools*`)

- `GET|POST /mcpserver/tools`
- `GET|PUT|DELETE /mcpserver/tools/{id}`
- `GET /mcpserver/tools/search`
- `GET|POST /mcpserver/tools/buckets`
- `DELETE /mcpserver/tools/buckets/{name}`
- `GET /mcpserver/tools/buckets/{name}/browse`
- `POST /mcpserver/tools/buckets/{name}/install`
- `POST /mcpserver/tools/buckets/{name}/sync`

### Tunnel controller (`/mcpserver/tunnel/*`)

- `GET /mcpserver/tunnel/list`
- `GET /mcpserver/tunnel/{name}/status`
- `POST /mcpserver/tunnel/{name}/start|stop|restart|enable|disable`

### Voice controller (`/mcpserver/voice/*`)

- `GET|POST /mcpserver/voice/session`
- `GET|DELETE /mcpserver/voice/session/{sessionId}`
- `POST /mcpserver/voice/session/{sessionId}/turn`
- `POST /mcpserver/voice/session/{sessionId}/turn/stream`
- `POST /mcpserver/voice/session/{sessionId}/interrupt`
- `POST /mcpserver/voice/session/{sessionId}/escape`
- `GET /mcpserver/voice/session/{sessionId}/transcript`

### Workspace controller (`/mcpserver/workspace*`)

- `GET|POST /mcpserver/workspace`
- `GET|PUT|DELETE /mcpserver/workspace/{key}`
- `POST /mcpserver/workspace/{key}/init|start|stop`
- `GET /mcpserver/workspace/{key}/status`
- `GET|PUT /mcpserver/workspace/prompt`
- `POST /mcpserver/workspace/policy`

### Runtime utility endpoints (non-controller)

- `GET /health`
- `GET /swagger`
- `GET /swagger/v1/swagger.json`
- `POST /mcp-transport`

### Complete endpoint inventory (OpenAPI snapshot)

```text
GET /auth/config
POST /auth/device
POST /auth/token
GET /auth/ui/{path}
POST /auth/ui/{path}
GET /mcpserver/agent-pool/agents
POST /mcpserver/agent-pool/agents/{agentName}/connect
POST /mcpserver/agent-pool/agents/{agentName}/recycle
POST /mcpserver/agent-pool/agents/{agentName}/start
POST /mcpserver/agent-pool/agents/{agentName}/stop
POST /mcpserver/agent-pool/connect
GET /mcpserver/agent-pool/jobs/{jobId}/stream
GET /mcpserver/agent-pool/notifications
GET /mcpserver/agent-pool/queue
DELETE /mcpserver/agent-pool/queue/{jobId}
POST /mcpserver/agent-pool/queue/{jobId}/cancel
POST /mcpserver/agent-pool/queue/{jobId}/move-down
POST /mcpserver/agent-pool/queue/{jobId}/move-up
POST /mcpserver/agent-pool/queue/one-shot
POST /mcpserver/agent-pool/queue/resolve
GET /mcpserver/agents
DELETE /mcpserver/agents/{agentId}
GET /mcpserver/agents/{agentId}
POST /mcpserver/agents/{agentId}
POST /mcpserver/agents/{agentId}/ban
GET /mcpserver/agents/{agentId}/events
POST /mcpserver/agents/{agentId}/events
POST /mcpserver/agents/{agentId}/launch
GET /mcpserver/agents/{agentId}/process-status
POST /mcpserver/agents/{agentId}/stop
POST /mcpserver/agents/{agentId}/unban
GET /mcpserver/agents/definitions
POST /mcpserver/agents/definitions
DELETE /mcpserver/agents/definitions/{agentType}
GET /mcpserver/agents/definitions/{agentType}
POST /mcpserver/agents/definitions/seed
GET /mcpserver/agents/running
GET /mcpserver/agents/validate
GET /mcpserver/configuration
PATCH /mcpserver/configuration
POST /mcpserver/context/ingest-website
POST /mcpserver/context/ingest-website/stream
POST /mcpserver/context/pack
POST /mcpserver/context/rebuild-index
POST /mcpserver/context/search
GET /mcpserver/context/sources
POST /mcpserver/desktop/launch  # also requires X-Desktop-Launch-Token when enabled
GET /mcpserver/events
GET /mcpserver/gh/actions/runs
GET /mcpserver/gh/actions/runs/{runId}
POST /mcpserver/gh/actions/runs/{runId}/cancel
POST /mcpserver/gh/actions/runs/{runId}/rerun
GET /mcpserver/gh/auth/status
DELETE /mcpserver/gh/auth/token
PUT /mcpserver/gh/auth/token
GET /mcpserver/gh/issues
POST /mcpserver/gh/issues
POST /mcpserver/gh/issues/{id}/comments
GET /mcpserver/gh/issues/{number}
PUT /mcpserver/gh/issues/{number}
POST /mcpserver/gh/issues/{number}/close
POST /mcpserver/gh/issues/{number}/reopen
POST /mcpserver/gh/issues/{number}/sync
POST /mcpserver/gh/issues/sync/from-github
POST /mcpserver/gh/issues/sync/to-github
GET /mcpserver/gh/labels
GET /mcpserver/gh/oauth/authorize-url
GET /mcpserver/gh/oauth/config
GET /mcpserver/gh/pulls
POST /mcpserver/gh/pulls/{id}/comments
POST /mcpserver/graphrag/index
POST /mcpserver/graphrag/query
GET /mcpserver/graphrag/status
GET /mcpserver/repo/file
POST /mcpserver/repo/file
GET /mcpserver/repo/list
GET /mcpserver/requirements/fr
POST /mcpserver/requirements/fr
DELETE /mcpserver/requirements/fr/{id}
GET /mcpserver/requirements/fr/{id}
PUT /mcpserver/requirements/fr/{id}
GET /mcpserver/requirements/generate
POST /mcpserver/requirements/ingest
GET /mcpserver/requirements/mapping
DELETE /mcpserver/requirements/mapping/{frId}
GET /mcpserver/requirements/mapping/{frId}
PUT /mcpserver/requirements/mapping/{frId}
GET /mcpserver/requirements/test
POST /mcpserver/requirements/test
DELETE /mcpserver/requirements/test/{id}
GET /mcpserver/requirements/test/{id}
PUT /mcpserver/requirements/test/{id}
GET /mcpserver/requirements/tr
POST /mcpserver/requirements/tr
DELETE /mcpserver/requirements/tr/{id}
GET /mcpserver/requirements/tr/{id}
PUT /mcpserver/requirements/tr/{id}
GET /mcpserver/sessionlog
POST /mcpserver/sessionlog
POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog
GET /mcpserver/templates
POST /mcpserver/templates
DELETE /mcpserver/templates/{id}
GET /mcpserver/templates/{id}
PUT /mcpserver/templates/{id}
POST /mcpserver/templates/{id}/resolve
POST /mcpserver/templates/{id}/test
POST /mcpserver/templates/test
GET /mcpserver/todo
POST /mcpserver/todo
DELETE /mcpserver/todo/{id}
GET /mcpserver/todo/{id}
PUT /mcpserver/todo/{id}
POST /mcpserver/todo/{id}/move
GET /mcpserver/todo/{id}/prompt/implement
POST /mcpserver/todo/{id}/prompt/implement/queue
GET /mcpserver/todo/{id}/prompt/plan
POST /mcpserver/todo/{id}/prompt/plan/queue
GET /mcpserver/todo/{id}/prompt/status
POST /mcpserver/todo/{id}/prompt/status/queue
POST /mcpserver/todo/{id}/requirements
GET /mcpserver/tools
POST /mcpserver/tools
DELETE /mcpserver/tools/{id}
GET /mcpserver/tools/{id}
PUT /mcpserver/tools/{id}
GET /mcpserver/tools/buckets
POST /mcpserver/tools/buckets
DELETE /mcpserver/tools/buckets/{name}
GET /mcpserver/tools/buckets/{name}/browse
POST /mcpserver/tools/buckets/{name}/install
POST /mcpserver/tools/buckets/{name}/sync
GET /mcpserver/tools/search
POST /mcpserver/tunnel/{name}/disable
POST /mcpserver/tunnel/{name}/enable
POST /mcpserver/tunnel/{name}/restart
POST /mcpserver/tunnel/{name}/start
GET /mcpserver/tunnel/{name}/status
POST /mcpserver/tunnel/{name}/stop
GET /mcpserver/tunnel/list
GET /mcpserver/voice/session
POST /mcpserver/voice/session
DELETE /mcpserver/voice/session/{sessionId}
GET /mcpserver/voice/session/{sessionId}
POST /mcpserver/voice/session/{sessionId}/escape
POST /mcpserver/voice/session/{sessionId}/interrupt
GET /mcpserver/voice/session/{sessionId}/transcript
POST /mcpserver/voice/session/{sessionId}/turn
POST /mcpserver/voice/session/{sessionId}/turn/stream
GET /mcpserver/workspace
POST /mcpserver/workspace
DELETE /mcpserver/workspace/{key}
GET /mcpserver/workspace/{key}
PUT /mcpserver/workspace/{key}
POST /mcpserver/workspace/{key}/init
POST /mcpserver/workspace/{key}/start
GET /mcpserver/workspace/{key}/status
POST /mcpserver/workspace/{key}/stop
POST /mcpserver/workspace/policy
GET /mcpserver/workspace/prompt
PUT /mcpserver/workspace/prompt

```

## 4) MCP tool catalog (STDIO tools)

Source: `src/McpServer.Support.Mcp/McpStdio/McpServerMcpTools.cs`

Current surface area: 42 tools.

### Workspace policy

- `workspace_policy_apply`

### Context and GraphRAG

- `context_search`, `context_pack`, `context_sources`, `context_ingest_website`
- `graphrag_status`, `graphrag_index`, `graphrag_query`

### Repo and sync

- `repo_read`, `repo_list`, `repo_write`, `sync_run`, `sync_status`

### TODO workflow

- `todo_list`, `todo_get`, `todo_create`, `todo_update`, `todo_delete`, `todo_move`
- `todo_plan`, `todo_implement`, `todo_status`

### Requirements

- `requirements_list`, `requirements_generate`, `requirements_create`, `requirements_update`, `requirements_delete`

### Session logs

- `sessionlog_submit`, `sessionlog_query`, `sessionlog_dialog`

### GitHub

- `github_list_issues`, `github_list_pulls`, `github_create_issue`, `github_comment_issue`, `github_comment_pull`

### Prompt templates and desktop

- `prompt_template_list`, `prompt_template_get`, `prompt_template_create`, `prompt_template_update`, `prompt_template_delete`, `prompt_template_test`
- `desktop_launch`

## 5) GraphRAG setup and usage

### Enable GraphRAG

Set `Mcp:GraphRag:Enabled` to `true` and configure:

- `RootPath`
- `DefaultQueryMode` (`local`, `global`, `drift`)
- `DefaultMaxChunks`
- `IndexTimeoutSeconds`, `QueryTimeoutSeconds`
- `MaxConcurrentIndexJobsPerWorkspace`

Example:

```yaml
Mcp:
  GraphRag:
    Enabled: true
    RootPath: mcp-data/graphrag
    DefaultQueryMode: local
    DefaultMaxChunks: 20
    IndexTimeoutSeconds: 900
    QueryTimeoutSeconds: 120
```

### Index workflow

1. Start server.
2. `POST /mcpserver/graphrag/index` (or MCP tool `graphrag_index`).
3. Monitor with `GET /mcpserver/graphrag/status`.
4. Query with `POST /mcpserver/graphrag/query`.

### Rollout checklist

- [ ] Confirm embedding/vector dimensions
- [ ] Ensure write permissions on `mcp-data/graphrag`
- [ ] Run first index in non-production
- [ ] Validate latency and answer quality
- [ ] Track coverage with `context_sources`
- [ ] Define rebuild cadence (`sync_run` + `graphrag_index`)
- [ ] Add alerting for failed index/query jobs

## 6) Agent Pool and workspace multi-tenancy

### Agent Pool setup

- inspect workers via `/mcpserver/agent-pool/agents`
- queue ad-hoc jobs via `/mcpserver/agent-pool/queue/one-shot`
- resolve queued orchestrations via `/mcpserver/agent-pool/queue/resolve`
- stream progress via `/mcpserver/agent-pool/jobs/{jobId}/stream`

Queue one-shot example:

```json
{
  "prompt": "Summarize recent TODO changes",
  "model": "gpt-5.3-codex"
}
```

### Multi-tenant workspace model

- one server port hosts all configured workspaces
- resolution order:
  1. `X-Workspace-Path`
  2. API-key reverse lookup
  3. primary workspace fallback
- workspaces are configured under `Mcp:Workspaces`
- each workspace uses scoped marker metadata and keys

## 7) Troubleshooting and FAQ

### 401 Unauthorized on `/mcpserver/*`

- refresh `apiKey` from `AGENTS-README-FIRST.yaml`
- verify `X-Workspace-Path` targets a registered workspace
- use full workspace key for non-TODO write operations

### Workspace not found or wrong data set

- send explicit `X-Workspace-Path`
- check registrations via `GET /mcpserver/workspace`

### MCP transport handshake issues

- ensure client uses `/mcp-transport`
- include `Accept: application/json, text/event-stream`

### GraphRAG returns empty or weak answers

- confirm GraphRAG is enabled and indexed
- verify ingestion (`sync_run`) and source coverage
- validate embedding/vector compatibility

### Tool registry or GitHub actions fail

- run `gh auth status`
- validate `Mcp:ToolRegistry` settings
- verify token status at `GET /mcpserver/gh/auth/status`

### Windows service deployment concerns

- always use `scripts\Update-McpService.ps1`
- do not manually overwrite `C:\ProgramData\McpServer`

## 8) Wire docs into README index and docs folder

This user guide is wired into:

- repository README (`README.md`)
- docs index (`docs/README.md`)
- docs navigation (`docs/toc.yml`)

## Reference links

- `MCP-SERVER.md`
- `README.md`
- `FAQ.md`
- `context/`
- `Operations/`

