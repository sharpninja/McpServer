<#
.SYNOPSIS
Extracts and optionally publishes generated requirements wiki files.

.DESCRIPTION
Extracts the Azure DevOps or GitHub folder from the requirements wiki export ZIP,
adds user-documentation links to the wiki landing page after extraction, and
optionally publishes the result to a wiki Git repository.

.PARAMETER Target
The wiki format to publish. Use Azure for Azure DevOps Wiki and GitHub for
GitHub Wiki.

.PARAMETER ExportZip
Path to requirements-wiki-documents.zip.

.PARAMETER OutputPath
Destination folder for the extracted and enriched wiki files. When -Push is
used this is treated as a working root and contains staged/checkout folders.

.PARAMETER RepositoryUrl
Wiki Git repository URL used when -Push is specified.

.PARAMETER Branch
Wiki repository branch to push. GitHub and Azure wiki repos commonly use master.

.PARAMETER AuthToken
Bearer token passed to git clone and push through http.extraheader.

.PARAMETER Push
Clone, commit, and push the extracted wiki files.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Azure', 'GitHub')]
    [string]$Target,

    [string]$ExportZip = (Join-Path $PSScriptRoot '..\docs\requirements\requirements-wiki-documents.zip'),

    [string]$OutputPath = '',

    [string]$RepositoryUrl = '',

    [string]$Branch = 'master',

    [string]$AuthToken = '',

    [string]$CommitMessage = '',

    [string]$UserDocsBranch = 'main',

    [string]$GitUserName = 'mcpserver-wiki-publisher',

    [string]$GitUserEmail = 'mcpserver-wiki-publisher@users.noreply.github.com',

    [switch]$Push
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([Parameter(Mandatory)][string]$Path)

    $executionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

function Reset-Directory {
    param([Parameter(Mandatory)][string]$Path)

    $full = Resolve-FullPath $Path
    $root = [System.IO.Path]::GetPathRoot($full)
    if ([string]::IsNullOrWhiteSpace($root) -or
        [string]::Equals($full.TrimEnd('\'), $root.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset unsafe directory '$full'."
    }

    if (Test-Path -LiteralPath $full) {
        Remove-Item -LiteralPath $full -Recurse -Force
    }

    New-Item -ItemType Directory -Path $full | Out-Null
    return $full
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $Destination -Recurse -Force
    }
}

function Get-UserDocumentationLinks {
    param(
        [Parameter(Mandatory)][string]$Platform,
        [Parameter(Mandatory)][string]$DocsBranch
    )

    $docs = @(
        @{ Label = 'User Guide'; Path = 'docs/USER-GUIDE.md' },
        @{ Label = 'REPL User Guide'; Path = 'docs/REPL-USER-GUIDE.md' },
        @{ Label = 'REPL Agent Guide'; Path = 'docs/REPL-AGENT-GUIDE.md' },
        @{ Label = 'Federation Guidance'; Path = 'docs/context/federation.md' },
        @{ Label = 'Agent Plugin Availability'; Path = 'docs/AGENT-PLUGIN-AVAILABILITY.md' }
    )

    foreach ($doc in $docs) {
        $path = [string]$doc.Path
        $url = if ($Platform -eq 'GitHub') {
            "https://github.com/sharpninja/McpServer/blob/$DocsBranch/$path"
        } else {
            "https://dev.azure.com/McpServer/McpServer/_git/McpServer?path=/$path&version=GB$DocsBranch"
        }

        [pscustomobject]@{
            Label = [string]$doc.Label
            Url = $url
        }
    }
}

function Add-UserDocumentationSection {
    param(
        [Parameter(Mandatory)][string]$HomePath,
        [Parameter(Mandatory)][string]$Platform,
        [Parameter(Mandatory)][string]$DocsBranch
    )

    if (-not (Test-Path -LiteralPath $HomePath)) {
        throw "Wiki Home.md was not found at '$HomePath'."
    }

    $content = Get-Content -LiteralPath $HomePath -Raw
    if ($content -match '(?m)^## User Documentation$') {
        return
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $trimmed = $content.TrimEnd()
    if (-not [string]::IsNullOrWhiteSpace($trimmed)) {
        $lines.Add($trimmed)
        $lines.Add('')
    }

    $lines.Add('## User Documentation')
    $lines.Add('')
    foreach ($link in Get-UserDocumentationLinks -Platform $Platform -DocsBranch $DocsBranch) {
        $lines.Add("- [$($link.Label)]($($link.Url))")
    }

    $newContent = ($lines -join [Environment]::NewLine) + [Environment]::NewLine
    Set-Content -LiteralPath $HomePath -Value $newContent -Encoding utf8NoBOM
}

function Invoke-Git {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$Token = ''
    )

    $gitArgs = @()
    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $gitArgs += '-c'
        $gitArgs += "http.extraheader=AUTHORIZATION: bearer $Token"
    }

    $gitArgs += $Arguments
    & git @gitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$resolvedZip = Resolve-FullPath $ExportZip
if (-not (Test-Path -LiteralPath $resolvedZip)) {
    throw "Requirements wiki export ZIP not found: '$resolvedZip'."
}

$platform = $Target.ToLowerInvariant()
$defaultOutputRoot = Join-Path (Resolve-FullPath (Join-Path $PSScriptRoot '..')) "artifacts\requirements-wiki\$platform"
$resolvedOutputRoot = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $defaultOutputRoot
} else {
    Resolve-FullPath $OutputPath
}

$temporaryExtractRoot = if ($Push) {
    Join-Path $resolvedOutputRoot 'extract'
} else {
    Join-Path ([System.IO.Path]::GetTempPath()) "mcpserver-requirements-wiki-$([Guid]::NewGuid().ToString('N'))"
}

$extractRoot = Reset-Directory $temporaryExtractRoot
Expand-Archive -LiteralPath $resolvedZip -DestinationPath $extractRoot -Force

$sourceFolder = Join-Path $extractRoot $platform
if (-not (Test-Path -LiteralPath $sourceFolder)) {
    throw "The export ZIP does not contain a '$platform' wiki folder."
}

$stagedFolder = if ($Push) {
    Reset-Directory (Join-Path $resolvedOutputRoot 'staged')
} else {
    Reset-Directory $resolvedOutputRoot
}

Copy-DirectoryContents -Source $sourceFolder -Destination $stagedFolder
Add-UserDocumentationSection -HomePath (Join-Path $stagedFolder 'Home.md') -Platform $Target -DocsBranch $UserDocsBranch

if (-not $Push) {
    if (Test-Path -LiteralPath $extractRoot) {
        Remove-Item -LiteralPath $extractRoot -Recurse -Force
    }

    Write-Host "Prepared $Target wiki files at $stagedFolder."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($RepositoryUrl)) {
    throw 'RepositoryUrl is required when -Push is specified.'
}

$checkoutFolder = Reset-Directory (Join-Path $resolvedOutputRoot 'checkout')
Invoke-Git -Arguments @('clone', '--branch', $Branch, $RepositoryUrl, $checkoutFolder) -Token $AuthToken
Copy-DirectoryContents -Source $stagedFolder -Destination $checkoutFolder

Invoke-Git -Arguments @('-C', $checkoutFolder, 'config', 'user.name', $GitUserName)
Invoke-Git -Arguments @('-C', $checkoutFolder, 'config', 'user.email', $GitUserEmail)
Invoke-Git -Arguments @('-C', $checkoutFolder, 'add', '.')

$status = (& git -C $checkoutFolder status --porcelain) -join [Environment]::NewLine
if ([string]::IsNullOrWhiteSpace($status)) {
    Write-Host "No $Target wiki changes to publish."
    exit 0
}

$message = if ([string]::IsNullOrWhiteSpace($CommitMessage)) {
    "Sync $Target requirements wiki"
} else {
    $CommitMessage
}

Invoke-Git -Arguments @('-C', $checkoutFolder, 'commit', '-m', $message)
Invoke-Git -Arguments @('-C', $checkoutFolder, 'push', 'origin', "HEAD:$Branch") -Token $AuthToken
Write-Host "Published $Target wiki changes to $RepositoryUrl ($Branch)."
