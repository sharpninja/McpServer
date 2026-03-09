$markerLines = Get-Content -LiteralPath ".\AGENTS-README-FIRST.yaml"
$baseUrl = (($markerLines | Select-String '^baseUrl:\s*(.+)$').Matches[0].Groups[1].Value).Trim()
$apiKey = (($markerLines | Select-String '^apiKey:\s*(.+)$').Matches[0].Groups[1].Value).Trim()
$headers = @{ "X-Api-Key" = $apiKey }
$expectedUrl = "https://raw.githubusercontent.com/sharpninja/McpServer/develop/tools/powershell/McpSession.psm1"

$buckets = Invoke-RestMethod -Uri "$baseUrl/mcpserver/tools/buckets" -Headers $headers
$officialBucket = @($buckets.buckets | Where-Object { $_.name -eq "official" })[0]
if (-not $officialBucket) {
    $bucketAddBody = @{
        name = "official"
        owner = "sharpninja"
        repo = "McpServerTools"
        branch = "main"
        manifestPath = "/"
    } | ConvertTo-Json
    $bucketAdd = Invoke-RestMethod -Method Post -Uri "$baseUrl/mcpserver/tools/buckets" -Headers $headers -ContentType "application/json" -Body $bucketAddBody
    if (-not $bucketAdd.success) {
        throw "Official bucket registration failed: $($bucketAdd.error)"
    }

    $buckets = Invoke-RestMethod -Uri "$baseUrl/mcpserver/tools/buckets" -Headers $headers
    $officialBucket = @($buckets.buckets | Where-Object { $_.name -eq "official" })[0]
}

if (-not $officialBucket) {
    throw "Official bucket is not registered after the add attempt."
}

$browse = Invoke-RestMethod -Uri "$baseUrl/mcpserver/tools/buckets/official/browse" -Headers $headers
$manifest = @($browse.tools | Where-Object { $_.name -eq "mcp-session-module" })[0]
if (-not $manifest) {
    throw "mcp-session-module manifest was not found in the official bucket browse response."
}

$searchUri = "$baseUrl/mcpserver/tools/search?keyword=$([uri]::EscapeDataString('mcp-session-module'))"
$searchBefore = Invoke-RestMethod -Uri $searchUri -Headers $headers
$tool = @($searchBefore.tools | Where-Object { $_.name -eq "mcp-session-module" })[0]

if (-not $tool) {
    $installUri = "$baseUrl/mcpserver/tools/buckets/official/install?toolName=$([uri]::EscapeDataString('mcp-session-module'))"
    $install = Invoke-RestMethod -Method Post -Uri $installUri -Headers $headers
    if (-not $install.success) {
        throw "Install failed: $($install.error)"
    }

    $searchAfter = Invoke-RestMethod -Uri $searchUri -Headers $headers
    $tool = @($searchAfter.tools | Where-Object { $_.name -eq "mcp-session-module" })[0]
}

if (-not $tool) {
    throw "Installed tool could not be retrieved from /mcpserver/tools/search."
}

$toolDetail = Invoke-RestMethod -Uri "$baseUrl/mcpserver/tools/$($tool.id)" -Headers $headers

[pscustomobject]@{
    baseUrl = $baseUrl
    bucketName = $officialBucket.name
    manifestFound = [bool]$manifest
    manifestFile = $manifest.manifestFile
    toolId = $toolDetail.id
    toolName = $toolDetail.name
    commandTemplate = $toolDetail.commandTemplate
    commandContainsExpectedUrl = ($toolDetail.commandTemplate -like "*$expectedUrl*")
} | ConvertTo-Json -Depth 6
