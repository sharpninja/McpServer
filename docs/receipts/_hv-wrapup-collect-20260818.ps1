#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-wrapup-collect-20260818.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function W([string]$s) { $script:lines.Add($s); Write-Output $s }

W "UTC=$(Get-Date -AsUTC -Format o)"
W "HOST=$env:COMPUTERNAME"

# Plugin version
$verPath = 'F:\GitHub\mcpserver-grok-plugin\.version'
$pj = 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json'
W "PLUGIN_VERSION_FILE=$((Get-Content -LiteralPath $verPath -Raw).Trim())"
$pjObj = Get-Content -LiteralPath $pj -Raw | ConvertFrom-Json
W "PLUGIN_JSON_VERSION=$($pjObj.version)"
W "PLUGIN_JSON_NAME=$($pjObj.name)"

# Marker signature
. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$marker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$sig = Test-MarkerSignature -MarkerFile $marker
W "TEST_MARKER_SIGNATURE=$sig"
$baseUrl = Get-MarkerField -MarkerFile $marker -FieldName 'baseUrl'
W "BASEURL=$baseUrl"

# Independent health + nonce
$nonce = [guid]::NewGuid().ToString('N')
W "NONCE_SENT=$nonce"
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health?nonce=$nonce" -TimeoutSec 20
    W "HEALTH_STATUS=$($health.status)"
    W "HEALTH_VERSION=$($health.version)"
    W "HEALTH_NONCE=$($health.nonce)"
    W "NONCE_MATCH=$($health.nonce -eq $nonce)"
    W "HEALTH_JSON=$($health | ConvertTo-Json -Compress -Depth 8)"
} catch {
    W "HEALTH_ERROR=$($_.Exception.Message)"
}

# Storage reachable: sessionlog list via health already; try a lightweight authenticated probe through plugin later
# ZIP hashes
function HashFile([string]$p) {
    if (-not (Test-Path -LiteralPath $p)) { return "MISSING" }
    $h = Get-FileHash -LiteralPath $p -Algorithm SHA256
    $len = (Get-Item -LiteralPath $p).Length
    return "sha256=$($h.Hash.ToLowerInvariant()) length=$len"
}
W "ZIP_REQ=$(HashFile 'F:\GitHub\McpServer\docs\requirements\requirements-wiki-documents.zip')"
W "ZIP_PROJ=$(HashFile 'F:\GitHub\McpServer\docs\Project\requirements-wiki-documents.zip')"

# ZIP entries
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = 'F:\GitHub\McpServer\docs\requirements\requirements-wiki-documents.zip'
if (Test-Path -LiteralPath $zipPath) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        W "ZIP_ENTRY_COUNT=$($zip.Entries.Count)"
        $handoff = @($zip.Entries | Where-Object { $_.FullName -match 'Handoff-Ingestion' } | ForEach-Object { $_.FullName })
        W "ZIP_HANDOFF=$($handoff -join ';')"
        $names = @($zip.Entries | ForEach-Object { $_.FullName } | Sort-Object)
        W "ZIP_ENTRIES_BEGIN"
        foreach ($n in $names) { W $n }
        W "ZIP_ENTRIES_END"
    } finally { $zip.Dispose() }
} else {
    W "ZIP_MISSING"
}

# wiki.yaml parse
$wikiPath = 'F:\GitHub\McpServer\docs\wiki.yaml'
W "WIKI_EXISTS=$(Test-Path -LiteralPath $wikiPath)"
try {
    $yamlMod = Get-Module -ListAvailable powershell-yaml | Select-Object -First 1
    if (-not $yamlMod) {
        Import-Module powershell-yaml -ErrorAction SilentlyContinue
    } else {
        Import-Module powershell-yaml -ErrorAction SilentlyContinue
    }
} catch {}

# object-first parse via plugin helper if available
$wikiText = Get-Content -LiteralPath $wikiPath -Raw
W "WIKI_SCHEMA_LINE=$((Select-String -LiteralPath $wikiPath -Pattern 'schema:' | Select-Object -First 1).Line)"
W "WIKI_DOC_COUNT_GREP=$((Select-String -LiteralPath $wikiPath -Pattern '^\s+id:' ).Count)"
W "WIKI_HAS_HANDOFF=$($wikiText -match 'handoff-ingestion')"

# Parse documents + source existence using regex-safe line walk (not YAML mutation)
$docIds = [System.Collections.Generic.List[string]]::new()
$sources = [System.Collections.Generic.List[string]]::new()
$missing = [System.Collections.Generic.List[string]]::new()
$currentId = $null
$inDocs = $false
foreach ($line in (Get-Content -LiteralPath $wikiPath)) {
    if ($line -match '^documents:') { $inDocs = $true; continue }
    if ($inDocs -and $line -match '^[A-Za-z]') { $inDocs = $false }
    if ($inDocs -and $line -match '^\s+-\s+id:\s*(.+)$') {
        $currentId = $Matches[1].Trim()
        $docIds.Add($currentId)
    }
    if ($inDocs -and $line -match '^\s+source:\s*(.+)$') {
        $src = $Matches[1].Trim()
        $sources.Add("$currentId|$src")
        $full = Join-Path 'F:\GitHub\McpServer' $src
        if (-not (Test-Path -LiteralPath $full)) { $missing.Add("$currentId|$src") }
    }
}
W "WIKI_DOC_IDS=$($docIds.Count)"
W "WIKI_IDS=$($docIds -join ',')"
W "WIKI_SOURCES=$($sources.Count)"
W "WIKI_MISSING=$($missing.Count)"
if ($missing.Count -gt 0) { foreach ($m in $missing) { W "MISSING_SOURCE=$m" } }

# git remotes / commit
Set-Location 'F:\GitHub\McpServer'
W "GIT_HEAD=$(git rev-parse HEAD)"
W "GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD)"
W "GIT_STATUS=$(git status -sb)"
W "ORIGIN_LS=$(git ls-remote origin refs/heads/develop)"
W "AZURE_LS=$(git ls-remote azure refs/heads/develop)"
W "ORIGIN_MAIN=$(git ls-remote origin refs/heads/main)"
W "AZURE_MAIN=$(git ls-remote azure refs/heads/main)"

# git remotes list
W "GIT_REMOTES"
git remote -v | ForEach-Object { W $_ }

# wiki remote
W "WIKI_REMOTE_HEAD=$(git ls-remote https://github.com/sharpninja/McpServer.wiki.git HEAD)"
W "WIKI_REMOTE_MASTER=$(git ls-remote https://github.com/sharpninja/McpServer.wiki.git refs/heads/master)"
W "WIKI_REMOTE_MAIN=$(git ls-remote https://github.com/sharpninja/McpServer.wiki.git refs/heads/main)"

# git diff --check
git diff --check
W "GIT_DIFF_CHECK_EXIT=$LASTEXITCODE"
git diff --check HEAD
W "GIT_DIFF_CHECK_HEAD_EXIT=$LASTEXITCODE"
git diff --check --cached
W "GIT_DIFF_CHECK_CACHED_EXIT=$LASTEXITCODE"

# show commit
W "COMMIT_ONELINE=$(git log -1 --format='%H %D %s')"
W "COMMIT_STAT"
git show --stat --format=fuller bf000bb7fc495b6011eb5888a8c9293c992eb305 | ForEach-Object { W $_ }

# API key literal
W "N3_GREP_BEGIN"
rg -n --hidden -S 'N3fWcoY' 'F:\GitHub\McpServer' | ForEach-Object { W $_ }
W "N3_GREP_EXIT=$LASTEXITCODE"
W "N3_GREP_END"

# Get-MarkerField usage in the two receipt scripts
foreach ($p in @(
    'F:\GitHub\McpServer\docs\receipts\_hv-h3-green-collect.ps1',
    'F:\GitHub\McpServer\docs\receipts\_hv-h3-green-collect2.ps1'
)) {
    W "SCRIPT=$p"
    if (Test-Path -LiteralPath $p) {
        $t = Get-Content -LiteralPath $p -Raw
        W "HAS_GET_MARKERFIELD=$($t -match 'Get-MarkerField')"
        W "HAS_N3=$($t -match 'N3fWcoY')"
        W "HAS_APIKEY_LITERAL=$($t -match 'apiKey\s*=\s*[''""][^''""]{10,}')"
    } else {
        W "SCRIPT_MISSING"
    }
}

# deleted wiki pages? compare wiki.yaml sources vs git history of docs wiki pages
W "WIKI_DELETED_CHECK"
git log --diff-filter=D --summary bf000bb7 -- 'docs/*.md' 'docs/**/*.md' 'wiki/*' | Select-Object -First 80 | ForEach-Object { W $_ }

W "UTC_END=$(Get-Date -AsUTC -Format o)"
$lines | Set-Content -LiteralPath $out -Encoding utf8
W "WROTE=$out"
