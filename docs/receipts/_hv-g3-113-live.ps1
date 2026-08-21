$ErrorActionPreference = 'Stop'
$logDir = 'F:\GitHub\McpServer\docs\receipts\_hv-g3-113'
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
$missingReq = "req-$utc-missing-turn"

$submitBody = @{
    sourceType = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile 113 live tags'
    status = 'in_progress'
    tags = @('hostile-113', 'cluster-closeout')
    turns = @(
        @{
            requestId = $existingReq
            timestamp = [DateTime]::UtcNow.ToString('o')
            queryText = 'hostile 113 live tags persist check'
            queryTitle = 'Hostile 113 live tags'
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

$queryUri = "$baseUrl/mcpserver/sessionlog?agent=GrokCode&limit=20"
$queryResp = Invoke-WebRequest -Uri $queryUri -Headers @{ 'X-Api-Key' = $apiKey } -Method Get -TimeoutSec 60
$queryPath = Join-Path $logDir 'live-query.json'
$queryResp.Content | Set-Content -LiteralPath $queryPath -Encoding utf8
$queryObj = $queryResp.Content | ConvertFrom-Json
$match = @($queryObj.items) | Where-Object { $_.sessionId -eq $sessionId }
$tags = @()
if ($match) { $tags = @($match[0].tags) }
$turnStatus = $null
if ($match -and $match[0].turns) { $turnStatus = $match[0].turns[0].status }
Write-Output ("QUERY_STATUS={0}" -f [int]$queryResp.StatusCode)
Write-Output ("QUERY_MATCH={0}" -f ([bool]$match))
Write-Output ("QUERY_TAGS={0}" -f ($tags -join ','))
Write-Output ("QUERY_TURN_STATUS={0}" -f $turnStatus)
[ordered]@{
    queryStatus = [int]$queryResp.StatusCode
    matched = [bool]$match
    tags = $tags
    turnStatus = $turnStatus
    sessionId = $sessionId
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $logDir 'live-tags-summary.json') -Encoding utf8

$replaceBody = @{
    requestId = $missingReq
    status = 'completed'
    planFile = 'None'
    todoId = 'None'
    queryText = 'missing replace probe'
} | ConvertTo-Json -Depth 6
$replaceUri = "$baseUrl/mcpserver/sessionlog/GrokCode/$sessionId/$missingReq"
try {
    $replaceResp = Invoke-WebRequest -Uri $replaceUri -Headers $headers -Method Put -Body $replaceBody -TimeoutSec 60
    $replaceResult = [ordered]@{
        statusCode = [int]$replaceResp.StatusCode
        body = $replaceResp.Content
    }
} catch {
    $exResp = $_.Exception.Response
    $readerBody = $null
    $code = 0
    if ($exResp) {
        $code = [int]$exResp.StatusCode
        $stream = $exResp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $readerBody = $reader.ReadToEnd()
    }
    $replaceResult = [ordered]@{
        statusCode = $code
        error = $_.Exception.Message
        body = $readerBody
    }
}
($replaceResult | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath (Join-Path $logDir 'live-replace-missing.json') -Encoding utf8
Write-Output ("REPLACE_MISSING_STATUS={0}" -f $replaceResult.statusCode)
Write-Output ("REPLACE_MISSING_BODY={0}" -f $replaceResult.body)

$payload = ('{"nested":"' + ('x' * 20000) + '","mojibake":"' + [char]0xFFFD + '"}')
$largeSession = "GrokCode-$utc-hv113large"
$largeReq = "req-$utc-large-001"
$largeBody = @{
    sourceType = 'GrokCode'
    sessionId = $largeSession
    title = 'Hostile 113 large queryText'
    status = 'in_progress'
    tags = @('hostile-113-large')
    turns = @(
        @{
            requestId = $largeReq
            timestamp = [DateTime]::UtcNow.ToString('o')
            queryText = $payload
            queryTitle = 'Hostile 113 large queryText'
            status = 'in_progress'
            planFile = 'None'
            todoId = 'None'
        }
    )
} | ConvertTo-Json -Depth 8
Write-Output ("LARGE_BODY_CHARS={0}" -f $largeBody.Length)
try {
    $largeResp = Invoke-WebRequest -Uri "$baseUrl/mcpserver/sessionlog" -Headers $headers -Method Post -Body $largeBody -TimeoutSec 60
    $largeResult = [ordered]@{
        statusCode = [int]$largeResp.StatusCode
        body = $largeResp.Content
        queryTextChars = $payload.Length
    }
} catch {
    $exResp = $_.Exception.Response
    $readerBody = $null
    $code = 0
    if ($exResp) {
        $code = [int]$exResp.StatusCode
        $stream = $exResp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $readerBody = $reader.ReadToEnd()
    }
    $largeResult = [ordered]@{
        statusCode = $code
        error = $_.Exception.Message
        body = $readerBody
        queryTextChars = $payload.Length
    }
}
($largeResult | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath (Join-Path $logDir 'live-large-querytext.json') -Encoding utf8
Write-Output ("LARGE_STATUS={0}" -f $largeResult.statusCode)
Write-Output ("LARGE_BODY={0}" -f $largeResult.body)
if ($largeResult.error) { Write-Output ("LARGE_ERROR={0}" -f $largeResult.error) }
