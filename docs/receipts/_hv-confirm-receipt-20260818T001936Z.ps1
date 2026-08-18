#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$paths = @(
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T001936Z.md',
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T001936Z.json'
)
foreach ($path in $paths) {
    $item = Get-Item -LiteralPath $path
    Write-Output ('Name=' + $item.Name)
    Write-Output ('Len=' + $item.Length)
    Write-Output ('Utc=' + $item.LastWriteTimeUtc.ToString('o'))
}
$j = Get-Content -LiteralPath $paths[1] -Raw | ConvertFrom-Json
Write-Output ('JSON.OverallVerdict=' + $j.OverallVerdict)
Write-Output ('JSON.FailListCount=' + @($j.FailList).Count)
Write-Output ('JSON.SessionId=' + $j.SessionId)
Write-Output ('JSON.ServerTurnId=' + $j.ServerTurnId)
Write-Output ('JSON.AddProfile=' + $j.AddProfileExecuted)
Write-Output ('JSON.ProfileFilesRead=' + $j.ProfileFilesRead)
