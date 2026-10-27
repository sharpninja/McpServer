#Requires -Version 7.5
# SPDX-License-Identifier: Apache-2.0
function Get-ServerAuditFramework {
    <#
    .SYNOPSIS
    Resolves the installed ASP.NET framework from dotnet's own runtime inventory.
    .DESCRIPTION
    Test-only assembly loading seam. PowerShell may carry a private runtime outside
    dotnet/shared; no runtime path is inferred from System.Private.CoreLib.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string[]]$RuntimeLine,[Parameter(Mandatory)][int]$RuntimeMajor)
    $candidates = @(
        foreach ($line in $RuntimeLine) {
            $match = [regex]::Match($line, '^Microsoft\.AspNetCore\.App (?<version>\d+\.\d+\.\d+) \[(?<root>.+)\]$')
            if (-not $match.Success) { continue }
            $version = [version]$match.Groups['version'].Value
            if ($version.Major -eq $RuntimeMajor) {
                [pscustomobject]@{ Version = $version; FullName = (Join-Path $match.Groups['root'].Value $version.ToString()) }
            }
        }
    )
    if (-not $candidates.Count) { throw 'Matching installed Microsoft.AspNetCore.App runtime required' }
    return $candidates | Sort-Object Version -Descending | Select-Object -First 1
}

function Get-ServerAuditAssemblyFile {
    <#
    .SYNOPSIS
    Selects one DLL path per file name, preferring the application's compiled copy.
    .DESCRIPTION
    Pure fixture loading selection; application dependencies override shared-framework
    duplicates before any assembly is loaded into the isolated fixture context.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][object[]]$FrameworkFile,[Parameter(Mandatory)][object[]]$ApplicationFile)
    $files = [ordered]@{}
    foreach ($file in @($FrameworkFile) + @($ApplicationFile)) {
        if ([string]::IsNullOrWhiteSpace($file.Name) -or [string]::IsNullOrWhiteSpace($file.FullName)) {
            throw 'Assembly candidate requires exact name and full path'
        }
        $files[[string]$file.Name] = [string]$file.FullName
    }
    return @($files.Values)
}


function Get-ServerAuditFixtureAssembly {
    <#
    .SYNOPSIS
    Loads only fixture dependencies into their own managed assembly context.
    .DESCRIPTION
    Reuses the existing eager preload seam before .NET continuations. It does not
    create a fixture/database or register a PowerShell callback on worker threads.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$AssemblyDirectory)
    $directory=[IO.Path]::GetFullPath($AssemblyDirectory)
    $assemblyPath=Join-Path $directory 'QBrainAi.Support.Mcp.Tests.dll'
    if(-not (Test-Path -LiteralPath $assemblyPath)){throw 'Compiled fixture assembly missing'}
    $runtimeLines=@(& dotnet --list-runtimes)
    if($LASTEXITCODE -ne 0){throw 'dotnet runtime inventory failed'}
    $framework=Get-ServerAuditFramework -RuntimeLine $runtimeLines -RuntimeMajor ([Environment]::Version.Major)
    $fixtureContext=[Runtime.Loader.AssemblyLoadContext]::new(('server-audit-fixture-'+[guid]::NewGuid().ToString('N')),$false)
    $dependencyFiles=Get-ServerAuditAssemblyFile -FrameworkFile @(Get-ChildItem -LiteralPath $framework.FullName -Filter '*.dll') -ApplicationFile @(Get-ChildItem -LiteralPath $directory -Filter '*.dll')
    foreach($dependencyPath in $dependencyFiles){
        try{$null=$fixtureContext.LoadFromAssemblyPath($dependencyPath)}
        catch [BadImageFormatException]{Write-Verbose ('Native dependency deferred: '+[IO.Path]::GetFileName($dependencyPath))}
    }
    $native=if($IsWindows){'runtimes/win-x64/native/e_sqlite3.dll'}elseif($IsLinux){'runtimes/linux-x64/native/libe_sqlite3.so'}else{'runtimes/osx-arm64/native/libe_sqlite3.dylib'}
    $null=[Runtime.InteropServices.NativeLibrary]::Load((Join-Path $directory $native))
    return $fixtureContext.LoadFromAssemblyPath($assemblyPath)
}

