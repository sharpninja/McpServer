<#
.SYNOPSIS
    Runs a GraphRAG smoke test sequence against MCP server.

.DESCRIPTION
    Executes status -> index -> query against /mcpserver/graphrag endpoints and exits non-zero on failure.
    Intended for local verification and CI smoke checks.

.PARAMETER BaseUrl
    MCP server base URL.

.PARAMETER ApiKey
    API key used for /mcpserver routes.

.PARAMETER WorkspacePath
    Workspace path routed via X-Workspace-Path header.

.PARAMETER Query
    Query text used for the final GraphRAG query call.
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:7147",
    [string]$ApiKey = "",
    [string]$WorkspacePath = "",
    [string]$Query = "health check"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ApiKey) -or [string]::IsNullOrWhiteSpace($WorkspacePath)) {
    throw "ApiKey and WorkspacePath are required."
}

$headers = @{
    "X-Api-Key" = $ApiKey
    "X-Workspace-Path" = $WorkspacePath
    "Content-Type" = "application/json"
}

Write-Host "1/3 GraphRAG status..." -ForegroundColor Cyan
$status = Invoke-RestMethod -Method Get -Uri "$BaseUrl/mcpserver/graphrag/status" -Headers $headers
Write-Host ("  State={0}; Indexed={1}; Backend={2}" -f $status.state, $status.isIndexed, $status.backend) -ForegroundColor DarkGray

Write-Host "2/3 GraphRAG index..." -ForegroundColor Cyan
$indexBody = @{ force = $false } | ConvertTo-Json
$indexed = Invoke-RestMethod -Method Post -Uri "$BaseUrl/mcpserver/graphrag/index" -Headers $headers -Body $indexBody
if (-not $indexed.isIndexed) {
    throw ("GraphRAG index failed: {0} ({1})" -f $indexed.lastError, $indexed.failureCode)
}
Write-Host ("  Indexed at {0}" -f $indexed.lastIndexedAtUtc) -ForegroundColor DarkGray

Write-Host "3/3 GraphRAG query..." -ForegroundColor Cyan
$queryBody = @{
    query = $Query
    mode = "local"
    maxChunks = 10
    includeContextChunks = $true
} | ConvertTo-Json
$queryResult = Invoke-RestMethod -Method Post -Uri "$BaseUrl/mcpserver/graphrag/query" -Headers $headers -Body $queryBody
if ([string]::IsNullOrWhiteSpace([string]$queryResult.answer)) {
    throw "GraphRAG query returned empty answer."
}

Write-Host "GraphRAG smoke test passed." -ForegroundColor Green
