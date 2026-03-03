<#
.SYNOPSIS
    Updates the installed MCP Server Windows service in-place, preserving configuration and data.

.DESCRIPTION
    Stops the service, publishes the latest build, restores preserved files
    (appsettings, databases), restarts the service, and verifies health.
    Run this script elevated (e.g. gsudo .\Update-McpService.ps1).

.PARAMETER ServiceName
    The Windows service name. Default: McpServer.

.PARAMETER InstallPath
    Service installation directory. Default: C:\ProgramData\McpServer.

.PARAMETER Port
    HTTP port for health check. Default: 7147.

.PARAMETER SkipBuild
    When set, copies from a pre-built publish folder instead of running dotnet publish.

.PARAMETER SkipVersionBump
    When set, skips the GitVersion.yml next-version patch bump.

.PARAMETER PublishSource
    Path to pre-built publish output. Only used with -SkipBuild.

.EXAMPLE
    gsudo .\Update-McpService.ps1
    gsudo .\Update-McpService.ps1 -SkipBuild -PublishSource E:\github\McpServer\_publish
#>
[CmdletBinding()]
param(
    [string]$ServiceName = 'McpServer',
    [string]$InstallPath = 'C:\ProgramData\McpServer',
    [int]$Port = 7147,
    [switch]$SkipBuild,
    [switch]$SkipVersionBump,
    [string]$PublishSource = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

$RepoRoot    = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ProjectDir  = Join-Path $RepoRoot 'src\McpServer.Support.Mcp'
$ProjectFile = Join-Path $ProjectDir 'McpServer.Support.Mcp.csproj'
$ExeName     = 'McpServer.Support.Mcp.exe'
$Timestamp   = Get-Date -Format 'yyyyMMdd-HHmmssfff'
$BackupDir   = Join-Path $env:TEMP "McpServer-update-backup-$Timestamp"
$ArchiveDir  = Join-Path $env:USERPROFILE 'McpServer-Backups'
$ArchivePath = Join-Path $ArchiveDir "McpServer-backup-$Timestamp.zip"

# Files to preserve across updates (glob patterns relative to InstallPath).
# appsettings files are restored into the service folder after deployment.
$PreservePatterns = @(
    'appsettings.json',
    'appsettings.yaml'
)

# Directories containing runtime data that should survive updates.
$PreserveDirectories = @(
    'logs',
    'tools'
)

# ---------------------------------------------------------------------------
# Shared: version bump helper (TR-MCP-DRY-001)
# ---------------------------------------------------------------------------

. (Join-Path $PSScriptRoot 'Bump-GitVersionPatch.ps1')

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Assert-Elevated {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$identity
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Error "This script must be run elevated. Use: gsudo .\Update-McpService.ps1"
    }
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n>> $Message" -ForegroundColor Cyan
}

function Wait-ProcessExit {
    param([string]$Name, [int]$TimeoutSeconds = 30)
    $elapsed = 0
    while ($elapsed -lt $TimeoutSeconds) {
        $procs = Get-Process -Name $Name -ErrorAction SilentlyContinue
        if (-not $procs) { return $true }
        Start-Sleep -Seconds 1
        $elapsed++
    }
    return $false
}

function Test-HealthEndpoint {
    param(
        [Parameter(Mandatory)]
        [int]$Port,
        [int]$Attempts = 1,
        [int]$TimeoutSeconds = 3,
        [int]$DelaySeconds = 2
    )

    $lastError = $null
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $r = Invoke-WebRequest -Uri "http://localhost:$Port/health" -TimeoutSec $TimeoutSeconds -UseBasicParsing -ErrorAction Stop
            return [pscustomobject]@{
                Healthy    = $true
                Port       = $Port
                StatusCode = [int]$r.StatusCode
                Content    = [string]$r.Content
                Error      = $null
            }
        }
        catch {
            $lastError = $_.Exception.Message
            if ($attempt -lt $Attempts) {
                Start-Sleep -Seconds $DelaySeconds
            }
        }
    }

    return [pscustomobject]@{
        Healthy    = $false
        Port       = $Port
        StatusCode = $null
        Content    = $null
        Error      = $lastError
    }
}

function Normalize-FullPath {
    param(
        [Parameter(Mandatory)]
        [string]$PathValue
    )

    return [System.IO.Path]::GetFullPath($PathValue).TrimEnd('\','/')
}

function Get-ConfiguredDataFolder {
    param(
        [Parameter(Mandatory)]
        [string]$InstallRoot
    )

    $yamlPath = Join-Path $InstallRoot 'appsettings.yaml'
    $jsonPath = Join-Path $InstallRoot 'appsettings.json'
    $configured = $null

    if (Test-Path $yamlPath) {
        try {
            $yamlContent = Get-Content -Path $yamlPath -Raw
            $match = [regex]::Match($yamlContent, '(?m)^\s*DataFolder\s*:\s*(.+?)\s*$')
            if ($match.Success) {
                $configured = $match.Groups[1].Value.Trim().Trim("'").Trim('"')
            }
        }
        catch {
            Write-Warning "Failed to parse DataFolder from appsettings.yaml: $($_.Exception.Message)"
        }
    }

    if ([string]::IsNullOrWhiteSpace($configured) -and (Test-Path $jsonPath)) {
        try {
            $json = Get-Content -Path $jsonPath -Raw | ConvertFrom-Json
            if ($null -ne $json.DataFolder) {
                $configured = [string]$json.DataFolder
            }
        }
        catch {
            Write-Warning "Failed to parse DataFolder from appsettings.json: $($_.Exception.Message)"
        }
    }

    if ([string]::IsNullOrWhiteSpace($configured)) {
        $configured = '.'
    }

    if ([System.IO.Path]::IsPathRooted($configured)) {
        return [System.IO.Path]::GetFullPath($configured)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $InstallRoot $configured))
}

function Backup-DataFolderContents {
    param(
        [Parameter(Mandatory)]
        [string]$DataFolderPath,
        [Parameter(Mandatory)]
        [string]$InstallRoot,
        [Parameter(Mandatory)]
        [string]$DestinationRoot
    )

    New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
    $copiedItems = @()

    $normalizedDataFolder = Normalize-FullPath -PathValue $DataFolderPath
    $normalizedInstallRoot = Normalize-FullPath -PathValue $InstallRoot

    if ($normalizedDataFolder -eq $normalizedInstallRoot) {
        Write-Warning "Configured DataFolder resolves to install root; backing up legacy runtime data patterns only."
        foreach ($pattern in @('*.db', '*.db-shm', '*.db-wal')) {
            foreach ($file in Get-ChildItem -Path $InstallRoot -Filter $pattern -File -ErrorAction SilentlyContinue) {
                Copy-Item -Path $file.FullName -Destination (Join-Path $DestinationRoot $file.Name) -Force
                $copiedItems += $file.Name
            }
        }

        foreach ($dirName in @('mcp-data', 'templates', 'tools', 'logs')) {
            $sourceDir = Join-Path $InstallRoot $dirName
            if (Test-Path $sourceDir) {
                Copy-Item -Path $sourceDir -Destination $DestinationRoot -Recurse -Force
                $copiedItems += $dirName
            }
        }
    }
    elseif (Test-Path $DataFolderPath) {
        foreach ($item in Get-ChildItem -Path $DataFolderPath -Force -ErrorAction SilentlyContinue) {
            Copy-Item -Path $item.FullName -Destination $DestinationRoot -Recurse -Force
            $copiedItems += $item.Name
        }
    }
    else {
        Write-Warning "Configured data folder not found: $DataFolderPath"
    }

    return ,$copiedItems
}

function Restore-DataFolderContents {
    param(
        [Parameter(Mandatory)]
        [string]$SourceRoot,
        [Parameter(Mandatory)]
        [string]$DestinationRoot
    )

    if (-not (Test-Path $SourceRoot)) {
        return @()
    }

    New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
    $restoredItems = @()
    foreach ($item in Get-ChildItem -Path $SourceRoot -Force -ErrorAction SilentlyContinue) {
        Copy-Item -Path $item.FullName -Destination $DestinationRoot -Recurse -Force
        $restoredItems += $item.Name
    }

    return ,$restoredItems
}

function Remove-StaleInstallContent {
    param(
        [Parameter(Mandatory)]
        [string]$InstallRoot,
        [Parameter(Mandatory)]
        [string]$PublishRoot,
        [string[]]$PreserveFilePatterns = @(),
        [string[]]$PreserveDirNames = @()
    )

    if (-not (Test-Path $InstallRoot) -or -not (Test-Path $PublishRoot)) {
        return [pscustomobject]@{
            FilesRemoved = 0
            DirsRemoved  = 0
        }
    }

    $pathComparer = [System.StringComparer]::OrdinalIgnoreCase
    $sourceFiles = [System.Collections.Generic.HashSet[string]]::new($pathComparer)
    $sourceDirs  = [System.Collections.Generic.HashSet[string]]::new($pathComparer)

    function Get-RelativePathCompat {
        param(
            [Parameter(Mandatory)][string]$BasePath,
            [Parameter(Mandatory)][string]$TargetPath
        )

        $baseFull = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\','/') + [System.IO.Path]::DirectorySeparatorChar
        $targetFull = [System.IO.Path]::GetFullPath($TargetPath)

        if ($targetFull.StartsWith($baseFull, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $targetFull.Substring($baseFull.Length)
        }

        try {
            $baseUri = [System.Uri]($baseFull)
            $targetUri = [System.Uri]($targetFull)
            return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/','\')
        }
        catch {
            return $TargetPath
        }
    }

    foreach ($item in Get-ChildItem -Path $PublishRoot -Recurse -Force -ErrorAction SilentlyContinue) {
        $relative = Get-RelativePathCompat -BasePath $PublishRoot -TargetPath $item.FullName
        if ([string]::IsNullOrWhiteSpace($relative) -or $relative -eq '.') { continue }
        if ($item.PSIsContainer) { [void]$sourceDirs.Add($relative) } else { [void]$sourceFiles.Add($relative) }
    }

    $preserveDirSet = [System.Collections.Generic.HashSet[string]]::new($pathComparer)
    foreach ($dirName in $PreserveDirNames) {
        if (-not [string]::IsNullOrWhiteSpace($dirName)) {
            [void]$preserveDirSet.Add($dirName.Trim('\','/'))
        }
    }

    function Test-IsUnderPreservedDir {
        param([string]$RelativePath)
        $topSegment = ($RelativePath -split '[\\/]', 2)[0]
        return $preserveDirSet.Contains($topSegment)
    }

    function Test-IsPreservedRootFile {
        param([string]$RelativePath)
        if (Test-IsUnderPreservedDir -RelativePath $RelativePath) { return $true }
        if ($RelativePath -match '[\\/]') { return $false }
        $fileName = [System.IO.Path]::GetFileName($RelativePath)
        foreach ($pattern in $PreserveFilePatterns) {
            if ($fileName -like $pattern) { return $true }
        }
        return $false
    }

    $filesRemoved = 0
    $dirsRemoved = 0

    foreach ($file in Get-ChildItem -Path $InstallRoot -Recurse -Force -File -ErrorAction SilentlyContinue) {
        $relative = Get-RelativePathCompat -BasePath $InstallRoot -TargetPath $file.FullName
        if ([string]::IsNullOrWhiteSpace($relative) -or $relative -eq '.') { continue }
        if (Test-IsPreservedRootFile -RelativePath $relative) { continue }
        if (-not $sourceFiles.Contains($relative)) {
            Remove-Item -Path $file.FullName -Force -ErrorAction SilentlyContinue
            $filesRemoved++
        }
    }

    $dirs = Get-ChildItem -Path $InstallRoot -Recurse -Force -Directory -ErrorAction SilentlyContinue |
        Sort-Object { $_.FullName.Length } -Descending
    foreach ($dir in $dirs) {
        $relative = Get-RelativePathCompat -BasePath $InstallRoot -TargetPath $dir.FullName
        if ([string]::IsNullOrWhiteSpace($relative) -or $relative -eq '.') { continue }
        if (Test-IsUnderPreservedDir -RelativePath $relative) { continue }
        if (-not $sourceDirs.Contains($relative)) {
            if (-not (Get-ChildItem -Path $dir.FullName -Force -ErrorAction SilentlyContinue | Select-Object -First 1)) {
                Remove-Item -Path $dir.FullName -Force -ErrorAction SilentlyContinue
                $dirsRemoved++
            }
        }
    }

    return [pscustomobject]@{
        FilesRemoved = $filesRemoved
        DirsRemoved  = $dirsRemoved
    }
}

function Remove-LegacyEnvironmentAppSettings {
    param(
        [Parameter(Mandatory)]
        [string]$InstallRoot
    )

    $removed = @()
    foreach ($name in @('appsettings.Production.json')) {
        $path = Join-Path $InstallRoot $name
        if (Test-Path $path) {
            Remove-Item -Path $path -Force -ErrorAction SilentlyContinue
            if (-not (Test-Path $path)) {
                $removed += $name
            }
        }
    }

    return ,$removed
}

# ---------------------------------------------------------------------------
# Pipeline
# ---------------------------------------------------------------------------

Assert-Elevated

# 0. Version bump
if (-not $SkipBuild -and -not $SkipVersionBump) {
    Write-Step "0/8  Bumping GitVersion next-version patch ..."
    $bumpResult = Bump-GitVersionPatch -RepoRoot $RepoRoot
    Write-Host "  $($bumpResult.OldVersion) -> $($bumpResult.NewVersion)" -ForegroundColor Green
}

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $svc) {
    Write-Error "Service '$ServiceName' is not installed. Use Manage-McpService.ps1 -Action Install first."
}

$wasRunning = $svc.Status -eq 'Running'

# 1. Stop the service
Write-Step "1/8  Stopping service '$ServiceName' ..."
if ($wasRunning) {
    sc.exe stop $ServiceName | Out-Null
    if (-not (Wait-ProcessExit -Name $ExeName.Replace('.exe','') -TimeoutSeconds 30)) {
        Write-Warning "Process did not exit within 30 s - forcing termination"
        Get-Process -Name $ExeName.Replace('.exe','') -ErrorAction SilentlyContinue | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
    Write-Host "  Service stopped." -ForegroundColor Green
}
else {
    Write-Host "  Service was not running." -ForegroundColor DarkGray
}

# 2. Backup preserved files
Write-Step "2/8  Backing up config and data files ..."
New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
$configuredDataFolder = Get-ConfiguredDataFolder -InstallRoot $InstallPath
$dataBackupDir = Join-Path $BackupDir 'data'
$backedUp = @()
foreach ($pattern in $PreservePatterns) {
    $files = Get-ChildItem -Path $InstallPath -Filter $pattern -ErrorAction SilentlyContinue
    foreach ($f in $files) {
        Copy-Item $f.FullName (Join-Path $BackupDir $f.Name) -Force
        $backedUp += $f.Name
    }
}
Write-Host "  Data folder: $configuredDataFolder" -ForegroundColor DarkGray
$dataBackedUp = Backup-DataFolderContents -DataFolderPath $configuredDataFolder -InstallRoot $InstallPath -DestinationRoot $dataBackupDir
if ($dataBackedUp.Count -gt 0) {
    Write-Host "  Backed up data items: $($dataBackedUp -join ', ')" -ForegroundColor DarkGray
}
if ($backedUp.Count -gt 0) {
    Write-Host "  Backed up config files: $($backedUp -join ', ')" -ForegroundColor DarkGray
}
else {
    Write-Host "  No config files matched preserve patterns." -ForegroundColor Yellow
}

if ($backedUp.Count -gt 0 -or $dataBackedUp.Count -gt 0) {
    # Archive to a timestamped zip in the user profile for safe keeping.
    if (-not (Test-Path $ArchiveDir)) {
        New-Item -ItemType Directory -Path $ArchiveDir -Force | Out-Null
    }
    Compress-Archive -Path "$BackupDir\*" -DestinationPath $ArchivePath -Force
    Write-Host "  Archived to: $ArchivePath" -ForegroundColor DarkGray
}

# 3. Build / Publish
Write-Step "3/8  Publishing new build ..."
if ($SkipBuild) {
    if (-not $PublishSource -or -not (Test-Path $PublishSource)) {
        Write-Error "PublishSource '$PublishSource' not found. Provide a valid path with -SkipBuild."
    }
    Write-Host "  Cleaning stale files before copy ..." -ForegroundColor DarkGray
    $cleanup = Remove-StaleInstallContent -InstallRoot $InstallPath -PublishRoot $PublishSource -PreserveFilePatterns $PreservePatterns -PreserveDirNames $PreserveDirectories
    Write-Host "  Removed stale items: $($cleanup.FilesRemoved) file(s), $($cleanup.DirsRemoved) director$(if ($cleanup.DirsRemoved -eq 1) { 'y' } else { 'ies' })" -ForegroundColor DarkGray
    Copy-Item -Path "$PublishSource\*" -Destination $InstallPath -Recurse -Force
}
else {
    if (-not (Test-Path $ProjectFile)) {
        Write-Error "Project file not found: $ProjectFile"
    }
    # Publish to a staging directory first, then copy to install path.
    $stageDir = Join-Path $env:TEMP "McpServer-publish-stage"
    if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
    dotnet publish $ProjectFile -c Release --self-contained -r win-x64 `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        -o $stageDir
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed (exit code $LASTEXITCODE)" }

    Write-Host "  Cleaning stale files before copy ..." -ForegroundColor DarkGray
    $cleanup = Remove-StaleInstallContent -InstallRoot $InstallPath -PublishRoot $stageDir -PreserveFilePatterns $PreservePatterns -PreserveDirNames $PreserveDirectories
    Write-Host "  Removed stale items: $($cleanup.FilesRemoved) file(s), $($cleanup.DirsRemoved) director$(if ($cleanup.DirsRemoved -eq 1) { 'y' } else { 'ies' })" -ForegroundColor DarkGray

    Copy-Item -Path "$stageDir\*" -Destination $InstallPath -Recurse -Force
    Remove-Item $stageDir -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "  Publish complete." -ForegroundColor Green

# 4. Restore preserved files
Write-Step "4/8  Restoring config and data files ..."
$restoreSource = $BackupDir
if (-not (Test-Path $BackupDir) -and (Test-Path $ArchivePath)) {
    Write-Host "  Backup directory missing - extracting from archive: $ArchivePath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    Expand-Archive -Path $ArchivePath -DestinationPath $BackupDir -Force
}
$restored = @()
foreach ($name in @('appsettings.json', 'appsettings.yaml')) {
    $sourcePath = Join-Path $restoreSource $name
    if (Test-Path $sourcePath) {
        $target = Join-Path $InstallPath $name
        Copy-Item -Path $sourcePath -Destination $target -Force
        $restored += $name
    }
}
if ($restored.Count -gt 0) {
    Write-Host "  Restored config files: $($restored -join ', ')" -ForegroundColor DarkGray
}

$restoredDataFolder = Get-ConfiguredDataFolder -InstallRoot $InstallPath
$dataRestoreSource = Join-Path $restoreSource 'data'
$dataRestored = Restore-DataFolderContents -SourceRoot $dataRestoreSource -DestinationRoot $restoredDataFolder
if ($dataRestored.Count -gt 0) {
    Write-Host "  Restored data folder: $restoredDataFolder" -ForegroundColor DarkGray
    Write-Host "  Restored data items: $($dataRestored -join ', ')" -ForegroundColor DarkGray
}

$legacyConfigRemoved = Remove-LegacyEnvironmentAppSettings -InstallRoot $InstallPath
if ($legacyConfigRemoved.Count -gt 0) {
    Write-Host "  Removed legacy environment config overrides: $($legacyConfigRemoved -join ', ')" -ForegroundColor DarkGray
}

# 5. Start the service
Write-Step "5/8  Starting service '$ServiceName' ..."
sc.exe start $ServiceName | Out-Null
Start-Sleep -Seconds 3
$svc = Get-Service -Name $ServiceName
Write-Host "  Service status: $($svc.Status)" -ForegroundColor $(if ($svc.Status -eq 'Running') { 'Green' } else { 'Red' })

# 6. Health check
Write-Step "6/8  Verifying health on port $Port ..."
$primaryHealth = Test-HealthEndpoint -Port $Port -Attempts 10 -TimeoutSeconds 3 -DelaySeconds 2
$healthy = [bool]$primaryHealth.Healthy
if ($healthy) {
    Write-Host "  Health: HTTP $($primaryHealth.StatusCode) - $($primaryHealth.Content)" -ForegroundColor Green
}
if (-not $healthy) {
    Write-Warning "Service did not respond to health check after 20 seconds."
}

# 7. Workspace health checks (reads deployed appsettings.json after restore to test configured/fallback ports).
Write-Step "7/8  Verifying workspace health checks from deployed config ..."
$workspaceChecks = @()
$workspaceHealthChecked = 0
$workspaceHealthOk = 0
$workspaceHealthFailed = 0
$appSettingsYamlPath = Join-Path $InstallPath 'appsettings.yaml'
$appSettingsJsonPath = Join-Path $InstallPath 'appsettings.json'
if (Test-Path $appSettingsYamlPath) {
    $appSettingsPath = $appSettingsYamlPath
    $appSettingsFormat = 'yaml'
}
elseif (Test-Path $appSettingsJsonPath) {
    $appSettingsPath = $appSettingsJsonPath
    $appSettingsFormat = 'json'
}
else {
    $appSettingsPath = $null
    $appSettingsFormat = $null
}

if ($null -eq $appSettingsPath) {
    Write-Warning "No deployed appsettings.json or appsettings.yaml found at $InstallPath; skipping workspace health checks."
}
else {
    try {
        if ($appSettingsFormat -eq 'json') {
            $deployedSettings = Get-Content -Path $appSettingsPath -Raw | ConvertFrom-Json
        }
        else {
            # YAML — requires powershell-yaml module
            if (-not (Get-Module -ListAvailable -Name powershell-yaml)) {
                Install-Module -Name powershell-yaml -Force -Scope CurrentUser -ErrorAction Stop
            }
            Import-Module powershell-yaml -ErrorAction Stop
            $yamlContent = Get-Content -Path $appSettingsPath -Raw
            $yamlHash = ConvertFrom-Yaml -Yaml $yamlContent
            $deployedSettings = $yamlHash | ConvertTo-Json -Depth 20 | ConvertFrom-Json
        }
        $workspaceChecks = @($deployedSettings.Mcp.Workspaces)
    }
    catch {
        Write-Warning "Failed to parse deployed $appSettingsFormat config for workspace health checks: $($_.Exception.Message)"
        $workspaceChecks = @()
    }

    if ($workspaceChecks.Count -eq 0) {
        Write-Host "  No workspaces defined in deployed appsettings.json." -ForegroundColor DarkGray
    }
    else {
        foreach ($ws in $workspaceChecks) {
            if ($null -eq $ws) { continue }

            $isEnabled = $true
            if ($null -ne $ws.IsEnabled) {
                $isEnabled = [bool]$ws.IsEnabled
            }
            if (-not $isEnabled) {
                continue
            }

            $workspaceHealthChecked++
            $wsName = if ([string]::IsNullOrWhiteSpace([string]$ws.Name)) { [string]$ws.WorkspacePath } else { [string]$ws.Name }

            # All workspaces use the global port (single-port model)
            $probe = Test-HealthEndpoint -Port $Port -Attempts 1 -TimeoutSeconds 2 -DelaySeconds 1
            if ($probe.Healthy) {
                $workspaceHealthOk++
                Write-Host "  OK $wsName health OK on port $Port" -ForegroundColor Green
            }
            else {
                $workspaceHealthFailed++
                Write-Warning ("Workspace health check failed: {0}; port={1}; error={2}" -f $wsName, $Port, $probe.Error)
            }
        }
    }
}

# 8. Cleanup
Write-Step "8/8  Cleanup ..."
Remove-Item $BackupDir -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  Backup directory removed." -ForegroundColor DarkGray

# Summary
Write-Host "`n=== Update complete ===" -ForegroundColor Green
Write-Host "  Service : $ServiceName ($($svc.Status))"
Write-Host "  Path    : $InstallPath"
Write-Host "  Health  : $(if ($healthy) { 'OK' } else { 'FAILED' })"
Write-Host "  WSHealth: $(if ($workspaceHealthFailed -eq 0) { 'OK' } else { 'WARN' }) ($workspaceHealthOk/$workspaceHealthChecked)"
Write-Host "  Config  : $($restored.Count) restored, $($backedUp.Count) backed up"
Write-Host "  Data    : $($dataRestored.Count) restored item(s), $($dataBackedUp.Count) backed up item(s)"
if (Test-Path $ArchivePath) {
    Write-Host "  Archive : $ArchivePath" -ForegroundColor DarkGray
}
