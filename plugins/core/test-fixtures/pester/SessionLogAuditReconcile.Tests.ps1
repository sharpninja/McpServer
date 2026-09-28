#Requires -Version 7.0

# TEST-MCP-SESSIONLIFE-005 / BUG-TRIAGE-165
# A classified dialog failure does not increment auditDialog.
# A successful complete sets auditDialog to the server dialog count.

Describe 'FR-MCP-SESSIONLIFE-005 audit dialog reconcile' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        . (Join-Path $script:RepoRoot 'plugins\core\lib-ps\repl-invoke.ps1')
        if ($env:MCP_PLUGIN_PERSIST_LOG) { Remove-Item Env:MCP_PLUGIN_PERSIST_LOG }
        function Invoke-ReplRaw {
            param(
                [Parameter(Mandatory)][string]$Method,
                [string]$ParamsYaml = ''
            )
            if ($Method -eq 'client.SessionLog.QueryAsync') {
                return @{ Success = $true; Output = $script:QueryYaml; Error = ''; ExitCode = 0 }
            }
            if ($Method -eq 'client.SessionLog.AppendDialogAsync') {
                return @{ Success = $false; Output = "payload:`n  code: conflict`n"; Error = 'conflict'; ExitCode = 1 }
            }
            return @{ Success = $false; Output = ''; Error = "unexpected $Method"; ExitCode = 1 }
        }
        function Invoke-ReplPersistTurn {
            param(
                [Parameter(Mandatory)][string]$RequestId,
                [AllowEmptyString()][string]$Title = '',
                [string]$Status = '',
                [string]$ResponseText = '',
                [string]$ActionsYaml = ''
            )
            return $true
        }
    }

    It 'classified dialog failure does not increment and complete reconciles to the server count' {
        $dir = Join-Path ([System.IO.Path]::GetTempPath()) ('sessionlife-audit-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($dir)
        $prior = $env:MCP_CACHE_DIR_OVERRIDE
        $env:MCP_CACHE_DIR_OVERRIDE = $dir
        $env:MCP_WORKSPACE_PATH = $script:RepoRoot
        $script:QueryYaml = @"
payload:
  result:
    items:
      - sessionId: GrokCode-20260923T205606Z-life
        turns:
          - requestId: req-cached
            processingDialog:
              - role: model
                content: one
              - role: user
                content: two
"@
        try {
            Write-McpYamlObject -Path (Join-Path $dir 'session-state.yaml') -Document ([ordered]@{
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'verified'
            })
            Write-McpYamlObject -Path (Join-Path $dir 'current-turn.yaml') -Document ([ordered]@{
                turnRequestId = 'req-cached'
                sessionId = 'GrokCode-20260923T205606Z-life'
                status = 'in_progress'
                queryText = 'keep'
                auditDialog = 5
            })
            $failed = Invoke-WorkflowAppendDialog -ParamsYaml "dialogItems:`n  - role: model`n    content: classified`n"
            $failed | Should -BeFalse
            [int](Get-ReplTurnCacheField -Field 'auditDialog') | Should -Be 5

            $completed = Invoke-WorkflowCompleteTurn -ParamsYaml "response: done`n"
            $completed | Should -BeTrue
            [int](Get-ReplTurnCacheField -Field 'auditDialog') | Should -Be 2
        } finally {
            if ($null -eq $prior) { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue } else { $env:MCP_CACHE_DIR_OVERRIDE = $prior }
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
