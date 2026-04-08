BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'McpContext.psm1') -Force

    Mock Invoke-RestMethod {
        param($Uri)
        if ($Uri -like '*/health?nonce=*') {
            $nonce = [regex]::Match($Uri, 'nonce=([^&]+)').Groups[1].Value
            return [pscustomobject]@{ status = 'Healthy'; nonce = $nonce }
        }

        return $null
    } -ModuleName McpContext
}

Describe 'McpContext Module' {
    BeforeEach {
        InModuleScope McpContext {
            $script:McpBaseUrl = $null
            $script:McpApiKey = $null
            $script:McpWorkspacePath = $null
            $script:McpHeaders = @{}
            $script:McpTransportUrl = $null
        }
    }

    Describe 'Initialize-McpContext' {
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

            $result = Initialize-McpContext -MarkerPath $marker
            $result.BaseUrl | Should -Be 'http://PAYTON-LEGION2:7147'
            $result.WorkspacePath | Should -Be 'C:\GitHub\sharpninja\TruckMate'
            $result.TransportUrl | Should -Be 'http://PAYTON-LEGION2:7147/mcp-transport'
        }
    }
}
