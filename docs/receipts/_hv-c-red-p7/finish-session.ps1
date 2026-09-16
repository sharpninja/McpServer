#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p7'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260822T010850Z-c-red-p7'
$requestId = 'req-20260822T010850Z-001-hostile-c-red-p7-adapter'
$now = [DateTime]::UtcNow.ToString('o')

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
        throw
    }
}

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T014442Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T014442Z.json'
$mdText = Get-Content -LiteralPath $receiptMd.FullName -Raw
$jsonObj = Get-Content -LiteralPath $receiptJson.FullName -Raw | ConvertFrom-Json
$proof = [ordered]@{
    MdExists = $true
    MdLength = $receiptMd.Length
    MdLastWriteTimeUtc = $receiptMd.LastWriteTimeUtc.ToString('o')
    MdHasAgree = [bool]($mdText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
    MdHasDisagree = [bool]($mdText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
    MdEmDash = $mdText.Contains([char]0x2014)
    MdEnDash = $mdText.Contains([char]0x2013)
    JsonExists = $true
    JsonLength = $receiptJson.Length
    JsonLastWriteTimeUtc = $receiptJson.LastWriteTimeUtc.ToString('o')
    JsonOverallVerdict = [string]$jsonObj.OverallVerdict
    JsonFailCount = $jsonObj.FailCount
    JsonPassCount = $jsonObj.PassCount
    JsonClaimCount = @($jsonObj.Claims).Count
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent re-run Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr Failed 8 Passed 0 Skipped 0. Full PluginIntegration.Tests Failed 8 Passed 14 Skipped 0 Total 22. LaunchAsync throws PluginHostProcessAdapter.LaunchAsync is not implemented for all eight host kinds. P8-P10 named tests MatchCount 0. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P7/P8/P9/P10 done false.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-red-P7. Attacks not sustained: missing named Theory; Skip on rows; LaunchAsync implemented and rows passing; P8-P10 greens mixed in; pins only in comments; trivial no-op would green; PLAN or PLUGININT marked done. Consequence: C-red-P7 gate may proceed to P8-P10 green only after this AGREE; do not mark PLAN or PLUGININT done; do not require P8-P10 green. Alternatives rejected: DISAGREE because node rows do not forbid extra pwsh flags (residual); DISAGREE because tests do not also assert absence of HttpClient (process-runner LastRequest is the AC3 process-capture pin); homemade HMAC false as MCP_UNTRUSTED.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-green-P5-P6 AGREE on disk. Adapter files LastWriteTime after 2026-08-22T01:25:09Z. Plugin signature true. Health nonce ff129a213c5344a0a2c83fe2837f1e7a echoed. Receipt docs/receipts/hostile-validator-20260822T014442Z.md OverallVerdict AGREE FailCount 0.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce ff129a213c5344a0a2c83fe2837f1e7a echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42845'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr Failed 8 Passed 0 Skipped 0; LaunchAsync throws not implemented'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapter.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P7 P8 P9 P10 done false; P8-P10 named tests absent'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p7/todo-plan.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260822T014442Z OverallVerdict AGREE FailCount 0'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T014442Z.md' }
    @{ order = 6; description = 'AGREE because eight P7 theory rows exist, fail, pin process-capture in the test body, P8-P10 are absent, PLAN stays not done'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P7 process-adapter reds'
        actions = $actions
        designDecisions = @(
            'Score C-red-P7 from independently re-run PluginHostProcessAdapterTests, live MCP store, C-green-P5-P6 AGREE timestamps, and adapter source, not implementer narrative.'
            'AGREE: named P7 Theory exists with eight InlineData host kinds and no Skip; filter Failed 8 Passed 0 Skipped 0; LaunchAsync throws not implemented; process-capture pins are in the test body so a trivial no-op would not green; P8-P10 named tests absent; PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. Do not require P8-P10 green.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P7','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapterTests.cs','tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapter.cs','docs/receipts/hostile-validator-20260822T012355Z.md','docs/receipts/hostile-validator-20260822T014442Z.md')
        interpretation = 'Hostile review Phase C-red-P7 after C-green-P5-P6 AGREE. Review only. Do not implement. Do not mark TODOs done. Do not require P8-P10 green.'
        filesModified = @('docs/receipts/hostile-validator-20260822T014442Z.md','docs/receipts/hostile-validator-20260822T014442Z.json')
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P7 review AGREE. Receipt docs/receipts/hostile-validator-20260822T014442Z.md. Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr Failed 8 Passed 0 Skipped 0. Full PluginIntegration.Tests Failed 8 Passed 14 Skipped 0 Total 22. LaunchAsync throws not implemented. Attacks not sustained. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not require P8-P10 green. This AGREE is not plan closeout.'
        queryTitle = 'Hostile C-red-P7 process-adapter reds'
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    limit = 10
} -Name 'sl-query-agent'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    todoId = 'PLAN-PLUGINHANDOFF-001'
    limit = 5
} -Name 'sl-query-todo'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    limit = 5
    offset = 0
} -Name 'sl-query-history'

$receiptMd2 = Get-Item -LiteralPath $receiptMd.FullName
$receiptJson2 = Get-Item -LiteralPath $receiptJson.FullName
[ordered]@{
    AfterQueryMdExists = $true
    AfterQueryMdLength = $receiptMd2.Length
    AfterQueryJsonExists = $true
    AfterQueryJsonLength = $receiptJson2.Length
    AfterQueryJsonVerdict = [string]((Get-Content -LiteralPath $receiptJson2.FullName -Raw | ConvertFrom-Json).OverallVerdict)
    AfterQueryJsonFailCount = ((Get-Content -LiteralPath $receiptJson2.FullName -Raw | ConvertFrom-Json).FailCount)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-after-query.json') -Encoding utf8

Write-Output 'FINISH_SESSION_DONE'
