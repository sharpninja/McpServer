$ErrorActionPreference = 'Continue'
Write-Output '=== PWSH COMMAND ==='
Get-Command pwsh -All | ForEach-Object { $_.CommandType.ToString() + ' ' + $_.Source + ' ' + $_.Definition }
Write-Output '=== DOTNET ==='
Get-Command dotnet -ErrorAction SilentlyContinue | ForEach-Object { $_.Source }
Write-Output ('DOTNET_ROOT=' + $env:DOTNET_ROOT)
Write-Output '=== WHERE ==='
where.exe pwsh
where.exe dotnet
