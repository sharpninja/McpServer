#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p7-p10'
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
$sessionId = 'GrokSubagentHostile-20260822T020204Z-c-green-p7-p10'
$requestId = 'req-20260822T020204Z-001-hostile-c-green-p7-p10'
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

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T020756Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T020756Z.json'
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
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent re-run PluginHostProcessAdapterTests Failed 0 Passed 16 Skipped 0. Full PluginIntegration.Tests Failed 0 Passed 30 Skipped 0 Total 30. LaunchAsync returns _runner.RunAsync. DummyLaunchResultCtorCount 0. P11 Theory_EachScenario_FailsUntilAdapterOperational MatchCount 0 in tests/src. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P7/P8/P9/P10/P11 done false.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-green-P7-P10. Attacks not sustained: LaunchAsync still throws not implemented; dummy result skipping the fake; named P8-P10 tests missing or skipped; independent filter not Failed 0 Skipped 0; full project not Failed 0 Skipped 0; P11 reds mixed in; PLAN or PLUGININT marked done. Consequence: C-green-P7-P10 gate may proceed to C-red-P11 only after this AGREE; do not mark PLAN or PLUGININT done; do not require P11-P20. Alternatives rejected: DISAGREE because tests do not also assert absence of HttpClient (adapter has zero HttpClient hits and returns runner.RunAsync); DISAGREE because Copilot repo also has Invoke-CopilotMcpPlugin.ps1 (catalog declares lib/Invoke-McpPlugin.ps1); homemade HMAC false as MCP_UNTRUSTED.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-red-P7 AGREE on disk. Adapter files LastWriteTime after 2026-08-22T01:44:42Z. Plugin signature true. Health nonce 77858f1f53cf4072b553e65e24861976 echoed. Receipt docs/receipts/hostile-validator-20260822T020756Z.md OverallVerdict AGREE FailCount 0 PassCount 21.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce 77858f1f53cf4072b553e65e24861976 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42850'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran PluginHostProcessAdapterTests Failed 0 Passed 16 Skipped 0; LaunchAsync calls IPluginProcessRunner.RunAsync'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapter.cs' }
    @{ order = 4; description = 'Full PluginIntegration.Tests Failed 0 Passed 30 Skipped 0; P11 named test absent from tests/src'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapterTests.cs' }
    @{ order = 5; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P7 P8 P9 P10 P11 done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p7-p10/todo-plan.txt' }
    @{ order = 6; description = 'Wrote hostile receipt pair 20260822T020756Z OverallVerdict AGREE FailCount 0 PassCount 21'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T020756Z.md' }
    @{ order = 7; description = 'AGREE because LaunchAsync calls the fake runner, P7-P10 named tests exist without Skip, independent filter and full project Failed 0 Skipped 0, P11 reds absent, PLAN stays not done'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P7-P10 plugin Session Log adapter greens'
        actions = $actions
        designDecisions = @(
            'Score C-green-P7-P10 from independently re-run PluginHostProcessAdapterTests, live MCP store, C-red-P7 AGREE timestamps, and adapter source, not implementer narrative.'
            'AGREE: LaunchAsync returns _runner.RunAsync rather than a dummy result; named P7-P10 tests exist with no Skip; filter Failed 0 Passed 16 Skipped 0; full project Failed 0 Passed 30 Skipped 0; P11 Theory_EachScenario_FailsUntilAdapterOperational absent from tests/src; PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. Do not require P11-P20.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P7-P10','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapterTests.cs','tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapter.cs','docs/receipts/hostile-validator-20260822T014442Z.md','docs/receipts/hostile-validator-20260822T020756Z.md')
        interpretation = 'Hostile review Phase C-green-P7-P10 after C-red-P7 AGREE. Review only. Do not implement. Do not mark TODOs done. Do not require P11-P20.'
        filesModified = @('docs/receipts/hostile-validator-20260822T020756Z.md','docs/receipts/hostile-validator-20260822T020756Z.json')
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P7-P10 review AGREE. Receipt docs/receipts/hostile-validator-20260822T020756Z.md. PluginHostProcessAdapterTests Failed 0 Passed 16 Skipped 0. Full PluginIntegration.Tests Failed 0 Passed 30 Skipped 0. LaunchAsync calls IPluginProcessRunner.RunAsync. P11 reds absent. Attacks not sustained. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not require P11-P20. This AGREE is not plan closeout.'
        queryTitle = 'Hostile C-green-P7-P10 plugin Session Log adapter greens'
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
