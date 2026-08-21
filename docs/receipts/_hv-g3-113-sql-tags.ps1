#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-g3-113-post-deploy'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$yamlPath = 'C:\ProgramData\McpServer\appsettings.yaml'
$yaml = Get-Content -LiteralPath $yamlPath -Raw
$csMatch = [regex]::Match($yaml, '(?m)^\s+ConnectionString:\s+(Server=.+)$')
if (-not $csMatch.Success) {
    throw 'ConnectionString not found in deployed appsettings.yaml'
}
$cs = $csMatch.Groups[1].Value.Trim()
$server = [regex]::Match($cs, 'Server=([^;]+)').Groups[1].Value
$database = [regex]::Match($cs, 'Database=([^;]+)').Groups[1].Value
$user = [regex]::Match($cs, 'User Id=([^;]+)').Groups[1].Value
$password = [regex]::Match($cs, 'Password=([^;]+)').Groups[1].Value
$sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE'
$sessionId = 'GrokCode-20260820T071556Z-hv113tags'

function Invoke-Sql([string]$query, [string]$outFile) {
    $args = @(
        '-S', $server
        '-d', $database
        '-U', $user
        '-P', $password
        '-W'
        '-h', '-1'
        '-s', '|'
        '-Q', $query
    )
    $output = & $sqlcmd @args
    $output | Set-Content -LiteralPath $outFile -Encoding utf8
    return $output
}

$tableOut = Invoke-Sql "SET NOCOUNT ON; SELECT CASE WHEN OBJECT_ID(N'dbo.SessionLogTags', N'U') IS NULL THEN 'MISSING' ELSE 'EXISTS' END;" (Join-Path $outDir 'sql-table.txt')
$sessionOut = Invoke-Sql "SET NOCOUNT ON; SELECT CAST(Id AS varchar(20)), SessionId, WorkspaceId FROM dbo.SessionLogs WHERE SessionId = N'$sessionId';" (Join-Path $outDir 'sql-session.txt')
$migOut = Invoke-Sql "SET NOCOUNT ON; SELECT TOP 15 MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId DESC;" (Join-Path $outDir 'sql-migrations.txt')
$tagOut = Invoke-Sql "SET NOCOUNT ON; IF OBJECT_ID(N'dbo.SessionLogTags', N'U') IS NULL SELECT 'NO_TABLE'; ELSE SELECT CAST(COUNT(*) AS varchar(20)) FROM dbo.SessionLogTags t INNER JOIN dbo.SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = N'$sessionId';" (Join-Path $outDir 'sql-tag-count.txt')
$colOut = Invoke-Sql "SET NOCOUNT ON; IF OBJECT_ID(N'dbo.SessionLogTags', N'U') IS NULL SELECT 'NO_TABLE'; ELSE SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SessionLogTags' ORDER BY ORDINAL_POSITION;" (Join-Path $outDir 'sql-tag-columns.txt')

Write-Output ('TABLE=' + (($tableOut | Select-Object -First 1).ToString().Trim()))
Write-Output ('SESSION=' + (($sessionOut | Select-Object -First 1).ToString().Trim()))
Write-Output ('TAG_COUNT=' + (($tagOut | Select-Object -First 1).ToString().Trim()))
Write-Output ('COLUMNS=' + (($colOut | Where-Object { $_ } | ForEach-Object { $_.ToString().Trim() }) -join ','))
Write-Output ('MIGRATIONS=' + (($migOut | Where-Object { $_ } | ForEach-Object { $_.ToString().Trim() }) -join ','))
