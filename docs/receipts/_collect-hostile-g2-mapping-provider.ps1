#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mapPath = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b4f-1f63-7bd2-ae2a-142fdf4e51df\mcp\call-7ce560a1-0ad0-46b9-bf1d-3ec1fc632968-100.json'
$raw = Get-Content -LiteralPath $mapPath -Raw
$idx = $raw.IndexOf('FR-MCP-TRIAGESCHEMA-001')
$snippet = if ($idx -ge 0) {
    $start = [Math]::Max(0, $idx - 40)
    $raw.Substring($start, [Math]::Min(600, $raw.Length - $start))
} else { $null }

$cfgPath = 'C:\ProgramData\McpServer\appsettings.yaml'
$cfgHits = @()
if (Test-Path -LiteralPath $cfgPath) {
    $cfg = Get-Content -LiteralPath $cfgPath
    $cfgHits = $cfg | Where-Object { $_ -match 'Provider|Database|SqlServer|Sqlite|TruckMate|Connection' }
}

$gitignore = Get-Content -LiteralPath 'F:\GitHub\McpServer\.gitignore' | Where-Object { $_ -match 'worktree' }

$update = Get-ChildItem -LiteralPath 'F:\GitHub\McpServer' -Recurse -Filter 'Update-McpService.ps1' -ErrorAction SilentlyContinue | Select-Object -First 1
$updateHits = @()
if ($update) {
    $updateHits = Select-String -LiteralPath $update.FullName -Pattern 'migrat|Migrate|schema' | ForEach-Object { $_.Line.Trim() }
}

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    MappingPresent = $idx -ge 0
    MappingSnippet = $snippet
    ProgramDataExists = Test-Path -LiteralPath $cfgPath
    ProgramDataHits = $cfgHits
    GitignoreWorktree = @($gitignore)
    UpdateMcpServicePath = if ($update) { $update.FullName } else { $null }
    UpdateMcpServiceHits = @($updateHits)
} | ConvertTo-Json -Depth 6
