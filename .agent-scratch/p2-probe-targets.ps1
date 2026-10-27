$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$targets = Join-Path $wt 'build\Directory.Build.targets'
$targetsText = @'
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.targets', '$(MSBuildThisFileDirectory)../'))" />
  <PropertyGroup>
    <CopyBuildOutputToOutputDirectory>false</CopyBuildOutputToOutputDirectory>
  </PropertyGroup>
  <Target Name="IncrementalClean" />
</Project>
'@
[System.IO.File]::WriteAllText($targets, $targetsText.Replace("`n", "`r`n"))
$log = Join-Path $env:USERPROFILE '.grok\p2-targets-probe.log'
try {
    Set-Location -LiteralPath $wt
    & dotnet build (Join-Path $wt 'build\_build.csproj') -c Debug --nologo -v:m *> $log
    $code = $LASTEXITCODE
    Write-Output ("HOST_BUILD_EXIT=" + $code)
    Select-String -LiteralPath $log -Pattern 'error |MSB3027|MSB3061|Build succeeded|Build FAILED' |
      ForEach-Object { $_.Line.Trim() }
    $ref = Join-Path $wt 'build\obj\Debug\ref\_build.dll'
    Write-Output ("REF_EXISTS=" + (Test-Path -LiteralPath $ref))
    if ($code -ne 0) { exit $code }
    & dotnet build (Join-Path $wt 'tests\Build.Tests\Build.Tests.csproj') -c Debug --nologo -v:q *> $log
    $code = $LASTEXITCODE
    Write-Output ("BUILDTESTS_BUILD_EXIT=" + $code)
    Select-String -LiteralPath $log -Pattern 'error |MSB3027|CS0006|Build succeeded|Build FAILED' |
      Select-Object -Last 15 |
      ForEach-Object { $_.Line.Trim() }
    exit $code
} finally {
    if (Test-Path -LiteralPath $targets) { Remove-Item -LiteralPath $targets -Force }
    Write-Output ("TARGETS_LEFT=" + (Test-Path -LiteralPath $targets))
}
