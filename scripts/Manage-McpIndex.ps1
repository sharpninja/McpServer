<#
.SYNOPSIS
    Manage MCP server FTS5 and vector index operations.
.DESCRIPTION
    Provides index rebuild, integrity check, and status queries for both the
    SQLite FTS5 full-text index and the HNSW vector index. Talks to the MCP
    server REST API (default http://localhost:7147).
.PARAMETER Action
    rebuild   - Trigger full re-ingestion (rebuilds both FTS5 and vector index).
    status    - Show current sync/index status.
    integrity - Run integrity check on FTS5 index via SQLite.
.PARAMETER McpUrl
    Base URL of the MCP server (default: http://localhost:7147).
.PARAMETER DbPath
    Path to the mcp.db SQLite database (for integrity checks only).
.EXAMPLE
    .\Manage-McpIndex.ps1 -Action status
    .\Manage-McpIndex.ps1 -Action rebuild
    .\Manage-McpIndex.ps1 -Action integrity -DbPath .\mcp.db
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("rebuild", "status", "integrity")]
    [string]$Action,

    [string]$McpUrl = "http://localhost:7147",

    [string]$DbPath = "mcp.db"
)

$ErrorActionPreference = "Stop"

function Test-McpHealth {
    try {
        $resp = Invoke-RestMethod -Uri "$McpUrl/health" -Method Get -TimeoutSec 5
        return $resp.status -eq "Healthy"
    } catch {
        return $false
    }
}

switch ($Action) {
    "status" {
        if (-not (Test-McpHealth)) {
            Write-Error "MCP server not reachable at $McpUrl"
            exit 1
        }
        Write-Host "=== Sync Status ===" -ForegroundColor Cyan
        $syncStatus = Invoke-RestMethod -Uri "$McpUrl/mcpserver/sync/status" -Method Get
        $syncStatus | ConvertTo-Json -Depth 5 | Write-Host

        Write-Host "`n=== Context Sources ===" -ForegroundColor Cyan
        $sources = Invoke-RestMethod -Uri "$McpUrl/mcpserver/context/sources" -Method Get
        if ($sources.Count -gt 0) {
            $sources | ForEach-Object {
                Write-Host "  $($_.sourceType): $($_.sourceKey) (ingested: $($_.ingestedAt))"
            }
            Write-Host "  Total sources: $($sources.Count)"
        } else {
            Write-Host "  No sources indexed yet."
        }
    }
    "rebuild" {
        if (-not (Test-McpHealth)) {
            Write-Error "MCP server not reachable at $McpUrl"
            exit 1
        }
        Write-Host "Triggering full re-ingestion..." -ForegroundColor Yellow
        $result = Invoke-RestMethod -Uri "$McpUrl/mcpserver/sync/ingest" -Method Post
        Write-Host "Ingestion result:" -ForegroundColor Green
        $result | ConvertTo-Json -Depth 5 | Write-Host
    }
    "integrity" {
        if (-not (Test-Path $DbPath)) {
            Write-Error "Database not found at: $DbPath"
            exit 1
        }
        Write-Host "Running FTS5 integrity check on $DbPath..." -ForegroundColor Cyan

        # Use dotnet-script or sqlite3 if available
        $sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue
        if ($sqlite3) {
            $result = & sqlite3 $DbPath "INSERT INTO context_chunks_fts(context_chunks_fts) VALUES('integrity-check');" 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "FTS5 integrity check: PASSED" -ForegroundColor Green
            } else {
                Write-Host "FTS5 integrity check: FAILED" -ForegroundColor Red
                Write-Host $result
            }

            $count = & sqlite3 $DbPath "SELECT COUNT(*) FROM context_chunks;" 2>&1
            Write-Host "Total context chunks: $count"

            $ftsCount = & sqlite3 $DbPath "SELECT COUNT(*) FROM context_chunks_fts;" 2>&1
            Write-Host "FTS5 indexed chunks: $ftsCount"
        } else {
            Write-Host "sqlite3 not found. Install SQLite CLI tools for integrity checks." -ForegroundColor Yellow
            Write-Host "Alternative: Use the MCP /mcpserver/sync/ingest endpoint to rebuild the index."
        }
    }
}
