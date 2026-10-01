#requires -Version 7.4
<#
.SYNOPSIS
    Launches the Sol coding persona on Codex CLI with workspace-write access.

.DESCRIPTION
    Composes docs/personas/sol.md with a per-TODO slice brief and pipes the result to
    'codex exec' as stdin. Sol is the only persona granted workspace-write. The run
    writes an untrusted implementer receipt that the hostile validator will attack;
    it does not mark anything complete.

.EXAMPLE
    ./tools/powershell/Start-Sol.ps1 -TodoId MCP-TODOPROGRESSION-001 -Brief ./prompts/sol-slice-MCP-TODOPROGRESSION-001.md

.EXAMPLE
    ./tools/powershell/Start-Sol.ps1 -TodoId MCP-TODOPROGRESSION-001 -Resume -Followup 'unit gate is red on TodoProgressionTests; fix without weakening assertions'
#>
[CmdletBinding()]
param(
    # Canonical TODO id for this slice.
    [Parameter(Mandatory)][string]$TodoId,

    # Slice brief. Not required with -Resume.
    [Parameter()][string]$Brief,

    # Workspace root.
    [Parameter()][string]$Workspace = (Get-Location).Path,

    # Resume the previous Sol session instead of starting a new one.
    [Parameter()][switch]$Resume,

    # Follow-up instruction used with -Resume.
    [Parameter()][string]$Followup,

    # Launch the TUI with the persona preloaded instead of a one-shot run.
    [Parameter()][switch]$Interactive,

    # Persona prompt path.
    [Parameter()][string]$Persona = './docs/personas/sol.md',

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
    throw "Sol persona prompt not found at '$Persona'. Sol must never run without its persona."
}
$personaText = Get-Content -LiteralPath $Persona -Raw

if ($Resume) {
    if (-not $Followup) { throw 'Provide -Followup with -Resume.' }
    # Resume keeps the original persona in context; do not re-paste it.
    codex exec --profile sol --sandbox workspace-write -C $Workspace resume --last $Followup
    if ($LASTEXITCODE -ne 0) { throw "Sol resume failed with exit code $LASTEXITCODE." }
    return
}

if ($Interactive) {
    codex --profile sol --sandbox workspace-write -C $Workspace $personaText
    return
}

if (-not $Brief) { throw 'Provide -Brief for an unattended slice, or use -Interactive or -Resume.' }
if (-not (Test-Path -LiteralPath $Brief)) { throw "Slice brief not found at '$Brief'." }

$receiptDir = Join-Path $Workspace 'docs/receipts'
New-Item -ItemType Directory -Force -Path $receiptDir | Out-Null
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$receipt = Join-Path $receiptDir "sol-$TodoId-$stamp.md"

$prompt = @(
    $personaText
    ''
    '--- TASK BRIEF ---'
    ''
    (Get-Content -LiteralPath $Brief -Raw)
    ''
    "Write your untrusted implementer receipt to $receipt with the exact commands run and"
    'the exact counts printed. Stop at TodoStatus Validating. Do not set done:true.'
) -join [Environment]::NewLine

$prompt | codex exec `
    --profile sol `
    --sandbox workspace-write `
    -C $Workspace `
    -c model_reasoning_effort="xhigh" `
    --json `
    --output-last-message $receipt `
    -

if ($LASTEXITCODE -ne 0) { throw "Sol slice run failed with exit code $LASTEXITCODE." }

Write-Host "Sol receipt (untrusted): $receipt"
Write-Host 'Independent gate check before handing to the validator:'
& (Join-Path $Workspace 'build.ps1') Test
if ($LASTEXITCODE -ne 0) { throw 'Unit gate is red. The slice is not ready for hostile validation.' }
Write-Host "Next: ./tools/powershell/Invoke-HostileGrok.ps1 -TodoId $TodoId -ImplementerReceipt $receipt"
