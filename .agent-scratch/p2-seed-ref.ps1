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
$impl = Join-Path $wt 'build\obj\Debug\_build.dll'
$ref = Join-Path $wt 'build\obj\Debug\ref\_build.dll'
try {
    if (-not (Test-Path -LiteralPath $impl)) { throw 'missing implementation dll' }
    New-Item -ItemType Directory -Force -Path (Split-Path $ref) | Out-Null
    Copy-Item -LiteralPath $impl -Destination $ref -Force
    Write-Output ("SEEDED=" + (Test-Path -LiteralPath $ref))
    Set-Location -LiteralPath $wt
    & dotnet build (Join-Path $wt 'build\_build.csproj') -c Debug --nologo -v:q
    Write-Output ("HOST_EXIT=" + $LASTEXITCODE)
    Write-Output ("REF_AFTER_HOST=" + (Test-Path -LiteralPath $ref))
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet build (Join-Path $wt 'tests\Build.Tests\Build.Tests.csproj') -c Debug --nologo -v:q
    Write-Output ("BUILDTESTS_EXIT=" + $LASTEXITCODE)
    Write-Output ("REF_AFTER_TESTS=" + (Test-Path -LiteralPath $ref))
    exit $LASTEXITCODE
} finally {
    if (Test-Path -LiteralPath $targets) { Remove-Item -LiteralPath $targets -Force }
    Write-Output ("TARGETS_LEFT=" + (Test-Path -LiteralPath $targets))
}
