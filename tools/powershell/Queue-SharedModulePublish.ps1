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
#   1. Set the secrets once in your local environment (PowerShell session, user profile, etc.):
#        $env:PSGALLERY_API_KEY = "your-psgallery-key-here"
#        $env:NPM_API_KEY       = "your-npm-token-here"
#
#   2. Run this script:
#        pwsh -File tools/powershell/Queue-SharedModulePublish.ps1
#
#   The actual secret values will NEVER appear in the script, git history,
#   pipeline logs (unless you explicitly log them), or this repo.

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$psGalleryKey = $env:PSGALLERY_API_KEY
$npmKey       = $env:NPM_API_KEY

if ([string]::IsNullOrWhiteSpace($psGalleryKey)) {
    Write-Error "Environment variable PSGALLERY_API_KEY is not set. Aborting."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($npmKey)) {
    Write-Error "Environment variable NPM_API_KEY is not set. Aborting."
    exit 1
}

Write-Host "Queueing manual build on main with publish secrets..." -ForegroundColor Cyan
Write-Host "Secrets will be passed at queue time and will not be echoed." -ForegroundColor DarkGray

try {
    $result = az pipelines build queue `
        --org https://dev.azure.com/McpServer `
        --project McpServer `
        --definition-id 1 `
        --branch main `
        --variables "PSGalleryApiKey=$psGalleryKey" "NPM_API_KEY=$npmKey" `
        --output json 2>&1 | ConvertFrom-Json

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
    } else {
        Write-Error "Failed to queue build. az pipelines returned no result."
    }
}
catch {
    Write-Error "Failed to queue build: $_"
    exit 1
}
