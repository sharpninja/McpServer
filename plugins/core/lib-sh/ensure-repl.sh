#!/usr/bin/env bash
set -euo pipefail

# Check if already installed
if command -v qbrain-ai-repl >/dev/null 2>&1; then
    exit 0
fi

# Verify prerequisites
if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet CLI not found. Install .NET 9.0 SDK." >&2
    exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
    echo "ERROR: gh CLI not found. Install GitHub CLI and authenticate." >&2
    exit 1
fi

# Download NuGet package from GitHub release
TMPDIR="${TMPDIR:-/tmp}/qbrain-ai-repl-$$"
mkdir -p "$TMPDIR"
trap 'rm -rf "$TMPDIR"' EXIT

echo "Downloading QBrainAi REPL tool..." >&2
if ! gh release download --repo sharpninja/McpServer --pattern "QBrainAI.Repl.*.nupkg" --dir "$TMPDIR" 2>/dev/null; then
    echo "ERROR: Failed to download REPL NuGet package from GitHub releases." >&2
    exit 1
fi

# Install globally from local package
echo "Installing qbrain-ai-repl globally..." >&2
if ! dotnet tool install --global --add-source "$TMPDIR" QBrainAI.Repl 2>/dev/null; then
    # Try update if already installed but not on PATH
    dotnet tool update --global --add-source "$TMPDIR" QBrainAI.Repl 2>/dev/null || {
        echo "ERROR: Failed to install qbrain-ai-repl." >&2
        exit 1
    }
fi

# Verify
if command -v qbrain-ai-repl >/dev/null 2>&1; then
    echo "qbrain-ai-repl installed successfully." >&2
    exit 0
else
    echo "ERROR: qbrain-ai-repl installed but not found on PATH." >&2
    exit 1
fi
