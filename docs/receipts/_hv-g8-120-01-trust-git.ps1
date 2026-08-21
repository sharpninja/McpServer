#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ws = 'F:\GitHub\McpServer'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-g8-120-01.json'
Push-Location $ws
try {
    $utc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    $branch = [string](git rev-parse --abbrev-ref HEAD)
    $sha = [string](git rev-parse HEAD)
    $dirty = @(git status --porcelain)
    . (Join-Path $ws 'plugins\core\lib-ps\marker-resolver.ps1')
    $marker = Join-Path $ws 'AGENTS-README-FIRST.yaml'
    $sigOk = [bool](Test-MarkerSignature -MarkerFile $marker)
    $nonce = 'hv-{0}-{1}' -f ([datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')), $PID
    $health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -TimeoutSec 10
    $echo = $null
    if ($health.PSObject.Properties.Name -contains 'nonce') { $echo = [string]$health.nonce }
    $obj = [ordered]@{
        TimestampUtc = $utc
        Branch = $branch
        Sha = $sha
        DirtyCount = $dirty.Count
        DirtyPreview = @($dirty | Select-Object -First 30)
        MarkerLastWriteUtc = (Get-Item -LiteralPath $marker).LastWriteTimeUtc.ToString('o')
        SignatureOk = $sigOk
        NonceSent = $nonce
        NonceEcho = $echo
        NonceMatch = ($echo -eq $nonce)
        HealthRaw = $health
    }
    $obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
    Write-Output "WROTE $out branch=$branch sha=$sha sig=$sigOk nonce=$($obj.NonceMatch)"
} finally {
    Pop-Location
}
