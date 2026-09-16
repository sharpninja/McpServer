#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p7-p10'
$hits = @(Get-ChildItem -Path $out -Filter *.ps1 | Select-String -Pattern '\bpython(3)?\b|\bpy\.exe\b')
$obj = @(foreach ($h in $hits) {
    [pscustomobject]@{
        Path = $h.Path
        LineNumber = $h.LineNumber
        Line = $h.Line.Trim()
    }
})
$obj | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'no-python-hits.json') -Encoding utf8

$copilotRoot = 'F:\GitHub\mcpserver-copilot-plugin'
$copilot = [ordered]@{
    InvokeCopilotMcpPluginExists = (Test-Path -LiteralPath (Join-Path $copilotRoot 'Invoke-CopilotMcpPlugin.ps1'))
    InvokeMcpPluginExists = (Test-Path -LiteralPath (Join-Path $copilotRoot 'lib\Invoke-McpPlugin.ps1'))
    CatalogEntrypoint = 'lib/Invoke-McpPlugin.ps1'
}
$copilot | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'copilot-entrypoint-probe.json') -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.json') -Encoding utf8

Write-Output ('PYTHON_HIT_COUNT=' + $hits.Count)
foreach ($row in $obj) {
    Write-Output ($row.Path + ':' + $row.LineNumber + ':' + $row.Line)
}
Write-Output ('COPILOT_INVOKE_COPILOT=' + $copilot.InvokeCopilotMcpPluginExists)
Write-Output ('TODO_YAML_PORCELAIN=[' + $todoYamlStatus + ']')
