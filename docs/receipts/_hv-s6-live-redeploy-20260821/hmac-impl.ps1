#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$chat = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\chat_history.jsonl'
Write-Output '===== HMACSHA256 in implementer chat after line 250 ====='
# late-file scan of last 50 lines is enough; also whole-file count
$all = Select-String -LiteralPath $chat -Pattern 'HMACSHA256'
Write-Output ('HMACSHA256_HITS=' + @($all).Count)
$all | Select-Object -Last 10 | ForEach-Object {
    $p = if ($_.Line.Length -gt 300) { $_.Line.Substring(0, 300) } else { $_.Line }
    Write-Output ($_.LineNumber.ToString() + ':' + $p)
}

Write-Output '===== Test-MarkerSignature in late chat ====='
Select-String -LiteralPath $chat -Pattern 'Test-MarkerSignature|Invoke-FullBootstrap' |
    Select-Object -Last 15 |
    ForEach-Object {
        $p = if ($_.Line.Length -gt 400) { $_.Line.Substring(0, 400) } else { $_.Line }
        Write-Output ($_.LineNumber.ToString() + ':' + $p)
    }

Write-Output '===== plugin version ====='
$verPath = 'C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\.version'
if (Test-Path -LiteralPath $verPath) { Write-Output ('PLUGIN_VERSION_FILE=' + (Get-Content -LiteralPath $verPath -Raw).Trim()) }
$pj = 'C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f\.grok-plugin\plugin.json'
if (Test-Path -LiteralPath $pj) {
    $j = Get-Content -LiteralPath $pj -Raw | ConvertFrom-Json
    Write-Output ('PLUGIN_JSON_VERSION=' + $j.version)
}

Write-Output '===== FR docs mtimes vs deploy window ====='
@(
    'F:\GitHub\McpServer\docs\Project\Functional-Requirements.md'
    'F:\GitHub\McpServer\docs\Project\Technical-Requirements.md'
    'F:\GitHub\McpServer\docs\Project\Testing-Requirements.md'
    'F:\GitHub\McpServer\docs\Project\Requirements-Matrix.md'
    'F:\GitHub\McpServer\docs\Project\TR-per-FR-Mapping.md'
    'F:\GitHub\McpServer\GitVersion.yml'
) | ForEach-Object {
    $i = Get-Item -LiteralPath $_
    Write-Output ($i.Name + ' Utc=' + $i.LastWriteTimeUtc.ToString('o'))
}

Write-Output '===== git status porcelain GitVersion and Project ====='
Set-Location 'F:\GitHub\McpServer'
git status --porcelain -- GitVersion.yml docs/Project/TODO.yaml docs/Project/Functional-Requirements.md

Write-Output 'DONE'
