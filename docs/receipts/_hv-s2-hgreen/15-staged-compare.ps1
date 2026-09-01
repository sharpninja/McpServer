#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$main = 'F:\GitHub\McpServer'
$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen\15-staged-compare.json'

$mainStaged = Join-Path $main 'plugins\core\.staged-plugin'
$wtStaged = Join-Path $wt 'plugins\core\.staged-plugin'
$gitignore = Join-Path $wt 'plugins\core\.gitignore'

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    MainStagedExists = (Test-Path -LiteralPath $mainStaged)
    WorktreeStagedExists = (Test-Path -LiteralPath $wtStaged)
    GitignoreMentionsStaged = $false
    GitignorePreview = ''
    MainStagedCount = 0
    WorktreeGitCheckIgnore = ''
}
if (Test-Path -LiteralPath $gitignore) {
    $text = Get-Content -LiteralPath $gitignore -Raw
    $obj.GitignorePreview = $text
    $obj.GitignoreMentionsStaged = ($text -match 'staged-plugin')
}
if ($obj.MainStagedExists) {
    $obj.MainStagedCount = @(Get-ChildItem -LiteralPath $mainStaged -Recurse -File).Count
}
Push-Location $wt
try {
    $obj.WorktreeGitCheckIgnore = [string](git check-ignore -v plugins/core/.staged-plugin 2>&1)
} finally {
    Pop-Location
}
$obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} mainStaged={1} wtStaged={2}" -f $out, $obj.MainStagedExists, $obj.WorktreeStagedExists)
