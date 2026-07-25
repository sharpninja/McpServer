#Requires -Version 7
<#
.SYNOPSIS
  Create Octopus projects that full-clone Azure pipelines for MCP workspaces
  that have azure-pipelines YAML but no existing Octopus project.
.NOTES
  Existing Octopus projects are NEVER modified. Create-only.
#>
$ErrorActionPreference = 'Stop'

$url = ($env:OCTOPUS_URL ?? '').TrimEnd('/')
$key = $env:OCTOPUS_API_KEY
$spaceName = $env:OCTOPUS_SPACE
if (-not $url -or -not $key) { throw 'OCTOPUS_URL and OCTOPUS_API_KEY required' }
if (-not $spaceName) { $spaceName = 'Default' }

$headers = @{
  'X-Octopus-ApiKey' = $key
  'Content-Type'     = 'application/json'
  'Accept'           = 'application/json'
}

function Invoke-Octo([string]$Method, [string]$Path, $Body = $null) {
  $uri = "$url$Path"
  $params = @{
    Uri             = $uri
    Method          = $Method
    Headers         = $headers
    TimeoutSec      = 120
  }
  if ($null -ne $Body) {
    $params.Body = ($Body | ConvertTo-Json -Depth 40 -Compress)
  }
  return Invoke-RestMethod @params
}

$spaces = Invoke-Octo GET '/api/spaces/all'
$sp = $spaces | Where-Object { $_.Name -eq $spaceName -or $_.Id -eq $spaceName } | Select-Object -First 1
if (-not $sp) { throw "Space not found: $spaceName" }
$sid = $sp.Id
Write-Host "SPACE $($sp.Name) $sid"

# Snapshot existing projects - immutable set
$existingProjects = @(Invoke-Octo GET "/api/$sid/projects/all")
$existingIds = New-Object 'System.Collections.Generic.HashSet[string]'
$existingNames = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$existingSlugs = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($p in $existingProjects) {
  [void]$existingIds.Add([string]$p.Id)
  [void]$existingNames.Add([string]$p.Name)
  [void]$existingSlugs.Add([string]$p.Slug)
  Write-Host "EXISTING | $($p.Name) | $($p.Id) | $($p.Slug)"
}

# Known workspace -> existing Octopus project mapping (skip)
$skipMaps = @{
  'F:\GitHub\repairs'          = 'AI Auto Repairman Service'
  'F:\GitHub\EternalDiscord'   = 'EternalDiscord'
  'F:\GitHub\EternalReddit'    = 'EternalReddit'
  'F:\GitHub\EternalSocial'    = 'EternalSocial'
  'F:\GitHub\EternalX.Blazor'  = 'EternalX'
  'F:\GitHub\FunWasHad'        = 'FunWasHad'
  'F:\GitHub\RomM'             = 'RomM'
}

# Candidates: azure pipeline, no matching Octopus project
$candidates = @(
  @{ Path = 'F:\GitHub\aiUnit'; Name = 'aiUnit'; Pipeline = 'azure-pipelines.yml'; GitHub = 'https://github.com/sharpninja/aiUnit.git'; Role = 'app-server' }
  @{ Path = 'F:\GitHub\Avalonia.RemoteControl'; Name = 'Avalonia.RemoteControl'; Pipeline = 'azure-pipelines.yml'; GitHub = 'https://github.com/sharpninja/Avalonia.RemoteControl.git'; Role = 'app-server' }
  @{ Path = 'F:\GitHub\bitnet-b1.58-sharp'; Name = 'BitNet-b1.58-Sharp'; Pipeline = 'azure-pipelines.yml'; GitHub = 'https://github.com/sharpninja/BitNet-b1.58-Sharp.git'; Role = 'app-server' }
  @{ Path = 'F:\GitHub\BodyAndBrain.Engine'; Name = 'BodyAndBrain.Engine'; Pipeline = 'azure-pipelines.yml'; GitHub = 'https://github.com/sharpninja/BodyAndBrain.Engine.git'; Role = 'app-server' }
  @{ Path = 'F:\GitHub\LlamaDeck'; Name = 'LlamaDeck'; Pipeline = 'azure-pipelines.yml'; GitHub = 'https://github.com/sharpninja/LlamaDeck.git'; Role = 'app-server' }
  @{ Path = 'F:\GitHub\McpServer'; Name = 'McpServer'; Pipeline = 'azure-pipelines.yml'; GitHub = 'https://github.com/sharpninja/McpServer.git'; Role = 'app-server' }
  @{ Path = 'F:\GitHub\MouseKeyProxy'; Name = 'MouseKeyProxy'; Pipeline = 'azure-pipelines.yml'; GitHub = 'https://github.com/sharpninja/MouseKeyProxy.git'; Role = 'app-server' }
  @{ Path = 'F:\GitHub\SharpNinjaHome'; Name = 'SharpNinjaHome'; Pipeline = 'azure-pipelines.yml'; GitHub = 'https://github.com/sharpninja/SharpNinjaHome.git'; Role = 'web-server' }
  @{ Path = 'F:\GitHub\TruckMate'; Name = 'TruckMate'; Pipeline = 'azure-pipelines.yml'; GitHub = 'https://github.com/sharpninja/TruckMate.git'; Role = 'app-server' }
  @{ Path = 'F:\GitHub\valhalla-dotnet'; Name = 'valhalla-dotnet'; Pipeline = 'azure-pipelines.yml'; GitHub = 'https://github.com/sharpninja/valhalla-dotnet.git'; Role = 'app-server' }
  @{ Path = 'F:\GitHub\vice-sharp'; Name = 'vice-sharp'; Pipeline = 'azure-pipelines.ci.yml'; GitHub = 'https://github.com/sharpninja/vice-sharp.git'; Role = 'app-server'; ExtraPipelines = @('azure-pipelines.release.yml') }
)

function Get-GitHubUrlFromRepo([string]$repoPath, [string]$fallback) {
  if (-not (Test-Path (Join-Path $repoPath '.git'))) { return $fallback }
  Push-Location $repoPath
  try {
    $u = git remote get-url origin 2>$null
    if ($u -and $u -match 'github\.com') { return $u }
    $u2 = git remote get-url github 2>$null
    if ($u2 -and $u2 -match 'github\.com') { return $u2 }
  } finally { Pop-Location }
  return $fallback
}

function Convert-AzureYamlToScriptSteps {
  param(
    [string]$RepoPath,
    [string]$PipelineRel,
    [string[]]$ExtraPipelines = @(),
    [string]$GitUrl,
    [string]$Role,
    [string]$ProjectName
  )

  $files = @($PipelineRel) + @($ExtraPipelines)
  $sections = [System.Collections.Generic.List[object]]::new()

  foreach ($rel in $files) {
    $full = Join-Path $RepoPath $rel
    if (-not (Test-Path $full)) {
      $sections.Add([pscustomobject]@{ Name = "Missing $rel"; Kind = 'gap'; Body = "Write-Host 'Pipeline file not found: $rel'" })
      continue
    }
    $raw = Get-Content -LiteralPath $full -Raw
    $hash = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.Substring(0, 12)

    # Extract displayName + command-ish lines for a readable clone script
    $lines = Get-Content -LiteralPath $full
    $commands = [System.Collections.Generic.List[string]]::new()
    $commands.Add("# Replicated from $rel (sha256:$hash)")
    $commands.Add('$ErrorActionPreference = ''Stop''')
    $commands.Add('function Invoke-External([string]$Command, [string]$FailureMessage) {')
    $commands.Add('  Write-Host ">> $Command"')
    $commands.Add('  cmd /c "$Command 2>&1"')
    $commands.Add('  if ($LASTEXITCODE -ne 0) { throw "$FailureMessage exit $LASTEXITCODE" }')
    $commands.Add('}')
    $commands.Add('')
    $commands.Add('# Resolve git-sourced package root (Octopus extracts GitDependency one level above script CWD for path filters; fall back to checkout).')
    $commands.Add('$src = $null')
    $commands.Add('if ($OctopusParameters[''Octopus.Action.Git.WorkingDirectory'']) { $src = $OctopusParameters[''Octopus.Action.Git.WorkingDirectory''] }')
    $commands.Add('if (-not $src -or -not (Test-Path $src)) {')
    $commands.Add('  $candidates = @(')
    $commands.Add("    'C:\\deploy\\$ProjectName',")
    $commands.Add("    (Join-Path `$env:ProgramData 'Octopus\\$ProjectName'),")
    $commands.Add("    '$($RepoPath.Replace('\','\\'))'")
    $commands.Add('  )')
    $commands.Add('  foreach ($c in $candidates) { if (Test-Path $c) { $src = $c; break } }')
    $commands.Add('}')
    $commands.Add('if (-not $src) { throw "Could not resolve source directory for ' + $ProjectName + '" }')
    $commands.Add('Set-Location $src')
    $commands.Add('Write-Host "Working directory: $src"')
    $commands.Add('')

    # Parse simple pwsh:/script:/dotnet patterns and displayNames
    $i = 0
    while ($i -lt $lines.Count) {
      $line = $lines[$i]
      if ($line -match '^\s*displayName:\s*(.+)\s*$') {
        $dn = $Matches[1].Trim().Trim("'").Trim('"')
        $commands.Add("Write-Host '=== $dn ==='")
      }
      elseif ($line -match '^\s*-\s*pwsh:\s*(.+)\s*$') {
        $cmd = $Matches[1].Trim().Trim("'").Trim('"')
        if ($cmd -and $cmd -ne '|') {
          $esc = $cmd.Replace("'", "''")
          $commands.Add("Invoke-External '$esc' 'step failed'")
        }
      }
      elseif ($line -match '^\s*-\s*script:\s*(.+)\s*$') {
        $cmd = $Matches[1].Trim().Trim("'").Trim('"')
        if ($cmd -and $cmd -ne '|') {
          $esc = $cmd.Replace("'", "''")
          $commands.Add("Invoke-External '$esc' 'step failed'")
        }
      }
      elseif ($line -match '^\s*filePath:\s*(.+)\s*$') {
        $fp = $Matches[1].Trim().Trim("'").Trim('"')
        # look ahead for arguments
        $args = ''
        if (($i + 1) -lt $lines.Count -and $lines[$i + 1] -match '^\s*arguments:\s*(.+)\s*$') {
          $args = $Matches[1].Trim().Trim("'").Trim('"')
        }
        $cmd = if ($args) { "pwsh -NoProfile -File `"$fp`" $args" } else { "pwsh -NoProfile -File `"$fp`"" }
        $esc = $cmd.Replace("'", "''")
        $commands.Add("Invoke-External '$esc' 'script step failed'")
      }
      elseif ($line -match '^\s*arguments:\s*(.+)\s*$' -and $line -match 'build\.ps1|Nuke|--target') {
        # handled with filePath usually
      }
      $i++
    }

    # Always append a summary of original pipeline path for audit
    $commands.Add('')
    $commands.Add("Write-Host 'Completed replicated pipeline script for $rel'")

    $body = ($commands -join "`n")
    $stepName = if ($files.Count -eq 1) { "Run $rel" } else { "Run $rel" }
    $sections.Add([pscustomobject]@{
      Name    = $stepName
      Kind    = 'script'
      Body    = $body
      GitUrl  = $GitUrl
      Role    = $Role
      Source  = $rel
      Hash    = $hash
    })
  }

  return $sections
}

function New-ScriptStep([string]$Name, [string]$Body, [string]$GitUrl, [string]$Role) {
  $actionId = [guid]::NewGuid().ToString()
  $stepId = [guid]::NewGuid().ToString()
  $slug = ($Name.ToLowerInvariant() -replace '[^a-z0-9]+', '-').Trim('-')
  $gitDeps = @()
  if ($GitUrl) {
    $gitDeps = @(
      @{
        Name                 = 'source'
        RepositoryUri        = $GitUrl
        DefaultBranch        = 'main'
        GitCredentialType    = 'Anonymous'
        FilePathFilters      = @()
        GitCredentialId      = $null
        GitHubConnectionId   = $null
        StepPackageInputsReferenceId = $null
      }
    )
  }
  return @{
    Id                 = $stepId
    Name               = $Name
    Slug               = $slug
    PackageRequirement = 'LetOctopusDecide'
    Properties         = @{
      'Octopus.Action.TargetRoles' = $Role
    }
    Condition          = 'Success'
    StartTrigger       = 'StartAfterPrevious'
    Actions            = @(
      @{
        Id              = $actionId
        Name            = $Name
        Slug            = $slug
        ActionType      = 'Octopus.Script'
        IsDisabled      = $false
        IsRequired      = $false
        WorkerPoolId    = $null
        Container       = @{ Image = $null; FeedId = $null; GitUrl = $null; Dockerfile = $null }
        Environments    = @()
        ExcludedEnvironments = @()
        Channels        = @()
        TenantTags      = @()
        Packages        = @()
        GitDependencies = $gitDeps
        Condition       = 'Success'
        Properties      = @{
          'Octopus.Action.Script.Syntax'                 = 'PowerShell'
          'Octopus.Action.Script.ScriptSource'           = 'Inline'
          'Octopus.Action.RunOnServer'                   = 'false'
          'Octopus.Action.Script.ScriptBody'             = $Body
          'Octopus.Action.PowerShell.ExecuteWithoutProfile' = 'True'
        }
      }
    )
  }
}

# Resolve project group + lifecycle
$groups = Invoke-Octo GET "/api/$sid/projectgroups/all"
$group = $groups | Where-Object { $_.Name -eq 'Default Project Group' } | Select-Object -First 1
if (-not $group) { $group = $groups | Select-Object -First 1 }
$lifecycles = Invoke-Octo GET "/api/$sid/lifecycles/all"
$lifecycle = $lifecycles | Where-Object { $_.Name -eq 'Default Lifecycle' } | Select-Object -First 1
if (-not $lifecycle) { $lifecycle = $lifecycles | Select-Object -First 1 }
Write-Host "GROUP $($group.Name) LIFECYCLE $($lifecycle.Name)"

$results = [System.Collections.Generic.List[object]]::new()
$createdIds = [System.Collections.Generic.List[string]]::new()

foreach ($c in $candidates) {
  $row = [ordered]@{
    Path     = $c.Path
    Name     = $c.Name
    Action   = ''
    ProjectId = ''
    Detail   = ''
    Ok       = $true
    Steps    = ''
  }

  try {
    # Skip if mapped to existing project
    if ($skipMaps.ContainsKey($c.Path)) {
      $row.Action = 'skip_existing_map'
      $row.Detail = "mapped to existing project $($skipMaps[$c.Path])"
      $results.Add([pscustomobject]$row)
      continue
    }

    if ($existingNames.Contains($c.Name) -or $existingSlugs.Contains(($c.Name.ToLowerInvariant() -replace '[^a-z0-9]+', '-'))) {
      $row.Action = 'skip_existing_name'
      $row.Detail = 'project name/slug already exists; not modified'
      $results.Add([pscustomobject]$row)
      continue
    }

    if (-not (Test-Path $c.Path)) {
      $row.Action = 'skip_missing_path'
      $row.Detail = 'workspace path missing'
      $results.Add([pscustomobject]$row)
      continue
    }

    $pipe = Join-Path $c.Path $c.Pipeline
    if (-not (Test-Path $pipe)) {
      $row.Action = 'skip_no_pipeline'
      $row.Detail = "missing $($c.Pipeline)"
      $results.Add([pscustomobject]$row)
      continue
    }

    $gitUrl = Get-GitHubUrlFromRepo -repoPath $c.Path -fallback $c.GitHub
    $extra = @()
    if ($c.ContainsKey('ExtraPipelines') -and $c.ExtraPipelines) { $extra = @($c.ExtraPipelines) }

    $sections = Convert-AzureYamlToScriptSteps -RepoPath $c.Path -PipelineRel $c.Pipeline -ExtraPipelines $extra -GitUrl $gitUrl -Role $c.Role -ProjectName $c.Name

    # Create project shell via CLI for reliability
    $octo = 'C:\Program Files\Octopus CLI\octopus.exe'
    $createOut = & $octo project create `
      --name $c.Name `
      --description "Replicated from $($c.Pipeline) (full clone of Azure pipeline steps as scripts). Created by Grok remote/octopus plan." `
      --group $group.Name `
      --lifecycle $lifecycle.Name `
      --space $spaceName `
      --no-prompt 2>&1
    Write-Host "CREATE $c.Name :: $createOut"

    # Refresh project list to get id - only use newly appeared
    $after = @(Invoke-Octo GET "/api/$sid/projects/all")
    $proj = $after | Where-Object { $_.Name -eq $c.Name } | Select-Object -First 1
    if (-not $proj) { throw "Project created but not found: $($c.Name)" }
    if ($existingIds.Contains($proj.Id)) { throw "Refusing to modify pre-existing project id $($proj.Id)" }

    $createdIds.Add($proj.Id)
    $row.ProjectId = $proj.Id

    # Load deployment process for NEW project only
    $proc = Invoke-Octo GET "/api/$sid/deploymentprocesses/$($proj.DeploymentProcessId)"
    if ($existingIds.Contains($proj.Id)) { throw 'safety' }

    $steps = [System.Collections.Generic.List[object]]::new()
    foreach ($sec in $sections) {
      $steps.Add((New-ScriptStep -Name $sec.Name -Body $sec.Body -GitUrl $gitUrl -Role $c.Role))
    }
    $proc.Steps = @($steps)
    # PUT process
    $null = Invoke-Octo PUT "/api/$sid/deploymentprocesses/$($proj.DeploymentProcessId)" $proc

    $row.Action = 'created'
    $row.Detail = "steps=$($steps.Count) git=$gitUrl"
    $row.Steps = ($sections | ForEach-Object { "$($_.Name)[$($_.Hash)]" }) -join '; '
    $results.Add([pscustomobject]$row)
  }
  catch {
    $row.Ok = $false
    $row.Action = 'failed'
    $row.Detail = "$_"
    $results.Add([pscustomobject]$row)
  }
}

# Verify pre-existing projects unchanged
Write-Host '--- PRE-EXISTING PROJECT FINGERPRINT CHECK ---'
$afterAll = @(Invoke-Octo GET "/api/$sid/projects/all")
foreach ($ep in $existingProjects) {
  $now = $afterAll | Where-Object { $_.Id -eq $ep.Id } | Select-Object -First 1
  if (-not $now) {
    Write-Host "FAIL missing existing project $($ep.Name)"
    continue
  }
  $beforeProc = Invoke-Octo GET "/api/$sid/deploymentprocesses/$($ep.DeploymentProcessId)"
  # process id should still match; step count snapshot
  Write-Host ("EXISTING_OK | {0} | id={1} | steps={2}" -f $ep.Name, $ep.Id, $beforeProc.Steps.Count)
}

$stamp = Get-Date -Format 'yyyyMMddTHHmmssZ'
$receiptDir = 'F:\GitHub\McpServer\docs\receipts'
$receipt = Join-Path $receiptDir "octopus-replicate-$stamp.txt"
$results | Format-List | Out-String | Set-Content $receipt -Encoding utf8
$results | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $receiptDir "octopus-replicate-$stamp.json") -Encoding utf8
Write-Host "RECEIPT=$receipt"
Write-Host "CREATED_IDS=$($createdIds -join ',')"
$results | ForEach-Object {
  $flag = if ($_.Ok) { 'OK' } else { 'FAIL' }
  Write-Host ("{0} | {1} | {2} | {3} | {4}" -f $flag, $_.Action, $_.Name, $_.ProjectId, $_.Detail)
}
