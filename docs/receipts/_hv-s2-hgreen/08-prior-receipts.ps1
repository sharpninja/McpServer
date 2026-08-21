#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$main = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
$out = Join-Path $outDir '08-prior-receipts.json'

function Read-Receipt {
    param([string]$Path)
    $exists = Test-Path -LiteralPath $Path
    if (-not $exists) {
        return [ordered]@{ Path = $Path; Exists = $false }
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $verdict = $null
    if ($text -match 'OverallVerdict:\s*(AGREE|DISAGREE)') { $verdict = $Matches[1] }
    $failListEmpty = $false
    if ($text -match '(?ms)## Explicit FAIL list\s+None\.') { $failListEmpty = $true }
    $failItems = @()
    $failSection = [regex]::Match($text, '(?ms)## Explicit FAIL list\s+(?<body>.*?)(?:\r?\n## |\z)')
    if ($failSection.Success) {
        $body = $failSection.Groups['body'].Value.Trim()
        if ($body -eq 'None.') { $failListEmpty = $true }
        else {
            $failItems = @([regex]::Matches($body, '(?m)^\d+\.\s+.+$') | ForEach-Object { $_.Value.Trim() })
            $failListEmpty = ($failItems.Count -eq 0)
        }
    }
    $jsonTwin = [System.IO.Path]::ChangeExtension($Path, '.json')
    $jsonVerdict = $null
    $jsonFail = $null
    if (Test-Path -LiteralPath $jsonTwin) {
        $j = Get-Content -LiteralPath $jsonTwin -Raw | ConvertFrom-Json
        if ($j.PSObject.Properties.Name -contains 'OverallVerdict') { $jsonVerdict = [string]$j.OverallVerdict }
        if ($j.PSObject.Properties.Name -contains 'FailList') { $jsonFail = @($j.FailList) }
    }
    return [ordered]@{
        Path = $Path
        Exists = $true
        Length = $text.Length
        LastWriteTimeUtc = (Get-Item -LiteralPath $Path).LastWriteTimeUtc.ToString('o')
        OverallVerdict = $verdict
        FailListEmpty = $failListEmpty
        FailItems = $failItems
        MentionsB2 = ($text -match 'B2')
        MentionsRedPhase = ($text -match 'red-phase|S2 red')
        MentionsTestPhase = ($text -match 'TEST-PHASE|test-phase')
        JsonTwinExists = (Test-Path -LiteralPath $jsonTwin)
        JsonOverallVerdict = $jsonVerdict
        JsonFailList = $jsonFail
        JsonFailCount = $(if ($null -eq $jsonFail) { $null } else { @($jsonFail).Count })
        MdJsonVerdictMatch = ($verdict -eq $jsonVerdict)
    }
}

$r1 = Read-Receipt -Path (Join-Path $main 'docs\receipts\hostile-validator-20260819T203601Z.md')
$r2 = Read-Receipt -Path (Join-Path $main 'docs\receipts\hostile-validator-20260819T205003Z.md')

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    ProductClaimsReceipt = $r1
    TestPhaseReceipt = $r2
    TestPhaseAgreeAndEmptyFail = ($r2.Exists -and $r2.OverallVerdict -eq 'AGREE' -and $r2.FailListEmpty -and $r2.JsonOverallVerdict -eq 'AGREE' -and $r2.JsonFailCount -eq 0)
    ProductClaimsDisagreeOnlyB2 = ($r1.Exists -and $r1.OverallVerdict -eq 'DISAGREE' -and (@($r1.FailItems).Count -eq 1) -and ($r1.FailItems[0] -match 'B2'))
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} r1={1}/{2} r2={3}/{4} testPhaseOk={5}" -f $out, $r1.OverallVerdict, $r1.FailItems.Count, $r2.OverallVerdict, $r2.JsonFailCount, $obj.TestPhaseAgreeAndEmptyFail)
