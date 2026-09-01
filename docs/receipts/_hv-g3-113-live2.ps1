$ErrorActionPreference = 'Stop'
$logDir = 'F:\GitHub\McpServer\docs\receipts\_hv-g3-113'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$marker = Get-Content -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
$apiKey = ([regex]::Match($marker, '(?m)^apiKey:\s*(.+)$')).Groups[1].Value.Trim()
$baseUrl = ([regex]::Match($marker, '(?m)^baseUrl:\s*(.+)$')).Groups[1].Value.Trim()
$headers = @{ 'X-Api-Key' = $apiKey; 'Content-Type' = 'application/json' }

function Invoke-McpJson {
    param($Method, $Uri, $Body)
    try {
        $resp = Invoke-WebRequest -Uri $Uri -Headers $headers -Method $Method -Body $Body -TimeoutSec 90
        return [ordered]@{ ok = $true; statusCode = [int]$resp.StatusCode; body = $resp.Content }
    } catch {
        $status = 0
        $body = $null
        $err = $_.Exception.Message
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) { $body = $_.ErrorDetails.Message }
        elseif ($_.Exception.Response) {
            try { $status = [int]$_.Exception.Response.StatusCode } catch {}
        }
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $status = [int]$_.Exception.Response.StatusCode
        }
        return [ordered]@{ ok = $false; statusCode = $status; error = $err; body = $body }
    }
}

$sessionId = 'GrokCode-20260819T184428Z-hv113tags'
$get = Invoke-McpJson -Method Get -Uri "$baseUrl/mcpserver/sessionlog/GrokCode/$sessionId"
($get | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath (Join-Path $logDir 'live-get-tags-session.json') -Encoding utf8
Write-Output ("GET_TAGS_STATUS={0}" -f $get.statusCode)
if ($get.body) {
    $parsed = $get.body | ConvertFrom-Json
    $tagText = @($parsed.tags) | ForEach-Object { $_ } | Join-String -Separator ','
    Write-Output ("GET_TAGS={0}" -f $tagText)
    Write-Output ("GET_TAGS_COUNT={0}" -f @($parsed.tags).Count)
    Write-Output ("GET_TURN_STATUS={0}" -f $parsed.turns[0].status)
}

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$missingReq = "req-$utc-missing-turn"
$replaceBody = '{"requestId":"' + $missingReq + '","status":"completed","planFile":"None","todoId":"None","queryText":"missing replace probe"}'
$replace = Invoke-McpJson -Method Put -Uri "$baseUrl/mcpserver/sessionlog/GrokCode/$sessionId/$missingReq" -Body $replaceBody
($replace | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath (Join-Path $logDir 'live-replace-missing.json') -Encoding utf8
Write-Output ("REPLACE_MISSING_STATUS={0}" -f $replace.statusCode)
Write-Output ("REPLACE_MISSING_BODY={0}" -f $replace.body)

$payload = ('{"nested":"' + ('x' * 20000) + '"}')
$largeSession = "GrokCode-$utc-hv113large"
$largeReq = "req-$utc-large-001"
$largeObj = @{
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
}
$largeBody = $largeObj | ConvertTo-Json -Depth 8
Write-Output ("LARGE_BODY_CHARS={0} QUERYTEXT_CHARS={1}" -f $largeBody.Length, $payload.Length)
$large = Invoke-McpJson -Method Post -Uri "$baseUrl/mcpserver/sessionlog" -Body $largeBody
($large | ConvertTo-Json -Depth 6) | Set-Content -LiteralPath (Join-Path $logDir 'live-large-querytext.json') -Encoding utf8
Write-Output ("LARGE_STATUS={0}" -f $large.statusCode)
Write-Output ("LARGE_OK={0}" -f $large.ok)
if ($large.body) { Write-Output ("LARGE_BODY={0}" -f $large.body.Substring(0, [Math]::Min(500, $large.body.Length))) }
if ($large.error) { Write-Output ("LARGE_ERROR={0}" -f $large.error) }

$generic = $false
if ($large.body) { $generic = $large.body.Contains('See the inner exception') -or $large.body.Contains('An error occurred while saving the entity changes') }
Write-Output ("LARGE_GENERIC_EF_TEXT={0}" -f $generic)
if ($large.body) {
    $codeMatch = [regex]::Match($large.body, '"code"\s*:\s*"([^"]+)"')
    Write-Output ("LARGE_CODE={0}" -f $codeMatch.Groups[1].Value)
}
