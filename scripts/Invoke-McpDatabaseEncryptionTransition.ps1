[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Enable', 'Disable', 'Verify')]
    [string]$Operation,

    [string]$Instance,

    [switch]$Execute,

    [string]$BackupPath,

    [string]$ReportPath,

    [string]$CurrentKey,

    [string]$TargetKey,

    [string]$SqliteSeeToolPath,

    [string]$PostgreSqlDumpToolPath,

    [int]$SqlServerTimeoutSeconds = 600,

    [string[]]$AdditionalArgument = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src/McpServer.Support.Mcp/McpServer.Support.Mcp.csproj'

$arguments = [System.Collections.Generic.List[string]]::new()
$arguments.Add('run')
$arguments.Add('--project')
$arguments.Add($projectPath)
$arguments.Add('--')
$arguments.Add('--database-encryption-transition')
$arguments.Add($Operation.ToLowerInvariant())

if ($Instance) {
    $arguments.Add('--instance')
    $arguments.Add($Instance)
}

if ($Execute.IsPresent) {
    $arguments.Add('--execute')
}

if ($BackupPath) {
    $arguments.Add('--backup-path')
    $arguments.Add($BackupPath)
}

if ($ReportPath) {
    $arguments.Add('--report-path')
    $arguments.Add($ReportPath)
}

if ($CurrentKey) {
    $arguments.Add('--current-key')
    $arguments.Add($CurrentKey)
}

if ($TargetKey) {
    $arguments.Add('--target-key')
    $arguments.Add($TargetKey)
}

if ($SqliteSeeToolPath) {
    $arguments.Add('--sqlite-see-tool-path')
    $arguments.Add($SqliteSeeToolPath)
}

if ($PostgreSqlDumpToolPath) {
    $arguments.Add('--postgres-dump-tool-path')
    $arguments.Add($PostgreSqlDumpToolPath)
}

if ($SqlServerTimeoutSeconds -gt 0) {
    $arguments.Add('--sqlserver-timeout-seconds')
    $arguments.Add($SqlServerTimeoutSeconds.ToString([System.Globalization.CultureInfo]::InvariantCulture))
}

foreach ($item in $AdditionalArgument) {
    $arguments.Add($item)
}

$display = "dotnet $($arguments -join ' ')"
if ($PSCmdlet.ShouldProcess($projectPath, "Run database encryption transition: $Operation")) {
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Database encryption transition command failed with exit code $LASTEXITCODE."
    }
}
