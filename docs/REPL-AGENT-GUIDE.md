# MCP REPL Agent Guide

## Overview

This guide provides detailed operational workflows for AI agents integrating with the MCP REPL tool. It covers trust bootstrap, session lifecycle, TODO management, requirements tracking, and protocol details for agent-facing automation.

## Target Audience

- AI agents (Copilot, Cline, Cursor, Aider, etc.)
- Automation scripts
- MCP protocol implementers
- Tool developers

## Trust Bootstrap

Before making any MCP endpoint calls, agents must verify the server's authenticity using the marker-file signature and nonce handshake.

### Step 1: Read Marker File

Read `AGENTS-README-FIRST.yaml` from the workspace root:

```yaml
version: 1.0
workspacePath: C:\workspace\project
apiKey: <rotating-api-key>
baseUrl: http://localhost:7147
markerSignature: <HMAC-SHA256-signature>
agent:
  model: claude-sonnet-4-20250514
  capabilities:
    - sessionlog
    - todo
    - requirements
promptSource: templates/prompt-templates.yaml
promptTemplateId: default-marker-prompt
```

### Step 2: Verify Marker Signature

Recompute the HMAC-SHA256 marker signature using the workspace API key as the
HMAC key. **The signed payload is canonicalised as `key=value` lines separated
by a single LF (`\n`) — never CRLF, regardless of the host operating system.**
The hex-encoded digest must match `signature.value` in the marker (case-insensitive).

The canonical key order is:

```text
canonicalization=marker-v1\n
port={Port}\n
baseUrl={BaseUrl}\n
apiKey={ApiKey}\n
workspace={Workspace}\n
workspacePath={WorkspacePath}\n
pid={Pid}\n
startedAt={StartedAt}\n
markerWrittenAtUtc={MarkerWrittenAtUtc}\n
serverStartedAtUtc={ServerStartedAtUtc}\n
endpoints.health={EndpointHealth}\n
endpoints.swagger={EndpointSwagger}\n
endpoints.swaggerUi={EndpointSwaggerUi}\n
endpoints.mcpTransport={EndpointMcpTransport}\n
endpoints.sessionLog={EndpointSessionLog}\n
endpoints.sessionLogDialog={EndpointSessionLogDialog}\n
endpoints.contextSearch={EndpointContextSearch}\n
endpoints.contextPack={EndpointContextPack}\n
endpoints.contextSources={EndpointContextSources}\n
endpoints.todo={EndpointTodo}\n
endpoints.repo={EndpointRepo}\n
endpoints.desktop={EndpointDesktop}\n
endpoints.gitHub={EndpointGitHub}\n
endpoints.tools={EndpointTools}\n
endpoints.workspace={EndpointWorkspace}\n
endpoints.serverStartupUtc={EndpointServerStartupUtc}\n
endpoints.markerFileTimestamp={EndpointMarkerFileTimestamp}\n
# Only emitted when agent_plugins is present in the marker:
agentPlugins.policy={AgentPluginsPolicy}\n
agentPlugins.contractDigest={AgentPluginsContractDigest}\n
```

```csharp
using System.Security.Cryptography;
using System.Text;

string ComputeMarkerSignature(MarkerData marker)
{
    var payload = new StringBuilder();
    void Line(string key, string value) =>
        payload.Append(key).Append('=').Append(value ?? string.Empty).Append('\n');

    Line("canonicalization", marker.SignatureCanonicalization); // "marker-v1"
    Line("port", marker.Port.ToString(CultureInfo.InvariantCulture));
    Line("baseUrl", marker.BaseUrl);
    Line("apiKey", marker.ApiKey);
    Line("workspace", marker.Workspace);
    Line("workspacePath", marker.WorkspacePath);
    Line("pid", marker.Pid.ToString(CultureInfo.InvariantCulture));
    Line("startedAt", marker.StartedAt);
    Line("markerWrittenAtUtc", marker.MarkerWrittenAtUtc);
    Line("serverStartedAtUtc", marker.ServerStartedAtUtc);
    foreach (var (key, value) in marker.Endpoints) // emit in canonical order above
        Line($"endpoints.{key}", value);
    if (marker.AgentPlugins is not null)
    {
        Line("agentPlugins.policy", marker.AgentPlugins.Policy);
        Line("agentPlugins.contractDigest", marker.AgentPlugins.ContractDigest);
    }

    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(marker.ApiKey));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload.ToString()));
    return Convert.ToHexString(hash); // uppercase hex; comparison is case-insensitive
}

// Verify
var computed = ComputeMarkerSignature(markerData);
if (!string.Equals(computed, markerData.SignatureValue, StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("MCP_UNTRUSTED: Marker signature verification failed");
}
```

> **Common pitfall:** `StringBuilder.AppendLine` honours
> `Environment.NewLine` and emits CRLF on Windows, which breaks signature
> verification against a server-produced marker. Always append a literal `\n`
> (or use the helper shown above). This was the root cause of the v6.0.0 REPL
> tool's "Authentication required" failure for agent-stdio mode on Windows -
> fixed in v6.1.0 (FR-MCP-REPL-007 / TR-MCP-REPL-008 regression).

### Step 3: Nonce Handshake

Call `/health?nonce=<random>` and verify the response echoes the exact nonce:

```bash
# Generate random nonce
NONCE=$(uuidgen)

# Call health endpoint with nonce
curl -H "X-Api-Key: $API_KEY" \
     -H "X-Workspace-Path: /workspace/path" \
     "http://localhost:7147/health?nonce=$NONCE"

# Expected response:
# {"status":"healthy","nonce":"<same-nonce>","timestamp":"2026-03-04T11:50:00Z"}
```

If signature verification or nonce verification fails, emit `MCP_UNTRUSTED`, clear all MCP connection state, and stop before probing additional endpoints.

### Step 4: Store Connection State

After successful bootstrap, store the connection metadata for subsequent requests:

```yaml
connectionState:
  workspacePath: C:\workspace\project
  apiKey: <rotating-api-key>
  baseUrl: http://localhost:7147
  verified: true
  verifiedAt: 2026-03-04T11:50:00Z
```

## Agent STDIO Mode

### Invocation

```bash
mcpserver-repl --agent-stdio
```

### Handshake

**Client → Server (hello request):**

```yaml
type: hello
payload:
  protocolVersion: 1.0
  clientName: Copilot
  clientVersion: 2.0.0
  capabilities:
    - sessionlog
    - todo
    - requirements
    - streaming
  metadata:
    workspacePath: C:\workspace\project
```

**Server → Client (hello response):**

```yaml
type: hello
payload:
  protocolVersion: 1.0
  serverVersion: 1.5.0
  capabilities:
    - sessionlog
    - todo
    - requirements
    - streaming
    - client-passthrough
  supportedNamespaces:
    - workflow.sessionlog
    - workflow.todo
    - workflow.requirements
    - client
```

### Command Dispatch

After handshake, send request envelopes over stdin, receive result/error/event envelopes on stdout.

**Request Format:**

```yaml
type: request
payload:
  requestId: <unique-request-id>
  method: <namespace>.<command>
  params:
    <param-name>: <param-value>
    ...
```

**Result Format:**

```yaml
type: result
payload:
  requestId: <matching-request-id>
  result:
    <result-data>
```

**Error Format:**

```yaml
type: error
payload:
  requestId: <matching-request-id>
  code: <error-code>
  message: <human-readable-message>
  details:
    <context-specific-details>
```

**Event Format (streaming):**

```yaml
type: event
payload:
  event: <event-name>
  data:
    <event-data>
```

## Session Log Workflow

### Operational Checklist

**On every agent session start:**

1. Read `AGENTS-README-FIRST.yaml` from workspace root
2. Verify marker signature using API key
3. Perform `/health` nonce handshake
4. Call `workflow.sessionlog.bootstrap` (idempotent)
5. Call `workflow.sessionlog.openSession` with agent/session ID/title/model
6. Store active session state

**On every user message:**

1. Call `workflow.sessionlog.beginTurn` with request ID/query title/query text
2. As work progresses, call `workflow.sessionlog.appendDialog` with reasoning items
3. As files change, call `workflow.sessionlog.appendActions` with file operations
4. When complete, call `workflow.sessionlog.completeTurn` with final response
5. Persist session log immediately after turn completion

**At regular intervals during long sessions (~10 interactions):**

1. Ensure all turns are persisted
2. Verify all design decisions are captured
3. Check requirements docs are up to date

### Bootstrap

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-bootstrap-001
  method: workflow.sessionlog.bootstrap
  params: {}
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-bootstrap-001
  result:
    success: true
    subsystem: sessionlog
    initialized: true
```

### Open Session

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-open-001
  method: workflow.sessionlog.openSession
  params:
    agent: Copilot
    sessionId: Copilot-20260304T113901Z-feature-auth
    title: Implementing JWT authentication
    model: claude-sonnet-4-20250514
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-open-001
  result:
    success: true
    sessionId: Copilot-20260304T113901Z-feature-auth
    agent: Copilot
    title: Implementing JWT authentication
    model: claude-sonnet-4-20250514
    status: in_progress
    started: 2026-03-04T11:39:01Z
```

### Begin Turn

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-begin-001
  method: workflow.sessionlog.beginTurn
  params:
    requestId: req-20260304T113901Z-add-jwt-001
    queryTitle: Add JWT authentication
    queryText: Implement JWT token generation and validation
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-begin-001
  result:
    success: true
    turnRequestId: req-20260304T113901Z-add-jwt-001
    status: in_progress
    timestamp: 2026-03-04T11:40:00Z
```

### Append Dialog

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-dialog-001
  method: workflow.sessionlog.appendDialog
  params:
    dialogItems:
      - timestamp: 2026-03-04T11:45:00Z
        role: model
        content: Analyzing requirements and existing authentication code...
        category: reasoning
      - timestamp: 2026-03-04T11:45:30Z
        role: tool
        content: Created src/Services/TokenService.cs
        category: tool_result
      - timestamp: 2026-03-04T11:46:00Z
        role: model
        content: |
          Decision: Use HS256 algorithm for JWT signing.
          Rationale: Symmetric key simplifies key management for this internal service.
          Alternatives considered: RS256 (asymmetric), but adds key distribution complexity.
        category: decision
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-dialog-001
  result:
    success: true
    itemsAppended: 3
```

### Append Actions

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-action-001
  method: workflow.sessionlog.appendActions
  params:
    actions:
      - order: 1
        description: Created TokenService with JWT generation
        type: create
        status: completed
        filePath: src/Services/TokenService.cs
      - order: 2
        description: Created JwtValidator for token validation
        type: create
        status: completed
        filePath: src/Services/JwtValidator.cs
      - order: 3
        description: Edited Startup.cs to register JWT services
        type: edit
        status: completed
        filePath: src/Startup.cs
      - order: 4
        description: Logged design decision for HS256 algorithm
        type: design_decision
        status: completed
        filePath: ""
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-action-001
  result:
    success: true
    actionsAppended: 4
```

### Complete Turn

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-complete-001
  method: workflow.sessionlog.completeTurn
  params:
    response: |
      JWT authentication implemented:
      - Created TokenService for JWT generation
      - Created JwtValidator for token validation
      - Registered services in Startup.cs
      - Used HS256 algorithm for symmetric signing
      - All unit tests passing
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-complete-001
  result:
    success: true
    turnRequestId: req-20260304T113901Z-add-jwt-001
    status: completed
    completedAt: 2026-03-04T11:50:00Z
```

### Fail Turn

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-fail-001
  method: workflow.sessionlog.failTurn
  params:
    errorMessage: Unable to complete task due to missing System.IdentityModel.Tokens.Jwt package
    errorCode: dependency_missing
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-fail-001
  result:
    success: true
    turnRequestId: req-20260304T113901Z-add-jwt-001
    status: failed
    errorMessage: Unable to complete task due to missing System.IdentityModel.Tokens.Jwt package
    errorCode: dependency_missing
    failedAt: 2026-03-04T11:50:00Z
```

### Query History

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-query-001
  method: workflow.sessionlog.queryHistory
  params:
    agent: Copilot
    limit: 5
    offset: 0
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-query-001
  result:
    items:
      - agent: Copilot
        sessionId: Copilot-20260304T113901Z-feature-auth
        title: Implementing JWT authentication
        model: claude-sonnet-4-20250514
        started: 2026-03-04T11:39:01Z
        lastUpdated: 2026-03-04T11:50:00Z
        status: completed
        turnCount: 1
        filesModifiedCount: 3
        tags: [auth, jwt, security]
      - agent: Copilot
        sessionId: Copilot-20260304T100000Z-refactor-session
        title: Refactoring session logging
        model: claude-sonnet-4-20250514
        started: 2026-03-04T10:00:00Z
        lastUpdated: 2026-03-04T10:30:00Z
        status: completed
        turnCount: 2
        filesModifiedCount: 5
        tags: [refactor, sessionlog]
    totalCount: 5
```

## TODO Workflow

### Query TODOs

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-query-001
  method: workflow.todo.query
  params:
    keyword: authentication
    priority: high
    section: Backend
    done: false
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-query-001
  result:
    items:
      - id: MCP-AUTH-001
        title: Implement JWT authentication
        section: Backend
        priority: high
        done: false
        estimate: 4h
        description:
          - Add JWT token generation
          - Add JWT token validation
        functionalRequirements: [FR-AUTH-001]
        technicalRequirements: [TR-AUTH-001]
    totalCount: 1
```

### Get TODO

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-get-001
  method: workflow.todo.get
  params:
    id: MCP-AUTH-001
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-get-001
  result:
    item:
      id: MCP-AUTH-001
      title: Implement JWT authentication
      section: Backend
      priority: high
      done: false
      estimate: 4h
      description:
        - Add JWT token generation
        - Add JWT token validation
      implementationTasks:
        - task: Create TokenService
          done: false
        - task: Create JwtValidator
          done: false
      remaining: Need integration tests
      functionalRequirements: [FR-AUTH-001]
      technicalRequirements: [TR-AUTH-001]
```

### Select TODO

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-select-001
  method: workflow.todo.select
  params:
    id: MCP-AUTH-001
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-select-001
  result:
    selected: true
    id: MCP-AUTH-001
    title: Implement JWT authentication
    section: Backend
    priority: high
    done: false
    selectedAt: 2026-03-04T11:45:23Z
```

### Create TODO

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-create-001
  method: workflow.todo.create
  params:
    id: MCP-AUTH-002
    title: Add rate limiting to auth endpoints
    section: Backend
    priority: medium
    estimate: 2h
    description:
      - Implement sliding window rate limiter
      - Configure 100 requests per 15 minutes
    dependsOn: [MCP-AUTH-001]
    functionalRequirements: [FR-AUTH-002]
    technicalRequirements: [TR-AUTH-002]
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-create-001
  result:
    success: true
    item:
      id: MCP-AUTH-002
      title: Add rate limiting to auth endpoints
      section: Backend
      priority: medium
      done: false
      estimate: 2h
      description:
        - Implement sliding window rate limiter
        - Configure 100 requests per 15 minutes
      dependsOn: [MCP-AUTH-001]
      functionalRequirements: [FR-AUTH-002]
      technicalRequirements: [TR-AUTH-002]
```

### Update TODO

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-update-001
  method: workflow.todo.update
  params:
    id: MCP-AUTH-001
    remaining: Integration tests needed
    implementationTasks:
      - task: Create TokenService
        done: true
      - task: Create JwtValidator
        done: true
      - task: Add integration tests
        done: false
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-update-001
  result:
    success: true
    item:
      id: MCP-AUTH-001
      title: Implement JWT authentication
      section: Backend
      priority: high
      done: false
      remaining: Integration tests needed
      implementationTasks:
        - task: Create TokenService
          done: true
        - task: Create JwtValidator
          done: true
        - task: Add integration tests
          done: false
```

### Stream Status Analysis

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-status-001
  method: workflow.todo.streamStatus
  params:
    id: MCP-AUTH-001
```

**Event Stream (progress):**

```yaml
type: event
payload:
  event: workflow.todo.streamStatus
  data:
    eventType: status.progress
    sequence: 1
    timestamp: 2026-03-04T11:45:30Z
    message: Analyzing TODO dependencies...
    progress: 25
---
type: event
payload:
  event: workflow.todo.streamStatus
  data:
    eventType: status.progress
    sequence: 2
    timestamp: 2026-03-04T11:45:45Z
    message: Checking requirement references...
    progress: 50
---
type: event
payload:
  event: workflow.todo.streamStatus
  data:
    eventType: status.progress
    sequence: 3
    timestamp: 2026-03-04T11:46:00Z
    message: Validating implementation tasks...
    progress: 75
---
type: event
payload:
  event: workflow.todo.streamStatus
  data:
    eventType: status.complete
    sequence: 4
    timestamp: 2026-03-04T11:46:15Z
    todoId: MCP-AUTH-001
    status: ready
    blockers: []
    dependencies: [MCP-AUTH-002]
```

**Cancellation:**

If the agent cancels the stream, a cancellation event is emitted:

```yaml
type: event
payload:
  event: workflow.todo.streamStatus
  data:
    eventType: status.cancelled
    sequence: 5
    timestamp: 2026-03-04T11:46:20Z
    message: Stream cancelled by user request
```

## Requirements Workflow

### List Functional Requirements

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-listfr-001
  method: workflow.requirements.listFr
  params:
    area: MCP
    status: in_progress
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-listfr-001
  result:
    items:
      - id: FR-MCP-001
        title: Agent authentication
        description: System must authenticate AI agents via API key
        status: completed
        priority: critical
        area: MCP
        createdAt: 2026-03-01T10:00:00Z
        updatedAt: 2026-03-04T11:30:00Z
      - id: FR-MCP-002
        title: Workspace isolation
        description: Each workspace must be isolated from others
        status: in_progress
        priority: high
        area: MCP
        createdAt: 2026-03-01T10:30:00Z
        updatedAt: 2026-03-04T11:45:00Z
    totalCount: 2
```

### Create Functional Requirement

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-createfr-001
  method: workflow.requirements.createFr
  params:
    id: FR-MCP-003
    title: Context search
    description: System must support semantic search across workspace documents
    priority: high
    area: MCP
    notes: Use hybrid search with BM25 and embeddings
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-createfr-001
  result:
    success: true
    item:
      id: FR-MCP-003
      title: Context search
      description: System must support semantic search across workspace documents
      status: pending
      priority: high
      area: MCP
      notes: Use hybrid search with BM25 and embeddings
      createdAt: 2026-03-04T11:50:00Z
      updatedAt: 2026-03-04T11:50:00Z
```

### Create Technical Requirement

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-createtr-001
  method: workflow.requirements.createTr
  params:
    id: TR-MCP-PERF-001
    title: Response time SLA
    description: All API endpoints must respond within 500ms p99
    priority: high
    area: MCP
    subarea: PERF
    notes: Measure at gateway, exclude network time
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-createtr-001
  result:
    success: true
    item:
      id: TR-MCP-PERF-001
      title: Response time SLA
      description: All API endpoints must respond within 500ms p99
      status: pending
      priority: high
      area: MCP
      subarea: PERF
      notes: Measure at gateway, exclude network time
      createdAt: 2026-03-04T11:50:00Z
      updatedAt: 2026-03-04T11:50:00Z
```

### Create Requirement Mapping

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-createmap-001
  method: workflow.requirements.createMapping
  params:
    frId: FR-MCP-001
    trId: TR-MCP-ARCH-001
    testId: TEST-MCP-001
    notes: Core authentication flow
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-createmap-001
  result:
    success: true
    item:
      frId: FR-MCP-001
      trId: TR-MCP-ARCH-001
      testId: TEST-MCP-001
      createdAt: 2026-03-04T11:50:00Z
      notes: Core authentication flow
```

### Generate Traceability Matrix

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-gendoc-001
  method: workflow.requirements.generateDocument
  params:
    format: markdown
    docType: matrix
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-gendoc-001
  result:
    success: true
    content: |
      # Requirement Traceability Matrix
      
      | FR ID | TR ID | TEST ID | Status |
      |-------|-------|---------|--------|
      | FR-MCP-001 | TR-MCP-ARCH-001 | TEST-MCP-001 | ✓ |
      | FR-MCP-002 | TR-MCP-ARCH-002 | TEST-MCP-003 | ○ |
    format: markdown
    docType: matrix
    generatedAt: 2026-03-04T11:50:00Z
```

## Client Passthrough

### Context Search

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T120000Z-search-001
  method: client.context.SearchAsync
  params:
    query: authentication flow
    sourceType: markdown
    limit: 10
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T120000Z-search-001
  result:
    results:
      - key: docs/auth.md
        content: "Authentication flow overview..."
        score: 0.95
      - key: src/AuthService.cs
        content: "public class AuthService..."
        score: 0.87
    totalResults: 2
```

### GitHub Issue Listing

**Request:**

```yaml
type: request
payload:
  requestId: req-20260304T120000Z-issues-001
  method: client.github.ListIssuesAsync
  params:
    state: open
    labels:
      - bug
      - priority-high
    assignee: johndoe
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T120000Z-issues-001
  result:
    issues:
      - number: 42
        title: Authentication timeout on slow networks
        state: open
        labels: [bug, priority-high]
        assignee: johndoe
      - number: 38
        title: Token refresh race condition
        state: open
        labels: [bug, priority-high]
        assignee: johndoe
    totalCount: 2
```

## Error Recovery

### API Key Rotation

If a request fails with 401 after a previously trusted bootstrap:

1. Re-read `AGENTS-README-FIRST.yaml` for fresh API key
2. Re-verify marker signature
3. Perform nonce handshake
4. Retry the failed request

**Error Response:**

```yaml
type: error
payload:
  requestId: req-20260304T113901Z-open-001
  code: unauthorized
  message: API key is invalid or expired
  details:
    hint: Re-read marker file for updated API key
```

**Recovery Steps:**

```bash
# 1. Re-read marker file
marker=$(cat AGENTS-README-FIRST.yaml)

# 2. Extract new API key
api_key=$(echo "$marker" | grep apiKey | cut -d: -f2 | xargs)

# 3. Re-verify signature and nonce
# (same steps as initial bootstrap)

# 4. Retry request with new API key
```

### Module Download Failure

If module download fails, retry with exponential backoff:

```bash
# Retry with exponential backoff
for i in 1 2 3; do
  if curl -H "X-Api-Key: $API_KEY" \
          "http://localhost:7147/mcpserver/tools/search?keyword=session" \
          -o McpSession.psm1; then
    break
  fi
  sleep $((2 ** i))
done
```

### Session Not Found

If `workflow.sessionlog.beginTurn` returns `session_not_found`:

1. Call `workflow.sessionlog.openSession` to create a new session
2. Retry `workflow.sessionlog.beginTurn`

**Error Response:**

```yaml
type: error
payload:
  requestId: req-20260304T113901Z-begin-001
  code: session_not_found
  message: No active session exists. Call openSession first.
  details:
    hint: Call workflow.sessionlog.openSession to create a session
```

### Turn Immutable

If `workflow.sessionlog.updateTurn` returns `turn_immutable`:

1. Accept that the turn is complete/failed and cannot be modified
2. Begin a new turn if needed

**Error Response:**

```yaml
type: error
payload:
  requestId: req-20260304T113901Z-update-001
  code: turn_immutable
  message: Turn is immutable (status: completed)
  details:
    turnRequestId: req-20260304T113901Z-add-jwt-001
    currentStatus: completed
    hint: Begin a new turn instead
```

## Design Decision Logging

When making design decisions during work:

1. Log as session log dialog item with category "decision"
2. Include: the decision, alternatives considered, rationale, and affected requirements
3. Add a session log action with type "design_decision"

**Example:**

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-dialog-002
  method: workflow.sessionlog.appendDialog
  params:
    dialogItems:
      - timestamp: 2026-03-04T11:46:00Z
        role: model
        content: |
          Decision: Use HS256 algorithm for JWT signing.
          Rationale: Symmetric key simplifies key management for this internal service.
          Alternatives considered:
          - RS256 (asymmetric): Adds key distribution complexity, overkill for internal service
          - HS512: Longer key but minimal security benefit for our threat model
          Affected requirements: TR-AUTH-001
        category: decision
---
type: request
payload:
  requestId: req-20260304T113901Z-action-002
  method: workflow.sessionlog.appendActions
  params:
    actions:
      - order: 5
        description: Chose HS256 algorithm for JWT signing (internal service, symmetric key simplifies management)
        type: design_decision
        status: completed
        filePath: ""
```

## Requirements Tracking

When discovering or agreeing on new requirements during a session:

1. Create FR/TR/TEST entries using `workflow.requirements.create*` methods
2. Update the requirements matrix using `workflow.requirements.createMapping`
3. Include requirement IDs in session log turn tags
4. Capture requirements as they emerge; do not defer

**Example:**

```yaml
# 1. Create functional requirement
type: request
payload:
  requestId: req-20260304T113901Z-createfr-002
  method: workflow.requirements.createFr
  params:
    id: FR-AUTH-003
    title: Token refresh mechanism
    description: System must support automatic token refresh before expiration
    priority: high
    area: AUTH
---
# 2. Create technical requirement
type: request
payload:
  requestId: req-20260304T113901Z-createtr-002
  method: workflow.requirements.createTr
  params:
    id: TR-AUTH-SEC-002
    title: Token refresh security
    description: Refresh tokens must be single-use and expire after 30 days
    priority: high
    area: AUTH
    subarea: SEC
---
# 3. Create mapping
type: request
payload:
  requestId: req-20260304T113901Z-createmap-002
  method: workflow.requirements.createMapping
  params:
    frId: FR-AUTH-003
    trId: TR-AUTH-SEC-002
    notes: Token refresh flow
---
# 4. Update turn with requirement tag
type: request
payload:
  requestId: req-20260304T113901Z-updateturn-001
  method: workflow.sessionlog.updateTurn
  params:
    tags: [auth, jwt, FR-AUTH-003, TR-AUTH-SEC-002]
```

## Streaming Semantics

### Progress Events

Streaming operations emit progress events with sequence numbers:

```yaml
type: event
payload:
  event: workflow.todo.streamStatus
  data:
    eventType: status.progress
    sequence: 3
    timestamp: 2026-03-04T11:45:45Z
    message: Checking requirement references...
    progress: 50
```

### Completion Events

Final event indicates successful completion:

```yaml
type: event
payload:
  event: workflow.todo.streamStatus
  data:
    eventType: status.complete
    sequence: 10
    timestamp: 2026-03-04T11:46:15Z
    todoId: MCP-AUTH-001
    status: ready
    blockers: []
```

### Error Events

Error events indicate stream failure:

```yaml
type: event
payload:
  event: workflow.todo.streamStatus
  data:
    eventType: status.error
    sequence: 5
    timestamp: 2026-03-04T11:45:50Z
    message: Failed to analyze dependencies
    errorCode: dependency_error
```

### Cancellation Events

Cancellation events indicate graceful stream termination:

```yaml
type: event
payload:
  event: workflow.todo.streamStatus
  data:
    eventType: status.cancelled
    sequence: 7
    timestamp: 2026-03-04T11:45:55Z
    message: Stream cancelled by user request
```

### Cancellation Guarantees

- Stream closes cleanly without partial state
- Final cancellation event is emitted
- No further events after cancellation
- Partial work is not persisted unless documented

## Identifier Rules

### Session IDs

**Format:** `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`

**Rules:**
- Agent name must be PascalCase (e.g., `Copilot`, `Cline`, `Cursor`)
- Timestamp must be ISO 8601 format: `yyyyMMddTHHmmssZ`
- Suffix must be lowercase kebab-case (e.g., `feature-auth`, `bugfix-timeout`)
- Session ID prefix must match agent name exactly (case-sensitive)

**Valid Examples:**
- `Copilot-20260304T113901Z-feature-auth`
- `Cline-20260304T120000Z-bugfix-timeout`
- `Cursor-20260304T150000Z-refactor-session`

**Invalid Examples:**
- `copilot-20260304T113901Z-feature` (lowercase agent)
- `Copilot-20260304-feature` (missing time)
- `Copilot-20260304T113901Z-Feature` (uppercase in suffix)
- `req-20260304T113901Z-feature` (wrong prefix)

### Request IDs

**Format:** `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`

**Rules:**
- Must start with `req-`
- Timestamp must be ISO 8601 format: `yyyyMMddTHHmmssZ`
- Suffix must be lowercase kebab-case or ordinal

**Valid Examples:**
- `req-20260304T113901Z-add-jwt-001`
- `req-20260304T120000Z-query-todos`
- `req-20260304T150000Z-create-fr-002`

**Invalid Examples:**
- `request-20260304T113901Z-task` (wrong prefix)
- `req-20260304-task` (missing time)
- `req-20260304T113901Z-Task` (uppercase in suffix)

### TODO IDs

**Format:** uppercase kebab-case ending in `-###` or `ISSUE-{number}`

**Rules:**
- Segments must use uppercase letters and digits
- IDs need at least one descriptive segment before the final three-digit suffix
- The final sequence suffix must be 3 digits
- Special case: `ISSUE-NEW` creates GitHub-backed TODO

**Valid Examples:**
- `MCP-AUTH-001`
- `PHASE0-REMOTE-001`
- `MCP-TODO-CREATE-001`
- `PLAN-NAMINGCONVENTIONS-001`
- `ISSUE-17`
- `ISSUE-NEW` (special case)

**Invalid Examples:**
- `mcp-auth-001` (lowercase)
- `MCP-AUTH-42` (not 3 digits)
- `MCP-001` (missing descriptive segment)
- `MCPAUTH001` (missing hyphens)

### Requirement IDs

**FR Format:** `^FR-[A-Z]+-\d{3}$`  
**TR Format:** `^TR-[A-Z]+-[A-Z]+-\d{3}$`  
**TEST Format:** `^TEST-[A-Z]+-\d{3}$`

**Valid Examples:**
- `FR-MCP-001`
- `TR-MCP-ARCH-001`
- `TEST-MCP-001`

**Invalid Examples:**
- `fr-mcp-001` (lowercase)
- `FR-MCP-1` (not 3 digits)
- `TR-MCP-001` (TR requires subarea)

## Session Continuity

### After Restart

- Call `workflow.sessionlog.queryHistory` to review past sessions
- Decide whether to resume or create new session
- If resuming, open new session and reference previous session in title
- If server restarted, read fresh marker file and re-bootstrap

### Long Sessions

Every ~10 interactions:
- Persist all turns
- Verify design decisions captured
- Check requirements docs up to date
- Update session title if scope changed

### Before Shutdown

- Complete any active turn
- Update session status to `completed`
- Persist final session state

## Agent Conduct

### Honesty

- Do not fabricate information, capabilities, or results
- Distinguish facts from opinions and speculation
- Acknowledge mistakes immediately

### Correctness

- Prioritize correctness over speed
- Log every decision to session log
- Follow DRY, SOLID, and existing project conventions
- All code must have XMLDocs; all public APIs documented

### Professional Representation

- Every interaction is audited via session log
- Every commit must be correct, clean, well-described, complete
- Log all commits as actions with type "commit"
- Log all PR/issue comments as actions with type "pr_comment" or "issue_comment"

### Source Attribution

- Document all web sources as actions with type "web_reference"
- Add source URLs to turn's contextList
- Attribute external code in both session log and code comments

## Additional Resources

- **User Guide**: `docs/REPL-USER-GUIDE.md`
- **API Documentation**: `docs/context/api-capabilities.md`
- **Session Log Schema**: `docs/context/session-log-schema.md`
- **TODO Schema**: `docs/context/todo-schema.md`
- **Module Bootstrap**: `docs/context/module-bootstrap.md`
- **Action Types**: `docs/context/action-types.md`
- **Compliance Rules**: `docs/context/compliance-rules.md`
- **Source Code**: https://github.com/SharpNinja/McpServer

## License

MIT

## Author

SharpNinja
