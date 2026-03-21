$p = 'tools/powershell/McpSession.Tests.ps1'
$c = Get-Content -Raw $p
$c = $c.Replace("Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'test-key'", "Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'test-key'")
$c = $c.Replace("Initialize-McpSession -BaseUrl 'http://test:9999/' -ApiKey 'k'", "Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999/' -ApiKey 'k'")
$c = $c.Replace("Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'k'", "Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -BaseUrl 'http://test:9999' -ApiKey 'k'")
$c = $c.Replace("Initialize-McpSession -MarkerPath $marker", "Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' -MarkerPath $marker")
$c = $c.Replace("{ Initialize-McpSession }", "{ Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex' }")
$c = $c.Replace("Initialize-McpSession`r`n", "Initialize-McpSession -Agent 'Copilotcli' -Model 'gpt-5.3-codex'`r`n")
Set-Content -Path $p -Value $c -Encoding UTF8
