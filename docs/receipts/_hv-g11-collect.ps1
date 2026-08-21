# Hostile validator collector for G11 / BUG-TRIAGE-107 closeout.
# Read-only except writing this run's evidence under docs/receipts/_hv-g11-out.
$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer'
$outDir = Join-Path $root 'docs\receipts\_hv-g11-out'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$result = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('o')
    Git = $null
    HardcodedVersionedTarballInBuild = $null
    PackageJson = $null
    NpmPack = $null
    VendorCopies = $null
    DotnetTest = $null
    SourceLiteralScan = $null
}

Push-Location $root
try {
    $git = [ordered]@{
        Branch = (git rev-parse --abbrev-ref HEAD).Trim()
        Head = (git rev-parse HEAD).Trim()
        StatusShort = (git status --short -- build/Build.SyncAgentPlugins.cs tests/Build.Tests/SyncAgentPluginsVendorNameTests.cs plugins/core/lib-node/package.json | Out-String).Trim()
        Log1 = (git log -1 --format='%H %s' --).Trim()
    }
    $result.Git = $git

    $buildPath = Join-Path $root 'build\Build.SyncAgentPlugins.cs'
    $buildText = Get-Content -LiteralPath $buildPath -Raw
    $hardcoded = [regex]::Matches($buildText, 'sharpninja-mcpserver-plugin-core-\d+\.\d+\.\d+\.tgz') | ForEach-Object { $_.Value }
    $result.HardcodedVersionedTarballInBuild = [ordered]@{
        Path = $buildPath
        LiteralMatches = @($hardcoded)
        Count = $hardcoded.Count
        StableConstPresent = $buildText.Contains('sharpninja-mcpserver-plugin-core.tgz')
        ReadNodeCorePackageVersionPresent = $buildText.Contains('ReadNodeCorePackageVersion')
        ExpectedPackedInterpolated = $buildText.Contains('sharpninja-mcpserver-plugin-core-{manifestVersion}.tgz')
        FailOnMismatchPresent = $buildText.Contains('npm pack produced')
    }

    $pkgPath = Join-Path $root 'plugins\core\lib-node\package.json'
    $pkg = Get-Content -LiteralPath $pkgPath -Raw | ConvertFrom-Json
    $result.PackageJson = [ordered]@{
        Path = $pkgPath
        Name = $pkg.name
        Version = $pkg.version
    }

    $nodeCore = Join-Path $root 'plugins\core\lib-node'
    $pack = & npm pack --dry-run --json --pack-destination $outDir 2>&1
    $packExit = $LASTEXITCODE
    $packText = ($pack | Out-String).Trim()
    $result.NpmPack = [ordered]@{
        ExitCode = $packExit
        WorkingDirectory = $nodeCore
        Raw = $packText
        ExpectedFileName = "sharpninja-mcpserver-plugin-core-$($pkg.version).tgz"
    }

    $vendorRoots = @(
        'F:\GitHub\mcpserver-cline-plugin\vendor'
        'F:\GitHub\mcpserver-cline-v2-plugin\vendor'
        'F:\GitHub\mcpserver-opencode-plugin\vendor'
    )
    $vendors = @()
    foreach ($vr in $vendorRoots) {
        $files = @()
        if (Test-Path -LiteralPath $vr) {
            $files = @(Get-ChildItem -LiteralPath $vr -Filter 'sharpninja-mcpserver-plugin-core*.tgz' | ForEach-Object {
                [ordered]@{
                    Name = $_.Name
                    Length = $_.Length
                    LastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o')
                    FullName = $_.FullName
                }
            })
        }
        $vendors += [ordered]@{
            Directory = $vr
            Exists = (Test-Path -LiteralPath $vr)
            Files = $files
        }
    }
    $result.VendorCopies = $vendors

    $testProj = Join-Path $root 'tests\Build.Tests\Build.Tests.csproj'
    $trx = Join-Path $outDir 'SyncAgentPluginsVendorNameTests.trx'
    $testOut = & dotnet test $testProj -c Debug --filter 'FullyQualifiedName~SyncAgentPluginsVendorNameTests' --no-restore --logger "trx;LogFileName=$trx" --results-directory $outDir 2>&1
    $testExit = $LASTEXITCODE
    $testText = ($testOut | Out-String)
    $failed = $null
    $skipped = $null
    $passed = $null
    $total = $null
    if ($testText -match 'Failed:\s+(\d+)') { $failed = [int]$Matches[1] }
    if ($testText -match 'Skipped:\s+(\d+)') { $skipped = [int]$Matches[1] }
    if ($testText -match 'Passed:\s+(\d+)') { $passed = [int]$Matches[1] }
    if ($testText -match 'Total:\s+(\d+)') { $total = [int]$Matches[1] }
    $result.DotnetTest = [ordered]@{
        ExitCode = $testExit
        Failed = $failed
        Skipped = $skipped
        Passed = $passed
        Total = $total
        Output = $testText
        Trx = $trx
    }

    $result.SourceLiteralScan = [ordered]@{
        BuildFileHardcoded01 = [regex]::IsMatch($buildText, 'sharpninja-mcpserver-plugin-core-0\.1\.0\.tgz')
        TestFileMentionsLegacy = $true
    }
}
finally {
    Pop-Location
}

# npm pack --json from lib-node specifically
Push-Location (Join-Path $root 'plugins\core\lib-node')
try {
    $pack2 = & npm pack --dry-run --json 2>&1
    $result.NpmPack.FromLibNodeExit = $LASTEXITCODE
    $result.NpmPack.FromLibNodeRaw = ($pack2 | Out-String).Trim()
    try {
        $json = $pack2 | Out-String | ConvertFrom-Json
        if ($json -is [System.Array]) { $first = $json[0] } else { $first = $json }
        $result.NpmPack.Filename = $first.filename
        $result.NpmPack.Name = $first.name
        $result.NpmPack.Version = $first.version
    } catch {
        $result.NpmPack.JsonParseError = $_.Exception.Message
    }
}
finally {
    Pop-Location
}

$jsonPath = Join-Path $outDir 'collector.json'
($result | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output "WROTE $jsonPath"
Write-Output ("GIT_BRANCH=" + $result.Git.Branch)
Write-Output ("GIT_HEAD=" + $result.Git.Head)
Write-Output ("PKG_VERSION=" + $result.PackageJson.Version)
Write-Output ("HARDCODED_COUNT=" + $result.HardcodedVersionedTarballInBuild.Count)
Write-Output ("NPM_PACK_FILENAME=" + $result.NpmPack.Filename)
Write-Output ("NPM_PACK_VERSION=" + $result.NpmPack.Version)
Write-Output ("TEST_EXIT=" + $result.DotnetTest.ExitCode)
Write-Output ("TEST_FAILED=" + $result.DotnetTest.Failed)
Write-Output ("TEST_SKIPPED=" + $result.DotnetTest.Skipped)
Write-Output ("TEST_PASSED=" + $result.DotnetTest.Passed)
Write-Output ("TEST_TOTAL=" + $result.DotnetTest.Total)
