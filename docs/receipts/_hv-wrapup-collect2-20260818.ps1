#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-wrapup-collect2-20260818.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function W([string]$s) { $script:lines.Add($s); Write-Output $s }

W "UTC=$(Get-Date -AsUTC -Format o)"
Set-Location 'F:\GitHub\McpServer'

# Proper wiki.yaml object parse
$yamlHelper = 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'
if (Test-Path -LiteralPath $yamlHelper) {
    . $yamlHelper
    Import-McpYamlSerializer
    $wikiObj = Get-Content -LiteralPath 'F:\GitHub\McpServer\docs\wiki.yaml' -Raw | ConvertFrom-Yaml
    W "WIKI_SCHEMA=$($wikiObj.schema)"
    $docs = @($wikiObj.documents)
    W "WIKI_DOC_COUNT=$($docs.Count)"
    $missing = [System.Collections.Generic.List[string]]::new()
    foreach ($d in $docs) {
        $id = [string]$d.id
        $src = [string]$d.source
        $title = [string]$d.title
        $full = Join-Path 'F:\GitHub\McpServer' $src
        $exists = Test-Path -LiteralPath $full
        W "DOC id=$id title=$title source=$src exists=$exists"
        if (-not $exists) { $missing.Add("$id|$src") }
    }
    W "MISSING_COUNT=$($missing.Count)"
    if ($wikiObj.navigation) {
        $navJson = $wikiObj.navigation | ConvertTo-Json -Compress -Depth 8
        W "NAV=$navJson"
    }
}

# Look-before-delete: files deleted in wrap-up commit
W "COMMIT_DELETED"
git show --name-status --format= --diff-filter=D bf000bb7 | ForEach-Object { W $_ }

W "COMMIT_RENAMED"
git show --name-status --format= --diff-filter=R bf000bb7 | ForEach-Object { W $_ }

# wiki.yaml pages that exist vs committed tree
W "COMMIT_WIKI_YAML_IN_COMMIT=$(git cat-file -e bf000bb7:docs/wiki.yaml; echo $LASTEXITCODE)"

# Search for wrap-up test transcripts
$candidates = @(
    'C:\Users\kingd\AppData\Local\Temp\grok-goal-18747a5af710\implementer',
    'C:\Users\kingd\AppData\Local\Temp',
    'F:\GitHub\McpServer\docs\receipts',
    'F:\GitHub\McpServer\.nuke'
)
W "TEST_LOG_SEARCH"
Get-ChildItem -Path 'F:\GitHub\McpServer\docs\receipts' -File -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTimeUtc -gt [datetime]'2026-08-18T18:00:00Z' -and $_.Name -match 'test|trace|wrap|nuke|gendoc' } |
    Sort-Object LastWriteTimeUtc |
    ForEach-Object { W ("RECEIPT {0:o} {1} {2}" -f $_.LastWriteTimeUtc, $_.Length, $_.FullName) }

foreach ($dir in @('C:\Users\kingd\AppData\Local\Temp\grok-goal-18747a5af710\implementer')) {
    if (Test-Path -LiteralPath $dir) {
        W "IMPL_DIR=$dir"
        Get-ChildItem -LiteralPath $dir -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc |
            Select-Object -Last 40 |
            ForEach-Object { W ("IMPL {0:o} {1} {2}" -f $_.LastWriteTimeUtc, $_.Length, $_.Name) }
    } else {
        W "IMPL_DIR_MISSING=$dir"
    }
}

# Hardcoded key scan without rg
W "N3_SCAN"
$hit = 0
Get-ChildItem -Path 'F:\GitHub\McpServer' -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(\.git|bin|obj|\.nuke)\\' } |
    ForEach-Object {
        try {
            $c = [System.IO.File]::ReadAllText($_.FullName)
            if ($c.Contains('N3fWcoY')) {
                $hit++
                W "HIT=$($_.FullName)"
            }
        } catch {}
    }
W "N3_HIT_COUNT=$hit"

# collect.ps1 key usage
foreach ($p in @(
    'F:\GitHub\McpServer\docs\receipts\_hv-h3-green-collect.ps1',
    'F:\GitHub\McpServer\docs\receipts\_hv-h3-green-collect2.ps1'
)) {
    W "======= $p ======="
    Select-String -LiteralPath $p -Pattern 'Get-MarkerField|apiKey|X-Api-Key|N3fWcoY|0hAm_' | ForEach-Object { W $_.Line }
}

W "UTC_END=$(Get-Date -AsUTC -Format o)"
$lines | Set-Content -LiteralPath $out -Encoding utf8
W "WROTE=$out"
