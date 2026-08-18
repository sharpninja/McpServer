#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-wrapup-wiki-20260818.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function W([string]$s) { $script:lines.Add($s); Write-Output $s }

Set-Location 'F:\GitHub\McpServer'
W "UTC=$(Get-Date -AsUTC -Format o)"

# Parent vs current wiki.yaml document ids
$parentIds = @(git show 298c5fde:docs/wiki.yaml | Select-String -Pattern '^\s+-\s+id:\s*(.+)$' | ForEach-Object { $_.Matches[0].Groups[1].Value.Trim() })
$currIds = @(git show bf000bb7:docs/wiki.yaml | Select-String -Pattern '^\s+-\s+id:\s*(.+)$' | ForEach-Object { $_.Matches[0].Groups[1].Value.Trim() })
W "PARENT_WIKI_DOC_COUNT=$($parentIds.Count)"
W "CURR_WIKI_DOC_COUNT=$($currIds.Count)"
W "PARENT_IDS=$($parentIds -join ',')"
W "CURR_IDS=$($currIds -join ',')"
$removed = @($parentIds | Where-Object { $currIds -notcontains $_ })
$added = @($currIds | Where-Object { $parentIds -notcontains $_ })
W "REMOVED_IDS=$($removed -join ',')"
W "ADDED_IDS=$($added -join ',')"

# File sources exist on disk
. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'
Import-McpYamlSerializer
$wikiObj = Get-Content -LiteralPath 'F:\GitHub\McpServer\docs\wiki.yaml' -Raw | ConvertFrom-Yaml
$fileMissing = [System.Collections.Generic.List[string]]::new()
$fileOk = 0
$generated = 0
foreach ($d in @($wikiObj.documents)) {
    $src = [string]$d.source
    if ($src.StartsWith('generated:')) {
        $generated++
        continue
    }
    $full = Join-Path 'F:\GitHub\McpServer' $src
    if (Test-Path -LiteralPath $full) { $fileOk++ } else { $fileMissing.Add("$($d.id)|$src") }
}
W "FILE_SOURCES_OK=$fileOk GENERATED=$generated FILE_MISSING=$($fileMissing.Count)"
foreach ($m in $fileMissing) { W "FILE_MISSING_ITEM=$m" }

# Clone GitHub wiki and list Handoff
$tmp = Join-Path $env:TEMP ('mcp-wiki-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null
Push-Location $tmp
try {
    git clone --depth 1 https://github.com/sharpninja/McpServer.wiki.git wiki 2>&1 | ForEach-Object { W "CLONE $_" }
    if (Test-Path -LiteralPath (Join-Path $tmp 'wiki')) {
        Push-Location (Join-Path $tmp 'wiki')
        W "WIKI_CLONE_HEAD=$(git rev-parse HEAD)"
        W "WIKI_CLONE_FILES"
        Get-ChildItem -Recurse -File | ForEach-Object { W $_.FullName.Substring((Get-Location).Path.Length + 1) }
        $handoff = Join-Path (Get-Location) 'Handoff-Ingestion.md'
        W "HANDOFF_EXISTS=$(Test-Path -LiteralPath $handoff)"
        if (Test-Path -LiteralPath $handoff) {
            W "HANDOFF_LEN=$((Get-Item -LiteralPath $handoff).Length)"
            W "HANDOFF_HEAD=$((Get-Content -LiteralPath $handoff -TotalCount 8) -join ' | ')"
        }
        Pop-Location
    }
} finally {
    Pop-Location
    Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

# Tracked N3fWcoY
W "TRACKED_N3"
git grep -n -I -F 'N3fWcoY' bf000bb7 -- 2>&1 | ForEach-Object { W $_ }
W "TRACKED_N3_EXIT=$LASTEXITCODE"

# Commit signature
W "COMMIT_SIG=$(git log -1 --format='%G?|%GS|%GK' bf000bb7)"
W "COMMIT_SHOW_SIG"
git log -1 --show-signature bf000bb7 2>&1 | Select-Object -First 20 | ForEach-Object { W $_ }

# todo.yaml in commit?
W "TODO_YAML_IN_COMMIT"
git show --name-only --pretty=format: bf000bb7 | Where-Object { $_ -match 'todo\.yaml|TODO\.yaml' } | ForEach-Object { W $_ }

# Python usage in wrap-up scripts? check commit for python in receipts around wrap-up
W "PYTHON_IN_WRAPUP_RECEIPT=$(Select-String -LiteralPath 'F:\GitHub\McpServer\docs\receipts\wrap-up-20260818T183800Z.md' -Pattern 'python' -SimpleMatch -Quiet)"

# TestResults timestamps
if (Test-Path 'F:\GitHub\McpServer\TestResults') {
    Get-ChildItem 'F:\GitHub\McpServer\TestResults' -Recurse -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 15 |
        ForEach-Object { W ("TESTRESULT {0:o} {1}" -f $_.LastWriteTimeUtc, $_.FullName) }
}

W "UTC_END=$(Get-Date -AsUTC -Format o)"
$lines | Set-Content -LiteralPath $out -Encoding utf8
W "WROTE=$out"
