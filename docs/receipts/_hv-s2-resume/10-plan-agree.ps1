#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$main = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-resume'
$out = Join-Path $outDir '10-plan-agree.json'

$receipts = Get-ChildItem -LiteralPath (Join-Path $main 'docs\receipts') -Filter 'hostile-validator-*.md' -File
$hits = @()
foreach ($f in $receipts) {
    $text = Get-Content -LiteralPath $f.FullName -Raw
    $verdict = $null
    if ($text -match 'OverallVerdict:\s*(AGREE|DISAGREE)') { $verdict = $Matches[1] }
    $s2 = ($text -match 'S2' -or $text -match 'plugin-core' -or $text -match 'TRIAGELEFTOVER')
    if ($s2) {
        $hits += [ordered]@{
            Name = $f.Name
            LastWriteTimeUtc = $f.LastWriteTimeUtc.ToString('o')
            Verdict = $verdict
            MentionsS2Red = ($text -match 'S2 red')
            MentionsHGreen = ($text -match 'H-green' -or $text -match 'hgreen')
        }
    }
}

$plan = Join-Path $main 'docs\plans\triage-cluster-002.md'
$planExists = Test-Path -LiteralPath $plan
$planText = if ($planExists) { Get-Content -LiteralPath $plan -Raw } else { '' }

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    ReceiptCount = $receipts.Count
    S2RelatedReceipts = $hits
    AgreeCount = @($hits | Where-Object { $_.Verdict -eq 'AGREE' }).Count
    DisagreeCount = @($hits | Where-Object { $_.Verdict -eq 'DISAGREE' }).Count
    PlanExists = $planExists
    PlanLength = $planText.Length
    PlanMentionsS2 = ($planText -match 'S2')
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} s2Receipts={1} agree={2} disagree={3}" -f $out, @($hits).Count, $obj.AgreeCount, $obj.DisagreeCount)
