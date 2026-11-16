# TEST-MCP-238 contract fixture. Production integration must retain these lifecycle calls.
switch ($HookName) {
    'session-start' { Initialize-McpProfileReloadCompaction -InjectionAvailable }
    'post-compact' { Initialize-McpProfileReloadCompaction; Write-PluginJson ([ordered]@{}) }
    'user-prompt-submit' { Invoke-McpProfileReloadCheck }
}
