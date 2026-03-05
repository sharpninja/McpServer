# MCP Server User Documentation

This guide is for operators and AI-agent users running `McpServer.Support.Mcp`.

## 1) Installation and prerequisites

### Supported host environment

- Windows 10/11 or Windows Server (service scripts are PowerShell-first)
- .NET SDK 9.x for local development
- `gh` CLI for GitHub issue and PR tooling
- Network access to configured MCP port (default `7147`)

### Prerequisite checks

```powershell
# .NET SDK
 dotnet --version

# GitHub CLI
 gh auth status

# MCP health
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

#### Windows service

```powershell
.\scripts\Manage-McpService.ps1 -Action Install
.\scripts\Manage-McpService.ps1 -Action Start
```

### Verify startup

- `GET /health` returns `{"status":"Healthy"}`
- `GET /swagger` opens API UI
- Marker file `AGENTS-README-FIRST.yaml` exists at workspace root

## 2) Configuration reference (appsettings + marker file)

### appsettings keys (root)

- `DataFolder`: base folder used for relative MCP data paths
- `Embedding:*` and `VectorIndex:*`: semantic indexing settings
- `Mcp:Port`: server port (default `7147`)
- `Mcp:DataSource`, `Mcp:DataDirectory`: storage path controls
- `Mcp:RepoRoot`, `Mcp:RepoAllowlist`: repository access and index scope
- `Mcp:TodoFilePath`, `Mcp:TodoStorage:*`: TODO backend and files
- `Mcp:GraphRag:*`: GraphRAG behavior
- `Mcp:ToolRegistry:*`: default tool bucket configuration
- `Mcp:Tunnel:*`: ngrok/cloudflare/frp provider settings
- `Mcp:Workspaces`: workspace registrations
- `Mcp:Instances:{name}:*`: per-instance overrides

### Configuration precedence

1. `PORT` environment variable
2. `Mcp:Instances:{name}:Port`
3. `Mcp:Port`
4. default `7147`

Instance-level `Mcp:Instances:{name}:*` overrides base `Mcp:*` settings.

### Marker file (`AGENTS-README-FIRST.yaml`)

Marker file fields used by agents:

- `baseUrl`, `port`
- `apiKey` (rotates when server restarts)
- endpoint map (`health`, `swagger`, `todo`, `sessionLog`, etc.)
- `workspacePath`, `workspace` name
- startup timestamps (`serverStartedAtUtc`, `markerWrittenAtUtc`)

Use marker data for authenticated calls:

```powershell
$marker = Get-Content .\AGENTS-README-FIRST.yaml -Raw
$apiKey = ([regex]::Match($marker,'apiKey:\s*(\S+)')).Groups[1].Value
Invoke-RestMethod -Uri "http://localhost:7147/mcpserver/todo" -Headers @{ 'X-Api-Key' = $apiKey }
```

### PowerShell helper modules

Import and initialize helper modules (preferred over raw REST for TODO/session logging):

```powershell
Import-Module .\tools\powershell\McpSession.psm1
Import-Module .\tools\powershell\McpTodo.psm1

Initialize-McpSession
Initialize-McpTodo
```

Sample session log flow:

```powershell
$s = New-McpSessionLog -SourceType "Copilot" -Title "MCP docs update" -Model "gpt-5.3-codex"
$e = Add-McpSessionEntry -Session $s -QueryTitle "Update docs" -QueryText "Create user docs" -Status in_progress
Add-McpAction -Entry $e -Description "Created docs\\USER-GUIDE.md" -Type edit -FilePath "docs/USER-GUIDE.md"
Set-McpSessionEntry -Session $s -Entry $e -Response "Docs complete" -Status completed
Update-McpSessionLog -Session $s -Status completed
```

Sample TODO flow:

```powershell
$todo = Get-McpTodo -Id "MCP-USERDOCS-001"
$tasks = @(
  @{ task = "Write Installation & Prerequisites guide"; done = $true },
  @{ task = "Write Configuration reference (appsettings + marker file)"; done = $true }
)
Update-McpTodo -Id $todo.id -ImplementationTasks $tasks -Note "Initial documentation sections complete."
```

## 3) REST API reference (all controllers)

Base URL: `http://<host>:7147`

Authentication: include `X-Api-Key` for `/mcpserver/*` endpoints; add `X-Workspace-Path` to explicitly target a workspace.

### AuthConfig controller (`/auth/*`)

- `GET /auth/config`
- `POST /auth/device`
- `POST /auth/token`
- `GET /auth/ui/{path}`
- `POST /auth/ui/{path}`

### AgentPool controller (`/mcpserver/agent-pool/*`)

- `GET /agents`, `POST /connect`
- `POST /agents/{agentName}/start|stop|connect|recycle`
- `GET /queue`, `DELETE /queue/{jobId}`
- `POST /queue/{jobId}/cancel|move-up|move-down`
- `POST /queue/one-shot`, `POST /queue/resolve`
- `GET /jobs/{jobId}/stream`, `GET /notifications`

### Agent controller (`/mcpserver/agents*`)

- `GET /mcpserver/agents`
- `GET|POST|DELETE /mcpserver/agents/{agentId}`
- `POST /mcpserver/agents/{agentId}/ban|unban`
- `GET|POST /mcpserver/agents/{agentId}/events`
- `GET|POST /mcpserver/agents/definitions`
- `GET|DELETE /mcpserver/agents/definitions/{agentType}`
- `POST /mcpserver/agents/definitions/seed`
- `GET /mcpserver/agents/validate`

### Context controller (`/mcpserver/context/*`)

- `POST /search`
- `POST /pack`
- `GET /sources`
- `POST /rebuild-index`

Request example:

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

### Diagnostic controller (`/mcpserver/diagnostic/*`)

- `GET /execution-path`
- `GET /appsettings-path`

### EventStream controller

- `GET /mcpserver/events`

### GitHub controller (`/mcpserver/gh/*`)

- Issues: `GET|POST /issues`, `GET|PUT /issues/{number}`
- Issue actions: `POST /issues/{number}/close|reopen|sync`, `POST /issues/{id}/comments`
- Bulk sync: `POST /issues/sync/from-github`, `POST /issues/sync/to-github`
- Metadata: `GET /labels`, `GET /pulls`, `POST /pulls/{id}/comments`

### GraphRag controller (`/mcpserver/graphrag/*`)

- `GET /status`
- `POST /index`
- `POST /query`

### PromptTemplate controller (`/mcpserver/templates*`)

- `GET|POST /mcpserver/templates`
- `GET|PUT|DELETE /mcpserver/templates/{id}`
- `POST /mcpserver/templates/{id}/resolve`
- `POST /mcpserver/templates/{id}/test`
- `POST /mcpserver/templates/test`

### Repo controller (`/mcpserver/repo/*`)

- `GET /repo/file`
- `POST /repo/file`
- `GET /repo/list`

Write example:

```json
{
  "path": "docs/example.md",
  "content": "# example"
}
```

### Requirements controller (`/mcpserver/requirements/*`)

- `GET /generate`
- `GET|POST /fr`, `GET|PUT|DELETE /fr/{id}`
- `GET|POST /tr`, `GET|PUT|DELETE /tr/{id}`
- `GET|POST /test`, `GET|PUT|DELETE /test/{id}`
- `GET /mapping`, `GET|PUT|DELETE /mapping/{frId}`

### SessionLog controller (`/mcpserver/sessionlog*`)

- `GET /mcpserver/sessionlog`
- `POST /mcpserver/sessionlog`
- `POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog`

Submit example:

```json
{
  "sourceType": "Codex",
  "sessionId": "Codex-20260305T160000Z-example",
  "title": "Session",
  "model": "gpt-5.3-codex",
  "started": "2026-03-05T16:00:00Z",
  "lastUpdated": "2026-03-05T16:00:00Z",
  "status": "in_progress",
  "entries": []
}
```

### Todo controller (`/mcpserver/todo*`)

- `GET|POST /mcpserver/todo`
- `GET|PUT|DELETE /mcpserver/todo/{id}`
- `POST /mcpserver/todo/{id}/move`
- `POST /mcpserver/todo/{id}/requirements`
- `GET /mcpserver/todo/{id}/prompt/implement|plan|status`
- `POST /mcpserver/todo/{id}/prompt/implement|plan|status/queue`

Update example:

```json
{
  "implementationTasks": [
    { "task": "Write Installation & Prerequisites guide", "done": true }
  ],
  "note": "Installation section published."
}
```

### ToolRegistry controller (`/mcpserver/tools*`)

- `GET|POST /mcpserver/tools`
- `GET|PUT|DELETE /mcpserver/tools/{id}`
- `GET /mcpserver/tools/search`
- Bucket routes: `GET|POST /buckets`, `DELETE /buckets/{name}`
- Bucket operations: `GET /buckets/{name}/browse`, `POST /buckets/{name}/install`, `POST /buckets/{name}/sync`

### Tunnel controller (`/mcpserver/tunnel/*`)

- `GET /mcpserver/tunnel/list`
- `GET /mcpserver/tunnel/{name}/status`
- `POST /mcpserver/tunnel/{name}/start|stop|restart|enable|disable`

### Voice controller (`/mcpserver/voice/*`)

- `GET|POST /session`
- `GET|DELETE /session/{sessionId}`
- `POST /session/{sessionId}/turn`
- `POST /session/{sessionId}/turn/stream`
- `POST /session/{sessionId}/interrupt`
- `POST /session/{sessionId}/escape`
- `GET /session/{sessionId}/transcript`

### Workspace controller (`/mcpserver/workspace*`)

- `GET|POST /mcpserver/workspace`
- `GET|PUT|DELETE /mcpserver/workspace/{key}`
- `POST /mcpserver/workspace/{key}/init|start|stop`
- `GET /mcpserver/workspace/{key}/status`
- `GET|PUT /mcpserver/workspace/prompt`

## 4) MCP tool catalog (STDIO tools)

Source: `src/McpServer.Support.Mcp/McpStdio/McpServerMcpTools.cs`.

### Workspace policy

- `workspace_policy_apply`

### Context and GraphRAG

- `context_search`, `context_pack`, `context_sources`
- `graphrag_status`, `graphrag_index`, `graphrag_query`

### Repo and sync

- `repo_read`, `repo_list`, `repo_write`
- `sync_run`, `sync_status`

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

- `RootPath` (artifact storage)
- `DefaultQueryMode` (`local`, `global`, `drift`)
- `DefaultMaxChunks`
- `IndexTimeoutSeconds`, `QueryTimeoutSeconds`
- `MaxConcurrentIndexJobsPerWorkspace`

### Index workflow

1. Start server
2. `POST /mcpserver/graphrag/index` (or tool `graphrag_index`)
3. Monitor with `GET /mcpserver/graphrag/status`
4. Query with `POST /mcpserver/graphrag/query`

Query example:

```json
{
  "query": "summarize workspace tenancy model",
  "mode": "local",
  "maxChunks": 20,
  "includeContextChunks": true
}
```

### Rollout checklist

- [ ] Confirm embeddings and vector index config are valid
- [ ] Ensure `mcp-data/graphrag` has write permissions
- [ ] Run initial index in non-production first
- [ ] Validate latency budgets with representative prompts
- [ ] Record source coverage via `context_sources`
- [ ] Define rebuild cadence (`sync_run` + `graphrag_index`)
- [ ] Add operational alerts for failed index/query runs

## 6) Agent Pool and workspace multi-tenancy

### Agent Pool setup

- Use `/mcpserver/agent-pool/agents` to inspect workers
- Use `/mcpserver/agent-pool/queue/one-shot` for ad-hoc jobs
- Use `/mcpserver/agent-pool/queue/resolve` for queued prompt orchestration
- Stream progress via `/mcpserver/agent-pool/jobs/{jobId}/stream`

Queue operations:

- reorder jobs (`move-up`, `move-down`)
- cancel (`queue/{jobId}/cancel`)
- remove (`DELETE queue/{jobId}`)

### Multi-tenant workspace model

- One server port for all workspaces
- Workspace selection order:
  1. `X-Workspace-Path`
  2. API key workspace reverse-lookup
  3. primary workspace fallback
- Workspaces are managed in `Mcp:Workspaces` (appsettings)
- Marker file per workspace contains scoped API key and endpoint details

Workspace create/start example:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:7147/mcpserver/workspace" -Headers @{ 'X-Api-Key' = '<key>' } -Body '{"workspacePath":"E:\\github\\MyProject"}' -ContentType 'application/json'
```

## 7) Troubleshooting and FAQ

### 401 Unauthorized on `/mcpserver/*`

- Re-read `AGENTS-README-FIRST.yaml` and refresh `apiKey`
- Confirm `X-Workspace-Path` points to a registered workspace

### Workspace not found / wrong data set

- Send explicit `X-Workspace-Path` header
- Validate workspace registration via `GET /mcpserver/workspace`

### MCP transport handshake issues

- Ensure client uses `/mcp-transport`
- Include `Accept: application/json, text/event-stream`

### GraphRAG returns empty/poor results

- Confirm GraphRAG is enabled and indexed
- Verify source ingestion (`sync_run`) and `context_sources`
- Check embedding/vector index settings (dimensions must match)

### Tool registry or GitHub calls fail

- Verify `gh auth status`
- Confirm bucket name/repo configuration under `Mcp:ToolRegistry`

### Windows service deployment risks

- Always use `scripts\Update-McpService.ps1` for updates
- Do not overwrite `C:\ProgramData\McpServer` directly

## 8) Reference links

- Server guide: `docs/MCP-SERVER.md`
- Endpoint audit: `docs/ENDPOINT-AUDIT.md`
- FAQ: `docs/FAQ.md`
- Context docs: `docs/context/`
- Tunnel runbooks: `docs/Operations/`
