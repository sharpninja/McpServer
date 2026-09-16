$ErrorActionPreference = 'Stop'
$nonce = 'd4reqscope020'
$r = Invoke-RestMethod -Uri "http://127.0.0.1:7147/health?nonce=$nonce" -TimeoutSec 10
[pscustomobject]@{
    status = $r.status
    nonce = $r.nonce
    pid = $r.pid
    utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    echoMatch = ($r.nonce -eq $nonce)
} | ConvertTo-Json -Compress
