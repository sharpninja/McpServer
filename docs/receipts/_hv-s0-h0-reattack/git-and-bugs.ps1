#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-s0-h0-reattack'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Set-Location -LiteralPath $workspace

function Save-Text {
    param([string]$Name, [string]$Value)
    Set-Content -LiteralPath (Join-Path $outDir $Name) -Value $Value -Encoding utf8
}

$gitStatus = & git status --short
$gitDiffNames = & git diff --name-only HEAD
$gitDiffStat = & git diff --stat HEAD
$gitUntracked = & git ls-files --others --exclude-standard
$gitWorktrees = & git worktree list
$gitLog = & git log -12 --oneline
Save-Text 'git-status.txt' (($gitStatus | Out-String).TrimEnd())
Save-Text 'git-diff-names.txt' (($gitDiffNames | Out-String).TrimEnd())
Save-Text 'git-untracked.txt' (($gitUntracked | Out-String).TrimEnd())
Save-Text 'git-diff-stat.txt' (($gitDiffStat | Out-String).TrimEnd())
Save-Text 'git-worktree-list.txt' (($gitWorktrees | Out-String).TrimEnd())
Save-Text 'git-log.txt' (($gitLog | Out-String).TrimEnd())

$srcHits = @($gitStatus | Where-Object { $_ -match '\s(src/|plugins/|tests/)' })
$untrackedSrc = @($gitUntracked | Where-Object { $_ -match '^(src/|plugins/|tests/)' })
Save-Text 'git-status-src-plugins-tests.txt' ((($srcHits + $untrackedSrc) | Out-String).TrimEnd())

$wt = Join-Path $workspace '.worktrees'
$wtExists = Test-Path -LiteralPath $wt
$wtKids = @()
if ($wtExists) { $wtKids = @(Get-ChildItem -LiteralPath $wt -Force | ForEach-Object { $_.FullName }) }
Save-Text 'worktrees-dir.txt' ("exists=$wtExists`nchildren=$($wtKids.Count)`n$($wtKids -join "`n")")

$gi = Get-Content -LiteralPath (Join-Path $workspace '.gitignore')
$giHits = @($gi | Select-String -Pattern '.worktrees' -SimpleMatch)
Save-Text 'gitignore-worktrees.txt' (($giHits | ForEach-Object { '{0}:{1}' -f $_.LineNumber, $_.Line }) -join "`n")

$areas = @('SESSIONATTR','FAILSAFE','STRICTCOUNT','XAGENT','SESSIONEND','VERIFYWRAP','TRANSCRIPT-SEARCH','TEMPVOL')
$idHits = @()
foreach ($area in $areas) {
    foreach ($kind in @('FR-MCP','TR-MCP','TEST-MCP')) {
        $id = "$kind-$area-001"
        $hits = @(& git grep -n -- "$id" -- '*.cs' '*.ps1' ':!docs/*' ':!*.md' 2>$null)
        if ($hits.Count -gt 0) {
            $idHits += "$id => $($hits -join ' | ')"
        }
    }
}
Save-Text 'product-id-hits.txt' (($idHits | Out-String).TrimEnd())

$plan = Get-Content -LiteralPath (Join-Path $workspace 'docs\plans\triage-cluster-002.md') -Raw
$expected = @(106,107,108,113,116,117,118,120,121,122,125,130,134,140,142,144,147,150,151,152,153,154,155,156,157,158,159)
$missing = @($expected | Where-Object { $plan -notmatch [regex]::Escape([string]$_) })
Save-Text 'plan-id-check.txt' ("present=$($expected.Count - $missing.Count)/$($expected.Count)`nmissing=$($missing -join ',')")

Write-Output 'GIT_STATUS_LINES=' + @($gitStatus).Count
Write-Output 'SRC_PLUGIN_TEST_DIRTY=' + $srcHits.Count
Write-Output 'UNTRACKED_SRC=' + $untrackedSrc.Count
Write-Output 'WORKTREES_DIR=' + $wtExists
Write-Output 'GITIGNORE_HITS=' + $giHits.Count
Write-Output 'PRODUCT_ID_HITS=' + $idHits.Count
Write-Output 'PLAN_MISSING=' + ($missing -join ',')
Write-Output '---STATUS---'
$gitStatus
Write-Output '---DIFF NAMES---'
$gitDiffNames
Write-Output '---UNTRACKED---'
$gitUntracked
Write-Output '---WORKTREES---'
$gitWorktrees
