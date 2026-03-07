# McpContext PowerShell Module User Guide

This guide covers day-to-day usage of `tools/powershell/McpContext.psm1`.

## Module Location

- Module path: `tools/powershell/McpContext.psm1`
- This matches the existing MCP PowerShell module pattern used by `tools/powershell/McpSession.psm1` and `tools/powershell/McpTodo.psm1`.

## Getting The Module From MCP Server

Store the module in your user profile tools path:

- `$env:UserProfile\McpServer\tools\powershell\McpContext.psm1`

Import directly from server install path:

```powershell
Import-Module (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
Initialize-McpContext
```

You can also fetch the module over the MCP Server HTTP endpoint.

### Load Module From MCP Server URL

Endpoint used:

- `GET /mcpserver/repo/file?path=tools/powershell/McpContext.psm1`

PowerShell example (download then import):

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

Companion docs via endpoint:

```powershell
$docs = @(
	"tools/powershell/McpContext.USER.md",
	"tools/powershell/McpContext.AGENT.md"
)

foreach ($doc in $docs) {
	$encoded = [System.Uri]::EscapeDataString($doc)
	$response = Invoke-RestMethod -Uri "$baseUrl/mcpserver/repo/file?path=$encoded" -Headers $headers -Method Get
	$dest = Join-Path (Join-Path $env:UserProfile "McpServer") ($doc -replace '/', '\\')
	New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force | Out-Null
	$response.content | Set-Content -Path $dest -Encoding UTF8
}
```

If endpoint retrieval returns `path not allowed or not found`, ensure `Mcp.RepoAllowlist` includes:

- `tools/powershell/`
- `tools/powershell`

Notes:

- The current server matcher is prefix-based for most patterns.
- Keep glob patterns if you want, but include the prefix entries above to guarantee match.

If the file is not present yet in your user profile tools folder, copy it from the repo checkout:

```powershell
$sourcePath = "<path-to-McpContext.psm1>"
Copy-Item $sourcePath (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
```

You can do the same for companion docs:

- `McpContext.USER.md`
- `McpContext.AGENT.md`

## What It Does

`McpContext.psm1` provides a simple workflow for:

- Ingesting local folders of docs into MCP context (via `docs/external` + sync)
- Ingesting URLs directly through the website ingestion endpoint
- Running context and GraphRAG queries
- Running sync/status and GraphRAG status/index operations

## Quick Start

```powershell
Import-Module .\tools\powershell\McpContext.psm1 -Force
Initialize-McpContext
Get-McpContextConnection
```

`Initialize-McpContext` auto-loads `baseUrl`, `apiKey`, and `workspacePath` from `AGENTS-README-FIRST.yaml` found in the current directory tree.

## Connection Options

### Auto-discovery (recommended)

```powershell
Initialize-McpContext
```

### Explicit marker path

```powershell
Initialize-McpContext -MarkerPath "<workspace-path>\AGENTS-README-FIRST.yaml"
```

### Explicit values (advanced)

```powershell
Initialize-McpContext -BaseUrl "http://localhost:7147" -ApiKey "<api-key>" -WorkspacePath "<workspace-path>"
```

## Ingest Local Folder Documents

### Stage and sync in one command

```powershell
Import-McpContextFolder -Path ".\my-docs" -Sync
```

### Stage only (no sync)

```powershell
Import-McpContextFolder -Path ".\my-docs"
```

### Stage only selected file types

```powershell
Import-McpContextFolder -Path ".\my-docs" -Include "*.md","*.txt"
```

### Stage to a fixed destination folder

```powershell
Import-McpContextFolder -Path ".\my-docs" -DestinationSubfolder "release-20260307"
```

## Ingest URLs

### Single page only

```powershell
Import-McpContextUrl -Url "https://example.com" -MaxPages 1 -MaxDepth 0
```

### Bounded crawl + GraphRAG index trigger

```powershell
Import-McpContextUrl -Url "https://docs.example.com" -IncludeSubpages -MaxPages 25 -MaxDepth 2 -TriggerGraphRagIndex
```

### Disable live stream fallback (legacy response mode)

```powershell
Import-McpContextUrl -Url "https://docs.example.com" -IncludeSubpages -MaxPages 25 -MaxDepth 2 -TriggerGraphRagIndex -NoStream
```

Note:

- `MaxPages` lower-bound validation is `>= 1`.
- Effective maximum pages is capped by `Mcp:MaxWebsitePages` from server config.
- If `MaxPages` is greater than configured `Mcp:MaxWebsitePages`, ingestion uses the configured cap.
- `Import-McpContextUrl` uses SSE (`/mcpserver/context/ingest-website/stream`) by default and prints progress in real time.

## Querying

### Context search

```powershell
Search-McpContext -Query "oauth refresh token" -Limit 10
```

### Context search with source filter

```powershell
Search-McpContext -Query "requirements mapping" -SourceType "repo" -Limit 15
```

### GraphRAG query

```powershell
Query-McpGraphRag -Query "Summarize recent auth changes" -Mode local -MaxChunks 8
```

### GraphRAG query without chunk payloads

```powershell
Query-McpGraphRag -Query "What are the key entities?" -NoContextChunks
```

## Sync and GraphRAG Operations

```powershell
Get-McpSyncStatus
Invoke-McpSyncRun

Get-McpGraphRagStatus
Invoke-McpGraphRagIndex
Invoke-McpGraphRagIndex -Force
```

## Troubleshooting

### Module not initialized

If you see `Module not initialized. Run Initialize-McpContext first.`:

1. Run `Initialize-McpContext`.
2. Confirm `AGENTS-README-FIRST.yaml` is available in current path or parent path.
3. Use `Initialize-McpContext -MarkerPath <path>` when needed.

### Marker not found

If marker auto-discovery fails, provide an explicit marker path:

```powershell
Initialize-McpContext -MarkerPath "<workspace-path>\AGENTS-README-FIRST.yaml"
```

### Sync failures

`Invoke-McpSyncRun` surfaces server-side sync errors (for example data conflicts). Check:

- `Get-McpSyncStatus`
- server logs
- whether previously ingested docs are causing duplicate key conflicts

### Invalid or missing API key (repo endpoint)

If `Invoke-RestMethod` returns:

- `Invalid or missing API key ... include it as the X-Api-Key header`

then your token and workspace do not match. Common cause:

- token came from `GET /api-key` default workspace
- `X-Workspace-Path` points to a different workspace

Use the workspace marker file (`AGENTS-README-FIRST.yaml`) for the same workspace path you pass in `X-Workspace-Path`.

### Initialize-McpContext not recognized after Import-Module

If `Import-Module (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1")` succeeds but `Initialize-McpContext` is not found, the module file may be empty/corrupt.

Recover with:

```powershell
$sourcePath = "<path-to-McpContext.psm1>"
Copy-Item $sourcePath (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
Remove-Module McpContext -ErrorAction SilentlyContinue
Import-Module (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
Initialize-McpContext -MarkerPath "<workspace-path>\AGENTS-README-FIRST.yaml"
```

If the error body is `path not allowed or not found`, use one of these fallbacks:

1. Import directly from user profile path:

```powershell
Import-Module (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
Initialize-McpContext -MarkerPath "<workspace-path>\AGENTS-README-FIRST.yaml"
```

2. Copy from repo checkout into user profile tools path:

```powershell
$sourcePath = "<path-to-McpContext.psm1>"
Copy-Item $sourcePath (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
Import-Module (Join-Path $env:UserProfile "McpServer\tools\powershell\McpContext.psm1") -Force
Initialize-McpContext -MarkerPath "<workspace-path>\AGENTS-README-FIRST.yaml"
```

## Discoverability

For function-level help:

```powershell
Get-Help Initialize-McpContext -Detailed
Get-Help Import-McpContextFolder -Detailed
Get-Help Import-McpContextUrl -Detailed
Get-Help Query-McpGraphRag -Detailed
```
