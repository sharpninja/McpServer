<#
.SYNOPSIS
    Bulk-ingest requirements markdown into MCP Requirements endpoints.
.DESCRIPTION
    Reads Functional/Technical/Testing/Mapping markdown files and POSTs them to
    /mcpserver/requirements/ingest, which parses and upserts FR/TR/TEST/mapping.
    You can also use -UseServerDefaults to let the server read configured files.
.PARAMETER McpUrl
    Base URL of the MCP server (default: http://localhost:7147).
.PARAMETER ApiKey
    Optional API key header value for X-Api-Key.
.PARAMETER FunctionalPath
    Path to Functional-Requirements.md.
.PARAMETER TechnicalPath
    Path to Technical-Requirements.md.
.PARAMETER TestingPath
    Path to Testing-Requirements.md.
.PARAMETER MappingPath
    Path to TR-per-FR-Mapping.md.
.PARAMETER UseServerDefaults
    If set, sends an empty ingest request and server uses configured file paths.
.EXAMPLE
    ./scripts/Ingest-McpRequirements.ps1 -UseServerDefaults
.EXAMPLE
    ./scripts/Ingest-McpRequirements.ps1 -ApiKey "<workspace-token>"
#>
[CmdletBinding()]
param(
    [string]$McpUrl = "http://localhost:7147",
    [string]$ApiKey = "",
    [string]$FunctionalPath = "docs/Project/Functional-Requirements.md",
    [string]$TechnicalPath = "docs/Project/Technical-Requirements.md",
    [string]$TestingPath = "docs/Project/Testing-Requirements.md",
    [string]$MappingPath = "docs/Project/TR-per-FR-Mapping.md",
    [switch]$UseServerDefaults
)

$ErrorActionPreference = "Stop"

function Read-RequiredFile {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "File not found: $Path"
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8
}

function Invoke-Ingest {
    param(
        [Parameter(Mandatory)]
        [string]$Url,
        [Parameter(Mandatory)]
        [hashtable]$Headers,
        [Parameter(Mandatory)]
        [object]$Payload
    )

    $json = $Payload | ConvertTo-Json -Depth 8
    return Invoke-RestMethod -Uri "$Url/mcpserver/requirements/ingest" -Method Post -Headers $Headers -ContentType "application/json" -Body $json
}

$headers = @{}
if (-not [string]::IsNullOrWhiteSpace($ApiKey)) {
    $headers["X-Api-Key"] = $ApiKey
}

try {
    if ($UseServerDefaults) {
        Write-Host "Ingesting requirements using server default file paths..." -ForegroundColor Cyan
        $result = Invoke-Ingest -Url $McpUrl -Headers $headers -Payload @{}
    }
    else {
        Write-Host "Reading markdown files and ingesting requirements..." -ForegroundColor Cyan
        $payload = @{
            functionalMarkdown = Read-RequiredFile -Path $FunctionalPath
            technicalMarkdown  = Read-RequiredFile -Path $TechnicalPath
            testingMarkdown    = Read-RequiredFile -Path $TestingPath
            mappingMarkdown    = Read-RequiredFile -Path $MappingPath
        }
        $result = Invoke-Ingest -Url $McpUrl -Headers $headers -Payload $payload
    }

    Write-Host "Ingest complete." -ForegroundColor Green
    $result | ConvertTo-Json -Depth 8
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
