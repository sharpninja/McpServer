#Requires -Version 7.0

# Workspace rule: JSON and YAML payloads are built from native objects and serialized, never
# handwritten as string literals. The command_timeout envelope emitted by Invoke-McpPlugin.ps1
# (TR-MCP-TRIAGEPLUGIN-004 / BUG-TRIAGE-120) must come from New-McpPluginTimeoutEnvelope and be
# serialized through ConvertTo-McpPluginYaml so that callers parsing wrapper stdout always
# receive valid YAML, including when the message contains YAML-significant characters.

Describe 'command_timeout envelope is object-built and serialized' {
    BeforeAll {
        $script:CoreRepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        . (Join-Path $PSScriptRoot '..\PluginTestFixtures.ps1')
        # Ported from the plugin repos (7991235): run against a staged plugin synced from plugins/core.
        $script:PluginRoot = Write-TestStagedPlugin -RepoRoot $script:CoreRepoRoot -Root (Join-Path ([System.IO.Path]::GetTempPath()) ('mcp-staged-yaml-' + [guid]::NewGuid().ToString('N')))
        Import-Module (Join-Path $script:PluginRoot 'lib\McpPluginShim.psm1') -Force
        . (Join-Path $script:PluginRoot 'lib\yaml-object-mutation.ps1')
        Import-McpYamlSerializer

        function Assert-TimeoutEnvelopeShape {
            param([Parameter(Mandatory)]$Envelope, [Parameter(Mandatory)][int]$ExpectedSeconds)
            $Envelope['type'] | Should -Be 'error'
            $payload = $Envelope['payload']
            $payload['code'] | Should -Be 'command_timeout'
            $payload['message'] | Should -Be "Plugin command timed out after ${ExpectedSeconds}s."
            $payload['retryable'] | Should -BeTrue
            $payload['details']['degraded'] | Should -BeTrue
            $payload['details']['queued'] | Should -BeTrue
        }
    }

    Context 'mock gate: the assertions discriminate correct and incorrect envelopes' {
        It 'accepts the contract-derived fixture envelope' {
            $fixture = [ordered]@{
                type = 'error'
                payload = [ordered]@{
                    code = 'command_timeout'
                    message = 'Plugin command timed out after 90s.'
                    retryable = $true
                    details = [ordered]@{ degraded = $true; queued = $true }
                }
            }
            Assert-TimeoutEnvelopeShape -Envelope $fixture -ExpectedSeconds 90
        }

        It 'rejects a fixture whose retryable flag is wrong (negative control)' {
            $wrong = [ordered]@{
                type = 'error'
                payload = [ordered]@{
                    code = 'command_timeout'
                    message = 'Plugin command timed out after 90s.'
                    retryable = $false
                    details = [ordered]@{ degraded = $true; queued = $true }
                }
            }
            { Assert-TimeoutEnvelopeShape -Envelope $wrong -ExpectedSeconds 90 } | Should -Throw
        }

        It 'rejects a fixture with the wrong error code (negative control)' {
            $wrong = [ordered]@{
                type = 'error'
                payload = [ordered]@{
                    code = 'backend_unavailable'
                    message = 'Plugin command timed out after 90s.'
                    retryable = $true
                    details = [ordered]@{ degraded = $true; queued = $true }
                }
            }
            { Assert-TimeoutEnvelopeShape -Envelope $wrong -ExpectedSeconds 90 } | Should -Throw
        }

        It 'shows why handwritten YAML is unsafe: a colon in a hand-joined message breaks the parse (negative control)' {
            $handwritten = @(
                'type: error'
                'payload:'
                '  code: command_timeout'
                '  message: Plugin command timed out: after 90s.'
                '  retryable: true'
            ) -join [Environment]::NewLine
            { ConvertFrom-Yaml -Yaml $handwritten } | Should -Throw
        }
    }

    Context 'production: New-McpPluginTimeoutEnvelope and ConvertTo-McpPluginYaml' {
        It 'builds the envelope as a native ordered object with the contract shape' {
            $envelope = New-McpPluginTimeoutEnvelope -TimeoutSeconds 90
            $envelope | Should -BeOfType [System.Collections.Specialized.OrderedDictionary]
            Assert-TimeoutEnvelopeShape -Envelope $envelope -ExpectedSeconds 90
        }

        It 'serializes to YAML that round-trips to the same contract shape' {
            $yaml = ConvertTo-McpPluginYaml -InputObject (New-McpPluginTimeoutEnvelope -TimeoutSeconds 45)
            $yaml | Should -BeOfType [string]
            $parsed = ConvertFrom-Yaml -Yaml $yaml
            Assert-TimeoutEnvelopeShape -Envelope $parsed -ExpectedSeconds 45
        }

        It 'keeps YAML valid when the message carries YAML-significant characters' {
            $envelope = New-McpPluginTimeoutEnvelope -TimeoutSeconds 7
            $envelope['payload']['message'] = 'Plugin command timed out: after 7s. #not-a-comment'
            $parsed = ConvertFrom-Yaml -Yaml (ConvertTo-McpPluginYaml -InputObject $envelope)
            $parsed['payload']['message'] | Should -Be 'Plugin command timed out: after 7s. #not-a-comment'
        }

        It 'rejects a non-positive timeout' {
            { New-McpPluginTimeoutEnvelope -TimeoutSeconds 0 } | Should -Throw
        }
    }

    Context 'wrapper source no longer hand-joins the envelope' {
        It 'Invoke-McpPlugin.ps1 contains no literal YAML envelope lines' {
            $source = Get-Content -LiteralPath (Join-Path $script:PluginRoot 'lib\Invoke-McpPlugin.ps1') -Raw
            $source | Should -Not -Match "'type: error'"
            $source | Should -Not -Match "'  code: command_timeout'"
            $source | Should -Match 'New-McpPluginTimeoutEnvelope'
            $source | Should -Match 'ConvertTo-McpPluginYaml'
        }
    }
}
