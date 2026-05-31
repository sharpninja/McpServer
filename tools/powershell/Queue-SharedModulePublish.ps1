# Queue-SharedModulePublish.ps1
#
# Purpose:
#   Queue a manual build on main that includes the two publishing secrets
#   (PSGalleryApiKey and NPM_API_KEY) so the publish_shared_modules job
#   can actually publish.
#
# Security:
#   - Read secrets ONLY from local environment variables.
#   - Never hardcode, echo, or store the actual values.
#   - The values are passed only to the az CLI at queue time.
#
# Usage:
#   1. Set the secrets once in your local environment using these exact names:
#        $env:PSGalleryApiKey = "your-psgallery-key-here"
#        $env:NPM_API_KEY     = "your-npm-token-here"
#
#   2. Run this script:
#        pwsh -File tools/powershell/Queue-SharedModulePublish.ps1
#
#   The actual secret values will NEVER appear in the script, git history,
#   pipeline logs (unless you explicitly log them), or this repo.

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$psGalleryKey = $env:PSGalleryApiKey
$npmKey       = $env:NPM_API_KEY

if ([string]::IsNullOrWhiteSpace($psGalleryKey)) {
    Write-Error "Environment variable PSGalleryApiKey is not set. Aborting."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($npmKey)) {
    Write-Error "Environment variable NPM_API_KEY is not set. Aborting."
    exit 1
}

# Pre-flight checks
Write-Host "Checking for Azure CLI..." -ForegroundColor Cyan
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI ('az') not found in PATH. Please install the Azure CLI first."
    exit 1
}
Write-Host "  Azure CLI found." -ForegroundColor Green

Write-Host "Checking Azure login status..." -ForegroundColor Cyan
Write-Host "  (If this hangs, run 'az login' or 'az login --use-device-code' in another terminal first.)" -ForegroundColor DarkYellow

# Use get-access-token instead of account show - it's lighter and less likely to trigger interactive login
$loginCheck = az account get-access-token --query "expiresOn" -o tsv 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "You are not logged in to Azure CLI (or token expired)."
    Write-Error "Please run one of these in another terminal and try again:"
    Write-Error "   az login"
    Write-Error "   az login --use-device-code   (good for headless/remote sessions)"
    Write-Error $loginCheck
    exit 1
}
Write-Host "  Azure login token appears valid (expires: $loginCheck)" -ForegroundColor Green

Write-Host "Queueing manual build on main with publish secrets..." -ForegroundColor Cyan
Write-Host "Secrets will be passed at queue time and will not be echoed." -ForegroundColor DarkGray
Write-Host "Calling 'az pipelines build queue' (this can take 10-30 seconds or hang if login is required)..." -ForegroundColor Cyan

$queueOutput = az pipelines build queue `
    --org https://dev.azure.com/McpServer `
    --project McpServer `
    --definition-id 1 `
    --branch main `
    --variables "PSGalleryApiKey=$psGalleryKey" "NPM_API_KEY=$npmKey" `
    --output json 2>&1

$exitCode = $LASTEXITCODE
Write-Host "  az command completed with exit code $exitCode." -ForegroundColor $(if ($exitCode -eq 0) { 'Green' } else { 'Red' })

if ($exitCode -ne 0) {
    Write-Host "`n=== Azure CLI Error ===" -ForegroundColor Red
    Write-Host $queueOutput -ForegroundColor Red
    Write-Host "=======================`n" -ForegroundColor Red

    $errorFile = Join-Path $env:TEMP "az-queue-error.txt"
    $queueOutput | Out-File -FilePath $errorFile -Encoding UTF8
    Write-Host "Full error also saved to: $errorFile" -ForegroundColor Yellow

    Write-Error "Failed to queue build (az exit code $exitCode). See the output above."
    exit 1
}

try {
    $result = $queueOutput | ConvertFrom-Json -ErrorAction Stop
} catch {
    Write-Error "Azure CLI returned output that could not be parsed as JSON."
    Write-Error "Raw output:"
    Write-Error $queueOutput
    exit 1
}

if ($result) {
    Write-Host "`nBuild queued successfully!" -ForegroundColor Green
    Write-Host "Build ID     : $($result.id)"
    Write-Host "Build Number : $($result.buildNumber)"
    Write-Host "Status       : $($result.status)"
    Write-Host ""
    Write-Host "Direct link  :" -ForegroundColor Green
    Write-Host "https://dev.azure.com/McpServer/McpServer/_build/results?buildId=$($result.id)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "In the new build, look for the 'Validate publish secrets' step first." -ForegroundColor Yellow
    Write-Host "It should now show both keys as PRESENT." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "After the build completes, verify with:" -ForegroundColor Cyan
    Write-Host "  Find-Module McpRepl -AllVersions" -ForegroundColor White
    Write-Host "  npm view @sharpninja/mcp-repl versions" -ForegroundColor White
} else {
    Write-Error "Failed to queue build. az pipelines returned no result."
    exit 1
}
