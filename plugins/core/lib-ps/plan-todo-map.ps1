#Requires -Version 7.0

# Plan-to-TODO map (plan-todo-map.yaml in the plugin cache). The file is read and written only as
# an object through yaml-object-mutation.ps1; nothing here assembles YAML text. Requires
# Read-McpYamlObject and Write-McpYamlObject to be loaded first.

function Read-PlanTodoMapEntry {
    <#
    .SYNOPSIS
        Returns every entry of a plan-to-TODO map file as ordered maps with planFile and todoId.
    .PARAMETER MapPath
        Path of plan-todo-map.yaml. A missing or empty file yields no entries.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$MapPath)

    if (-not (Test-Path -LiteralPath $MapPath -PathType Leaf)) { return @() }
    $document = Read-McpYamlObject -Path $MapPath
    if ($document -isnot [System.Collections.IDictionary] -or -not $document.Contains('entries')) { return @() }
    $entries = @()
    foreach ($raw in @($document['entries'])) {
        if ($raw -isnot [System.Collections.IDictionary]) { continue }
        $planFile = if ($raw.Contains('planFile') -and $null -ne $raw['planFile']) { [string]$raw['planFile'] } else { '' }
        $todoId = if ($raw.Contains('todoId') -and $null -ne $raw['todoId']) { [string]$raw['todoId'] } else { '' }
        if ([string]::IsNullOrWhiteSpace($planFile)) { continue }
        $entries += [ordered]@{ planFile = $planFile; todoId = $todoId }
    }
    return $entries
}

function Get-LastPlanTodoMapEntryFromFile {
    <#
    .SYNOPSIS
        Returns the last entry of the map, or $null when the map has none.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$MapPath)

    $entries = @(Read-PlanTodoMapEntry -MapPath $MapPath)
    if ($entries.Count -eq 0) { return $null }
    $last = $entries[-1]
    return [ordered]@{ planFile = $last.planFile; todoId = $(if ($last.todoId) { $last.todoId } else { $null }) }
}

function Find-PlanTodoIdInMap {
    <#
    .SYNOPSIS
        Returns the todoId recorded for the given plan file, or $null.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$MapPath,
        [Parameter(Mandatory)][string]$PlanFile
    )

    $match = $null
    foreach ($entry in @(Read-PlanTodoMapEntry -MapPath $MapPath)) {
        if ($entry.planFile -eq $PlanFile -and $entry.todoId) { $match = $entry.todoId }
    }
    return $match
}

function Write-PlanTodoMapEntry {
    <#
    .SYNOPSIS
        Writes the map with a single entry (the hook records the most recent approved plan only).
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$MapPath,
        [Parameter(Mandatory)][string]$PlanFile,
        [Parameter(Mandatory)][string]$TodoId
    )

    $document = [ordered]@{
        entries = @(
            [ordered]@{ planFile = $PlanFile; todoId = $TodoId }
        )
    }
    Write-McpYamlObject -Path $MapPath -Document $document
    return $MapPath
}
