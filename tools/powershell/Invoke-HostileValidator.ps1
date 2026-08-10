#Requires -Version 7.0
<#
.SYNOPSIS
  OPTIONAL evidence collector only. NOT the hostile validator.

.DESCRIPTION
  Demoted 2026-08-08. The hostile validator is an adversarial Grok sub-agent
  (skill: hostile-validator). This script may gather raw file/test/HTTP facts
  for a human or for the sub-agent; it must never be cited as OverallVerdict
  for plan completion. See docs/McpServer-UseCase-Extension-Design-v3.0.md §6.1.

.PARAMETER ClaimsPath
  Path to a JSON claims file (array of claim objects).

.PARAMETER ReceiptPath
  Output receipt markdown path. Default: docs/receipts/hostile-validator-<utc>.md

.PARAMETER RepoRoot
  Repository root. Default: current directory.

.PARAMETER BaseUrl
  Live service base URL for smoke claims. Default: http://localhost:7147
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ClaimsPath,

    [string] $ReceiptPath,

    [string] $RepoRoot = (Get-Location).Path,

    [string] $BaseUrl = 'http://localhost:7147'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-Result {
    param(
        [string] $Id,
        [string] $Claim,
        [ValidateSet('PASS', 'FAIL', 'UNKNOWN')]
        [string] $Verdict,
        [string] $Evidence,
        [string] $WhyHostile
    )
    [pscustomobject]@{
        Id         = $Id
        Claim      = $Claim
        Verdict    = $Verdict
        Evidence   = $Evidence
        WhyHostile = $WhyHostile
    }
}

function Test-FileContains {
    param([string] $Path, [string] $Pattern, [switch] $NotMatch)
    if (-not (Test-Path -LiteralPath $Path)) {
        return @{ Ok = $false; Detail = "missing file: $Path" }
    }
    $text = Get-Content -LiteralPath $Path -Raw -ErrorAction Stop
    $matched = $text -match $Pattern
    if ($NotMatch) {
        return @{ Ok = (-not $matched); Detail = if ($matched) { "pattern unexpectedly present: $Pattern" } else { "pattern absent as required: $Pattern" } }
    }
    return @{ Ok = $matched; Detail = if ($matched) { "matched: $Pattern" } else { "pattern not found: $Pattern" } }
}

function Invoke-DotNetTestFilter {
    param([string] $Project, [string] $Filter)
    $projPath = Join-Path $RepoRoot $Project
    if (-not (Test-Path -LiteralPath $projPath)) {
        return @{ Ok = $false; Detail = "missing project: $projPath"; ExitCode = -1; Summary = '' }
    }
    $tmpOut = Join-Path ([IO.Path]::GetTempPath()) ("hv-test-" + [guid]::NewGuid().ToString('N') + '.out.log')
    $tmpErr = Join-Path ([IO.Path]::GetTempPath()) ("hv-test-" + [guid]::NewGuid().ToString('N') + '.err.log')
    $args = @('test', $projPath, '-c', 'Debug', '--filter', $Filter, '--nologo')
    $p = Start-Process -FilePath 'dotnet' -ArgumentList $args -WorkingDirectory $RepoRoot -NoNewWindow -PassThru -Wait -RedirectStandardOutput $tmpOut -RedirectStandardError $tmpErr
    $stdout = if (Test-Path -LiteralPath $tmpOut) { Get-Content -LiteralPath $tmpOut -Raw } else { '' }
    $stderr = if (Test-Path -LiteralPath $tmpErr) { Get-Content -LiteralPath $tmpErr -Raw } else { '' }
    $out = "$stdout`n$stderr"
    $summary = ''
    if ($out -match 'Passed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
        $failed = [int]$Matches[1]; $passed = [int]$Matches[2]; $skipped = [int]$Matches[3]; $total = [int]$Matches[4]
        $summary = "Failed=$failed Passed=$passed Skipped=$skipped Total=$total"
        $ok = ($p.ExitCode -eq 0 -and $failed -eq 0 -and $skipped -eq 0 -and $total -gt 0)
        return @{ Ok = $ok; Detail = $summary; ExitCode = $p.ExitCode; Summary = $summary; Output = $out }
    }
    if ($out -match 'Failed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
        $summary = "Failed=$($Matches[1]) Passed=$($Matches[2]) Skipped=$($Matches[3]) Total=$($Matches[4])"
        return @{ Ok = $false; Detail = $summary; ExitCode = $p.ExitCode; Summary = $summary; Output = $out }
    }
    return @{ Ok = $false; Detail = "could not parse test summary; exit=$($p.ExitCode)"; ExitCode = $p.ExitCode; Summary = ''; Output = $out }
}

function Invoke-HttpGet {
    param([string] $Url, [hashtable] $Headers = @{})
    try {
        $resp = Invoke-WebRequest -Uri $Url -Headers $Headers -UseBasicParsing -TimeoutSec 15
        return @{ Ok = $true; StatusCode = [int]$resp.StatusCode; Body = $resp.Content; Detail = "HTTP $($resp.StatusCode)" }
    }
    catch {
        $code = $null
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        return @{ Ok = $false; StatusCode = $code; Body = ''; Detail = $_.Exception.Message }
    }
}

if (-not (Test-Path -LiteralPath $ClaimsPath)) {
    throw "Claims file not found: $ClaimsPath"
}

$claimsRaw = Get-Content -LiteralPath $ClaimsPath -Raw | ConvertFrom-Json
$claims = @($claimsRaw)
if ($claims.Count -eq 0) {
    throw "Claims file is empty: $ClaimsPath"
}

$utc = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssZ')
if (-not $ReceiptPath) {
    $ReceiptPath = Join-Path $RepoRoot "docs/receipts/hostile-validator-$utc.md"
}

$results = [System.Collections.Generic.List[object]]::new()

foreach ($c in $claims) {
    $id = [string]$c.id
    $claim = [string]$c.claim
    $kind = [string]$c.kind
    $why = if ($c.whyHostile) { [string]$c.whyHostile } else { 'Hostile default: prove it or fail it.' }

    switch ($kind) {
        'file_exists' {
            $path = Join-Path $RepoRoot ([string]$c.path)
            $ok = Test-Path -LiteralPath $path
            $results.Add((New-Result -Id $id -Claim $claim -Verdict ($(if ($ok) { 'PASS' } else { 'FAIL' })) -Evidence $(if ($ok) { "exists: $path" } else { "missing: $path" }) -WhyHostile $why))
        }
        'file_contains' {
            $path = Join-Path $RepoRoot ([string]$c.path)
            $r = Test-FileContains -Path $path -Pattern ([string]$c.pattern)
            $results.Add((New-Result -Id $id -Claim $claim -Verdict ($(if ($r.Ok) { 'PASS' } else { 'FAIL' })) -Evidence $r.Detail -WhyHostile $why))
        }
        'file_not_contains' {
            $path = Join-Path $RepoRoot ([string]$c.path)
            $r = Test-FileContains -Path $path -Pattern ([string]$c.pattern) -NotMatch
            $results.Add((New-Result -Id $id -Claim $claim -Verdict ($(if ($r.Ok) { 'PASS' } else { 'FAIL' })) -Evidence $r.Detail -WhyHostile $why))
        }
        'dotnet_test' {
            $r = Invoke-DotNetTestFilter -Project ([string]$c.project) -Filter ([string]$c.filter)
            $results.Add((New-Result -Id $id -Claim $claim -Verdict ($(if ($r.Ok) { 'PASS' } else { 'FAIL' })) -Evidence ("exit=$($r.ExitCode); $($r.Detail)") -WhyHostile $why))
        }
        'http_get' {
            $url = if ($c.url) { [string]$c.url } else { ($BaseUrl.TrimEnd('/') + '/' + ([string]$c.path).TrimStart('/')) }
            $headers = @{}
            if ($c.headers) {
                foreach ($prop in $c.headers.PSObject.Properties) { $headers[$prop.Name] = [string]$prop.Value }
            }
            $r = Invoke-HttpGet -Url $url -Headers $headers
            $expect = if ($null -ne $c.expectStatus) { [int]$c.expectStatus } else { 200 }
            $bodyOk = $true
            $bodyDetail = ''
            if ($c.bodyPattern -and $r.Ok) {
                $bodyOk = $r.Body -match [string]$c.bodyPattern
                $bodyDetail = if ($bodyOk) { " body matched $($c.bodyPattern)" } else { " body did not match $($c.bodyPattern)" }
            }
            $ok = $r.Ok -and ($r.StatusCode -eq $expect) -and $bodyOk
            $results.Add((New-Result -Id $id -Claim $claim -Verdict ($(if ($ok) { 'PASS' } else { 'FAIL' })) -Evidence ("$($r.Detail)$bodyDetail") -WhyHostile $why))
        }
        'assert_false' {
            # Hostile claim that something must NOT be true / complete.
            # Passes when the negative condition holds (agent overclaim detected = FAIL for agent, PASS for validator negative check).
            # Here assert_false means: "the claim under review is false" — if evidence shows false, PASS (validator agrees claim is false).
            $path = Join-Path $RepoRoot ([string]$c.path)
            $r = Test-FileContains -Path $path -Pattern ([string]$c.pattern)
            # If pattern (overclaim marker) is present, agent claim of completeness fails validation of honesty.
            # This kind is used as: "UI is a full diagram editor" should FAIL when only <pre> exists.
            $ok = -not $r.Ok  # PASS when pattern is NOT found (negative claim verified)
            if ($c.invertEvidence) { $ok = $r.Ok }
            $results.Add((New-Result -Id $id -Claim $claim -Verdict ($(if ($ok) { 'PASS' } else { 'FAIL' })) -Evidence $r.Detail -WhyHostile $why))
        }
        'manual_require_fail' {
            # Explicit FAIL: used when claim asserts completeness that operator scope requires more.
            $results.Add((New-Result -Id $id -Claim $claim -Verdict 'FAIL' -Evidence ([string]$c.evidence) -WhyHostile $why))
        }
        default {
            $results.Add((New-Result -Id $id -Claim $claim -Verdict 'UNKNOWN' -Evidence "unsupported kind: $kind" -WhyHostile $why))
        }
    }
}

$pass = @($results | Where-Object { $_.Verdict -eq 'PASS' }).Count
$fail = @($results | Where-Object { $_.Verdict -eq 'FAIL' }).Count
$unknown = @($results | Where-Object { $_.Verdict -eq 'UNKNOWN' }).Count
$overall = if ($fail -eq 0 -and $unknown -eq 0 -and $pass -eq $results.Count -and $results.Count -gt 0) { 'AGREE' } else { 'DISAGREE' }

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Hostile Validator Receipt')
$lines.Add('')
$lines.Add("TimestampUtc: $([DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))")
$lines.Add("RepoRoot: $RepoRoot")
$lines.Add("ClaimsPath: $ClaimsPath")
$lines.Add("BaseUrl: $BaseUrl")
$lines.Add("ClaimsTotal: $($results.Count)")
$lines.Add("PASS: $pass")
$lines.Add("FAIL: $fail")
$lines.Add("UNKNOWN: $unknown")
$lines.Add("OverallVerdict: $overall")
$lines.Add('')
$lines.Add('## Rule')
$lines.Add('Hostile by default. Agent narrative is not evidence. AGREE only if every claim independently re-verified PASS with 0 FAIL and 0 UNKNOWN.')
$lines.Add('Any status report without this receipt (or with DISAGREE) is not trustworthy for completion claims.')
$lines.Add('')
$lines.Add('## Results')
$lines.Add('')
foreach ($r in $results) {
    $lines.Add("### $($r.Id) — $($r.Verdict)")
    $lines.Add("- Claim: $($r.Claim)")
    $lines.Add("- Evidence: $($r.Evidence)")
    $lines.Add("- Why hostile: $($r.WhyHostile)")
    $lines.Add('')
}

$lines.Add('## Operator scope checks (must not be papered over)')
$lines.Add('- Built-in diagram view/edit UI is operator-in-scope (not external-only Mermaid dump).')
$lines.Add('- First-party management UI must cover actors, flows, steps, FR links, coverage, diagram view/edit.')
$lines.Add('- Prior plan [x] checkmarks without AGREE receipt are invalid.')
$lines.Add('')
$lines.Add("## Exit")
$lines.Add("OverallVerdict=$overall (exit $(if ($overall -eq 'AGREE') { 0 } else { 2 }))")

$dir = Split-Path -Parent $ReceiptPath
if (-not (Test-Path -LiteralPath $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}
$lines -join "`n" | Set-Content -LiteralPath $ReceiptPath -Encoding utf8

$jsonPath = [IO.Path]::ChangeExtension($ReceiptPath, '.json')
[pscustomobject]@{
    TimestampUtc    = [DateTimeOffset]::UtcNow.ToString('o')
    OverallVerdict  = $overall
    Pass            = $pass
    Fail            = $fail
    Unknown         = $unknown
    ClaimsPath      = $ClaimsPath
    Results         = $results
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding utf8

Write-Output "Receipt: $ReceiptPath"
Write-Output "Json: $jsonPath"
Write-Output "OverallVerdict: $overall"
Write-Output "PASS=$pass FAIL=$fail UNKNOWN=$unknown"

if ($overall -ne 'AGREE') {
    exit 2
}
exit 0
