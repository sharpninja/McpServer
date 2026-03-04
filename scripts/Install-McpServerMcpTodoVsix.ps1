#Requires -Version 5.1
# Install fwh-mcp-todo *.vsix from extensions\McpServer-mcp-todo into VS Code and Cursor.
$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$extDir = Join-Path $repoRoot "extensions\McpServer-mcp-todo"
$vsixPath = Get-ChildItem -Path $extDir -Filter "fwh-mcp-todo-*.vsix" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName

if (-not $vsixPath -or -not (Test-Path $vsixPath)) {
    Write-Error "VSIX not found in $extDir. Run: cd extensions\McpServer-mcp-todo; npm run compile; npx @vscode/vsce package"
    exit 1
}

# Extract target dir name: fwh-mcp-todo-0.8.1.vsix -> FunWasHad.fwh-mcp-todo-0.8.1
$vsixName = [System.IO.Path]::GetFileNameWithoutExtension($vsixPath)
$extractTarget = "FunWasHad.$vsixName"

# Uninstall existing, then remove leftover dirs so --install-extension does not hit ScanningExtension errors
$codeCmd = Get-Command code -ErrorAction SilentlyContinue
$cursorCmd = Get-Command cursor -ErrorAction SilentlyContinue
if ($codeCmd) { & code --uninstall-extension FunWasHad.fwh-mcp-todo 2>$null; Start-Sleep -Milliseconds 800 }
if ($cursorCmd) { & cursor --uninstall-extension FunWasHad.fwh-mcp-todo 2>$null; Start-Sleep -Milliseconds 800 }

# Remove any existing fwh-mcp-todo extension dirs (any version)
$vscodeExtBase = Join-Path $env:USERPROFILE ".vscode\extensions"
$cursorExtBase = Join-Path $env:USERPROFILE ".cursor\extensions"
$extDirs = @(
    (Get-ChildItem -Path $vscodeExtBase -Directory -Filter "funwashad.fwh-mcp-todo-*" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName),
    (Get-ChildItem -Path $vscodeExtBase -Directory -Filter "FunWasHad.fwh-mcp-todo-*" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName),
    (Get-ChildItem -Path $cursorExtBase -Directory -Filter "funwashad.fwh-mcp-todo-*" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName),
    (Get-ChildItem -Path $cursorExtBase -Directory -Filter "FunWasHad.fwh-mcp-todo-*" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)
) | Where-Object { $_ }
foreach ($d in $extDirs) {
    if (Test-Path $d) {
        Write-Host "Removing leftover extension dir: $d" -ForegroundColor Yellow
        Remove-Item $d -Recurse -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 200
    }
}

Write-Host "Installing McpServer MCP Todo from $vsixPath" -ForegroundColor Cyan

$vscodeExtDir = Join-Path $env:USERPROFILE ".vscode\extensions"
$cursorExtDir = Join-Path $env:USERPROFILE ".cursor\extensions"

function Install-VsixByExtract {
    param([string]$extensionsDir)
    $targetDir = Join-Path $extensionsDir $extractTarget
    if (Test-Path $targetDir) { Remove-Item $targetDir -Recurse -Force }
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($vsixPath, $targetDir)
    $inner = Join-Path $targetDir "extension"
    if (Test-Path $inner) {
        Get-ChildItem -Path $inner -Force | Move-Item -Destination $targetDir -Force
        Remove-Item $inner -Force -ErrorAction SilentlyContinue
    }
    @("[Content_Types].xml", "_rels", "extension.vsixmanifest") | ForEach-Object {
        $p = Join-Path $targetDir $_
        if (Test-Path $p) { Remove-Item $p -Recurse -Force -ErrorAction SilentlyContinue }
    }
    Write-Host "  Installed to $targetDir" -ForegroundColor Green
}

if ($codeCmd) {
    Write-Host "Installing into VS Code..." -ForegroundColor Cyan
    & code --install-extension $vsixPath --force 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  CLI failed; extracting VSIX to VS Code extensions dir." -ForegroundColor Yellow
        Install-VsixByExtract -extensionsDir $vscodeExtDir
    }
} else {
    Write-Warning "VS Code CLI (code) not in PATH; skip VS Code install."
}

if ($cursorCmd) {
    Write-Host "Installing into Cursor..." -ForegroundColor Cyan
    & cursor --install-extension $vsixPath --force 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  CLI failed; extracting VSIX to Cursor extensions dir." -ForegroundColor Yellow
        Install-VsixByExtract -extensionsDir $cursorExtDir
    }
} else {
    Write-Warning "Cursor CLI (cursor) not in PATH; skip Cursor install."
}

Write-Host "Done. Reload the editor window to use the updated extension (Ctrl+Shift+P -> Developer: Reload Window)." -ForegroundColor Green
