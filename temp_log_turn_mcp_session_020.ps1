Import-Module .\tools\powershell\McpSession.psm1 -Force
Initialize-McpSession -MarkerPath .\AGENTS-README-FIRST.yaml
$session = New-McpSessionLog -SourceType 'Copilotcli' -Title 'Implement MCP-SESSION-020' -Model 'gpt-5.3-codex'
$requestId = 'req-' + (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ') + '-mcp-session-020'
Add-McpSessionTurn -Session $session -RequestId $requestId -QueryTitle 'Finish MCP-SESSION-020 to completion' -QueryText 'Finish MCP-SESSION-020 to completion' -Interpretation 'Implement local session state persistence and reuse rules in PowerShell and bash helpers, with tests/docs updates.' -Response 'Started implementation.' -Status in_progress -ContextList @('docs/Project/TODO.yaml','tools/powershell/McpSession.psm1','tools/bash/mcp-session.sh') | Out-Null
[pscustomobject]@{ sessionId = $session.sessionId; requestId = $requestId } | ConvertTo-Json -Compress
