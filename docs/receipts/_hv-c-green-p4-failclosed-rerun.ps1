#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p4'
$probeRoot = Join-Path $out 'failclosed-probe'
$sandbox = Join-Path $out 'failclosed-sandbox'
New-Item -ItemType Directory -Force -Path $probeRoot | Out-Null

$csprojPath = Join-Path $probeRoot 'FailClosedProbe.csproj'
$csproj = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>false</IsTestProject>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <NoWarn>$(NoWarn);1591;CS1591</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj" />
  </ItemGroup>
</Project>
'@
Set-Content -LiteralPath $csprojPath -Value $csproj -Encoding utf8

$buildLog = Join-Path $out 'failclosed-build.log'
$buildErr = Join-Path $out 'failclosed-build.err.log'
$buildProc = Start-Process -FilePath 'dotnet' -ArgumentList @('build', $csprojPath, '-c', 'Debug', '--nologo') -Wait -PassThru -NoNewWindow -RedirectStandardOutput $buildLog -RedirectStandardError $buildErr
Write-Output ("FAILCLOSED_BUILD_EXIT=" + $buildProc.ExitCode)
if ($buildProc.ExitCode -ne 0) {
    Write-Output 'FAILCLOSED_BUILD_FAILED'
    exit $buildProc.ExitCode
}

$dll = Join-Path $probeRoot 'bin\Debug\net10.0\FailClosedProbe.dll'
$cases = @(
    @{ Name = 'real'; Args = @('real') }
    @{ Name = 'missing-catalog'; Args = @('missing-catalog', $sandbox) }
    @{ Name = 'missing-repo'; Args = @('missing-repo', $sandbox) }
    @{ Name = 'missing-entrypoint'; Args = @('missing-entrypoint', $sandbox) }
    @{ Name = 'missing-version'; Args = @('missing-version', $sandbox) }
    @{ Name = 'missing-ac1'; Args = @('missing-ac1', $sandbox) }
    @{ Name = 'duplicate-key'; Args = @('duplicate-key', $sandbox) }
    @{ Name = 'enabled-count'; Args = @('enabled-count', $sandbox) }
)

$probeResults = @()
foreach ($case in $cases) {
    if (Test-Path -LiteralPath $sandbox) {
        Remove-Item -LiteralPath $sandbox -Recurse -Force -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Force -Path $sandbox | Out-Null
    $caseLog = Join-Path $out ("failclosed2-" + $case.Name + ".log")
    $caseErr = Join-Path $out ("failclosed2-" + $case.Name + ".err.log")
    $argList = @($dll) + $case.Args
    $proc = Start-Process -FilePath 'dotnet' -ArgumentList $argList -Wait -PassThru -NoNewWindow -RedirectStandardOutput $caseLog -RedirectStandardError $caseErr
    $stdout = if (Test-Path $caseLog) { (Get-Content -LiteralPath $caseLog -Raw) } else { '' }
    $stderr = if (Test-Path $caseErr) { (Get-Content -LiteralPath $caseErr -Raw) } else { '' }
    $probeResults += [ordered]@{
        Case = $case.Name
        ExitCode = $proc.ExitCode
        StdOut = ([string]$stdout).Trim()
        StdErr = ([string]$stderr).Trim()
    }
    Write-Output ("FAILCLOSED2_" + $case.Name + "_EXIT=" + $proc.ExitCode)
}

$probeResults | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'failclosed2-results.json') -Encoding utf8
Write-Output 'FAILCLOSED2_DONE'
