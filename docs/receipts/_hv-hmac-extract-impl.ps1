$ErrorActionPreference = 'Stop'
$hist = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\chat_history.jsonl'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-hmac-plugin-only'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Extract homemade HMAC result (call-3b3c6885 ...-52) and plugin-this-turn result (call-05acd9b2 ...-323)
$homemade = $null
$plugin = $null
$hmacAfterUser = @()
$pythonAfterUser = @()
$seenUser = $false
$lineNo = 0
Get-Content -LiteralPath $hist | ForEach-Object {
    $lineNo++
    $line = $_
    if ($line -match 'Use the plugin to validate, never roll your own HMAC') { $seenUser = $true }
    if ($line -match 'call-3b3c6885-6b2f-4617-bb79-93518c8f852a-52' -and $line -match '"type":"tool_result"') {
        $homemade = $line
        Set-Content -LiteralPath (Join-Path $outDir 'homemade-hmac-tool-result.jsonl') -Value $line -Encoding utf8
    }
    if ($line -match 'call-05acd9b2-1313-44e9-ab39-6923fbc3a916-323' -and $line -match '"type":"tool_result"') {
        $plugin = $line
        Set-Content -LiteralPath (Join-Path $outDir 'plugin-this-turn-tool-result.jsonl') -Value $line -Encoding utf8
    }
    if ($seenUser) {
        if ($line -match 'HMACSHA256') {
            $hmacAfterUser += ('L{0}' -f $lineNo)
        }
        if ($line -match '(?i)\bpython(?:3)?\b|\bpy\.exe\b') {
            $pythonAfterUser += ('L{0}' -f $lineNo)
        }
    }
}

[pscustomobject]@{
    homemadeCaptured = [bool]$homemade
    pluginCaptured = [bool]$plugin
    hmacAfterUserInstructionLines = $hmacAfterUser
    pythonAfterUserInstructionLines = $pythonAfterUser
    homemadeSnippet = if ($homemade) { $homemade.Substring(0, [Math]::Min(2500, $homemade.Length)) } else { $null }
    pluginSnippet = if ($plugin) { $plugin.Substring(0, [Math]::Min(2500, $plugin.Length)) } else { $null }
} | ConvertTo-Json -Depth 6
