<#
.SYNOPSIS
    Updates or restores the installed MCP Server Windows service in-place while preserving configuration and data.

.DESCRIPTION
    Stops the service, publishes the latest build, restores preserved files
    (appsettings, databases), restarts the service, and verifies health.
    It can also restore the service configuration and data from a previously archived backup.
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

.PARAMETER Restore
    Restores the installed service configuration and data from the latest archived backup,
    or from -BackupArchive when explicitly provided.

.PARAMETER BackupArchive
    Optional path to a specific backup zip created by this script. When omitted with -Restore,
    the newest archive in %USERPROFILE%\McpServer-Backups is used.

.EXAMPLE
    gsudo .\Update-McpService.ps1
    gsudo .\Update-McpService.ps1 -SkipBuild -PublishSource E:\github\McpServer\_publish
    gsudo .\Update-McpService.ps1 -Restore
    gsudo .\Update-McpService.ps1 -Restore -BackupArchive C:\Users\kingd\McpServer-Backups\McpServer-backup-20260309-160925552.zip
#>
[CmdletBinding(DefaultParameterSetName = 'Update')]
param(
    [string]$ServiceName = 'McpServer',
    [string]$InstallPath = 'C:\ProgramData\McpServer',
    [int]$Port = 7147,
    [Parameter(ParameterSetName = 'Update')]
    [switch]$SkipBuild,
    [Parameter(ParameterSetName = 'Update')]
    [switch]$SkipVersionBump,
    [Parameter(ParameterSetName = 'Update')]
    [string]$PublishSource = '',
    [Parameter(ParameterSetName = 'Restore', Mandatory)]
    [switch]$Restore,
    [Parameter(ParameterSetName = 'Restore')]
    [string]$BackupArchive = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

$RepoRoot    = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ProjectDir  = Join-Path $RepoRoot 'src\McpServer.Support.Mcp'
$ProjectFile = Join-Path $ProjectDir 'McpServer.Support.Mcp.csproj'
$LauncherProjectDir  = Join-Path $RepoRoot 'src\McpServer.Launcher'
$LauncherProjectFile = Join-Path $LauncherProjectDir 'McpServer.Launcher.csproj'
$ExeName     = 'McpServer.Support.Mcp.exe'
$LauncherExeName = 'McpServer.Launcher.exe'
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

function Publish-LauncherSidecar {
    param(
        [string]$ProjectFile,
        [string]$DestinationDirectory,
        [string]$ExecutableName
    )

    if (-not (Test-Path $ProjectFile)) {
        Write-Error "Launcher project file not found: $ProjectFile"
    }

    if (-not (Test-Path $DestinationDirectory)) {
        New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null
    }

    $launcherStageDir = Join-Path $env:TEMP "McpServer-launcher-stage"
    if (Test-Path $launcherStageDir) { Remove-Item $launcherStageDir -Recurse -Force }

    dotnet publish $ProjectFile -c Release --self-contained -r win-x64 `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        -o $launcherStageDir
    if ($LASTEXITCODE -ne 0) { Write-Error "Launcher publish failed (exit code $LASTEXITCODE)" }

    $launcherExe = Join-Path $launcherStageDir $ExecutableName
    if (-not (Test-Path $launcherExe)) {
        Write-Error "Launcher publish output missing executable: $launcherExe"
    }

    Copy-Item -Path $launcherExe -Destination (Join-Path $DestinationDirectory $ExecutableName) -Force
    Remove-Item $launcherStageDir -Recurse -Force -ErrorAction SilentlyContinue
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
        [string]$DestinationRoot,
        [switch]$PurgeDestination
    )

    if (-not (Test-Path $SourceRoot)) {
        return @()
    }

    if ($PurgeDestination -and (Test-Path $DestinationRoot)) {
        foreach ($item in Get-ChildItem -Path $DestinationRoot -Force -ErrorAction SilentlyContinue) {
            Remove-Item -Path $item.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
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

function Resolve-BackupArchivePath {
    param(
        [Parameter(Mandatory)]
        [string]$ArchiveDirectory,
        [string]$ExplicitArchivePath = ''
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitArchivePath)) {
        $resolved = (Resolve-Path -Path $ExplicitArchivePath -ErrorAction Stop).Path
        if (-not $resolved.EndsWith('.zip', [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "BackupArchive must point to a .zip file: $resolved"
        }

        return $resolved
    }

    $latest = Get-ChildItem -Path $ArchiveDirectory -Filter 'McpServer-backup-*.zip' -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $latest) {
        throw "No backup archives were found under '$ArchiveDirectory'."
    }

    return $latest.FullName
}

function Backup-PreservedState {
    param(
        [Parameter(Mandatory)]
        [string]$InstallRoot,
        [Parameter(Mandatory)]
        [string]$BackupRoot,
        [Parameter(Mandatory)]
        [string]$ArchivePath
    )

    if (Test-Path $BackupRoot) {
        Remove-Item -Path $BackupRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null
    $configuredDataFolder = Get-ConfiguredDataFolder -InstallRoot $InstallRoot
    $dataBackupDir = Join-Path $BackupRoot 'data'
    $backedUp = @()
    foreach ($pattern in $PreservePatterns) {
        $files = Get-ChildItem -Path $InstallRoot -Filter $pattern -ErrorAction SilentlyContinue
        foreach ($f in $files) {
            Copy-Item $f.FullName (Join-Path $BackupRoot $f.Name) -Force
            $backedUp += $f.Name
        }
    }

    $dataBackedUp = Backup-DataFolderContents -DataFolderPath $configuredDataFolder -InstallRoot $InstallRoot -DestinationRoot $dataBackupDir

    if (Test-Path $ArchivePath) {
        Remove-Item -Path $ArchivePath -Force -ErrorAction SilentlyContinue
    }

    if ($backedUp.Count -gt 0 -or $dataBackedUp.Count -gt 0) {
        if (-not (Test-Path $ArchiveDir)) {
            New-Item -ItemType Directory -Path $ArchiveDir -Force | Out-Null
        }

        Compress-Archive -Path "$BackupRoot\*" -DestinationPath $ArchivePath -Force
    }

    return [pscustomobject]@{
        ConfiguredDataFolder = $configuredDataFolder
        BackedUpConfig       = @($backedUp)
        BackedUpData         = @($dataBackedUp)
        ArchivePath          = if (Test-Path $ArchivePath) { $ArchivePath } else { $null }
    }
}

function Restore-PreservedState {
    param(
        [Parameter(Mandatory)]
        [string]$RestoreSource,
        [Parameter(Mandatory)]
        [string]$InstallRoot,
        [switch]$PurgeDataFolder
    )

    if (-not (Test-Path $RestoreSource)) {
        throw "Restore source path not found: $RestoreSource"
    }

    $restored = @()
    foreach ($name in @('appsettings.json', 'appsettings.yaml')) {
        $sourcePath = Join-Path $RestoreSource $name
        if (Test-Path $sourcePath) {
            $target = Join-Path $InstallRoot $name
            Copy-Item -Path $sourcePath -Destination $target -Force
            $restored += $name
        }
    }

    $restoredDataFolder = Get-ConfiguredDataFolder -InstallRoot $InstallRoot
    $dataRestoreSource = Join-Path $RestoreSource 'data'
    $dataRestored = Restore-DataFolderContents -SourceRoot $dataRestoreSource -DestinationRoot $restoredDataFolder -PurgeDestination:$PurgeDataFolder
    $legacyConfigRemoved = Remove-LegacyEnvironmentAppSettings -InstallRoot $InstallRoot

    return [pscustomobject]@{
        RestoredConfig      = @($restored)
        RestoredDataFolder  = $restoredDataFolder
        RestoredData        = @($dataRestored)
        LegacyConfigRemoved = @($legacyConfigRemoved)
    }
}

function Stop-InstalledService {
    param(
        [Parameter(Mandatory)]
        [string]$Name,
        [Parameter(Mandatory)]
        [string]$ProcessName
    )

    $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $svc) {
        throw "Service '$Name' is not installed. Use Manage-McpService.ps1 -Action Install first."
    }

    $wasRunning = $svc.Status -eq 'Running'
    if ($wasRunning) {
        sc.exe stop $Name | Out-Null
        if (-not (Wait-ProcessExit -Name $ProcessName -TimeoutSeconds 30)) {
            Write-Warning "Process did not exit within 30 s - forcing termination"
            Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Stop-Process -Force
            Start-Sleep -Seconds 2
        }

        Write-Host "  Service stopped." -ForegroundColor Green
    }
    else {
        Write-Host "  Service was not running." -ForegroundColor DarkGray
    }

    return $wasRunning
}

function Start-InstalledService {
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )

    sc.exe start $Name | Out-Null
    Start-Sleep -Seconds 3
    return Get-Service -Name $Name
}

function Test-InstalledWorkspaceHealth {
    param(
        [Parameter(Mandatory)]
        [string]$InstallRoot,
        [Parameter(Mandatory)]
        [int]$Port
    )

    $workspaceChecks = @()
    $workspaceHealthChecked = 0
    $workspaceHealthOk = 0
    $workspaceHealthFailed = 0
    $appSettingsYamlPath = Join-Path $InstallRoot 'appsettings.yaml'
    $appSettingsJsonPath = Join-Path $InstallRoot 'appsettings.json'
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
        Write-Warning "No deployed appsettings.json or appsettings.yaml found at $InstallRoot; skipping workspace health checks."
    }
    else {
        try {
            if ($appSettingsFormat -eq 'json') {
                $deployedSettings = Get-Content -Path $appSettingsPath -Raw | ConvertFrom-Json
            }
            else {
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
            Write-Host "  No workspaces defined in deployed configuration." -ForegroundColor DarkGray
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

    return [pscustomobject]@{
        Checked = $workspaceHealthChecked
        Healthy = $workspaceHealthOk
        Failed  = $workspaceHealthFailed
    }
}

# ---------------------------------------------------------------------------
# Pipeline
# ---------------------------------------------------------------------------

Assert-Elevated
$serviceProcessName = $ExeName.Replace('.exe','')

if ($Restore) {
    Write-Step "0/8  Resolving backup archive ..."
    $restoreArchivePath = Resolve-BackupArchivePath -ArchiveDirectory $ArchiveDir -ExplicitArchivePath $BackupArchive
    Write-Host "  Restore archive: $restoreArchivePath" -ForegroundColor DarkGray

    Write-Step "1/8  Stopping service '$ServiceName' ..."
    Stop-InstalledService -Name $ServiceName -ProcessName $serviceProcessName | Out-Null

    $preRestoreArchivePath = Join-Path $ArchiveDir "McpServer-pre-restore-$Timestamp.zip"
    Write-Step "2/8  Backing up current config and data files ..."
    $backupSummary = Backup-PreservedState -InstallRoot $InstallPath -BackupRoot $BackupDir -ArchivePath $preRestoreArchivePath
    Write-Host "  Data folder: $($backupSummary.ConfiguredDataFolder)" -ForegroundColor DarkGray
    if ($backupSummary.BackedUpData.Count -gt 0) {
        Write-Host "  Backed up data items: $($backupSummary.BackedUpData -join ', ')" -ForegroundColor DarkGray
    }
    if ($backupSummary.BackedUpConfig.Count -gt 0) {
        Write-Host "  Backed up config files: $($backupSummary.BackedUpConfig -join ', ')" -ForegroundColor DarkGray
    }
    if ($backupSummary.ArchivePath) {
        Write-Host "  Archived current state to: $($backupSummary.ArchivePath)" -ForegroundColor DarkGray
    }

    $restoreExtractDir = Join-Path $env:TEMP "McpServer-restore-$Timestamp"
    Write-Step "3/8  Extracting restore archive ..."
    if (Test-Path $restoreExtractDir) {
        Remove-Item -Path $restoreExtractDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Path $restoreExtractDir -Force | Out-Null
    Expand-Archive -Path $restoreArchivePath -DestinationPath $restoreExtractDir -Force

    Write-Step "4/8  Restoring config and data files ..."
    $restoreSummary = Restore-PreservedState -RestoreSource $restoreExtractDir -InstallRoot $InstallPath -PurgeDataFolder
    if ($restoreSummary.RestoredConfig.Count -gt 0) {
        Write-Host "  Restored config files: $($restoreSummary.RestoredConfig -join ', ')" -ForegroundColor DarkGray
    }
    if ($restoreSummary.RestoredData.Count -gt 0) {
        Write-Host "  Restored data folder: $($restoreSummary.RestoredDataFolder)" -ForegroundColor DarkGray
        Write-Host "  Restored data items: $($restoreSummary.RestoredData -join ', ')" -ForegroundColor DarkGray
    }
    if ($restoreSummary.LegacyConfigRemoved.Count -gt 0) {
        Write-Host "  Removed legacy environment config overrides: $($restoreSummary.LegacyConfigRemoved -join ', ')" -ForegroundColor DarkGray
    }

    Write-Step "5/8  Starting service '$ServiceName' ..."
    $svc = Start-InstalledService -Name $ServiceName
    Write-Host "  Service status: $($svc.Status)" -ForegroundColor $(if ($svc.Status -eq 'Running') { 'Green' } else { 'Red' })

    Write-Step "6/8  Verifying health on port $Port ..."
    $primaryHealth = Test-HealthEndpoint -Port $Port -Attempts 10 -TimeoutSeconds 3 -DelaySeconds 2
    $healthy = [bool]$primaryHealth.Healthy
    if ($healthy) {
        Write-Host "  Health: HTTP $($primaryHealth.StatusCode) - $($primaryHealth.Content)" -ForegroundColor Green
    }
    if (-not $healthy) {
        Write-Warning "Service did not respond to health check after 20 seconds."
    }

    Write-Step "7/8  Verifying workspace health checks from deployed config ..."
    $workspaceHealth = Test-InstalledWorkspaceHealth -InstallRoot $InstallPath -Port $Port

    Write-Step "8/8  Cleanup ..."
    Remove-Item $BackupDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $restoreExtractDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Temporary restore directories removed." -ForegroundColor DarkGray

    Write-Host "`n=== Restore complete ===" -ForegroundColor Green
    Write-Host "  Service : $ServiceName ($($svc.Status))"
    Write-Host "  Path    : $InstallPath"
    Write-Host "  Health  : $(if ($healthy) { 'OK' } else { 'FAILED' })"
    Write-Host "  WSHealth: $(if ($workspaceHealth.Failed -eq 0) { 'OK' } else { 'WARN' }) ($($workspaceHealth.Healthy)/$($workspaceHealth.Checked))"
    Write-Host "  Restored: $restoreArchivePath" -ForegroundColor DarkGray
    if ($backupSummary.ArchivePath) {
        Write-Host "  Snapshot: $($backupSummary.ArchivePath)" -ForegroundColor DarkGray
    }
    Write-Host "  Config  : $($restoreSummary.RestoredConfig.Count) restored, $($backupSummary.BackedUpConfig.Count) backed up"
    Write-Host "  Data    : $($restoreSummary.RestoredData.Count) restored item(s), $($backupSummary.BackedUpData.Count) backed up item(s)"
    return
}

# 0. Version bump
if (-not $SkipBuild -and -not $SkipVersionBump) {
    Write-Step "0/8  Bumping GitVersion next-version patch ..."
    $bumpResult = Bump-GitVersionPatch -RepoRoot $RepoRoot
    Write-Host "  $($bumpResult.OldVersion) -> $($bumpResult.NewVersion)" -ForegroundColor Green
}

Write-Step "1/8  Stopping service '$ServiceName' ..."
Stop-InstalledService -Name $ServiceName -ProcessName $serviceProcessName | Out-Null

Write-Step "2/8  Backing up config and data files ..."
$backupSummary = Backup-PreservedState -InstallRoot $InstallPath -BackupRoot $BackupDir -ArchivePath $ArchivePath
Write-Host "  Data folder: $($backupSummary.ConfiguredDataFolder)" -ForegroundColor DarkGray
if ($backupSummary.BackedUpData.Count -gt 0) {
    Write-Host "  Backed up data items: $($backupSummary.BackedUpData -join ', ')" -ForegroundColor DarkGray
}
if ($backupSummary.BackedUpConfig.Count -gt 0) {
    Write-Host "  Backed up config files: $($backupSummary.BackedUpConfig -join ', ')" -ForegroundColor DarkGray
}
else {
    Write-Host "  No config files matched preserve patterns." -ForegroundColor Yellow
}
if ($backupSummary.ArchivePath) {
    Write-Host "  Archived to: $($backupSummary.ArchivePath)" -ForegroundColor DarkGray
}

# 3. Build / Publish
Write-Step "3/8  Publishing new build ..."
if ($SkipBuild) {
    if (-not $PublishSource -or -not (Test-Path $PublishSource)) {
        Write-Error "PublishSource '$PublishSource' not found. Provide a valid path with -SkipBuild."
    }
    if (-not (Test-Path (Join-Path $PublishSource $LauncherExeName))) {
        Write-Warning "PublishSource '$PublishSource' does not contain $LauncherExeName. Desktop launch will not work until the sidecar launcher is deployed."
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
    $stageDir = Join-Path $env:TEMP "McpServer-publish-stage"
    if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
    dotnet publish $ProjectFile -c Release --self-contained -r win-x64 `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        -o $stageDir
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed (exit code $LASTEXITCODE)" }
    Publish-LauncherSidecar -ProjectFile $LauncherProjectFile -DestinationDirectory $stageDir -ExecutableName $LauncherExeName

    Write-Host "  Cleaning stale files before copy ..." -ForegroundColor DarkGray
    $cleanup = Remove-StaleInstallContent -InstallRoot $InstallPath -PublishRoot $stageDir -PreserveFilePatterns $PreservePatterns -PreserveDirNames $PreserveDirectories
    Write-Host "  Removed stale items: $($cleanup.FilesRemoved) file(s), $($cleanup.DirsRemoved) director$(if ($cleanup.DirsRemoved -eq 1) { 'y' } else { 'ies' })" -ForegroundColor DarkGray

    Copy-Item -Path "$stageDir\*" -Destination $InstallPath -Recurse -Force
    Remove-Item $stageDir -Recurse -Force -ErrorAction SilentlyContinue
}
if (-not (Test-Path (Join-Path $InstallPath $LauncherExeName))) {
    Write-Error "Deployment is missing $LauncherExeName under $InstallPath."
}
Write-Host "  Launcher sidecar present: $(Join-Path $InstallPath $LauncherExeName)" -ForegroundColor DarkGray
Write-Host "  Publish complete." -ForegroundColor Green

Write-Step "4/8  Restoring config and data files ..."
$restoreSummary = Restore-PreservedState -RestoreSource $BackupDir -InstallRoot $InstallPath
if ($restoreSummary.RestoredConfig.Count -gt 0) {
    Write-Host "  Restored config files: $($restoreSummary.RestoredConfig -join ', ')" -ForegroundColor DarkGray
}
if ($restoreSummary.RestoredData.Count -gt 0) {
    Write-Host "  Restored data folder: $($restoreSummary.RestoredDataFolder)" -ForegroundColor DarkGray
    Write-Host "  Restored data items: $($restoreSummary.RestoredData -join ', ')" -ForegroundColor DarkGray
}
if ($restoreSummary.LegacyConfigRemoved.Count -gt 0) {
    Write-Host "  Removed legacy environment config overrides: $($restoreSummary.LegacyConfigRemoved -join ', ')" -ForegroundColor DarkGray
}

Write-Step "5/8  Starting service '$ServiceName' ..."
$svc = Start-InstalledService -Name $ServiceName
Write-Host "  Service status: $($svc.Status)" -ForegroundColor $(if ($svc.Status -eq 'Running') { 'Green' } else { 'Red' })

Write-Step "6/8  Verifying health on port $Port ..."
$primaryHealth = Test-HealthEndpoint -Port $Port -Attempts 10 -TimeoutSeconds 3 -DelaySeconds 2
$healthy = [bool]$primaryHealth.Healthy
if ($healthy) {
    Write-Host "  Health: HTTP $($primaryHealth.StatusCode) - $($primaryHealth.Content)" -ForegroundColor Green
}
if (-not $healthy) {
    Write-Warning "Service did not respond to health check after 20 seconds."
}

Write-Step "7/8  Verifying workspace health checks from deployed config ..."
$workspaceHealth = Test-InstalledWorkspaceHealth -InstallRoot $InstallPath -Port $Port

Write-Step "8/8  Cleanup ..."
Remove-Item $BackupDir -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  Backup directory removed." -ForegroundColor DarkGray

Write-Host "`n=== Update complete ===" -ForegroundColor Green
Write-Host "  Service : $ServiceName ($($svc.Status))"
Write-Host "  Path    : $InstallPath"
Write-Host "  Health  : $(if ($healthy) { 'OK' } else { 'FAILED' })"
Write-Host "  WSHealth: $(if ($workspaceHealth.Failed -eq 0) { 'OK' } else { 'WARN' }) ($($workspaceHealth.Healthy)/$($workspaceHealth.Checked))"
Write-Host "  Config  : $($restoreSummary.RestoredConfig.Count) restored, $($backupSummary.BackedUpConfig.Count) backed up"
Write-Host "  Data    : $($restoreSummary.RestoredData.Count) restored item(s), $($backupSummary.BackedUpData.Count) backed up item(s)"
if ($backupSummary.ArchivePath) {
    Write-Host "  Archive : $($backupSummary.ArchivePath)" -ForegroundColor DarkGray
}
