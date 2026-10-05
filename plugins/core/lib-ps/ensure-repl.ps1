$ErrorActionPreference = 'Stop'

if (Get-Command qbrain-ai-repl -ErrorAction SilentlyContinue) { exit 0 }
if (Get-Command mcpserver-repl -ErrorAction SilentlyContinue) { exit 0 }

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "gh CLI not found. Install GitHub CLI to auto-install qbrain-ai-repl."
    exit 1
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet CLI not found. Install .NET 9+ SDK."
    exit 1
}

$tmpDir = Join-Path ([System.IO.Path]::GetTempPath()) "qbrain-ai-repl-$PID"
New-Item -ItemType Directory -Path $tmpDir -Force | Out-Null

try {
    gh release download --repo sharpninja/McpServer --pattern "QBrainAI.Repl.*.nupkg" --dir $tmpDir
    $nupkg = Get-ChildItem -Path $tmpDir -Filter "QBrainAI.Repl.*.nupkg" | Select-Object -First 1
    if (-not $nupkg) { Write-Error "No .nupkg found after download."; exit 1 }

    dotnet tool install --global --add-source $tmpDir QBrainAI.Repl
    if (-not (Get-Command qbrain-ai-repl -ErrorAction SilentlyContinue)) {
        Write-Error "qbrain-ai-repl not on PATH after install."
        exit 1
    }
    Write-Host "qbrain-ai-repl installed successfully."
} finally {
    Remove-Item -Path $tmpDir -Recurse -Force -ErrorAction SilentlyContinue
}
