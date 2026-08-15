#Requires -Version 7
<#
.SYNOPSIS
  Standardize MCP workspace git remotes: origin=GitHub, azure=Azure DevOps.
.NOTES
  Idempotent. No force-push. Receipts to stdout as objects.
#>
$ErrorActionPreference = 'Stop'

$marker = Get-Content 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
$keyMatch = [regex]::Match($marker, 'apiKey:\s*(\S+)')
$baseMatch = [regex]::Match($marker, 'baseUrl:\s*(\S+)')
if (-not $keyMatch.Success) { throw 'apiKey not found in marker' }
if (-not $baseMatch.Success) { throw 'baseUrl not found in marker' }
$key = $keyMatch.Groups[1].Value
$base = $baseMatch.Groups[1].Value
$headers = @{ 'X-Api-Key' = $key; 'X-Workspace-Path' = 'F:\GitHub\McpServer' }
$ws = Invoke-RestMethod -Uri "$base/mcpserver/workspace" -Headers $headers -TimeoutSec 60
$paths = @($ws.items | ForEach-Object { $_.workspacePath } | Where-Object { $_ } | Sort-Object -Unique)

function Test-IsAzureUrl([string]$u) {
  return $u -match 'dev\.azure\.com|visualstudio\.com'
}
function Test-IsGitHubUrl([string]$u) {
  return $u -match 'github\.com'
}

function Get-RemoteMap {
  $map = @{}
  git remote -v 2>$null | ForEach-Object {
    if ($_ -match '^(\S+)\s+(\S+)\s+\((fetch|push)\)$') {
      $n = $Matches[1]; $u = $Matches[2]; $d = $Matches[3]
      if (-not $map.ContainsKey($n)) { $map[$n] = @{} }
      $map[$n][$d] = $u
    }
  }
  return $map
}

$results = [System.Collections.Generic.List[object]]::new()

foreach ($p in $paths) {
  $row = [ordered]@{
    Path     = $p
    Action   = 'skip'
    Before   = ''
    After    = ''
    Detail   = ''
    Ok       = $true
  }
  try {
    if (-not (Test-Path -LiteralPath $p)) {
      $row.Action = 'skip'; $row.Detail = 'missing_path'; $results.Add([pscustomobject]$row); continue
    }
    if (-not (Test-Path -LiteralPath (Join-Path $p '.git'))) {
      $row.Action = 'skip'; $row.Detail = 'no_git'; $results.Add([pscustomobject]$row); continue
    }

    Push-Location -LiteralPath $p
    try {
      $map = Get-RemoteMap
      $row.Before = (($map.Keys | Sort-Object | ForEach-Object {
        $u = $map[$_]['fetch']
        "$_=$u"
      }) -join ' | ')

      if ($map.Count -eq 0) {
        $row.Action = 'skip'; $row.Detail = 'no_remotes'; $results.Add([pscustomobject]$row); continue
      }

      $originUrl = if ($map.ContainsKey('origin')) { $map['origin']['fetch'] } else { $null }
      $githubUrl = if ($map.ContainsKey('github')) { $map['github']['fetch'] } else { $null }
      $azureUrl  = if ($map.ContainsKey('azure')) { $map['azure']['fetch'] } else { $null }

      # Already correct: origin=GitHub and azure=Azure
      if ($originUrl -and (Test-IsGitHubUrl $originUrl) -and $azureUrl -and (Test-IsAzureUrl $azureUrl) -and -not $githubUrl) {
        $row.Action = 'already_ok'
        $row.Detail = 'origin_github_azure_present'
        $map2 = Get-RemoteMap
        $row.After = (($map2.Keys | Sort-Object | ForEach-Object { "$_=$($map2[$_]['fetch'])" }) -join ' | ')
        $results.Add([pscustomobject]$row); continue
      }

      # origin and github same GitHub URL -> remove redundant github
      if ($originUrl -and $githubUrl -and (Test-IsGitHubUrl $originUrl) -and $originUrl -eq $githubUrl) {
        git remote remove github
        if ($LASTEXITCODE -ne 0) { throw "git remote remove github failed exit $LASTEXITCODE" }
        $row.Action = 'cleanup_dup_github'
        $row.Detail = 'removed_duplicate_github_remote'
        $map2 = Get-RemoteMap
        $row.After = (($map2.Keys | Sort-Object | ForEach-Object { "$_=$($map2[$_]['fetch'])" }) -join ' | ')
        $results.Add([pscustomobject]$row); continue
      }

      # Dual swap: origin Azure + github GitHub
      if ($originUrl -and (Test-IsAzureUrl $originUrl) -and $githubUrl -and (Test-IsGitHubUrl $githubUrl)) {
        if ($map.ContainsKey('azure')) { throw 'remote named azure already exists; abort swap' }
        git remote rename origin azure
        if ($LASTEXITCODE -ne 0) { throw "rename origin->azure failed exit $LASTEXITCODE" }
        git remote rename github origin
        if ($LASTEXITCODE -ne 0) { throw "rename github->origin failed exit $LASTEXITCODE" }

        # Best-effort fetch
        git fetch origin --prune 2>&1 | Out-Null
        git fetch azure --prune 2>&1 | Out-Null

        # Retarget branches tracking azure/<branch> to origin/<branch> when available
        $branches = git for-each-ref --format='%(refname:short)|%(upstream:short)' refs/heads 2>$null
        foreach ($line in $branches) {
          if (-not $line) { continue }
          $parts = $line -split '\|', 2
          $b = $parts[0]
          $up = if ($parts.Count -gt 1) { $parts[1] } else { '' }
          if ($up -match '^azure/(.+)$') {
            $bn = $Matches[1]
            $exists = git rev-parse --verify "origin/$bn" 2>$null
            if ($LASTEXITCODE -eq 0 -and $exists) {
              git branch --set-upstream-to="origin/$bn" $b 2>&1 | Out-Null
            }
          }
        }

        $row.Action = 'swap'
        $row.Detail = 'origin_was_azure_now_github'
        $map2 = Get-RemoteMap
        $row.After = (($map2.Keys | Sort-Object | ForEach-Object { "$_=$($map2[$_]['fetch'])" }) -join ' | ')
        $results.Add([pscustomobject]$row); continue
      }

      # Azure-only: rename origin -> azure
      if ($originUrl -and (Test-IsAzureUrl $originUrl) -and -not $githubUrl) {
        if ($map.ContainsKey('azure')) { throw 'azure remote already exists on azure-only repo' }
        git remote rename origin azure
        if ($LASTEXITCODE -ne 0) { throw "rename origin->azure failed exit $LASTEXITCODE" }
        $row.Action = 'azure_only_rename'
        $row.Detail = 'origin_renamed_to_azure_no_github'
        $map2 = Get-RemoteMap
        $row.After = (($map2.Keys | Sort-Object | ForEach-Object { "$_=$($map2[$_]['fetch'])" }) -join ' | ')
        $results.Add([pscustomobject]$row); continue
      }

      # origin already GitHub, no azure (github-only) - leave
      if ($originUrl -and (Test-IsGitHubUrl $originUrl) -and -not $azureUrl) {
        # if leftover github remote with different URL, leave for review
        if ($githubUrl -and $githubUrl -ne $originUrl) {
          $row.Action = 'review'
          $row.Detail = 'origin_github_plus_extra_github_remote'
        } else {
          $row.Action = 'github_only_ok'
          $row.Detail = 'origin_github_no_azure'
        }
        $map2 = Get-RemoteMap
        $row.After = (($map2.Keys | Sort-Object | ForEach-Object { "$_=$($map2[$_]['fetch'])" }) -join ' | ')
        $results.Add([pscustomobject]$row); continue
      }

      # origin GitHub + azure already - maybe also has github name
      if ($originUrl -and (Test-IsGitHubUrl $originUrl) -and $azureUrl -and (Test-IsAzureUrl $azureUrl)) {
        if ($githubUrl) {
          # remove github if same as origin
          if ($githubUrl -eq $originUrl) {
            git remote remove github
            $row.Action = 'cleanup_dup_github'
            $row.Detail = 'already_ok_removed_dup_github'
          } else {
            $row.Action = 'review'
            $row.Detail = 'origin_azure_ok_but_extra_github'
          }
        } else {
          $row.Action = 'already_ok'
          $row.Detail = 'origin_github_azure_present'
        }
        $map2 = Get-RemoteMap
        $row.After = (($map2.Keys | Sort-Object | ForEach-Object { "$_=$($map2[$_]['fetch'])" }) -join ' | ')
        $results.Add([pscustomobject]$row); continue
      }

      $row.Action = 'review'
      $row.Detail = 'unclassified_remote_layout'
      $map2 = Get-RemoteMap
      $row.After = (($map2.Keys | Sort-Object | ForEach-Object { "$_=$($map2[$_]['fetch'])" }) -join ' | ')
      $results.Add([pscustomobject]$row)
    }
    finally {
      Pop-Location
    }
  }
  catch {
    $row.Ok = $false
    $row.Action = 'failed'
    $row.Detail = "$_"
    try {
      $map2 = Get-RemoteMap
      $row.After = (($map2.Keys | Sort-Object | ForEach-Object { "$_=$($map2[$_]['fetch'])" }) -join ' | ')
    } catch {}
    $results.Add([pscustomobject]$row)
    try { Pop-Location } catch {}
  }
}

$receiptDir = 'F:\GitHub\McpServer\docs\receipts'
if (-not (Test-Path $receiptDir)) { New-Item -ItemType Directory -Path $receiptDir -Force | Out-Null }
$stamp = Get-Date -Format 'yyyyMMddTHHmmssZ'
$receiptPath = Join-Path $receiptDir "remote-rename-$stamp.txt"
$results | Format-List | Out-String | Set-Content -Path $receiptPath -Encoding utf8
$results | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $receiptDir "remote-rename-$stamp.json") -Encoding utf8

Write-Host "RECEIPT=$receiptPath"
Write-Host "TOTAL=$($results.Count)"
$results | Group-Object Action | ForEach-Object { Write-Host ("COUNT {0}={1}" -f $_.Name, $_.Count) }
$results | ForEach-Object {
  $flag = if ($_.Ok) { 'OK' } else { 'FAIL' }
  Write-Host ("{0} | {1} | {2} | {3}" -f $flag, $_.Action, $_.Path, $_.Detail)
}
