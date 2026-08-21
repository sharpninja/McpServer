#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen\02-files.json'
$claimed = @(
    'plugins/core/lib-ps/McpPluginShim.psm1'
    'plugins/core/lib-ps/repl-invoke.ps1'
    'plugins/core/lib-ps/plugin-hook.ps1'
    'plugins/core/lib-ps/Invoke-McpPlugin.ps1'
    'plugins/core/lib-ps/resolve-cache-dir.ps1'
    'plugins/core/hooks-templates/wrapper.ps1.template'
    'plugins/core/test-fixtures/pester/TriagePluginIdentity.Tests.ps1'
    'plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1'
)

Push-Location $wt
try {
    $rows = foreach ($rel in $claimed) {
        $full = Join-Path $wt $rel
        $exists = Test-Path -LiteralPath $full
        $item = if ($exists) { Get-Item -LiteralPath $full } else { $null }
        $diffStat = ''
        if ($exists) {
            $diffStat = [string](git diff --stat -- $rel)
            if ([string]::IsNullOrWhiteSpace($diffStat)) {
                $diffStat = [string](git diff --stat HEAD -- $rel)
            }
        }
        $vsDevelop = ''
        $vsDevelop = [string](git diff --stat origin/develop -- $rel)
        [ordered]@{
            Rel = $rel
            Exists = $exists
            Length = if ($item) { $item.Length } else { $null }
            LastWriteTimeUtc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { $null }
            DiffStatHead = $diffStat
            DiffStatVsDevelop = $vsDevelop
        }
    }

    $codeVerify = Join-Path $wt 'plugins/core/lib-ps/code-verify.ps1'
    $cacheMgr = Join-Path $wt 'plugins/core/lib-ps/cache-manager.ps1'
    $obj = [ordered]@{
        TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        Claimed = @($rows)
        PlanTouchAlsoNamed = [ordered]@{
            CodeVerifyExists = (Test-Path -LiteralPath $codeVerify)
            CacheManagerExists = (Test-Path -LiteralPath $cacheMgr)
            CodeVerifyLastWriteUtc = if (Test-Path -LiteralPath $codeVerify) { (Get-Item -LiteralPath $codeVerify).LastWriteTimeUtc.ToString('o') } else { $null }
            CacheManagerLastWriteUtc = if (Test-Path -LiteralPath $cacheMgr) { (Get-Item -LiteralPath $cacheMgr).LastWriteTimeUtc.ToString('o') } else { $null }
            CodeVerifyVsDevelop = [string](git diff --stat origin/develop -- plugins/core/lib-ps/code-verify.ps1)
            CacheManagerVsDevelop = [string](git diff --stat origin/develop -- plugins/core/lib-ps/cache-manager.ps1)
        }
    }
    $obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
    Write-Output ("WROTE {0} claimed={1}" -f $out, $claimed.Count)
} finally {
    Pop-Location
}
