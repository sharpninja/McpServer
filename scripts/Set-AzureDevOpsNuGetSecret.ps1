<#
.SYNOPSIS
    Pushes the local NUGET_API_KEY environment variable into the Azure DevOps
    McpServer pipeline as a secret variable.

.DESCRIPTION
    Reads $env:NUGET_API_KEY from the current shell and writes it to the
    pipeline-level secret variable named NUGET_API_KEY on the McpServer
    pipeline (id 1) in the McpServer/McpServer org/project.

    If the variable already exists, it is updated. If it does not exist, it is
    created. The variable is marked secret so it is masked in logs and cannot
    be retrieved by subsequent reads.

    The script does not log the key value. It only confirms whether the API
    accepted the update.

.PARAMETER PipelineId
    Optional override for the pipeline id. Defaults to 1 (the McpServer
    pipeline).

.PARAMETER Organization
    Optional override for the Azure DevOps organization URL. Defaults to the
    `az devops configure -l` default, which is
    https://dev.azure.com/McpServer.

.PARAMETER Project
    Optional override for the Azure DevOps project name. Defaults to McpServer.

.EXAMPLE
    $env:NUGET_API_KEY = '<your nuget.org key>'
    .\scripts\Set-AzureDevOpsNuGetSecret.ps1

.NOTES
    Requires:
      - Azure CLI logged in with rights to manage pipeline variables in the
        McpServer project (`az login` then `az devops login` if needed).
      - The azure-devops extension installed (`az extension add -n azure-devops`).
#>
[CmdletBinding()]
param(
    [int]$PipelineId = 1,
    [string]$Organization = 'https://dev.azure.com/McpServer',
    [string]$Project = 'McpServer'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$variableName = 'NUGET_API_KEY'

# 1. Confirm the local env var is set and non-empty without ever echoing it.
$key = $env:NUGET_API_KEY
if ([string]::IsNullOrWhiteSpace($key)) {
    throw "Local environment variable NUGET_API_KEY is not set. Set it in this shell (e.g. `\$env:NUGET_API_KEY = '<your key>'`) and re-run."
}
$keyLength = $key.Length
Write-Host "Found local NUGET_API_KEY (length=$keyLength); value will not be logged."

# 2. Confirm Azure CLI + devops extension are available.
$azVersion = (az --version 2>$null | Select-Object -First 1)
if (-not $azVersion) {
    throw "Azure CLI ('az') is not available on PATH. Install from https://aka.ms/azure-cli."
}
$devops = az extension show --name azure-devops --only-show-errors 2>$null
if (-not $devops) {
    Write-Host 'azure-devops extension missing; installing.'
    az extension add --name azure-devops --only-show-errors | Out-Null
}

# 3. Decide whether to create or update.
Write-Host "Querying existing pipeline variables for pipeline $PipelineId ..."
$existingJson = az pipelines variable list `
    --pipeline-id $PipelineId `
    --org $Organization `
    --project $Project `
    --only-show-errors 2>$null
if ($LASTEXITCODE -ne 0) {
    throw "az pipelines variable list failed. Check that you can reach $Organization/$Project and pipeline id $PipelineId exists."
}

$existing = if ([string]::IsNullOrWhiteSpace($existingJson)) { @{} } else { $existingJson | ConvertFrom-Json -AsHashtable }
$exists = $existing.PSObject.Properties.Name -contains $variableName -or ($existing.ContainsKey -and $existing.ContainsKey($variableName))
if ($existing -is [hashtable]) {
    $exists = $existing.ContainsKey($variableName)
}

if ($exists) {
    Write-Host "Variable '$variableName' already exists; updating secret value."
    az pipelines variable update `
        --pipeline-id $PipelineId `
        --org $Organization `
        --project $Project `
        --name $variableName `
        --value $key `
        --secret true `
        --only-show-errors | Out-Null
}
else {
    Write-Host "Variable '$variableName' not found; creating as a secret."
    az pipelines variable create `
        --pipeline-id $PipelineId `
        --org $Organization `
        --project $Project `
        --name $variableName `
        --value $key `
        --secret true `
        --only-show-errors | Out-Null
}

if ($LASTEXITCODE -ne 0) {
    throw "Failed to write '$variableName' to pipeline $PipelineId."
}

# 4. Read back the variable metadata (NOT the value) to confirm it is registered as a secret.
$verifyJson = az pipelines variable list `
    --pipeline-id $PipelineId `
    --org $Organization `
    --project $Project `
    --only-show-errors
$verify = $verifyJson | ConvertFrom-Json -AsHashtable
if ($verify.ContainsKey($variableName)) {
    $entry = $verify[$variableName]
    $isSecret = $entry.isSecret
    Write-Host "Variable '$variableName' is registered. isSecret=$isSecret"
    if (-not $isSecret) {
        Write-Warning "Variable '$variableName' is NOT marked secret. Re-run with -Verbose, or update via Azure DevOps UI."
    }
}
else {
    throw "Verification failed: '$variableName' is missing from the post-write variable list."
}

Write-Host ''
Write-Host "Done. Next pipeline run on main will use the refreshed NUGET_API_KEY."
Write-Host "Queue a run with:"
Write-Host "  az pipelines run --id $PipelineId --branch main --org $Organization --project $Project"
