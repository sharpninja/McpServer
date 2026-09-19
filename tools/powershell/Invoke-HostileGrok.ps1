#requires -Version 7.4
<#
.SYNOPSIS
    Spawns the adversarial Grok hostile validator for a slice.

.DESCRIPTION
    Implements the invocation half of the hostile validation contract in
    docs/McpServer-UseCase-Extension-Design-v3.0.md section 6.1. The validator runs as a
    separate Grok spawn, loads docs/personas/grok.md as session rules so the adversarial
    stance survives the whole run, works in its own git worktree so it cannot disturb the
    implementer's tree, and is confined to plan permission mode so it cannot repair what
    it finds.

    This script is a launcher. It is not evidence, and its exit code is not a verdict.
    The verdict is OverallVerdict in the receipt the validator writes.

.EXAMPLE
    ./tools/powershell/Invoke-HostileGrok.ps1 -TodoId MCP-TODOPROGRESSION-001 `
        -ImplementerReceipt ./docs/receipts/sol-MCP-TODOPROGRESSION-001-20260918T203344Z.md
#>
[CmdletBinding()]
param(
    # Canonical TODO id under validation.
    [Parameter(Mandatory)][string]$TodoId,

    # Path to Sol's receipt. Passed in as untrusted input, never as authority.
    [Parameter(Mandatory)][string]$ImplementerReceipt,

    # Workspace root.
    [Parameter()][string]$Workspace = (Get-Location).Path,

    # Running server base URL for live checks. Defaults to the value in the verified marker.
    [Parameter()][string]$LiveBase,

    # Optional claim brief. Generated from the TODO id when omitted.
    [Parameter()][string]$Brief,

    # Persona prompt path, loaded as Grok session rules.
    [Parameter()][string]$Persona = './docs/personas/grok.md',

    # Turn ceiling for the validation run.
    [Parameter()][int]$MaxTurns = 60,

    # Skip the trust handshake. Only for a server you have already verified this session.
    [Parameter()][switch]$SkipHandshake
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'McpPersonaCommon.ps1')

if (-not $SkipHandshake) {
    $trust = Assert-McpTrust -Workspace $Workspace
    Write-Host "Trusted marker: $($trust.MarkerFile) ($($trust.BaseUrl))"
    # Prefer the signed marker's baseUrl over a hardcoded default for live checks.
    if (-not $LiveBase) { $LiveBase = $trust.BaseUrl }
}
if (-not $LiveBase) { $LiveBase = 'http://localhost:7147' }

if (-not (Test-Path -LiteralPath $Persona)) {
    throw "Hostile validator persona not found at '$Persona'. Refusing to run a validator without its stance."
}
if (-not (Test-Path -LiteralPath $ImplementerReceipt)) {
    throw "Implementer receipt not found at '$ImplementerReceipt'."
}

$rules = Get-Content -LiteralPath $Persona -Raw
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$receiptBase = "docs/receipts/hostile-validator-$stamp"

$task = if ($Brief -and (Test-Path -LiteralPath $Brief)) {
    Get-Content -LiteralPath $Brief -Raw
}
else {
    @(
        "Hostile validation of $TodoId."
        ''
        "Untrusted implementer receipt: $ImplementerReceipt"
        "Live base: $LiveBase"
        ''
        'Enumerate every completion claim in that receipt and in the TODO doneSummary.'
        'Start each claim at FAIL or UNKNOWN. Re-verify each one yourself with tools:'
        're-run ./build.ps1 Test and confirm zero failures and zero skips, re-run'
        './build.ps1 ValidateConfig and ./build.ps1 ValidateTraceability, confirm every'
        'claimed requirement id exists in docs/Project and in the requirements store, and'
        'make live checks against the running server for any behavioral claim.'
        ''
        "Write both receipts: $receiptBase.md and $receiptBase.json."
        'Set ValidatorIdentity to GrokSubagentHostile. OverallVerdict is AGREE only if'
        'every claim is PASS, otherwise DISAGREE. Do not fix anything you find.'
    ) -join [Environment]::NewLine
}

# --effort high is the strongest level accepted; the server strategy records that 'max' is rejected.
grok -p $task `
    --rules $rules `
    --cwd $Workspace `
    --permission-mode plan `
    --output-format plain `
    --effort high `
    --reasoning-effort high `
    --max-turns $MaxTurns `
    -w "hostile-$TodoId"

$exit = $LASTEXITCODE

$json = Join-Path $Workspace "$receiptBase.json"
if (-not (Test-Path -LiteralPath $json)) {
    throw "No hostile receipt at '$json'. A missing receipt is not an AGREE; treat the slice as unvalidated."
}

$verdict = (Get-Content -LiteralPath $json -Raw | ConvertFrom-Json).OverallVerdict
Write-Host "Hostile receipt: $json"
Write-Host "OverallVerdict: $verdict (launcher exit code $exit, which is not a verdict)"

if ($verdict -ne 'AGREE') {
    Write-Host 'DISAGREE is the process working. Route the FailList back to Sol for code defects'
    Write-Host 'or to Astra for requirement defects. Do not mark the TODO Complete.'
    exit 1
}
Write-Host "AGREE. The coordinator may now move $TodoId to Complete."
