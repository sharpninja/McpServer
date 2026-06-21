[CmdletBinding()]
param(
    [string]$ProjectDocsPath = "docs/Project",
    [switch]$StrictTrAndTestCoverage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-IdsFromHeadings {
    param(
        [string]$Path,
        [string]$Prefix
    )

    $idPattern = "$Prefix-[A-Z0-9]+(?:[-.–]+[A-Z0-9]+)*"

    return Get-Content $Path |
        Where-Object { $_ -match "^\#\#\s+($idPattern)\b" } |
        ForEach-Object {
            if ($_ -match "^\#\#\s+($idPattern)\b") { $matches[1] }
        }
}

function Expand-RangeToken {
    param([string]$Token)

    if ($Token -match "^([A-Z]+(?:-[A-Z0-9]+)+-)(\d{3})[–-](\d{3})$") {
        $base = $matches[1]
        $start = [int]$matches[2]
        $end = [int]$matches[3]
        if ($end -lt $start) { return @($Token) }
        return @($start..$end | ForEach-Object { "{0}{1:D3}" -f $base, $_ })
    }

    return @($Token)
}

function Get-MatrixRequirementIds {
    param([string]$Path)

    $ids = New-Object System.Collections.Generic.HashSet[string]
    $lines = Get-Content $Path | Where-Object { $_ -match '^\|\s*(FR-|TR-|TEST-)' }
    foreach ($line in $lines) {
        $req = ($line -split '\|')[1].Trim()
        [void]$ids.Add($req)
        foreach ($expanded in (Expand-RangeToken -Token $req)) {
            [void]$ids.Add($expanded)
        }
    }
    return $ids
}

$functionalPath = Join-Path $ProjectDocsPath "Functional-Requirements.md"
$technicalPath = Join-Path $ProjectDocsPath "Technical-Requirements.md"
$testingPath = Join-Path $ProjectDocsPath "Testing-Requirements.md"
$mappingPath = Join-Path $ProjectDocsPath "TR-per-FR-Mapping.md"
$matrixPath = Join-Path $ProjectDocsPath "Requirements-Matrix.md"

$frIds = Get-IdsFromHeadings -Path $functionalPath -Prefix "FR"
$trIds = Get-IdsFromHeadings -Path $technicalPath -Prefix "TR"
$testIds = Get-Content $testingPath |
    Where-Object { $_ -match '\b(TEST-[A-Z0-9]+(?:[-.–]+[A-Z0-9]+)*)\b' } |
    ForEach-Object { [regex]::Matches($_, '\b(TEST-[A-Z0-9]+(?:[-.–]+[A-Z0-9]+)*)\b') } |
    ForEach-Object { $_ } |
    ForEach-Object { $_.Groups[1].Value } |
    Select-Object -Unique

$mappingFr = Get-Content $mappingPath |
    Where-Object { $_ -match '^\|\s*FR-' } |
    ForEach-Object { ($_ -split '\|')[1].Trim() }

$matrixIds = Get-MatrixRequirementIds -Path $matrixPath

$missingFrInMapping = @($frIds | Where-Object { $_ -notin $mappingFr })
$missingFrInMatrix = @($frIds | Where-Object { -not $matrixIds.Contains($_) })
$missingTrInMatrix = @($trIds | Where-Object { -not $matrixIds.Contains($_) })
$missingTestInMatrix = @($testIds | Where-Object { -not $matrixIds.Contains($_) })

Write-Host "FR count: $($frIds.Count)" -ForegroundColor Cyan
Write-Host "TR count: $($trIds.Count)" -ForegroundColor Cyan
Write-Host "TEST count: $($testIds.Count)" -ForegroundColor Cyan

if ($missingFrInMapping.Count -gt 0) {
    Write-Warning "Missing FR in TR-per-FR-Mapping:"
    $missingFrInMapping | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}
if ($missingFrInMatrix.Count -gt 0) {
    Write-Warning "Missing FR in Requirements-Matrix:"
    $missingFrInMatrix | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}
if ($missingTrInMatrix.Count -gt 0) {
    Write-Warning "Missing TR in Requirements-Matrix:"
    $missingTrInMatrix | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}
if ($missingTestInMatrix.Count -gt 0) {
    Write-Warning "Missing TEST in Requirements-Matrix:"
    $missingTestInMatrix | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

if (
    $missingFrInMapping.Count -gt 0 -or
    $missingFrInMatrix.Count -gt 0 -or
    ($StrictTrAndTestCoverage -and $missingTrInMatrix.Count -gt 0) -or
    ($StrictTrAndTestCoverage -and $missingTestInMatrix.Count -gt 0)
) {
    throw "Traceability validation failed."
}

if ($missingTrInMatrix.Count -gt 0 -or $missingTestInMatrix.Count -gt 0) {
    Write-Host "Traceability validation passed with TR/TEST coverage warnings." -ForegroundColor Yellow
}
else {
    Write-Host "Traceability validation passed." -ForegroundColor Green
}
