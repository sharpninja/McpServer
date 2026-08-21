#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f'
$plugin = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-refresh-docs-20260821T104955Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

. (Join-Path $pluginRoot 'lib\yaml-object-mutation.ps1')
Import-McpYamlSerializer

$params = [ordered]@{
    format  = 'wiki'
    docType = 'all'
}
$yaml = ConvertTo-Yaml -Data $params -Options WithIndentedSequences
$paramsPath = Join-Path $outDir 'generate-wiki-params.yaml'
[System.IO.File]::WriteAllText($paramsPath, $yaml)

Write-Output 'GENERATE_START'
$raw = & $plugin -Command Invoke -Method 'workflow.requirements.generateDocument' -ParamsPath $paramsPath -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 180
$text = if ($null -eq $raw) { '' } elseif ($raw -is [string]) { $raw } else { ($raw | Out-String) }
[System.IO.File]::WriteAllText((Join-Path $outDir 'generate-wiki-stdout.txt'), $text)
Write-Output 'GENERATE_DONE'
Write-Output $text.Substring(0, [Math]::Min(4000, $text.Length))
