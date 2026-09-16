#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p4'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

function Get-TrxSummary {
    param([string]$TrxPath)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        return [ordered]@{ exists = $false; path = $TrxPath }
    }
    [xml]$x = Get-Content -LiteralPath $TrxPath
    $c = $x.TestRun.ResultSummary.Counters
    $outcomes = @()
    foreach ($u in @($x.TestRun.Results.UnitTestResult)) {
        $err = ''
        if ($null -ne $u.Output -and $null -ne $u.Output.ErrorInfo) {
            $err = [string]$u.Output.ErrorInfo.Message
        }
        $outcomes += [ordered]@{
            name = [string]$u.testName
            outcome = [string]$u.outcome
            duration = [string]$u.duration
            output = $err
        }
    }
    return [ordered]@{
        exists = $true
        path = $TrxPath
        outcome = [string]$x.TestRun.ResultSummary.outcome
        total = [string]$c.total
        executed = [string]$c.executed
        passed = [string]$c.passed
        failed = [string]$c.failed
        skipped = $(if ($null -ne $c.skipped) { [string]$c.skipped } else { 'ATTR_MISSING' })
        notExecuted = $(if ($null -ne $c.notExecuted) { [string]$c.notExecuted } else { 'ATTR_MISSING' })
        unitOutcomes = $outcomes
    }
}

$allLog = Join-Path $out 'dotnet-pluginintegration-all.log'
$allErr = Join-Path $out 'dotnet-pluginintegration-all.err.log'

Write-Output 'START_PLUGININT_ALL'
$allArgs = @(
    'test'
    'tests/McpServer.PluginIntegration.Tests'
    '-c'
    'Debug'
    '--logger'
    'trx;LogFileName=pluginintegration-all.trx'
    '--results-directory'
    $out
    '--nologo'
)
$allProc = Start-Process -FilePath 'dotnet' -ArgumentList $allArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput $allLog -RedirectStandardError $allErr
$allExit = $allProc.ExitCode
Write-Output ("PLUGININT_ALL_EXIT=$allExit")

$allSummary = Get-TrxSummary -TrxPath (Join-Path $out 'pluginintegration-all.trx')
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    PluginAllExitCode = $allExit
    PluginAllTrx = $allSummary
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'trx-summary.json') -Encoding utf8

# Independent fail-closed runtime against compiled LoadAndValidate.
$probeRoot = Join-Path $out 'failclosed-probe'
New-Item -ItemType Directory -Force -Path $probeRoot | Out-Null
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
    <ProjectReference Include="..\..\..\tests\McpServer.PluginIntegration.Tests\McpServer.PluginIntegration.Tests.csproj" />
  </ItemGroup>
</Project>
'@
Set-Content -LiteralPath (Join-Path $probeRoot 'FailClosedProbe.csproj') -Value $csproj -Encoding utf8

$program = @'
using System.Text.Json;
using McpServer.PluginIntegration.Tests;

internal static class Program
{
    private static int Main(string[] args)
    {
        var caseName = args.Length > 0 ? args[0] : "real";
        var sandbox = args.Length > 1 ? args[1] : "";
        try
        {
            if (caseName == "real")
            {
                var rows = PluginSessionLogCatalog.LoadAndValidate(@"F:\GitHub\McpServer");
                Console.WriteLine("SUCCESS count=" + rows.Count);
                return 0;
            }

            var fakeRepo = Path.Combine(sandbox, "McpServer");
            Directory.CreateDirectory(Path.Combine(fakeRepo, "tests", "McpServer.PluginIntegration.Tests", "scenarios"));
            var catalogPath = Path.Combine(fakeRepo, "tests", "McpServer.PluginIntegration.Tests", "scenarios", "plugin-sessionlog-scenarios.json");
            var livePath = @"F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json";
            var live = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(livePath));
            var rows = live.GetProperty("scenarios").EnumerateArray().Select(e => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(e.GetRawText())!).ToList();

            void WriteCatalog(IEnumerable<Dictionary<string, JsonElement>> selected)
            {
                var payload = JsonSerializer.Serialize(new { scenarios = selected });
                File.WriteAllText(catalogPath, payload);
            }

            Dictionary<string, JsonElement> Clone(int i) => new(rows[i]);

            JsonElement S(string value) => JsonSerializer.SerializeToElement(value);
            JsonElement A(string[] value) => JsonSerializer.SerializeToElement(value);
            JsonElement B(bool value) => JsonSerializer.SerializeToElement(value);

            switch (caseName)
            {
                case "missing-catalog":
                    break;
                case "missing-repo":
                    {
                        var row = Clone(0);
                        row["repositoryName"] = S("mcpserver-missing-repo-plugin");
                        WriteCatalog(new[] { row }.Concat(Enumerable.Range(1, 7).Select(Clone)));
                        EnsurePlugin(sandbox, "mcpserver-claude-code-plugin", "lib/Invoke-McpPlugin.ps1", true);
                        EnsurePlugin(sandbox, "mcpserver-claude-cowork-plugin", "lib/Invoke-McpPlugin.ps1", true);
                        EnsurePlugin(sandbox, "mcpserver-copilot-plugin", "lib/Invoke-McpPlugin.ps1", true);
                        EnsurePlugin(sandbox, "mcpserver-grok-plugin", "lib/Invoke-McpPlugin.ps1", true);
                        EnsurePlugin(sandbox, "mcpserver-cline-plugin", "dist/index.js", true);
                        EnsurePlugin(sandbox, "mcpserver-cline-v2-plugin", "dist/index.js", true);
                        EnsurePlugin(sandbox, "mcpserver-opencode-plugin", "dist/index.js", true);
                    }
                    break;
                case "missing-entrypoint":
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        var name = rows[i]["repositoryName"].GetString()!;
                        var ep = rows[i]["entrypoint"].GetString()!;
                        EnsurePlugin(sandbox, name, ep, true);
                    }
                    {
                        var row = Clone(0);
                        row["entrypoint"] = S("no-such-entrypoint.ps1");
                        WriteCatalog(new[] { row }.Concat(Enumerable.Range(1, 7).Select(Clone)));
                    }
                    break;
                case "missing-version":
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        var name = rows[i]["repositoryName"].GetString()!;
                        var ep = rows[i]["entrypoint"].GetString()!;
                        EnsurePlugin(sandbox, name, ep, i != 0);
                    }
                    WriteCatalog(Enumerable.Range(0, 8).Select(Clone));
                    break;
                case "missing-ac1":
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        var name = rows[i]["repositoryName"].GetString()!;
                        var ep = rows[i]["entrypoint"].GetString()!;
                        EnsurePlugin(sandbox, name, ep, true);
                    }
                    {
                        var row = Clone(0);
                        row["cacheFolder"] = S("");
                        WriteCatalog(new[] { row }.Concat(Enumerable.Range(1, 7).Select(Clone)));
                    }
                    break;
                case "duplicate-key":
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        var name = rows[i]["repositoryName"].GetString()!;
                        var ep = rows[i]["entrypoint"].GetString()!;
                        EnsurePlugin(sandbox, name, ep, true);
                    }
                    {
                        var row = Clone(6);
                        row["cacheFolder"] = S("cline");
                        WriteCatalog(Enumerable.Range(0, 6).Select(Clone).Concat(new[] { row }).Concat(new[] { Clone(7) }));
                    }
                    break;
                case "enabled-count":
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        var name = rows[i]["repositoryName"].GetString()!;
                        var ep = rows[i]["entrypoint"].GetString()!;
                        EnsurePlugin(sandbox, name, ep, true);
                    }
                    {
                        var row = Clone(7);
                        row["enabled"] = B(false);
                        WriteCatalog(Enumerable.Range(0, 7).Select(Clone).Concat(new[] { row }));
                    }
                    break;
                default:
                    Console.WriteLine("UNKNOWN_CASE " + caseName);
                    return 2;
            }

            if (caseName != "missing-catalog")
            {
                // catalog already written except missing-catalog
            }

            var loaded = PluginSessionLogCatalog.LoadAndValidate(fakeRepo);
            Console.WriteLine("UNEXPECTED_SUCCESS count=" + loaded.Count);
            return 3;
        }
        catch (Exception ex)
        {
            Console.WriteLine("THROW " + ex.GetType().FullName + " :: " + ex.Message);
            return 1;
        }
    }

    private static void EnsurePlugin(string sandbox, string repoName, string entrypoint, bool withVersion)
    {
        var root = Path.Combine(sandbox, repoName);
        var epPath = Path.Combine(root, entrypoint.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(epPath)!);
        if (!File.Exists(epPath))
        {
            File.WriteAllText(epPath, "probe");
        }
        if (withVersion)
        {
            File.WriteAllText(Path.Combine(root, ".version"), "0.0.0-probe");
        }
    }
}
'@
Set-Content -LiteralPath (Join-Path $probeRoot 'Program.cs') -Value $program -Encoding utf8

$sandbox = Join-Path $out 'failclosed-sandbox'
New-Item -ItemType Directory -Force -Path $sandbox | Out-Null

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
    $caseLog = Join-Path $out ("failclosed-" + $case.Name + ".log")
    $caseErr = Join-Path $out ("failclosed-" + $case.Name + ".err.log")
    $argList = @('run', '--project', $probeRoot, '-c', 'Debug', '--nologo', '--') + $case.Args
    $proc = Start-Process -FilePath 'dotnet' -ArgumentList $argList -Wait -PassThru -NoNewWindow -RedirectStandardOutput $caseLog -RedirectStandardError $caseErr
    $stdout = if (Test-Path $caseLog) { Get-Content -LiteralPath $caseLog -Raw } else { '' }
    $probeResults += [ordered]@{
        Case = $case.Name
        ExitCode = $proc.ExitCode
        StdOut = $stdout.Trim()
    }
    Write-Output ("FAILCLOSED_" + $case.Name + "_EXIT=" + $proc.ExitCode)
}

$probeResults | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'failclosed-results.json') -Encoding utf8
Write-Output 'TESTS_DONE'

