#Requires -Version 7.0
<#
.SYNOPSIS
    Generates the TEST-MCP-PLUGIN-PSONLY-001 Bats-to-Pester parity matrix.
.DESCRIPTION
    Inventories every current plugins/core/test-fixtures/*.bats scenario and
    writes deterministic JSON and Markdown parity artifacts. The generated
    Pester IDs are stable for a given Bats file and test order, giving the
    migration a complete checklist before the Bats files are quarantined.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath,
    [string]$OutputDirectory = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-Slug {
    param([Parameter(Mandatory)][string]$Value)

    $slug = $Value.ToLowerInvariant() -replace '[^a-z0-9]+', '-'
    $slug = $slug.Trim('-')
    if (-not $slug) { return 'scenario' }
    if ($slug.Length -gt 80) { return $slug.Substring(0, 80).Trim('-') }
    return $slug
}

$fixtureRoot = Join-Path $RepoRoot 'plugins\core\test-fixtures\legacy-bats'
if (-not (Test-Path -LiteralPath $fixtureRoot)) {
    $fixtureRoot = Join-Path $RepoRoot 'plugins\core\test-fixtures'
}
$rows = [System.Collections.Generic.List[object]]::new()

foreach ($file in Get-ChildItem -LiteralPath $fixtureRoot -Filter '*.bats' -File | Sort-Object Name) {
    $lines = [System.IO.File]::ReadAllLines($file.FullName)
    $ordinal = 0
    for ($index = 0; $index -lt $lines.Length; $index++) {
        if ($lines[$index] -match '^@test\s+"(?<name>.*)"\s+\{') {
            $ordinal++
            $stem = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
            $pesterId = ('PSONLY-{0}-{1:D3}' -f @(($stem -replace '[^A-Za-z0-9]+', '-').ToUpperInvariant(), $ordinal))
            $rows.Add([pscustomobject]@{
                testRequirement = 'TEST-MCP-PLUGIN-PSONLY-001'
                batsFile = ([System.IO.Path]::GetRelativePath($RepoRoot, $file.FullName).Replace('\', '/'))
                batsLine = $index + 1
                batsName = $matches['name']
                pesterFile = 'plugins/core/test-fixtures/pester/PluginBatsParity.Tests.ps1'
                pesterId = $pesterId
                pesterName = ('TEST-MCP-PLUGIN-PSONLY-001 {0} {1:D3} {2}' -f @($stem, $ordinal, (ConvertTo-Slug $matches['name'])))
            })
        }
    }
}

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    [void][System.IO.Directory]::CreateDirectory($OutputDirectory)
}

$jsonPath = Join-Path $OutputDirectory 'bats-pester-parity.generated.json'
$markdownPath = Join-Path $OutputDirectory 'BATS-PESTER-PARITY.md'

$rows | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$markdown = [System.Collections.Generic.List[string]]::new()
$markdown.Add('# Bats to Pester Parity Matrix')
$markdown.Add('')
$markdown.Add('Traceability: TEST-MCP-PLUGIN-PSONLY-001')
$markdown.Add('')
$markdown.Add('This generated matrix maps every current Bats scenario to a Pester parity ID before Bash/Bats surfaces are removed.')
$markdown.Add('')
$markdown.Add('| Bats file | Line | Bats scenario | Pester ID | Pester test name |')
$markdown.Add('| --- | ---: | --- | --- | --- |')
foreach ($row in $rows) {
    $scenario = $row.batsName.Replace('|', '\|')
    $pesterName = $row.pesterName.Replace('|', '\|')
    $markdown.Add(('| {0} | {1} | {2} | {3} | {4} |' -f @($row.batsFile, $row.batsLine, $scenario, $row.pesterId, $pesterName)))
}
$markdown | Set-Content -LiteralPath $markdownPath -Encoding UTF8

[pscustomobject]@{
    JsonPath = $jsonPath
    MarkdownPath = $markdownPath
    Count = $rows.Count
}
