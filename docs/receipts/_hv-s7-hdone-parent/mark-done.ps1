#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f'
. (Join-Path $pluginRoot 'lib\yaml-object-mutation.ps1')
Import-McpYamlSerializer
$plugin = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s7-hdone-parent\mark'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$summary = 'S7 H-done AGREE docs/receipts/hostile-validator-20260821T020957Z.md (A10 rescore; prior 20260821T020355Z DISAGREE FAIL empty). Named tests Failed 0 Skipped 0: Pester TEST-MCP-195 4, unit sanitizer+persist 42, integration S15/S16 2, planFile/todoId 30, ValidateTraceability Succeeded. Persist merged 4605eab6/0e0c5763. Sanitizer merged ee89cd63/d54f4e32. Live planFile/todoId on hosted 20db61aa. Plugin HMAC 1.97.0. HEAD ee89cd63.'

function Invoke-MarkDone {
    param(
        [Parameter(Mandatory)][string]$Id,
        [hashtable]$Extra = @{}
    )

    $obj = [ordered]@{
        id          = $Id
        done        = $true
        doneSummary = $summary
        remaining   = 'Closed after S7 H-done AGREE docs/receipts/hostile-validator-20260821T020957Z.md.'
    }
    foreach ($key in $Extra.Keys) {
        $obj[$key] = $Extra[$key]
    }

    $yaml = ConvertTo-Yaml -Data $obj -Options WithIndentedSequences
    $paramsPath = Join-Path $outDir ("mark-$Id.yaml")
    [System.IO.File]::WriteAllText($paramsPath, $yaml)
    Write-Output ("UPDATE " + $Id)
    $raw = & $plugin -Command Invoke -Method 'workflow.todo.update' -ParamsPath $paramsPath -WorkspacePath $workspace -TimeoutSeconds 120
    $text = if ($null -eq $raw) { '' } elseif ($raw -is [string]) { $raw } else { ($raw | Out-String) }
    [System.IO.File]::WriteAllText((Join-Path $outDir ("mark-$Id.txt")), $text)
    if ($text -notmatch 'done:\s*true') {
        throw ("Failed to mark " + $Id + " done:true. Output: " + $text.Substring(0, [Math]::Min(2000, $text.Length)))
    }
    Write-Output ("OK " + $Id)
}

$sessionLog001Tasks = @(
    [ordered]@{ Task = 'S0 [Planning] Persist FR-MCP-SESSIONLOGSAN-001, TR-MCP-SESSIONLOGSAN-001, TEST-MCP-SESSIONLOGSAN-001, their mapping, and TODO requirement links through the MCP plugins.'; Done = $true }
    [ordered]@{ Task = 'S1 [Red] Add SessionLogSanitizationOptionsValidatorTests'; Done = $true }
    [ordered]@{ Task = 'S2 [Green] Add SessionLogSanitizationOptions and validator'; Done = $true }
    [ordered]@{ Task = 'S3 [Red] Add SessionLogSanitizerTests'; Done = $true }
    [ordered]@{ Task = 'S4 [Red] Add DTO graph tests'; Done = $true }
    [ordered]@{ Task = 'S5 [Red] Add recursive payload tests'; Done = $true }
    [ordered]@{ Task = 'S6 [Green] Define ISessionLogSanitizer'; Done = $true }
    [ordered]@{ Task = 'S7 [Green] Implement SessionLogSanitizer'; Done = $true }
    [ordered]@{ Task = 'S8 [Green] Implement explicit DTO cloning'; Done = $true }
    [ordered]@{ Task = 'S9 [Red] Add timeout-path tests'; Done = $true }
    [ordered]@{ Task = 'S10 [Green] On regex timeout redact fail-closed'; Done = $true }
    [ordered]@{ Task = 'S11 [Red] Add SessionLogSanitizingServiceTests'; Done = $true }
    [ordered]@{ Task = 'S12 [Green] Implement SessionLogSanitizingService decorator'; Done = $true }
    [ordered]@{ Task = 'S13 [Red] Extend FederatedSessionLogService tests to prove local and remote items are both sanitized after merge'; Done = $true }
    [ordered]@{ Task = 'S14 [Green] Register SessionLogSanitizingService as the outermost HTTP/federation decorator in Program.cs and as the outermost stdio decorator in McpStdioHost.cs'; Done = $true }
    [ordered]@{ Task = 'S15 [Red] Extend SessionLogController integration tests with a raw secret fixture'; Done = $true }
    [ordered]@{ Task = 'S16 [Red] Add query-semantics cases proving a secret-containing raw record still participates in text filtering'; Done = $true }
    [ordered]@{ Task = 'S17 [Integration] Add stdio tools/list plus sessionlog query/get invocations and a federated remote fixture'; Done = $true }
    [ordered]@{ Task = 'S18 [Config/Docs] Add Mcp:SessionLogSanitization configuration examples'; Done = $true }
    [ordered]@{ Task = 'S19 [Gate] Run sanitizer/options/service tests, Support.Mcp tests, HTTP integration tests, stdio/federation scopes'; Done = $true }
)

Invoke-MarkDone -Id 'BUG-TRIAGE-160'
Invoke-MarkDone -Id 'BUG-TRIAGE-161'
Invoke-MarkDone -Id 'BUG-TRIAGE-162'
Invoke-MarkDone -Id 'BUG-TRIAGE-164'
Invoke-MarkDone -Id 'MCP-SESSIONLOG-001' -Extra @{ implementationTasks = $sessionLog001Tasks }
Invoke-MarkDone -Id 'MCP-SESSIONLOG-002'
Invoke-MarkDone -Id 'PLAN-SESSIONLOGREMEDIATE-001'

Write-Output 'ALL_MARK_DONE_OK'
