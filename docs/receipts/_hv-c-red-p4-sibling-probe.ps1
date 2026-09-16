#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$parent = 'F:\GitHub'
$rows = @(
    'mcpserver-codex-plugin'
    'mcpserver-claude-code-plugin'
    'mcpserver-claude-cowork-plugin'
    'mcpserver-copilot-plugin'
    'mcpserver-grok-plugin'
    'mcpserver-cline-plugin'
    'mcpserver-cline-v2-plugin'
    'mcpserver-opencode-plugin'
)
$out = foreach ($r in $rows) {
    $root = Join-Path $parent $r
    [pscustomobject]@{
        name = $r
        dirExists = (Test-Path -LiteralPath $root -PathType Container)
        repl = (Test-Path -LiteralPath (Join-Path $root 'lib\repl-invoke.ps1'))
        invoke = (Test-Path -LiteralPath (Join-Path $root 'lib\Invoke-McpPlugin.ps1'))
        dist = (Test-Path -LiteralPath (Join-Path $root 'dist\index.js'))
        version = (Test-Path -LiteralPath (Join-Path $root '.version'))
        packageJson = (Test-Path -LiteralPath (Join-Path $root 'package.json'))
        entrypointWouldPass = (
            (Test-Path -LiteralPath (Join-Path $root 'lib\repl-invoke.ps1')) -or
            (Test-Path -LiteralPath (Join-Path $root 'lib\Invoke-McpPlugin.ps1')) -or
            (Test-Path -LiteralPath (Join-Path $root 'dist\index.js'))
        )
        versionWouldPass = (
            (Test-Path -LiteralPath (Join-Path $root '.version')) -or
            (Test-Path -LiteralPath (Join-Path $root 'package.json'))
        )
    }
}
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4\sibling-plugin-probe.json'
$out | ConvertTo-Json | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output 'PROBE_DONE'
