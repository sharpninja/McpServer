<#
.SYNOPSIS
    Migrates TODO data between MCP servers (YAML-backed or SQLite-backed).
.DESCRIPTION
    Reads all TODO items from a source MCP server and upserts them into a target MCP server
    through MCP REST endpoints. This supports YAML->SQLite, SQLite->YAML, or same-provider sync.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceBaseUrl,
    [Parameter(Mandatory = $true)]
    [string]$TargetBaseUrl,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

function Invoke-McpJson {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body
    )

    if ($null -eq $Body) {
        return Invoke-RestMethod -Method $Method -Uri $Url
    }
    return Invoke-RestMethod -Method $Method -Uri $Url -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 20)
}

$sourceUrl = $SourceBaseUrl.TrimEnd('/')
$targetUrl = $TargetBaseUrl.TrimEnd('/')

Write-Host "Fetching source TODO items from $sourceUrl/mcp/todo ..."
$source = Invoke-McpJson -Method Get -Url "$sourceUrl/mcp/todo" -Body $null
$items = @($source.items)
Write-Host "Found $($items.Count) TODO items."

$created = 0
$updated = 0
$failed = 0

foreach ($item in $items) {
    $createBody = [ordered]@{
        id = $item.id
        title = $item.title
        section = $item.section
        priority = $item.priority
        estimate = $item.estimate
        description = $item.description
        technicalDetails = $item.technicalDetails
        dependsOn = $item.dependsOn
        functionalRequirements = $item.functionalRequirements
        technicalRequirements = $item.technicalRequirements
        implementationTasks = $item.implementationTasks
    }

    $updateBody = [ordered]@{
        title = $item.title
        section = $item.section
        priority = $item.priority
        done = $item.done
        estimate = $item.estimate
        description = $item.description
        technicalDetails = $item.technicalDetails
        note = $item.note
        completedDate = $item.completedDate
        doneSummary = $item.doneSummary
        remaining = $item.remaining
        dependsOn = $item.dependsOn
        functionalRequirements = $item.functionalRequirements
        technicalRequirements = $item.technicalRequirements
        implementationTasks = $item.implementationTasks
    }

    if ($WhatIf) {
        Write-Host "[WhatIf] Upsert $($item.id)"
        continue
    }

    try {
        $createResult = Invoke-McpJson -Method Post -Url "$targetUrl/mcp/todo" -Body $createBody
        if ($createResult.success -eq $true) {
            $created++
            continue
        }
    }
    catch {
        # Create may fail for existing item. Fall through to update.
    }

    try {
        $updateResult = Invoke-McpJson -Method Put -Url "$targetUrl/mcp/todo/$($item.id)" -Body $updateBody
        if ($updateResult.success -eq $true) {
            $updated++
        }
        else {
            $failed++
            Write-Warning "Update failed for $($item.id): $($updateResult.error)"
        }
    }
    catch {
        $failed++
        Write-Warning "Upsert failed for $($item.id): $($_.Exception.Message)"
    }
}

Write-Host "Migration complete. Created=$created Updated=$updated Failed=$failed"
if ($failed -gt 0) {
    exit 1
}
