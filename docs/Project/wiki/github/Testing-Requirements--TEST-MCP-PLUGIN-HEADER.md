# TEST-MCP-PLUGIN-HEADER

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-PLUGIN-HEADER

### TEST-MCP-PLUGIN-HEADER-002

Validates TR-MCP-PLUGIN-HEADER-001. plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1 'TEST-MCP-PLUGIN-HEADER-002 emits the cache transcript path only when that file exists': with the cache session.jsonl actually created, the resolver reports it; the companion assertion in 'TEST-MCP-PLUGIN-PSONLY-001 resolves default agent runtime header fields' proves it is empty when the file was never created. Scope: layer-1+.


### TEST-MCP-PLUGIN-HEADER-003

Validates TR-MCP-PLUGIN-HEADER-001. PluginPowerShellRuntime.Tests.ps1 'TEST-MCP-PLUGIN-HEADER-003 prefers the verified provider session id and transcript from the host payload': ProviderSessionId and an existing TranscriptPath supplied from the host hook payload win over the MCP session id and cache path. Scope: layer-1+.


### TEST-MCP-PLUGIN-HEADER-004

Validates TR-MCP-PLUGIN-HEADER-001. PluginPowerShellRuntime.Tests.ps1 'TEST-MCP-PLUGIN-HEADER-004 never reports the plugin version as the agent executable version': with MCP_PLUGIN_VERSION set to 1.82.0 and executable discovery impossible, agentExecutableVersion is 'unknown' and never 1.82.0. Red before the fallback removal, green after. Scope: layer-1+.


### TEST-MCP-PLUGIN-HEADER-005

Validates TR-MCP-PLUGIN-HEADER-001. PluginPowerShellRuntime.Tests.ps1 'TEST-MCP-PLUGIN-HEADER-005 never re-submits a stale fabricated transcript path or echoed session id from cache': seeds session-state.yaml with a non-existent transcript path and an agentSessionId equal to the MCP session id, then asserts the SubmitAsync payload contains neither. Red before the submit-layer guard in Invoke-ReplPersistTurn, green after. Scope: layer-1+.
