$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$props = Join-Path $wt 'build\Directory.Build.props'
$owned = $false
if (-not (Test-Path -LiteralPath $props)) {
    $propsText = @'
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  <PropertyGroup>
    <CopyBuildOutputToOutputDirectory>false</CopyBuildOutputToOutputDirectory>
    <IncrementalClean>false</IncrementalClean>
  </PropertyGroup>
</Project>
'@
    [System.IO.File]::WriteAllText($props, $propsText.Replace("`n", "`r`n"))
    $owned = $true
}
$log = Join-Path $env:USERPROFILE '.grok\p2-props-build.log'
try {
    Set-Location -LiteralPath $wt
    & dotnet build (Join-Path $wt 'build\_build.csproj') -c Debug --nologo -v:m *> $log
    $code = $LASTEXITCODE
    Write-Output ("BUILD_EXIT=" + $code)
    Write-Output ("PROPS_OWNED=" + $owned)
    Select-String -LiteralPath $log -Pattern 'error |warning MSB|Build succeeded|Build FAILED' |
      ForEach-Object { $_.Line.Trim() }
} finally {
    if ($owned -and (Test-Path -LiteralPath $props)) {
        Remove-Item -LiteralPath $props -Force
    }
    Write-Output ("PROPS_LEFT=" + (Test-Path -LiteralPath $props))
}
exit $code
