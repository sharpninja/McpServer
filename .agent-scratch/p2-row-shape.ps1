$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$audit = Get-Content -LiteralPath (Join-Path $wt 'docs\receipts\hv\20261002T203600Z-trace-audit.json') -Raw | ConvertFrom-Json
$row = @($audit.p2Rows)[0]
Write-Output 'ROW PROPS:'
$row.PSObject.Properties.Name | ForEach-Object { Write-Output $_ }
Write-Output '--- FIRST ROW ---'
($row | ConvertTo-Json -Depth 5).Substring(0, [Math]::Min(1500, (($row | ConvertTo-Json -Depth 5).Length)))
Write-Output '--- IDS ---'
@($audit.p2Rows) | ForEach-Object {
    $name = $_.PSObject.Properties.Name | Select-Object -First 1
    $idProp = $_.PSObject.Properties | Where-Object { $_.Name -match 'id|Id|criterion' } | Select-Object -First 3
    $bits = @()
    foreach ($p in @($_.PSObject.Properties)) {
        if ($p.Name -match 'id|Id|key|Key|name|Name') {
            $bits += ($p.Name + '=' + [string]$p.Value)
        }
    }
    Write-Output ($bits -join ' | ')
}
