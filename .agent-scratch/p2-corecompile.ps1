$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$cache = Join-Path $wt 'build\obj\Debug\_build.csproj.CoreCompileInputs.cache'
if (Test-Path -LiteralPath $cache) { Remove-Item -LiteralPath $cache -Force }
$log = Join-Path $env:USERPROFILE '.grok\p2-corecompile.log'
Set-Location -LiteralPath $wt
& dotnet build (Join-Path $wt 'build\_build.csproj') -c Debug -t:CoreCompile -p:ProduceReferenceAssembly=true -p:CopyBuildOutputToOutputDirectory=false --no-incremental --nologo -v:m *> $log
Write-Output ("CORECOMPILE_EXIT=" + $LASTEXITCODE)
Select-String -LiteralPath $log -Pattern 'error |GenerateReferenceAssembly|CoreCompile|Build succeeded|Build FAILED' |
  Select-Object -Last 20 |
  ForEach-Object { $_.Line.Trim() }
$ref = Join-Path $wt 'build\obj\Debug\ref\_build.dll'
Write-Output ("REF_EXISTS=" + (Test-Path -LiteralPath $ref))
if (Test-Path -LiteralPath $ref) {
  $i = Get-Item -LiteralPath $ref
  Write-Output ("REF_LEN=" + $i.Length)
}
