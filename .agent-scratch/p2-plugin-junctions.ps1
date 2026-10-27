$ErrorActionPreference = 'Stop'
$parent = 'F:\GitHub\McpServer\.worktrees'
$plugins = @(
    'mcpserver-claude-code-plugin',
    'mcpserver-copilot-plugin',
    'mcpserver-cline-plugin',
    'mcpserver-grok-plugin',
    'mcpserver-codex-plugin'
)
foreach ($name in $plugins) {
    $link = Join-Path $parent $name
    $target = Join-Path 'F:\GitHub' $name
    $scriptOk = Test-Path -LiteralPath (Join-Path $target 'Invoke-CodexMcpPlugin.ps1') -ErrorAction SilentlyContinue
    $targetExists = Test-Path -LiteralPath $target
    Write-Output ("TARGET $name exists=$targetExists")
    if (-not $targetExists) { throw "Missing real plugin $target" }
    if (Test-Path -LiteralPath $link) {
        $item = Get-Item -LiteralPath $link -Force
        Write-Output ("EXISTS $name attrs=$($item.Attributes) target=$($item.Target)")
        continue
    }
    New-Item -ItemType Junction -Path $link -Target $target | Out-Null
    Write-Output ("CREATED $link -> $target")
}
$env:PATH = 'C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.6.0_x64__8wekyb3d8bbwe;' + $env:PATH
Set-Location 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
dotnet test 'tests\Build.Tests\Build.Tests.csproj' -c Debug --filter 'FullyQualifiedName~SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore|FullyQualifiedName~G8_SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore' --nologo --verbosity minimal
Write-Output ('DOTNET_EXIT=' + $LASTEXITCODE)
exit $LASTEXITCODE
