#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$logDir = 'F:\GitHub\McpServer\docs\receipts\_hv-g3-113-post-deploy'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$marker = Get-Content -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
$apiKey = ([regex]::Match($marker, '(?m)^apiKey:\s*(.+)$')).Groups[1].Value.Trim()
$baseUrl = ([regex]::Match($marker, '(?m)^baseUrl:\s*(.+)$')).Groups[1].Value.Trim()
$headers = @{
    'X-Api-Key' = $apiKey
    'Content-Type' = 'application/json'
}

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utc-hv113tags"
$existingReq = "req-$utc-entry-001"

$submitBody = @{
    sourceType = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile 113 live tags after UpdateService'
    status = 'in_progress'
    tags = @('hostile-113', 'cluster-closeout', 'after-updateservice')
    turns = @(
        @{
            requestId = $existingReq
            timestamp = [DateTime]::UtcNow.ToString('o')
            queryText = 'hostile 113 live tags persist check after Nuke UpdateService'
            queryTitle = 'Hostile 113 live tags after deploy'
            status = 'canceled'
            planFile = 'None'
            todoId = 'None'
        }
    )
} | ConvertTo-Json -Depth 8

$submitOut = Join-Path $logDir 'live-submit.json'
try {
    $submitResp = Invoke-WebRequest -Uri "$baseUrl/mcpserver/sessionlog" -Headers $headers -Method Post -Body $submitBody -TimeoutSec 60
    $submitResult = [ordered]@{
        statusCode = [int]$submitResp.StatusCode
        body = $submitResp.Content
    }
} catch {
    $exResp = $_.Exception.Response
    $readerBody = $null
    if ($exResp) {
        $stream = $exResp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $readerBody = $reader.ReadToEnd()
    }
    $submitResult = [ordered]@{
        statusCode = if ($exResp) { [int]$exResp.StatusCode } else { 0 }
        error = $_.Exception.Message
        body = $readerBody
    }
}
($submitResult | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $submitOut -Encoding utf8
Write-Output ("SUBMIT_STATUS={0}" -f $submitResult.statusCode)

$getUri = "$baseUrl/mcpserver/sessionlog/$([uri]::EscapeDataString('GrokCode'))/$([uri]::EscapeDataString($sessionId))"
$getOut = Join-Path $logDir 'live-get-tags-session.json'
try {
    $getResp = Invoke-WebRequest -Uri $getUri -Headers @{ 'X-Api-Key' = $apiKey } -Method Get -TimeoutSec 60
    $getBody = $getResp.Content
    $getStatus = [int]$getResp.StatusCode
} catch {
    $exResp = $_.Exception.Response
    $getBody = $null
    if ($exResp) {
        $stream = $exResp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $getBody = $reader.ReadToEnd()
    }
    $getStatus = if ($exResp) { [int]$exResp.StatusCode } else { 0 }
}

$getObj = $null
$tagsJson = $null
$tagsJoin = ''
$turnStatus = $null
try {
    $getObj = $getBody | ConvertFrom-Json
    $tagsJson = $getObj.tags | ConvertTo-Json -Compress
    if ($null -ne $getObj.tags) {
        $tagsJoin = @($getObj.tags) -join ','
    }
    if ($getObj.turns) { $turnStatus = [string]$getObj.turns[0].status }
} catch {
    $tagsJson = 'parse-error'
}

[ordered]@{
    ok = ($getStatus -eq 200)
    statusCode = $getStatus
    sessionId = $sessionId
    tagsJsonField = $tagsJson
    tagsJoin = $tagsJoin
    turnStatus = $turnStatus
    body = $getBody
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $getOut -Encoding utf8

Write-Output ("GET_STATUS={0}" -f $getStatus)
Write-Output ("SESSION_ID={0}" -f $sessionId)
Write-Output ("TAGS_JSON_FIELD={0}" -f $tagsJson)
Write-Output ("TAGS_JOIN={0}" -f $tagsJoin)
Write-Output ("GET_TURN_STATUS={0}" -f $turnStatus)
Write-Output ("GET_OUT={0}" -f $getOut)
