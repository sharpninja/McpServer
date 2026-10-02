#Requires -Version 7.0

# TEST-MCP-SESSIONLIFE-002/004: regressions from the independent unit-repair review.
Describe 'Session lifecycle review regressions' {
    BeforeAll {
        $script:ReviewRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).ProviderPath
        . (Join-Path $script:ReviewRoot 'plugins/core/lib-ps/repl-invoke.ps1')
    }

    BeforeEach {
        $script:ReviewCache = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($script:ReviewCache)
        $script:ReviewSession = 'Codex-20261002T004900Z-review'
        $script:ReviewRequest = 'req-20261002T004900Z-review'
        $script:LastReplPersistenceDetails = $null
        Mock Get-ReplInvokeCacheDir { $script:ReviewCache }
        Mock Get-ReplSessionMeta { @{ SourceType = 'Codex'; SessionId = $script:ReviewSession } }
        Mock Get-ReplSessionStateValue { param($Key) if ($Key -eq 'sessionId') { $script:ReviewSession } }
        Write-McpYamlObject -Path (Join-Path $script:ReviewCache 'current-turn.yaml') -Document ([ordered]@{
            status = 'in_progress'; turnRequestId = $script:ReviewRequest
            sessionId = $script:ReviewSession; queryTitle = 'Refined review title'
            queryText = 'Original request'; planFile = 'docs/plans/approved.md'; todoId = 'BUG-UNITFAIL-001'
        })
    }

    It 'preserves supersession metadata with durability=<Durable>, cached=<Cached>' -ForEach @(
        @{ Durable = $true; Cached = $true },
        @{ Durable = $false; Cached = $true },
        @{ Durable = $false; Cached = $false }
    ) {
        $state = Read-ReplCurrentTurnState
        $state['persisted'] = $Durable
        if (-not $Cached) { $state.Remove('planFile'); $state.Remove('todoId') }
        Write-ReplCurrentTurnState -State $state
        Mock Invoke-ReplPersistTurn {
            param($RequestId, $Title, $Status, $ResponseText, $PlanFile, $TodoId)
            $script:ReviewBound = @{} + $PSBoundParameters
            return $true
        }

        Invoke-ReplSupersedeCurrentTurnIfInProgress -NextRequestId 'req-20261002T005000Z-next'

        $script:ReviewBound.Status | Should -Be 'canceled'
        if ($Durable) {
            $script:ReviewBound.ContainsKey('PlanFile') | Should -BeFalse
            $script:ReviewBound.ContainsKey('TodoId') | Should -BeFalse
        } else {
            $script:ReviewBound.PlanFile | Should -Be $(if ($Cached) { 'docs/plans/approved.md' } else { 'None' })
            $script:ReviewBound.TodoId | Should -Be $(if ($Cached) { 'BUG-UNITFAIL-001' } else { 'None' })
        }
    }

    It 'classifies empty typed turn output rather than throwing' {
        $result = Test-ReplTypedSessionMutationResult -Method 'workflow.sessionlog.setTurnTitle' -Output '' -ExpectedSessionId $script:ReviewSession -ExpectedRequestId $script:ReviewRequest -RequireRetitled
        $result.Ok | Should -BeFalse
        $result.Message | Should -Match 'missing or unparseable'
    }

    It 'rejects an empty turn-title response and retains the actual failsafe file' {
        $script:ReviewFailsafe = Join-Path $script:ReviewCache 'turn-retitle.yaml'
        Write-McpYamlObject -Path $script:ReviewFailsafe -Document (@{ requestId = $script:ReviewRequest })
        $beforeHash = (Get-FileHash -LiteralPath $script:ReviewFailsafe).Hash
        Mock Assert-ReplCurrentTurnFresh { $true }
        Mock Find-ReplFailsafeByFingerprint { '' }
        Mock Write-ReplFailsafe { $script:ReviewFailsafe }
        Mock Invoke-ReplRaw { @{ Success = $true; Output = ''; Error = '' } }
        $result = Invoke-WorkflowSetTurnTitle -ParamsYaml (@{ queryTitle = 'Revised turn title' } | ConvertTo-Yaml)
        $result | Should -BeFalse
        Test-Path -LiteralPath $script:ReviewFailsafe | Should -BeTrue
        (Get-FileHash -LiteralPath $script:ReviewFailsafe).Hash | Should -BeExactly $beforeHash
        $receipt = Read-McpYamlObject -Path (Join-Path $script:ReviewCache 'session-verb-outcome.yaml')
        $receipt.code | Should -Be 'rejected'
        $receipt.message | Should -Match 'missing or unparseable'
    }

    It 'rejects empty appendDialog output without reusing success or deleting retained recovery bytes' {
        Mock Get-ReplFailsafeDir { Join-Path $script:ReviewCache 'failsafe' }
        Mock Assert-ReplCurrentTurnFresh { $true }
        $script:ReviewTransport = @{ Success = $false; Output = ''; Error = 'HTTP 503 backend_unavailable' }
        Mock Invoke-ReplRaw { $script:ReviewTransport }
        $parameters = @{ dialogItems = @(@{ role = 'assistant'; category = 'result'; content = 'Retain this dialog' }) } | ConvertTo-Yaml
        Invoke-WorkflowAppendDialog -ParamsYaml $parameters | Should -BeTrue
        $script:LastReplPersistenceDetails.code | Should -Be 'queued'
        $path = $script:LastReplPersistenceDetails.failsafePath
        Test-Path -LiteralPath $path | Should -BeTrue
        $beforeHash = (Get-FileHash -LiteralPath $path).Hash
        $script:LastReplPersistenceDetails = New-ReplSessionVerbReceipt -Disposition primary -Method 'workflow.sessionlog.setTurnTitle' -RequestId 'stale-request'
        $script:ReviewTransport = @{ Success = $true; Output = ''; Error = '' }
        $output = Invoke-ReplMethod -Method 'workflow.sessionlog.appendDialog' -ParamsYaml $parameters
        $receipt = (ConvertFrom-Yaml -Yaml $output).payload.result
        $receipt.code | Should -Be 'rejected'
        $receipt.method | Should -BeExactly 'workflow.sessionlog.appendDialog'
        $receipt.requestId | Should -BeExactly $script:ReviewRequest
        $script:LastInvokeReplMethodSuccess | Should -BeFalse
        (Get-FileHash -LiteralPath $path).Hash | Should -BeExactly $beforeHash
    }

    It 'requires confirmed session-title identity and retitled flag for <Case>' -ForEach @(
        @{ Case = 'empty'; Valid = $false },
        @{ Case = 'wrong session'; Valid = $false },
        @{ Case = 'case mismatch'; Valid = $false },
        @{ Case = 'not retitled'; Valid = $false },
        @{ Case = 'valid session-only receipt'; Valid = $true }
    ) {
        $reply = [ordered]@{ sessionId = $script:ReviewSession; retitled = $true }
        if ($Case -eq 'wrong session') { $reply.sessionId = 'Codex-other' }
        if ($Case -eq 'case mismatch') { $reply.sessionId = $script:ReviewSession.ToLowerInvariant() }
        if ($Case -eq 'not retitled') { $reply.retitled = $false }
        $script:ReviewOutput = if ($Case -eq 'empty') { '' } else {
            [ordered]@{ type = 'result'; payload = @{ result = $reply } } | ConvertTo-Yaml
        }
        $script:ReviewFailsafe = Join-Path $script:ReviewCache 'retitle.yaml'
        Write-McpYamlObject -Path $script:ReviewFailsafe -Document $reply
        Mock Set-ReplSessionStateValue { $true }
        Mock Find-ReplFailsafeByFingerprint { '' }
        Mock Write-ReplFailsafe { $script:ReviewFailsafe }
        Mock Invoke-ReplRaw { @{ Success = $true; Output = $script:ReviewOutput; Error = '' } }

        $result = Invoke-WorkflowSetSessionTitle -ParamsYaml (@{ title = 'Revised title' } | ConvertTo-Yaml)

        $result | Should -Be $Valid
        Test-Path -LiteralPath $script:ReviewFailsafe | Should -Be (-not $Valid)
        $receipt = Read-McpYamlObject -Path (Join-Path $script:ReviewCache 'session-verb-outcome.yaml')
        $receipt.code | Should -Be $(if ($Valid) { 'persisted' } else { 'rejected' })
    }

    It 'exposes one structured <Disposition> result without booleans at the public method boundary' -ForEach @(
        @{ Disposition = 'primary'; Code = 'persisted'; Success = $true },
        @{ Disposition = 'queued'; Code = 'queued'; Success = $true },
        @{ Disposition = 'rejected'; Code = 'rejected'; Success = $false },
        @{ Disposition = 'lost'; Code = 'lost'; Success = $false },
        @{ Disposition = 'unchanged'; Code = 'unchanged'; Success = $true }
    ) {
        $script:ReviewDisposition = $Disposition
        Mock Invoke-WorkflowUpdateTurn {
            return (Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition $script:ReviewDisposition -Method 'workflow.sessionlog.updateTurn' -RequestId $script:ReviewRequest -Message 'review fixture'))
        }

        $output = @(Invoke-ReplMethod -Method 'workflow.sessionlog.updateTurn' -ParamsYaml (@{ response = 'updated' } | ConvertTo-Yaml))

        $output.Count | Should -Be 1
        $output[0] | Should -BeOfType [string]
        $documents = @(ConvertFrom-Yaml -Yaml ($output -join "`n") -AllDocuments)
        $documents.Count | Should -Be 1
        $documents[0].type | Should -Be 'result'
        $receipt = $documents[0].payload.result
        $receipt.code | Should -Be $Code
        $receipt.sessionId | Should -BeExactly $script:ReviewSession
        $receipt.requestId | Should -BeExactly $script:ReviewRequest
        $receipt.method | Should -BeExactly 'workflow.sessionlog.updateTurn'
        $receipt.persisted | Should -Be ($Disposition -eq 'primary')
        $receipt.queued | Should -Be ($Disposition -eq 'queued')
        $receipt.retryable | Should -Be ($Disposition -eq 'queued')
        $script:LastInvokeReplMethodSuccess | Should -Be $Success
    }

    It 'does not reuse an earlier receipt when the next verb rejects before persistence' {
        $script:LastReplPersistenceDetails = New-ReplSessionVerbReceipt -Disposition primary -Method 'workflow.sessionlog.updateTurn' -RequestId 'stale-request'
        $output = Invoke-ReplMethod -Method 'workflow.sessionlog.setSessionTitle' -ParamsYaml ''
        $receipt = (ConvertFrom-Yaml -Yaml $output).payload.result
        $receipt.code | Should -Be 'rejected'
        $receipt.method | Should -BeExactly 'workflow.sessionlog.setSessionTitle'
        $receipt.requestId | Should -Not -Be 'stale-request'
        $script:LastInvokeReplMethodSuccess | Should -BeFalse
    }

    It 'does not turn a failed multi-step verb into success from an earlier persisted substep' {
        Mock Invoke-WorkflowUpdateTurn {
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition primary -Method 'workflow.sessionlog.updateTurn' -RequestId $script:ReviewRequest))
            return $false
        }
        $output = Invoke-ReplMethod -Method 'workflow.sessionlog.updateTurn'
        $receipt = (ConvertFrom-Yaml -Yaml $output).payload.result
        $receipt.code | Should -Be 'rejected'
        $receipt.persisted | Should -BeFalse
        $script:LastInvokeReplMethodSuccess | Should -BeFalse
    }
}

Describe 'Session lifecycle public wrapper processes' {
    BeforeAll {
        $script:WrapperRepo = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).ProviderPath
        $script:WrapperLib = Join-Path $script:WrapperRepo 'plugins/core/lib-ps'
        . (Join-Path $script:WrapperLib 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer
    }

    It '<Wrapper> exposes exactly one <Disposition> receipt and the matching process exit' -ForEach @(
        foreach ($wrapper in 'shared', 'codex') {
            foreach ($disposition in 'primary', 'queued', 'rejected', 'lost', 'unchanged') {
                @{ Wrapper = $wrapper; Disposition = $disposition }
            }
        }
    ) {
        $root = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $lib = Join-Path $root 'lib'
        $cache = Join-Path $root 'cache'
        [void][System.IO.Directory]::CreateDirectory($cache)
        Copy-Item -LiteralPath $script:WrapperLib -Destination $lib -Recurse
        Copy-Item -LiteralPath (Join-Path $lib 'repl-invoke.ps1') -Destination (Join-Path $lib 'repl-implementation.ps1')
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../SessionLogWrapperHarness.ps1') -Destination (Join-Path $lib 'repl-invoke.ps1') -Force
        $wrapperPath = Join-Path $lib 'Invoke-McpPlugin.ps1'
        if ($Wrapper -eq 'codex') {
            $wrapperPath = Join-Path (Split-Path $script:WrapperRepo -Parent) 'mcpserver-codex-plugin/Invoke-CodexMcpPlugin.ps1'
        }
        Write-McpYamlObject -Path (Join-Path $cache 'current-turn.yaml') -Document (@{
            turnRequestId = 'req-20261002T004900Z-review'; sessionId = 'Codex-20261002T004900Z-review'
            status = 'in_progress'; queryTitle = 'Wrapper fixture'; auditActions = 0
        })
        $method = if ($Disposition -eq 'unchanged') { 'workflow.sessionlog.appendActions' } else { 'workflow.sessionlog.setSessionTitle' }
        $psi = [System.Diagnostics.ProcessStartInfo]::new((Get-Command pwsh).Source)
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.WorkingDirectory = $root
        foreach ($key in @($psi.Environment.Keys | Where-Object { $_ -match '^(MCP_|MCPSERVER_|CODEX_|CLAUDE_|GROK_|PLUGIN_)' })) {
            [void]$psi.Environment.Remove($key)
        }
        $psi.Environment['MCP_PLUGIN_HOST'] = 'codex'
        $psi.Environment['MCP_AGENT_NAME'] = 'Codex'
        $psi.Environment['MCP_REVIEW_DISPOSITION'] = $Disposition
        $psi.Environment['NO_COLOR'] = '1'
        $psi.Environment['MCPSERVER_FAILSAFE_DIR'] = Join-Path $cache 'failsafe'
        $parameters = if ($Disposition -eq 'unchanged') { '' } else { @{ title = 'Wrapper retitle' } | ConvertTo-Yaml }
        foreach ($arg in @('-NoProfile', '-NonInteractive', '-File', $wrapperPath, '-Command', 'Invoke', '-PluginRoot', $root, '-WorkspacePath', $root, '-CacheRoot', $cache, '-Method', $method, '-Params', $parameters)) {
            $psi.ArgumentList.Add($arg)
        }
        $process = [System.Diagnostics.Process]::Start($psi)
        try {
            $process.StandardInput.Close()
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(30000)) { $process.Kill($true); throw 'Wrapper fixture timed out.' }
            $expectedExit = if ($Disposition -in 'rejected', 'lost') { 1 } else { 0 }
            $diagnostic = $stderr.Result -replace '\x1b\[[0-9;]*m', ''
            $process.ExitCode | Should -Be $expectedExit -Because $diagnostic
            $documents = @(ConvertFrom-Yaml -Yaml $stdout.Result -AllDocuments)
            $documents.Count | Should -Be 1
            $documents[0].type | Should -Be 'result'
            $receipt = $documents[0].payload.result
            $receipt.code | Should -Be $(if ($Disposition -eq 'primary') { 'persisted' } else { $Disposition })
            $receipt.method | Should -BeExactly $method
            $receipt.sessionId | Should -BeExactly 'Codex-20261002T004900Z-review'
            $receipt.persisted | Should -Be ($Disposition -eq 'primary')
            $receipt.queued | Should -Be ($Disposition -eq 'queued')
            if ($Disposition -ne 'unchanged') { $receipt.requestId | Should -BeNullOrEmpty }
            $stdout.Result | Should -Not -Match '(?m)^(True|False)$'
        } finally {
            if (-not $process.HasExited) { $process.Kill($true) }
            $process.Dispose()
        }
    }
}
