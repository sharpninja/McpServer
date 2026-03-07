# Agent Instructions: McpContext PowerShell Module

Use this file when an automated agent needs to ingest content and query MCP context with minimal setup.

## Scope

- Module: `tools/powershell/McpContext.psm1`
- Audience: AI agents and automation scripts
- Goal: deterministic initialization and repeatable context workflows

## Obtain Module From MCP Server

Preferred source:

- `$env:UserProfile\McpServer\tools\powershell\McpContext.psm1`

Import using absolute path when operating against service-hosted installs:

```powershell
Import-Module (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
```

If unavailable in user profile tools path, use repo source and copy it:

```powershell
$sourcePath = "<path-to-McpContext.psm1>"
Copy-Item $sourcePath (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
```

If endpoint reads return `path not allowed or not found`, verify `Mcp.RepoAllowlist` contains matcher-compatible prefixes:

- `tools/powershell/`
- `tools/powershell`

If `Initialize-McpContext` is not recognized after import, restore the module from repo checkout:

```powershell
$sourcePath = "<path-to-McpContext.psm1>"
Copy-Item $sourcePath (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
Remove-Module McpContext -ErrorAction SilentlyContinue
Import-Module (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
Initialize-McpContext -MarkerPath "<workspace-path>\AGENTS-README-FIRST.yaml"
```

If you need to bootstrap from the MCP Server endpoint, use the marker file for workspace-correct auth:

```powershell
# Paste and run this entire block at once.
& {
$cwdPath = (Get-Location).Path
function Find-McpMarkerUpTree {
  param([Parameter(Mandatory = $true)][string]$StartPath)

  $current = $StartPath
  while ($true) {
    $candidate = Join-Path $current "AGENTS-README-FIRST.yaml"
    if (Test-Path $candidate) { return $candidate }
    $parent = Split-Path $current -Parent
    if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) { break }
    $current = $parent
  }

  return $null
}

$markerPath = Find-McpMarkerUpTree -StartPath $cwdPath
if (-not $markerPath) {
  throw "Could not find AGENTS-README-FIRST.yaml by walking parent folders from: $cwdPath"
}

$workspacePath = Split-Path $markerPath -Parent
$destination = Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1"
New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null

# Read baseUrl + apiKey from the target workspace marker file.
$marker = Get-Content -Path $markerPath -Raw
$baseUrl = [regex]::Match($marker, '(?m)^\s*baseUrl:\s*"?(?<v>[^"\r\n]+)"?\s*$').Groups['v'].Value
$apiKey = [regex]::Match($marker, '(?m)^\s*apiKey:\s*"?(?<v>[^"\r\n]+)"?\s*$').Groups['v'].Value
$markerWorkspacePath = [regex]::Match($marker, '(?m)^\s*workspacePath:\s*"?(?<v>[^"\r\n]+)"?\s*$').Groups['v'].Value
$targetWorkspacePath = if ($markerWorkspacePath) { $markerWorkspacePath } else { $workspacePath }

if (-not $baseUrl -or -not $apiKey) {
  throw "Could not read baseUrl/apiKey from marker file: $markerPath"
}

function Import-McpContextModuleOrNull {
  param([Parameter(Mandatory = $true)][string]$Path)

  if (-not (Test-Path $Path)) { return $false }
  Import-Module $Path -Force
  return [bool](Get-Command Initialize-McpContext -ErrorAction SilentlyContinue)
}

function Find-LocalMcpContextModule {
  param([Parameter(Mandatory = $true)][string[]]$RootHints)

  foreach ($root in ($RootHints | Where-Object { $_ } | Select-Object -Unique)) {
    if (-not (Test-Path $root)) { continue }
    try {
      $match = Get-ChildItem -Path $root -Filter "McpContext.psm1" -Recurse -File -ErrorAction Stop |
        Where-Object { $_.FullName -match '[\\/]tools[\\/]powershell[\\/]McpContext\.psm1$' } |
        Select-Object -First 1
      if ($match) { return $match.FullName }
    }
    catch {
      # Ignore inaccessible paths and continue searching.
    }
  }

  return $null
}

$headers = @{
  "X-Api-Key" = $apiKey
  "X-Workspace-Path" = $targetWorkspacePath
}

$path = [System.Uri]::EscapeDataString("tools/powershell/McpContext.psm1")
$uri = "$baseUrl/mcpserver/repo/file?path=$path"

# Use Invoke-WebRequest so non-2xx responses still return parseable body text.
$response = Invoke-WebRequest -Uri $uri -Headers $headers -Method Get -SkipHttpErrorCheck
if ($response.StatusCode -eq 200) {
  $result = $response.Content | ConvertFrom-Json
  if (-not $result.content) {
    throw "Downloaded response did not contain file content."
  }

  New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
  $result.content | Set-Content -Path $destination -Encoding UTF8

  if (Import-McpContextModuleOrNull -Path $destination) {
    Initialize-McpContext -MarkerPath $markerPath
    return
  }

  throw "Module import failed after endpoint download; Initialize-McpContext was not exported from $destination"
}
else {
  Write-Warning "Endpoint download failed (HTTP $($response.StatusCode)): $($response.Content)"

  # Fallback 1: use installed module path if present.
  if (Import-McpContextModuleOrNull -Path $destination) {
    Initialize-McpContext -MarkerPath $markerPath
    return
  }

  # Fallback 2: copy from local source candidates, then import.
  $discoveredModule = Find-LocalMcpContextModule -RootHints @(
    $cwdPath,
    (Split-Path $cwdPath -Parent),
    $env:UserProfile
  )

  $sourceCandidates = @(
    (Join-Path $targetWorkspacePath "tools/powershell/McpContext.psm1"),
    (Join-Path (Split-Path $markerPath -Parent) "tools/powershell/McpContext.psm1"),
    (Join-Path $workspacePath "tools/powershell/McpContext.psm1"),
    $discoveredModule,
    $env:MCP_CONTEXT_MODULE_SOURCE
  ) | Select-Object -Unique

  foreach ($repoModule in $sourceCandidates) {
    if (-not (Test-Path $repoModule)) { continue }
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item $repoModule $destination -Force
    if (Import-McpContextModuleOrNull -Path $destination) {
      Initialize-McpContext -MarkerPath $markerPath
      return
    }
  }

  throw "Failed to download module from $uri and no local fallback module was found."
}
}
```

## Required Initialization

Always initialize first:

```powershell
Import-Module .\tools\powershell\McpContext.psm1 -Force
Initialize-McpContext
```

If working outside repo root, use explicit marker path:

```powershell
Initialize-McpContext -MarkerPath "<workspace-path>\AGENTS-README-FIRST.yaml"
```

## Standard Agent Workflow

1. Confirm connection.

```powershell
Get-McpContextConnection
```

2. Ingest local artifacts (folder path provided by task).

```powershell
Import-McpContextFolder -Path ".\artifacts" -Sync
```

3. Ingest reference URLs when required.

```powershell
Import-McpContextUrl -Url "https://example.com" -MaxPages 1 -MaxDepth 0
```

4. Query for evidence.

```powershell
Search-McpContext -Query "target topic" -Limit 20
Query-McpGraphRag -Query "answer question from context" -Mode local -MaxChunks 20
```

5. Optionally refresh GraphRAG index if task requires latest graph state.

```powershell
Invoke-McpGraphRagIndex
```

## Command Selection Guidance

- Use `Import-McpContextFolder` for local documents.
- Use `Import-McpContextUrl` for website ingestion.
- Use `Search-McpContext` for direct chunk retrieval.
- Use `Query-McpGraphRag` for synthesized answers with citations.
- Use `Invoke-McpSyncRun` when you staged files and need them indexed.
- Use `Get-McpSyncStatus` to check ingestion completion/errors.

## Safety and Reliability Notes

- Do not hardcode API keys; rely on marker file parsing.
- Do not assume REST `/mcpserver/sync/*` routes exist; this module uses MCP transport tools for sync.
- Treat sync failures as actionable status; inspect returned `error` and continue with targeted ingestion/query when possible.
- Keep folder ingestion bounded by using `-Include` when staging large trees.
- Keep URL ingestion bounded using `-MaxPages`, `-MaxDepth`, and `-MaxBytesPerPage`.

## Minimal Script Template for Agents

```powershell
Import-Module .\tools\powershell\McpContext.psm1 -Force
Initialize-McpContext

$folderResult = Import-McpContextFolder -Path ".\docs\input" -Sync
$urlResult = Import-McpContextUrl -Url "https://example.com" -MaxPages 1 -MaxDepth 0

$search = Search-McpContext -Query "release notes" -Limit 10
$answer = Query-McpGraphRag -Query "Summarize release risks" -Mode local -MaxChunks 10

[pscustomobject]@{
  Folder = $folderResult
  Url = $urlResult
  Search = $search
  GraphRag = $answer
}
```
