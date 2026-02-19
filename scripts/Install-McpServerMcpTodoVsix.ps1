#Requires -Version 5.1
# Install fwh-mcp-todo-0.1.0.vsix into VS Code and Cursor.
$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$vsixPath = Join-Path $repoRoot "extensions\fwh-mcp-todo\fwh-mcp-todo-0.1.0.vsix"

if (-not (Test-Path $vsixPath)) {
    Write-Error "VSIX not found: $vsixPath. Run: cd extensions\fwh-mcp-todo; npm run compile; npx @vscode/vsce package"
    exit 1
}

# Uninstall existing, then remove leftover dirs so --install-extension does not hit ScanningExtension errors
$codeCmd = Get-Command code -ErrorAction SilentlyContinue
$cursorCmd = Get-Command cursor -ErrorAction SilentlyContinue
if ($codeCmd) { & code --uninstall-extension FunWasHad.fwh-mcp-todo 2>$null; Start-Sleep -Milliseconds 800 }
if ($cursorCmd) { & cursor --uninstall-extension FunWasHad.fwh-mcp-todo 2>$null; Start-Sleep -Milliseconds 800 }

$extDirs = @(
    (Join-Path $env:USERPROFILE ".vscode\extensions\funwashad.fwh-mcp-todo-0.1.0"),
    (Join-Path $env:USERPROFILE ".vscode\extensions\FunWasHad.fwh-mcp-todo-0.1.0"),
    (Join-Path $env:USERPROFILE ".cursor\extensions\funwashad.fwh-mcp-todo-0.1.0"),
    (Join-Path $env:USERPROFILE ".cursor\extensions\FunWasHad.fwh-mcp-todo-0.1.0")
)
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
$extractTarget = "FunWasHad.fwh-mcp-todo-0.1.0"

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
