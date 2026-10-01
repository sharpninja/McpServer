#requires -Version 7.4
<#
.SYNOPSIS
    Launches the Astra planning persona on Codex CLI, read-only.

.DESCRIPTION
    Composes docs/personas/astra.md with a per-iteration planning brief and pipes the
    result to 'codex exec' as stdin, because Codex CLI has no system-prompt flag:
    persona text must be part of the prompt. The sandbox is pinned read-only so the
    planning persona cannot write to the tree even if it decides it should.

.EXAMPLE
    ./tools/powershell/Start-Astra.ps1 -Brief ./prompts/astra-plan-ITER-07.md -Iteration ITER-07

.EXAMPLE
    ./tools/powershell/Start-Astra.ps1 -Iteration ITER-07 -Interactive
#>
[CmdletBinding()]
param(
    # Planning brief for this iteration. Not required in -Interactive mode.
    [Parameter()][string]$Brief,

    # Iteration phase identifier, used to name the receipt.
    [Parameter(Mandatory)][string]$Iteration,

    # Workspace root. Defaults to the current location.
    [Parameter()][string]$Workspace = (Get-Location).Path,

    # Launch the TUI with the persona preloaded instead of a one-shot run.
    [Parameter()][switch]$Interactive,

    # Persona prompt path.
    [Parameter()][string]$Persona = './docs/personas/astra.md',

    # Skip the trust handshake. Only for a server you have already verified this session.
    [Parameter()][switch]$SkipHandshake
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'McpPersonaCommon.ps1')

if (-not $SkipHandshake) {
    $trust = Assert-McpTrust -Workspace $Workspace
    Write-Host "Trusted marker: $($trust.MarkerFile) ($($trust.BaseUrl))"
}

if (-not (Test-Path -LiteralPath $Persona)) {
    throw "Astra persona prompt not found at '$Persona'. Astra must never run without its persona."
}

$personaText = Get-Content -LiteralPath $Persona -Raw

if ($Interactive) {
    # Persona goes in as the opening instruction; the operator drives the rest.
    codex --profile astra --sandbox read-only -C $Workspace $personaText
    return
}

if (-not $Brief) { throw 'Provide -Brief for an unattended planning pass, or use -Interactive.' }
if (-not (Test-Path -LiteralPath $Brief)) { throw "Planning brief not found at '$Brief'." }

$receiptDir = Join-Path $Workspace 'docs/receipts'
New-Item -ItemType Directory -Force -Path $receiptDir | Out-Null
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$receipt = Join-Path $receiptDir "astra-plan-$Iteration-$stamp.md"

$prompt = @(
    $personaText
    ''
    '--- TASK BRIEF ---'
    ''
    (Get-Content -LiteralPath $Brief -Raw)
) -join [Environment]::NewLine

# '-' makes codex exec read the prompt from stdin, so the composed persona plus brief
# never hits a command-line length limit.
$prompt | codex exec `
    --profile astra `
    --sandbox read-only `
    -C $Workspace `
    -c model_reasoning_effort="xhigh" `
    --json `
    --output-last-message $receipt `
    -

if ($LASTEXITCODE -ne 0) { throw "Astra planning pass failed with exit code $LASTEXITCODE." }
Write-Host "Astra plan summary: $receipt"
Write-Host 'Verify in MCP: iteration phase created, TODOs complete, PlanningDecision checkpoints logged.'
