# Deploy-McpTodoExtension.ps1
# Run AFTER closing Visual Studio.
# Copies the rebuilt DLL+pkgdef, clears caches, and runs /updateconfiguration.

$ErrorActionPreference = "Stop"

$devenv = Get-Process devenv -ErrorAction SilentlyContinue
if ($devenv) {
    Write-Host "Visual Studio is currently running (PID: $($devenv.Id -join ', '))." -ForegroundColor Yellow
    $choice = Read-Host "Kill Visual Studio and continue? (y/n)"
    if ($choice -eq 'y' -or $choice -eq 'Y') {
        foreach ($proc in $devenv) {
            Write-Host "Stopping devenv PID $($proc.Id)..."
            Stop-Process -Id $proc.Id -Force
        }
        Start-Sleep -Seconds 3
    } else {
        Write-Host "Cancelled."
        exit 1
    }
}

$instDir = "C:\Users\kingd\AppData\Local\Microsoft\VisualStudio\18.0_3667cb05\Extensions\5aepmlfh.rpt"
$srcDir  = "E:\github\FunWasHad\src\McpServer.VsExtension.McpTodo.Vsix\bin\Debug"

Write-Host "Copying DLL..."
Copy-Item "$srcDir\McpServer.VsExtension.McpTodo.dll" "$instDir\McpServer.VsExtension.McpTodo.dll" -Force

Write-Host "Copying pkgdef..."
Copy-Item "$srcDir\McpServer.VsExtension.McpTodo.pkgdef" "$instDir\McpServer.VsExtension.McpTodo.pkgdef" -Force

Write-Host "Copying all runtime dependency DLLs..."
Get-ChildItem "$srcDir\*.dll" | ForEach-Object {
    Copy-Item $_.FullName "$instDir\$($_.Name)" -Force
}
Write-Host "  Copied $((Get-ChildItem "$srcDir\*.dll").Count) DLLs"

Write-Host "Clearing ComponentModelCache..."
$cmc = "C:\Users\kingd\AppData\Local\Microsoft\VisualStudio\18.0_3667cb05\ComponentModelCache"
if (Test-Path $cmc) { Remove-Item $cmc -Recurse -Force }

Write-Host "Running devenv /updateconfiguration..."
$proc = Start-Process "G:\VS\VS2026\Common7\IDE\devenv.exe" -ArgumentList "/updateconfiguration" -Wait -PassThru
Write-Host "Exit: $($proc.ExitCode)"

Write-Host ""
Write-Host "DONE. Open Visual Studio now."
