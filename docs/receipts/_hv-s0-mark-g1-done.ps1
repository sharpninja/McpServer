#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s0-leftover-verify'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$ids = @(
    'BUG-TRIAGE-134'
    'BUG-TRIAGE-147'
    'BUG-TRIAGE-150'
    'BUG-TRIAGE-151'
    'BUG-TRIAGE-152'
    'BUG-TRIAGE-153'
    'BUG-TRIAGE-154'
    'BUG-TRIAGE-155'
    'BUG-TRIAGE-156'
    'BUG-TRIAGE-157'
)

$summary = 'Closeout AGREE docs/receipts/hostile-validator-20260819T184746Z.md. Canceled omitted planFile/todoId stamps None (STORE-006). Non-canceled first persist still rejects omitted fields. Named unit tests 36/0/0. Live canceled omit persisted None/None. Session GrokCode-20260819T183656Z-hostile-g1-closeout.'

foreach ($id in $ids) {
    $paramsPath = Join-Path $outDir ("mark-$id.yaml")
    $yaml = @"
id: $id
done: true
doneSummary: $summary
"@
    Set-Content -LiteralPath $paramsPath -Value $yaml -Encoding utf8
    Write-Output ("UPDATE " + $id)
    $raw = & $plugin -Command Invoke -Method 'workflow.todo.update' -ParamsPath $paramsPath -WorkspacePath $workspace
    $raw | Set-Content -LiteralPath (Join-Path $outDir ("mark-$id.txt")) -Encoding utf8
    if ($raw -notmatch 'done: true') {
        throw ("Failed to mark " + $id)
    }
}

Write-Output 'G1_DONE_UPDATES_OK'
