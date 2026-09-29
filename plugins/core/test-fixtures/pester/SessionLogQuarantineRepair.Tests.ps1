#Requires -Version 7.0

# TEST-MCP-SESSIONLIFE-004 / BUG-TRIAGE-172
# Repair numeric-keyed and list-shaped failsafe turns, then replay or keep the file.

Describe 'FR-MCP-SESSIONLIFE-004 quarantine repair' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        . (Join-Path $script:RepoRoot 'plugins\core\lib-ps\repl-invoke.ps1')
        if ($env:MCP_PLUGIN_PERSIST_LOG) { Remove-Item Env:MCP_PLUGIN_PERSIST_LOG }
        function Invoke-ReplRaw {
            param(
                [Parameter(Mandatory)][string]$Method,
                [string]$ParamsYaml = ''
            )
            return $script:DialogResult
        }
    }

    It 'repairs an exhausted numeric-keyed record and deletes it only after durable success' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-3358-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $priorFailsafe = $env:MCPSERVER_FAILSAFE_DIR
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCPSERVER_FAILSAFE_DIR = Join-Path $dir 'failsafe-only'
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:DialogResult = @{ Success = $true; Output = "type: result`n"; Error = ''; ExitCode = 0 }
        try {
            $failsafeDir = Get-ReplFailsafeDir
            [void][System.IO.Directory]::CreateDirectory($failsafeDir)
            $record = Join-Path $failsafeDir '20260722T232406Z-session_submit-3358.invalid-requestid-corrupted-shape.yaml'
            Write-McpYamlObject -Path $record -Document ([ordered]@{
                method = 'client.SessionLog.SubmitAsync'
                drainAttempts = 9
                params = [ordered]@{
                    turns = [ordered]@{
                        '0' = [ordered]@{ requestId = 'req-only' }
                    }
                }
            })
            $summary = Invoke-ReplFailsafeDrain -MaxAttempts 5
            $summary.replayed | Should -Be 1
            Test-Path -LiteralPath $record | Should -BeFalse
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            if ($null -eq $priorFailsafe) { Remove-Item Env:MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue } else { $env:MCPSERVER_FAILSAFE_DIR = $priorFailsafe }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'fills missing list metadata with None and keeps the file when submit fails' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-d5f9-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $priorFailsafe = $env:MCPSERVER_FAILSAFE_DIR
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCPSERVER_FAILSAFE_DIR = Join-Path $dir 'failsafe-only'
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:DialogResult = @{ Success = $false; Output = ''; Error = 'validation rejected the turn'; ExitCode = 1 }
        try {
            $failsafeDir = Get-ReplFailsafeDir
            [void][System.IO.Directory]::CreateDirectory($failsafeDir)
            $record = Join-Path $failsafeDir '20260817T123057Z-session_submit-d5f9.yaml'
            Write-McpYamlObject -Path $record -Document ([ordered]@{
                method = 'client.SessionLog.SubmitAsync'
                drainAttempts = 0
                params = [ordered]@{
                    turns = @(
                        [ordered]@{ requestId = 'req-missing' }
                        [ordered]@{ requestId = 'req-kept'; planFile = 'docs/plans/keep.md'; todoId = 'PLAN-KEEP-001' }
                    )
                }
            })
            $summary = Invoke-ReplFailsafeDrain -MaxAttempts 5
            $summary.failed | Should -Be 1
            Test-Path -LiteralPath $record | Should -BeTrue
            $kept = Read-McpYamlObject -Path $record
            [string]$kept.params.turns[0].planFile | Should -Be 'None'
            [string]$kept.params.turns[0].todoId | Should -Be 'None'
            [string]$kept.params.turns[1].planFile | Should -Be 'docs/plans/keep.md'
            [string]$kept.params.turns[1].todoId | Should -Be 'PLAN-KEEP-001'
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            if ($null -eq $priorFailsafe) { Remove-Item Env:MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue } else { $env:MCPSERVER_FAILSAFE_DIR = $priorFailsafe }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It 'a locked 9ad7 record stays retryable and a later drain deletes it' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-9ad7-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $priorFailsafe = $env:MCPSERVER_FAILSAFE_DIR
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCPSERVER_FAILSAFE_DIR = Join-Path $dir 'failsafe-only'
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:DialogResult = @{ Success = $false; Output = ''; Error = 'SQLite Error 5: database is locked'; ExitCode = 1 }
        $script:ReplFailsafeDrainCompleted = $false
        try {
            $failsafeDir = Get-ReplFailsafeDir
            [void][System.IO.Directory]::CreateDirectory($failsafeDir)
            $record = Join-Path $failsafeDir '20260817T123219Z-session_submit-9ad7.yaml'
            Write-McpYamlObject -Path $record -Document ([ordered]@{
                method = 'client.SessionLog.SubmitAsync'
                drainAttempts = 1
                params = [ordered]@{
                    turns = @(
                        [ordered]@{ requestId = 'req-lock' }
                    )
                }
            })
            $locked = Invoke-WorkflowFailsafeDrain -ParamsYaml 'maxAttempts: 5'
            $locked.Success | Should -BeFalse
            $script:ReplFailsafeDrainCompleted | Should -BeFalse
            Test-Path -LiteralPath $record | Should -BeTrue
            $kept = Read-McpYamlObject -Path $record
            [int]$kept.drainAttempts | Should -Be 1

            $script:DialogResult = @{ Success = $true; Output = "type: result`n"; Error = ''; ExitCode = 0 }
            Invoke-ReplFailsafeDrainOnFirstSuccess
            Test-Path -LiteralPath $record | Should -BeFalse
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            if ($null -eq $priorFailsafe) { Remove-Item Env:MCPSERVER_FAILSAFE_DIR -ErrorAction SilentlyContinue } else { $env:MCPSERVER_FAILSAFE_DIR = $priorFailsafe }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
