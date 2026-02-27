<#
.SYNOPSIS
    Bumps the patch level of GitVersion.yml next-version.

.DESCRIPTION
    Reads GitVersion.yml, increments the patch component of next-version
    (e.g. 0.2.0 -> 0.2.1), and writes the file back. Designed to be
    dot-sourced by other scripts to avoid duplication (TR-MCP-DRY-001).

.EXAMPLE
    . .\Bump-GitVersionPatch.ps1
    $result = Bump-GitVersionPatch -RepoRoot 'E:\github\McpServer'
    # $result.OldVersion = '0.2.0', $result.NewVersion = '0.2.1'
#>

function Bump-GitVersionPatch {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $gitVersionPath = Join-Path $RepoRoot 'GitVersion.yml'
    if (-not (Test-Path $gitVersionPath)) {
        Write-Error "GitVersion.yml not found at $gitVersionPath"
    }

    $content = Get-Content -Path $gitVersionPath -Raw
    $match = [regex]::Match($content, '(?m)^next-version:\s*(\d+)\.(\d+)\.(\d+)')
    if (-not $match.Success) {
        Write-Error "Could not parse next-version from GitVersion.yml"
    }

    $major = [int]$match.Groups[1].Value
    $minor = [int]$match.Groups[2].Value
    $patch = [int]$match.Groups[3].Value
    $oldVersion = "$major.$minor.$patch"
    $newVersion = "$major.$minor.$($patch + 1)"

    $newContent = $content -replace '(?m)^(next-version:\s*)\d+\.\d+\.\d+', "`${1}$newVersion"
    Set-Content -Path $gitVersionPath -Value $newContent -NoNewline
    git -C $RepoRoot add GitVersion.yml 2>&1 | Out-Null

    return [pscustomobject]@{
        OldVersion = $oldVersion
        NewVersion = $newVersion
    }
}
