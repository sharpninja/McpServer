BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'McpSession.psm1') -Force

    function New-TrustedSessionMarker {
        param(
            [string]$BaseUrl = 'http://marker-host:7150',
            [string]$ApiKey = 'marker-key-456',
            [string]$WorkspacePath = 'C:\workspace'
        )

        $marker = [pscustomobject]@{
            port = 7147
            baseUrl = $BaseUrl
            apiKey = $ApiKey
            workspace = 'demo'
            workspacePath = $WorkspacePath
            pid = 12345
            startedAt = '2026-03-28T16:00:00.0000000Z'
            markerWrittenAtUtc = '2026-03-28T16:00:00.0000000Z'
            serverStartedAtUtc = '2026-03-28T15:59:00.0000000Z'
            endpoints = [pscustomobject]@{
                health = '/health'
                swagger = '/swagger/v1/swagger.json'
                swaggerUi = '/swagger'
                mcpTransport = '/mcp-transport'
                sessionLog = '/mcpserver/sessionlog'
                sessionLogDialog = '/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog'
                contextSearch = '/mcpserver/context/search'
                contextPack = '/mcpserver/context/pack'
                contextSources = '/mcpserver/context/sources'
                todo = '/mcpserver/todo'
                repo = '/mcpserver/repo'
                desktop = '/mcpserver/desktop'
                gitHub = '/mcpserver/gh'
                tools = '/mcpserver/tools'
                workspace = '/mcpserver/workspace'
                serverStartupUtc = '/server-startup-utc'
                markerFileTimestamp = '/marker-file-timestamp?repoPath={workspacePath}'
            }
            signature = [pscustomobject]@{
                algorithm = 'HMAC-SHA256'
                canonicalization = 'marker-v1'
                verifier = 'workspace_api_key'
                value = ''
            }
            trust_bootstrap = [pscustomobject]@{
                description = 'Trust bootstrap'
                health_nonce_endpoint = '/health'
                health_nonce_parameter = 'nonce'
                fallback = 'If health check, nonce verification, or signature verification fails, log MCP_UNTRUSTED.'
                recommended_usage = 'Use endpoints only after verification.'
            }
            prompt = "Prompt"
        }

        $signature = InModuleScope McpSession -Parameters @{ Marker = $marker } {
            param($Marker)
            Get-McpMarkerSignatureValue -Marker $Marker
        }

        @"
port: 7147
baseUrl: $BaseUrl
apiKey: $ApiKey
endpoints:
  health: /health
  swagger: /swagger/v1/swagger.json
  swaggerUi: /swagger
  mcpTransport: /mcp-transport
  sessionLog: /mcpserver/sessionlog
  sessionLogDialog: /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog
  contextSearch: /mcpserver/context/search
  contextPack: /mcpserver/context/pack
  contextSources: /mcpserver/context/sources
  todo: /mcpserver/todo
  repo: /mcpserver/repo
  desktop: /mcpserver/desktop
  gitHub: /mcpserver/gh
  tools: /mcpserver/tools
  workspace: /mcpserver/workspace
  serverStartupUtc: /server-startup-utc
  markerFileTimestamp: /marker-file-timestamp?repoPath={workspacePath}
workspace: demo
workspacePath: $WorkspacePath
pid: 12345
startedAt: 2026-03-28T16:00:00.0000000Z
markerWrittenAtUtc: 2026-03-28T16:00:00.0000000Z
serverStartedAtUtc: 2026-03-28T15:59:00.0000000Z
signature:
  algorithm: HMAC-SHA256
  canonicalization: marker-v1
  verifier: workspace_api_key
  value: $signature
trust_bootstrap:
  description: Trust bootstrap
  health_nonce_endpoint: /health
  health_nonce_parameter: nonce
  fallback: If health check, nonce verification, or signature verification fails, log MCP_UNTRUSTED.
  recommended_usage: Use endpoints only after verification.
prompt: |-
  Prompt
"@
    }
}

Describe 'McpSession Module' {
    # Default mock — absorbs all HTTP calls
    BeforeAll {
        Mock Invoke-RestMethod {
            param($Uri)
            if ($Uri -like '*/health?nonce=*') {
                $nonce = [regex]::Match($Uri, 'nonce=([^&]+)').Groups[1].Value
                return [pscustomobject]@{ status = 'Healthy'; nonce = $nonce }
            }

            return $null
        } -ModuleName McpSession
    }

    # Reset module state between tests
    BeforeEach {
        $workspaceRoot = Join-Path $TestDrive 'workspace'
        New-Item $workspaceRoot -ItemType Directory -Force | Out-Null

        InModuleScope McpSession -Parameters @{ WorkspaceRoot = $workspaceRoot } {
            param($WorkspaceRoot)
            $script:McpWorkspacePath = $WorkspaceRoot
            $script:McpSessionAgent = $null
            $script:McpSessionModel = $null
            $script:McpSessionSlug = $null
        }

        InModuleScope McpSession {
            $script:McpBaseUrl = $null
            $script:McpApiKey  = $null
            $script:McpHeaders = @{}
        }
    }

    # ── Initialize ────────────────────────────────────────────────────────────

    Describe 'Initialize-McpSession' {
        It 'sets connection from explicit BaseUrl and ApiKey' {
            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'test-key'
            InModuleScope McpSession {
                $script:McpBaseUrl | Should -Be 'http://test:9999'
                $script:McpApiKey  | Should -Be 'test-key'
                $script:McpHeaders['X-Api-Key'] | Should -Be 'test-key'
                $script:McpHeaders['Content-Type'] | Should -Be 'application/json'
            }
        }

        It 'trims trailing slash from BaseUrl' {
            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999/' -ApiKey 'k'
            InModuleScope McpSession { $script:McpBaseUrl | Should -Be 'http://test:9999' }
        }

        It 'parses marker file for baseUrl and apiKey' {
            $marker = Join-Path $TestDrive 'AGENTS-README-FIRST.yaml'
            New-TrustedSessionMarker | Set-Content $marker

            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -MarkerPath $marker
            InModuleScope McpSession {
                $script:McpBaseUrl | Should -Be 'http://marker-host:7150'
                $script:McpApiKey  | Should -Be 'marker-key-456'
            }
        }

        It 'discovers marker by walking up from current directory' {
            $sub = Join-Path (Join-Path (Join-Path $TestDrive 'a') 'b') 'c'
            New-Item $sub -ItemType Directory -Force | Out-Null
            $marker = Join-Path $TestDrive 'AGENTS-README-FIRST.yaml'
            (New-TrustedSessionMarker -BaseUrl 'http://walk:1234' -ApiKey 'walk-key') | Set-Content $marker

            Push-Location $sub
            try {
                Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex'
                InModuleScope McpSession { $script:McpBaseUrl | Should -Be 'http://walk:1234' }
            } finally { Pop-Location }
        }

        It 'throws when marker file not found and no explicit params' {
            # Use a temp dir outside TestDrive to avoid walk-up finding markers from other tests
            $isolatedDir = Join-Path ([System.IO.Path]::GetTempPath()) ("pester-no-marker-" + [guid]::NewGuid().ToString())
            New-Item $isolatedDir -ItemType Directory -Force | Out-Null
            Push-Location $isolatedDir
            try {
                { Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' } | Should -Throw '*not found*'
            } finally {
                Pop-Location
                Remove-Item $isolatedDir -Recurse -Force
            }
        }

        It 'calls the health endpoint' {
            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k'
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Uri -like 'http://test:9999/health?nonce=*'
            }
        }

        It 'throws MCP_UNTRUSTED when marker signature verification fails' {
            $marker = Join-Path $TestDrive 'AGENTS-README-FIRST.yaml'
            @"
port: 7147
baseUrl: http://marker-host:7150
apiKey: marker-key-456
workspace: demo
workspacePath: C:\workspace
pid: 12345
startedAt: 2026-03-28T16:00:00.0000000Z
markerWrittenAtUtc: 2026-03-28T16:00:00.0000000Z
serverStartedAtUtc: 2026-03-28T15:59:00.0000000Z
signature:
  algorithm: HMAC-SHA256
  canonicalization: marker-v1
  verifier: workspace_api_key
  value: BAD
trust_bootstrap:
  description: Trust bootstrap
prompt: |-
  Prompt
"@ | Set-Content $marker

            { Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -MarkerPath $marker } | Should -Throw '*MCP_UNTRUSTED*'
        }

        It 'accepts quoted marker scalars when recomputing the signature' {
            $quotedMarker = @'
port: "7147"
baseUrl: "http://PAYTON-LEGION2:7147"
apiKey: "QYDndy2mJrmiDNK2noenYxJQOmNcq77cawFktjvo2ck"
endpoints:
  health: "/health"
  swagger: "/swagger/v1/swagger.json"
  swaggerUi: "/swagger"
  mcpTransport: "/mcp-transport"
  sessionLog: "/mcpserver/sessionlog"
  sessionLogDialog: "/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog"
  contextSearch: "/mcpserver/context/search"
  contextPack: "/mcpserver/context/pack"
  contextSources: "/mcpserver/context/sources"
  todo: "/mcpserver/todo"
  repo: "/mcpserver/repo"
  desktop: "/mcpserver/desktop"
  gitHub: "/mcpserver/gh"
  tools: "/mcpserver/tools"
  workspace: "/mcpserver/workspace"
  serverStartupUtc: "/server-startup-utc"
  markerFileTimestamp: "/marker-file-timestamp?repoPath={workspacePath}"
workspace: "TruckMate"
workspacePath: "C:\GitHub\sharpninja\TruckMate"
pid: "6444"
startedAt: "2026-04-08T20:07:33.4596237+00:00"
markerWrittenAtUtc: "2026-04-08T20:07:33.4596237+00:00"
serverStartedAtUtc: "2026-04-08T20:07:28.5450988+00:00"
signature:
  algorithm: "HMAC-SHA256"
  canonicalization: "marker-v1"
  verifier: "workspace_api_key"
  value: "ED769FDFAF0376790EB7EE498B23797393FAFFB74E033D4DD9705B62480421F8"
trust_bootstrap:
  description: "Trust bootstrap"
prompt: |
  Prompt
'@
            $marker = Join-Path $TestDrive 'AGENTS-README-FIRST.yaml'
            $quotedMarker | Set-Content $marker

            { Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -MarkerPath $marker } | Should -Not -Throw
            InModuleScope McpSession {
                $script:McpBaseUrl | Should -Be 'http://PAYTON-LEGION2:7147'
                $script:McpApiKey | Should -Be 'QYDndy2mJrmiDNK2noenYxJQOmNcq77cawFktjvo2ck'
            }
        }
    }

    # ── Assert-Initialized guard ──────────────────────────────────────────────

    Describe 'Assert-Initialized guard' {
        It 'throws on New-McpSessionLog without init' {
            { New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm' } | Should -Throw '*not initialized*'
        }

        It 'throws on Get-McpSessionLog without init' {
            { Get-McpSessionLog } | Should -Throw '*not initialized*'
        }

        It 'throws on Send-McpDialog without init' {
            $fake = [PSCustomObject]@{ sourceType = 'T'; sessionId = 's' }
            { Send-McpDialog -Session $fake -RequestId 'r' -Content 'c' } | Should -Throw '*not initialized*'
        }
    }

    # ── New-McpSessionLog ─────────────────────────────────────────────────────

    Describe 'New-McpSessionLogSlug' {
        It 'builds canonical slug from agent, timestamp, and model' {
            $timestamp = [datetime]::Parse('2026-03-07T13:58:44Z')
            $slug = New-McpSessionLogSlug -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -TimestampUtc $timestamp
            $slug | Should -Be 'Copilotcli-20260307T135844Z-gpt-5-3-codex'
        }

        It 'throws when agent is not in canonical form' {
            { New-McpSessionLogSlug -Agent 'copilot' -Model 'gpt-5.3-codex' } | Should -Throw '*must match*'
        }
    }

    Describe 'New-McpSessionLog' {
        BeforeEach {
            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'returns session with correct properties' {
            $s = New-McpSessionLog -SourceType 'TestAgent' -Title 'Test session' -Model 'gpt-4'
            $s.sourceType | Should -Be 'TestAgent'
            $s.title      | Should -Be 'Test session'
            $s.model      | Should -Be 'gpt-4'
            $s.status     | Should -Be 'in_progress'
        }

        It 'initializes empty turns list' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $s.turns.GetType().Name | Should -BeLike 'List*'
            $s.turns.Count | Should -Be 0
            $s.turns.Count | Should -Be 0
        }

        It 'auto-generates sessionId with source prefix' {
            InModuleScope McpSession {
                $script:McpSessionSlug = $null
            }

            $s = New-McpSessionLog -SourceType 'Copilot' -Title 't' -Model 'm'
            $s.sessionId | Should -Match '^Copilot-\d{8}T\d{6}Z-m$'
            $s.sessionId.Length | Should -BeGreaterThan 10
        }

        It 'uses explicit sessionId when provided' {
            $s = New-McpSessionLog -SourceType 'A' -SessionId 'my-sess-42' -Title 't' -Model 'm'
            $s.sessionId | Should -Be 'my-sess-42'
        }

        It 'sets started and lastUpdated to UTC ISO 8601' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $s.started     | Should -Match '^\d{4}-\d{2}-\d{2}T'
            $s.lastUpdated | Should -Match '^\d{4}-\d{2}-\d{2}T'
        }

        It 'pushes to server on creation' {
            New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Method -eq 'Post' -and $Uri -like '*/mcpserver/sessionlog'
            }
        }

        It 'posts canonical turns payload to server' {
            $script:capturedBody = $null
            Mock Invoke-RestMethod {
                param($Uri, $Method, $Body)
                if ($Method -eq 'Post' -and $Uri -like '*/mcpserver/sessionlog') {
                    $script:capturedBody = $Body
                }
                return $null
            } -ModuleName McpSession

            New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm' | Out-Null
            $script:capturedBody | Should -Match '"turns"'
            $script:capturedBody | Should -Not -Match '"entries"'
            $script:capturedBody | Should -Match '"turnCount"'
            $script:capturedBody | Should -Not -Match '"entryCount"'
        }

        It 'mirrors the current session object into .mcpSession/current-session.json' {
            $workspaceRoot = Join-Path $TestDrive 'workspace'
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'

            $currentSessionPath = Join-Path $workspaceRoot '.mcpSession\current-session.json'
            Test-Path $currentSessionPath | Should -BeTrue

            $persistedSession = Get-Content -LiteralPath $currentSessionPath -Raw | ConvertFrom-Json -Depth 50
            $persistedSession.sourceType | Should -Be 'T'
            $persistedSession.sessionId | Should -Be $s.sessionId
            $persistedSession.turnCount | Should -Be 0
        }
    }

    # ── Add-McpSessionTurn ───────────────────────────────────────────────────

    Describe 'Add-McpSessionTurn' {
        BeforeEach {
            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'adds turn to session turns list' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'Fix bug' -QueryText 'Fix the auth bug'
            $s.turns.Count | Should -Be 1
            $e.queryTitle    | Should -Be 'Fix bug'
            $e.queryText     | Should -Be 'Fix the auth bug'
        }

        It 'defaults status to in_progress' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q'
            $e.status | Should -Be 'in_progress'
        }

        It 'auto-generates canonical requestIds' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e1 = Add-McpSessionTurn -Session $s -QueryTitle 'First' -QueryText 'q1' -NoPush
            $e2 = Add-McpSessionTurn -Session $s -QueryTitle 'Second' -QueryText 'q2' -NoPush
            $e3 = Add-McpSessionTurn -Session $s -QueryTitle 'Third' -QueryText 'q3' -NoPush
            $e1.requestId | Should -Match '^req-\d{8}T\d{6}Z-first$'
            $e2.requestId | Should -Match '^req-\d{8}T\d{6}Z-second$'
            $e3.requestId | Should -Match '^req-\d{8}T\d{6}Z-third$'
        }

        It 'inherits model from session' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'claude-sonnet'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $e.model | Should -Be 'claude-sonnet'
        }

        It 'accepts explicit model override' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'default'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -Model 'override' -NoPush
            $e.model | Should -Be 'override'
        }

        It 'initializes empty mutable collections' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $e.designDecisions.Count        | Should -Be 0
            $e.requirementsDiscovered.Count | Should -Be 0
            $e.filesModified.Count          | Should -Be 0
            $e.blockers.Count               | Should -Be 0
            $e.actions.Count                | Should -Be 0
            $e.processingDialog.Count       | Should -Be 1
        }

        It 'adds turn locally without extra push when -NoPush is set' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $originalUpdated = $s.lastUpdated
            Add-McpSessionTurn -Session $s -QueryTitle 'NoPush test' -QueryText 'test' -NoPush
            # Turn was added to in-memory list
            $s.turns.Count | Should -Be 1
            $s.turns[0].queryTitle | Should -Be 'NoPush test'
            # lastUpdated should NOT have been bumped (Update-McpSessionLog was not called)
            $s.lastUpdated | Should -Be $originalUpdated
        }

        It 'carries the successful trust note into the first appended turn' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $turn = Add-McpSessionTurn -Session $s -QueryTitle 'Trust' -QueryText 'Trust' -NoPush
            $turn.processingDialog.Count | Should -BeGreaterThan 0
            $turn.processingDialog[0].content | Should -Match '(trusted MCP Server|established MCP connectivity)'
        }
    }

    # ── Set-McpSessionTurn ───────────────────────────────────────────────────

    Describe 'Set-McpSessionTurn' {
        BeforeEach {
            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'updates response field' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Set-McpSessionTurn -Turn $e -Session $s -Response 'All done!' -NoPush
            $e.response | Should -Be 'All done!'
        }

        It 'updates status field' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Set-McpSessionTurn -Turn $e -Session $s -Status completed -NoPush
            $e.status | Should -Be 'completed'
        }

        It 'appends to filesModified' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Set-McpSessionTurn -Turn $e -Session $s -FilesModified @('a.cs', 'b.cs') -NoPush
            Set-McpSessionTurn -Turn $e -Session $s -FilesModified @('c.cs') -NoPush
            $e.filesModified.Count | Should -Be 3
            $e.filesModified[2]    | Should -Be 'c.cs'
        }

        It 'appends to designDecisions' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Set-McpSessionTurn -Turn $e -Session $s -DesignDecisions @('Use JWT', 'Skip caching') -NoPush
            $e.designDecisions.Count | Should -Be 2
        }

        It 'pushes to server when Session is provided and NoPush is not set' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Mock Invoke-RestMethod { $null } -ModuleName McpSession
            Set-McpSessionTurn -Turn $e -Session $s -Response 'done'
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Method -eq 'Post' -and $Uri -like '*/mcpserver/sessionlog'
            }
        }
    }

    # ── Add-McpAction ─────────────────────────────────────────────────────────

    Describe 'Add-McpAction' {
        BeforeEach {
            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'adds action with auto-incrementing order' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $a1 = Add-McpAction -Turn $e -Description 'Created file' -Type create -FilePath 'new.cs'
            $a2 = Add-McpAction -Turn $e -Description 'Edited file' -Type edit -FilePath 'old.cs'
            $a3 = Add-McpAction -Turn $e -Description 'Committed' -Type commit
            $e.actions.Count | Should -Be 3
            $a1.order | Should -Be 1
            $a2.order | Should -Be 2
            $a3.order | Should -Be 3
        }

        It 'sets correct type and description' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $a = Add-McpAction -Turn $e -Description 'Deleted unused file' -Type delete -FilePath 'old.txt'
            $a.description | Should -Be 'Deleted unused file'
            $a.type        | Should -Be 'delete'
            $a.filePath    | Should -Be 'old.txt'
        }

        It 'defaults status to completed' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $a = Add-McpAction -Turn $e -Description 'test' -Type edit
            $a.status | Should -Be 'completed'
        }

        It 'accepts explicit status' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $a = Add-McpAction -Turn $e -Description 'WIP' -Type edit -Status in_progress
            $a.status | Should -Be 'in_progress'
        }

        It 'defaults filePath to empty string' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $a = Add-McpAction -Turn $e -Description 'Design choice' -Type design_decision
            $a.filePath | Should -Be ''
        }

        It 'pushes to server when Session is provided and NoPush is not set' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Mock Invoke-RestMethod { $null } -ModuleName McpSession
            Add-McpAction -Turn $e -Session $s -Description 'Tracked change' -Type edit -FilePath 'src/a.cs' | Out-Null
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Method -eq 'Post' -and $Uri -like '*/mcpserver/sessionlog'
            }
        }

        It 'normalizes persisted fixed-size action arrays before appending' {
            $workspaceRoot = Join-Path $TestDrive 'workspace'
            New-Item $workspaceRoot -ItemType Directory -Force | Out-Null

            InModuleScope McpSession -Parameters @{ WorkspaceRoot = $workspaceRoot } {
                param($WorkspaceRoot)
                $script:McpWorkspacePath = $WorkspaceRoot
            }

            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionTurn -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush

            $statePath = Join-Path $workspaceRoot '.mcpServer\session.yaml'
            $persisted = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json -Depth 50
            $persistedSession = $persisted.session
            $persistedSession.PSObject.Properties.Name | Should -Contain 'turns'
            $persistedSession.PSObject.Properties.Name | Should -Contain 'turnCount'
            $persistedSession.PSObject.Properties.Name | Should -Not -Contain 'entries'
            $persistedSession.PSObject.Properties.Name | Should -Not -Contain 'entryCount'
            $persistedTurn = $persistedSession.turns[0]

            { Add-McpAction -Turn $persistedTurn -Session $persistedSession -Description 'Recovered from persisted state' -Type edit -NoPush } | Should -Not -Throw
            $persistedTurn.actions.Count | Should -Be 1
            $persistedTurn.actions[0].description | Should -Be 'Recovered from persisted state'
        }
    }

    # ── Update-McpSessionLog ──────────────────────────────────────────────────

    Describe 'Update-McpSessionLog' {
        BeforeEach {
            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'deletes both persisted session cache files when status becomes completed' {
            $workspaceRoot = Join-Path $TestDrive 'workspace'
            $stateDir = Join-Path $workspaceRoot '.mcpServer'
            $statePath = Join-Path $stateDir 'session.yaml'
            $currentSessionDir = Join-Path $workspaceRoot '.mcpSession'
            $currentSessionPath = Join-Path $currentSessionDir 'current-session.json'
            New-Item $stateDir -ItemType Directory -Force | Out-Null
            New-Item $currentSessionDir -ItemType Directory -Force | Out-Null
            '{}' | Set-Content $statePath
            '{}' | Set-Content $currentSessionPath

            InModuleScope McpSession -Parameters @{ WorkspaceRoot = $workspaceRoot } {
                param($WorkspaceRoot)
                $script:McpWorkspacePath = $WorkspaceRoot
            }

            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            Update-McpSessionLog -Session $s -Status completed

            Test-Path $statePath | Should -BeFalse
            Test-Path $currentSessionPath | Should -BeFalse
        }

        It 'updates lastUpdated timestamp' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $before = $s.lastUpdated
            Start-Sleep -Milliseconds 100
            Update-McpSessionLog -Session $s
            $s.lastUpdated | Should -Not -Be $before
        }

        It 'updates status when provided' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            Update-McpSessionLog -Session $s -Status completed
            $s.status | Should -Be 'completed'
        }

        It 'preserves status when not provided' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            Update-McpSessionLog -Session $s
            $s.status | Should -Be 'in_progress'
        }

        It 'updates title when provided' {
            $s = New-McpSessionLog -SourceType 'T' -Title 'Old' -Model 'm'
            Update-McpSessionLog -Session $s -Title 'New Title'
            $s.title | Should -Be 'New Title'
        }

        It 'pushes to server' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            Mock Invoke-RestMethod { $null } -ModuleName McpSession
            Update-McpSessionLog -Session $s
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Method -eq 'Post' -and $Uri -like '*/mcpserver/sessionlog'
            }
        }

        It 'resolves the persisted current session object from .mcpSession when no explicit session is provided' {
            $workspaceRoot = Join-Path $TestDrive 'workspace'
            $s = New-McpSessionLog -SourceType 'Copilotcli' -Title 't' -Model 'gpt-5.3-codex'
            $legacyStatePath = Join-Path $workspaceRoot '.mcpServer\session.yaml'
            Remove-Item -LiteralPath $legacyStatePath -Force

            Update-McpSessionLog -Title 'updated from current cache'

            $currentSessionPath = Join-Path $workspaceRoot '.mcpSession\current-session.json'
            $persistedSession = Get-Content -LiteralPath $currentSessionPath -Raw | ConvertFrom-Json -Depth 50
            $persistedSession.sessionId | Should -Be $s.sessionId
            $persistedSession.title | Should -Be 'updated from current cache'
        }
    }

    Describe 'Current session cache reuse' {
        It 'reuses the current session id from .mcpSession when the legacy wrapper state file is missing' {
            $workspaceRoot = Join-Path $TestDrive 'workspace'

            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k' | Out-Null
            $s = New-McpSessionLog -SourceType 'Copilotcli' -Title 't' -Model 'gpt-5.3-codex'
            $legacyStatePath = Join-Path $workspaceRoot '.mcpServer\session.yaml'
            Remove-Item -LiteralPath $legacyStatePath -Force

            InModuleScope McpSession {
                $script:McpSessionAgent = $null
                $script:McpSessionModel = $null
                $script:McpSessionSlug = $null
            }

            $slug = Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k'
            $slug | Should -Be $s.sessionId
        }
    }

    # ── Get-McpSessionLog ─────────────────────────────────────────────────────

    Describe 'Get-McpSessionLog' {
        BeforeEach {
            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'uses default limit=5 and offset=0' {
            Get-McpSessionLog
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Uri -eq 'http://test:9999/mcpserver/sessionlog?limit=5&offset=0'
            }
        }

        It 'passes custom limit and offset' {
            Get-McpSessionLog -Limit 20 -Offset 10
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Uri -eq 'http://test:9999/mcpserver/sessionlog?limit=20&offset=10'
            }
        }
    }

    # ── Send-McpDialog ────────────────────────────────────────────────────────

    Describe 'Send-McpDialog' {
        BeforeEach {
            Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'posts to the correct dialog endpoint' {
            $s = New-McpSessionLog -SourceType 'Agent' -SessionId 'sess-42' -Title 't' -Model 'm'
            Send-McpDialog -Session $s -RequestId 'req-001' -Content 'Thinking...'
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Uri -eq 'http://test:9999/mcpserver/sessionlog/Agent/sess-42/req-001/dialog' -and
                $Method -eq 'Post'
            }
        }

        It 'defaults role to model and category to reasoning' {
            $s = New-McpSessionLog -SourceType 'A' -SessionId 's1' -Title 't' -Model 'm'
            Send-McpDialog -Session $s -RequestId 'r1' -Content 'test'
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Body -like '*"role":*"model"*' -and $Body -like '*"category":*"reasoning"*'
            }
        }

        It 'accepts custom role and category' {
            $s = New-McpSessionLog -SourceType 'A' -SessionId 's1' -Title 't' -Model 'm'
            Send-McpDialog -Session $s -RequestId 'r1' -Content 'Result' -Role tool -Category tool_result
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Body -like '*"role":*"tool"*' -and $Body -like '*"category":*"tool_result"*'
            }
        }
    }
}
