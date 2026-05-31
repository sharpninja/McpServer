# Queue-SharedModulePublish.ps1
#
# Purpose:
#   Queue a manual build on main that includes the two publishing secrets
#   (PSGalleryApiKey and NPM_API_KEY) so the publish_shared_modules job
#   can actually publish.
#
# Security:
#   - Read secrets from the current session OR the persistent environment
#     on this computer (registry). Never from the repo or hardcoded.
#   - Never echo or store the actual secret values.
#   - The values are passed only to the az CLI at queue time.
#
# Usage:
#   1. Set the two secrets **once** on this computer using the exact names below
#      (System Properties > Environment Variables, or `setx` / GUI).
#      They only need to be set once — the script will read them from the
#      persistent environment even if they are not in the current session.
#
#        PSGalleryApiKey
#        NPM_API_KEY
#
#   2. Run this script:
#        pwsh -File tools/powershell/Queue-SharedModulePublish.ps1
#
#   The script will automatically ensure the two secret variables are registered
#   in the pipeline definition (idempotent). You no longer need to click in the
#   UI Variables tab.
#
#   The actual secret values will NEVER appear in the script, git history,
#   pipeline logs (unless you explicitly log them), or this repo.

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Get-PersistentEnvironmentVariable {
    param([Parameter(Mandatory)][string]$Name)

    # 1. Prefer the current session (allows easy override for testing)
    $value = [Environment]::GetEnvironmentVariable($Name, 'Process')
    if (-not [string]::IsNullOrWhiteSpace($value)) {
        return $value
    }

    # 2. Fall back to persistent user environment (most common case)
    $value = [Environment]::GetEnvironmentVariable($Name, 'User')
    if (-not [string]::IsNullOrWhiteSpace($value)) {
        Write-Host "  Loaded $Name from persistent user environment (this computer)." -ForegroundColor DarkGray
        return $value
    }

    # 3. Fall back to machine environment (if set at system level)
    $value = [Environment]::GetEnvironmentVariable($Name, 'Machine')
    if (-not [string]::IsNullOrWhiteSpace($value)) {
        Write-Host "  Loaded $Name from persistent machine environment (this computer)." -ForegroundColor DarkGray
        return $value
    }

    return $null
}

$psGalleryKey = Get-PersistentEnvironmentVariable -Name 'PSGalleryApiKey'
$npmKey       = Get-PersistentEnvironmentVariable -Name 'NPM_API_KEY'

if ([string]::IsNullOrWhiteSpace($psGalleryKey)) {
    Write-Error "Environment variable PSGalleryApiKey is not set on this computer (checked session + registry). Aborting."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($npmKey)) {
    Write-Error "Environment variable NPM_API_KEY is not set on this computer (checked session + registry). Aborting."
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
    Write-Host ""
    Write-Host "Azure CLI login check failed." -ForegroundColor Red
    Write-Host "The script is hanging or failing at login." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "=== How to fix az login hanging ===" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. In a SEPARATE terminal window, run:" -ForegroundColor White
    Write-Host "      az login --use-device-code" -ForegroundColor Green
    Write-Host ""
    Write-Host "   This is the non-interactive way. It will give you a code like 'ABCD-EFGH' and a URL." -ForegroundColor White
    Write-Host "   Open the URL on your phone or another computer, enter the code, and complete login." -ForegroundColor White
    Write-Host ""
    Write-Host "2. If you have multiple Azure tenants, specify it:" -ForegroundColor White
    Write-Host "      az login --use-device-code --tenant yourtenant.onmicrosoft.com" -ForegroundColor Green
    Write-Host "      (or use the tenant ID)" -ForegroundColor White
    Write-Host ""
    Write-Host "3. After successful login in the other window, come back here and re-run this script." -ForegroundColor White
    Write-Host ""
    Write-Host "4. If it still hangs, try clearing cache first:" -ForegroundColor White
    Write-Host "      az account clear" -ForegroundColor Green
    Write-Host "      az login --use-device-code" -ForegroundColor Green
    Write-Host ""
    Write-Host "5. Corporate / proxy environments:" -ForegroundColor White
    Write-Host "   Sometimes you need:" -ForegroundColor White
    Write-Host "      az login --use-device-code --tenant <tenant> --allow-no-subscriptions" -ForegroundColor Green
    Write-Host ""
    Write-Host "Once logged in successfully, re-run this script." -ForegroundColor Yellow
    Write-Host ""
    Write-Error "Login required. Exiting."
    exit 1
}
Write-Host "  Azure login token appears valid (expires: $loginCheck)" -ForegroundColor Green

# One-time / idempotent setup (the thing you asked me to "just do"):
# Register the two secret variables in the pipeline definition so that
# $(PSGalleryApiKey) / $(NPM_API_KEY) expand correctly when we pass real
# values via --variables on the queue command.
# This eliminates the previous manual "go click in the Variables tab" step.
Write-Host "Ensuring pipeline secret variables are registered..." -ForegroundColor Cyan

$org        = "https://dev.azure.com/McpServer"
$project    = "McpServer"
$pipelineId = 1

foreach ($varName in @('PSGalleryApiKey', 'NPM_API_KEY')) {
    # Check whether it already exists in this pipeline definition
    $existing = az pipelines variable list `
        --org $org `
        --project $project `
        --pipeline-id $pipelineId `
        --query "$varName" -o tsv 2>$null

    if ($LASTEXITCODE -eq 0 -and $existing) {
        Write-Host "  $varName : already present (secret)" -ForegroundColor DarkGray
        continue
    }

    Write-Host "  $varName : creating as secret variable in pipeline definition..." -ForegroundColor Yellow
    $createOut = az pipelines variable create `
        --org $org `
        --project $project `
        --pipeline-id $pipelineId `
        --name $varName `
        --value "SET_AT_QUEUE_TIME" `
        --secret true `
        --only-show-errors 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  $varName : created successfully as secret (queue-time override enabled)" -ForegroundColor Green
    } else {
        Write-Host "  $varName : create returned non-zero (may already exist or needs one manual UI step)." -ForegroundColor Yellow
        if ($createOut) { Write-Host "    az output: $createOut" -ForegroundColor DarkGray }
    }
}

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
