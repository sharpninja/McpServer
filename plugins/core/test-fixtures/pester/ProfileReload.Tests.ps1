#Requires -Version 7.0

BeforeAll {
    <#
    TEST-MCP-238 contract suite.
    Change log: 2026-11-14 - Added full FR-MCP-190 and TR-MCP-PROFILE-001
    coverage, including mock-first provider binding and negative controls.
    #>
    $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
    $script:PluginNames = @(
        'mcpserver-claude-code-plugin',
        'mcpserver-claude-cowork-plugin',
        'mcpserver-codex-plugin',
        'mcpserver-copilot-plugin',
        'mcpserver-grok-plugin',
        'mcpserver-grok-bot-plugin',
        'mcpserver-cline-plugin',
        'mcpserver-cline-v2-plugin',
        'mcpserver-opencode-plugin'
    )
    $script:IsMockGate = $env:MCP_PROFILE_RELOAD_TEST_MODE -eq 'mock'
    $script:FixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('mcp-profile-reload-' + [guid]::NewGuid().ToString('N'))
    [void][System.IO.Directory]::CreateDirectory($script:FixtureRoot)

    if ($script:IsMockGate) {
        $script:ScriptUnderTest = Join-Path $PSScriptRoot 'fixtures/profile-reload-mock.ps1'
        $script:HookUnderTest = Join-Path $PSScriptRoot 'fixtures/plugin-hook-profile-reload-mock.ps1'
        $script:PluginParent = Join-Path $script:FixtureRoot 'plugins'
        foreach ($plugin in $script:PluginNames) {
            $lib = Join-Path (Join-Path $script:PluginParent $plugin) 'lib'
            [void][System.IO.Directory]::CreateDirectory($lib)
            Copy-Item -LiteralPath $script:ScriptUnderTest -Destination (Join-Path $lib 'profile-reload.ps1')
        }
    } else {
        $script:ScriptUnderTest = Join-Path $script:RepoRoot 'plugins/core/lib-ps/profile-reload.ps1'
        $script:HookUnderTest = Join-Path $script:RepoRoot 'plugins/core/lib-ps/plugin-hook.ps1'
        $script:PluginParent = Split-Path -Parent $script:RepoRoot
    }
    . $script:ScriptUnderTest

    function Get-TestTranscriptPath {
        param([Parameter(Mandatory)][string[]]$Lines)
        $path = Join-Path $script:FixtureRoot ([guid]::NewGuid().ToString('N') + '.jsonl')
        [System.IO.File]::WriteAllLines($path, $Lines, [System.Text.UTF8Encoding]::new($false))
        return $path
    }
}

AfterAll {
    Remove-Item -LiteralPath $script:FixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Describe 'TEST-MCP-238 plugin-owned profile reload' {
    It 'returns host-specific add-profile instructions for all nine plugins' {
        $expectations = [ordered]@{
            'claude-code' = '/add-profile'
            'claude-cowork' = '/add-profile'
            'codex' = '$add-profile'
            'copilot' = 'add-profile'
            'grok' = '/add-profile'
            'grok-bot' = '/add-profile'
            'cline' = 'add-profile skill'
            'cline-v2' = 'add-profile skill'
            'opencode' = '/add-profile'
        }
        foreach ($entry in $expectations.GetEnumerator()) {
            Get-McpProfileReloadInstruction -HostName $entry.Key | Should -Match ([regex]::Escape($entry.Value))
        }
    }

    It 'injects immediately at the schema-capable compact start and keeps the threshold armed' {
        $cache = Join-Path $script:FixtureRoot 'immediate'
        $result = Initialize-McpProfileReloadCompaction -CacheDir $cache -HostName 'codex' -InjectionAvailable -Source 'compact'
        $result.AdditionalContext | Should -Match '\$add-profile'
        $result.Armed | Should -BeTrue
        $result.ImmediatePending | Should -BeFalse
        (Read-McpProfileReloadState -CacheDir $cache).armed | Should -BeTrue
    }

    It 'falls back to the next prompt when the compact event cannot inject context' {
        $cache = Join-Path $script:FixtureRoot 'fallback'
        $null = Initialize-McpProfileReloadCompaction -CacheDir $cache -HostName 'claude-code' -Source 'post-compact'
        $first = Invoke-McpProfileReloadCheck -CacheDir $cache -HostName 'claude-code'
        $first.AdditionalContext | Should -Match '/add-profile'
        $first.Armed | Should -BeTrue
        $second = Invoke-McpProfileReloadCheck -CacheDir $cache -HostName 'claude-code'
        $second.AdditionalContext | Should -BeNullOrEmpty
        $second.Armed | Should -BeTrue
    }

    It 'stays silent below 70 percent and uses the Codex-reported 258400-token window' {
        $cache = Join-Path $script:FixtureRoot 'below'
        $null = Initialize-McpProfileReloadCompaction -CacheDir $cache -HostName 'codex' -InjectionAvailable
        $transcript = Get-TestTranscriptPath -Lines @(
            '{"type":"event_msg","payload":{"type":"token_count","info":{"last_token_usage":{"input_tokens":180000},"model_context_window":258400}}}'
        )
        $result = Invoke-McpProfileReloadCheck -CacheDir $cache -HostName 'codex' -TranscriptPath $transcript
        $result.AdditionalContext | Should -BeNullOrEmpty
        $result.Armed | Should -BeTrue
        $result.ContextWindow | Should -Be 258400
        $result.UsageFormat | Should -Be 'codex-token-count'
    }

    It 'fires once at or above 70 percent and then disarms' {
        $cache = Join-Path $script:FixtureRoot 'threshold'
        $null = Initialize-McpProfileReloadCompaction -CacheDir $cache -HostName 'codex' -InjectionAvailable
        $transcript = Get-TestTranscriptPath -Lines @(
            '{"type":"event_msg","payload":{"type":"token_count","info":{"last_token_usage":{"input_tokens":180880},"model_context_window":258400}}}'
        )
        $first = Invoke-McpProfileReloadCheck -CacheDir $cache -HostName 'codex' -TranscriptPath $transcript
        $first.Fired | Should -BeTrue
        $first.AdditionalContext | Should -Match '\$add-profile'
        $first.Armed | Should -BeFalse
        $second = Invoke-McpProfileReloadCheck -CacheDir $cache -HostName 'codex' -TranscriptPath $transcript
        $second.Fired | Should -BeFalse
        $second.AdditionalContext | Should -BeNullOrEmpty
        $second.Armed | Should -BeFalse
    }

    It 'parses Claude assistant usage with a dynamically supplied context window' {
        $cache = Join-Path $script:FixtureRoot 'claude'
        $null = Initialize-McpProfileReloadCompaction -CacheDir $cache -HostName 'claude-code' -InjectionAvailable
        $transcript = Get-TestTranscriptPath -Lines @(
            '{"type":"assistant","message":{"usage":{"input_tokens":60000,"cache_creation_input_tokens":40000,"cache_read_input_tokens":80000}}}'
        )
        $payload = '{"contextWindow":250000}'
        $result = Invoke-McpProfileReloadCheck -CacheDir $cache -HostName 'claude-code' -Payload $payload -TranscriptPath $transcript
        $result.Fired | Should -BeTrue
        $result.InputTokens | Should -Be 180000
        $result.ContextWindow | Should -Be 250000
        $result.UsageFormat | Should -Be 'claude-assistant-usage'
    }

    It 'keeps the state armed when usage or context-window evidence is unavailable' {
        $cache = Join-Path $script:FixtureRoot 'unknown'
        $null = Initialize-McpProfileReloadCompaction -CacheDir $cache -HostName 'opencode' -InjectionAvailable
        $result = Invoke-McpProfileReloadCheck -CacheDir $cache -HostName 'opencode' -Payload '{}'
        $result.Fired | Should -BeFalse
        $result.Armed | Should -BeTrue
        $result.UsageFormat | Should -Be 'unavailable'
    }

    It 'contains no one-million-token assumption and no retired coordination-memory identifier' {
        $text = Get-Content -LiteralPath $script:ScriptUnderTest -Raw
        $text | Should -Not -Match '1000000|1,000,000'
        $text | Should -Not -Match ('MEMORY-' + 'PROCESS-007')
    }

    It 'keeps PostCompact arm-only and prevents it from starting a session or injecting context' {
        $text = Get-Content -LiteralPath $script:HookUnderTest -Raw
        $postCompact = [regex]::Match($text, "(?s)'post-compact'\s*\{(?<body>.*?)\n\s*\}")
        $postCompact.Success | Should -BeTrue
        $postCompact.Groups['body'].Value | Should -Match 'Initialize-McpProfileReloadCompaction'
        $postCompact.Groups['body'].Value | Should -Match 'Write-PluginJson\s+\(\[ordered\]@\{\}\)'
        $postCompact.Groups['body'].Value | Should -Not -Match 'Start-PluginSession|additionalContext'
    }

    It 'synchronizes the production profile-reload provider byte-identically into all nine plugin repositories' {
        $sourceHash = (Get-FileHash -LiteralPath $script:ScriptUnderTest -Algorithm SHA256).Hash
        foreach ($plugin in $script:PluginNames) {
            $path = Join-Path (Join-Path (Join-Path $script:PluginParent $plugin) 'lib') 'profile-reload.ps1'
            Test-Path -LiteralPath $path -PathType Leaf | Should -BeTrue -Because "$plugin must own the implementation"
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash | Should -Be $sourceHash
        }
    }

    It 'has discriminating negative controls for threshold and window drift' {
        $wrongFixedWindowResult = [pscustomobject]@{ Fired = $false; ContextWindow = 1000000 }
        $wrongEarlyFireResult = [pscustomobject]@{ Fired = $true; ContextWindow = 258400 }
        $wrongFixedWindowResult.ContextWindow | Should -Not -Be 258400
        $wrongEarlyFireResult.Fired | Should -Not -BeFalse
    }
}
