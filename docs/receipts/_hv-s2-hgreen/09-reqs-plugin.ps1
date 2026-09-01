#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$plugin = 'F:\GitHub\mcpserver-grok-plugin'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
$out = Join-Path $outDir '09-reqs.json'
$invoke = Join-Path $plugin 'lib\repl-invoke.ps1'

$env:MCP_PLUGIN_ROOT = $plugin
$env:MCP_PLUGIN_HOST = 'grok'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:PLUGIN_AGENT_NAME = 'GrokCode'

function Invoke-PluginMethod {
    param(
        [string]$Method,
        [string]$ParamsYaml,
        [string]$Label
    )
    $rawOut = Join-Path $outDir ("09-raw-$Label.json")
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = (Get-Command pwsh.exe -ErrorAction Stop).Source
    $psi.ArgumentList.Add('-NoLogo')
    $psi.ArgumentList.Add('-NoProfile')
    $psi.ArgumentList.Add('-NonInteractive')
    $psi.ArgumentList.Add('-File')
    $psi.ArgumentList.Add($invoke)
    $psi.ArgumentList.Add('-Method')
    $psi.ArgumentList.Add($Method)
    $psi.ArgumentList.Add('-ParamsYaml')
    $psi.ArgumentList.Add($ParamsYaml)
    $psi.WorkingDirectory = 'F:\GitHub\McpServer'
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $psi.Environment['MCP_PLUGIN_ROOT'] = $plugin
    $psi.Environment['MCP_PLUGIN_HOST'] = 'grok'
    $psi.Environment['MCP_AGENT_NAME'] = 'GrokCode'
    $psi.Environment['PLUGIN_AGENT_NAME'] = 'GrokCode'
    $proc = [System.Diagnostics.Process]::Start($psi)
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()
    if (-not $proc.WaitForExit(90000)) {
        try { $proc.Kill($true) } catch { }
        return [ordered]@{ Label = $Label; Method = $Method; ExitCode = -1; TimedOut = $true; Stdout = ''; Stderr = 'killed-after-90s' }
    }
    $stdout = [string]$stdoutTask.Result
    $stderr = [string]$stderrTask.Result
    Set-Content -LiteralPath $rawOut -Value $stdout -Encoding utf8
    $parsed = $null
    try { $parsed = $stdout | ConvertFrom-Json -ErrorAction Stop } catch { $parsed = $null }
    return [ordered]@{
        Label = $Label
        Method = $Method
        ExitCode = $proc.ExitCode
        TimedOut = $false
        RawPath = $rawOut
        Stderr = $stderr.Trim()
        Parsed = $parsed
        StdoutLength = $stdout.Length
    }
}

$frIds = @(
    'FR-MCP-STRICTCOUNT-001',
    'FR-MCP-FAILSAFE-001',
    'FR-MCP-SESSIONEND-001',
    'FR-MCP-XAGENT-001',
    'FR-MCP-VERIFYWRAP-001',
    'FR-MCP-TRIAGEPLUGIN-001'
)
$trIds = @(
    'TR-MCP-STRICTCOUNT-001',
    'TR-MCP-FAILSAFE-001',
    'TR-MCP-SESSIONEND-001',
    'TR-MCP-XAGENT-001',
    'TR-MCP-VERIFYWRAP-001',
    'TR-MCP-TRIAGEPLUGIN-001'
)
$testIds = @(
    'TEST-MCP-STRICTCOUNT-001',
    'TEST-MCP-FAILSAFE-001',
    'TEST-MCP-SESSIONEND-001',
    'TEST-MCP-XAGENT-001',
    'TEST-MCP-VERIFYWRAP-001',
    'TEST-MCP-TRIAGEPLUGIN-004'
)
$mapFrIds = @(
    'FR-MCP-STRICTCOUNT-001',
    'FR-MCP-FAILSAFE-001',
    'FR-MCP-SESSIONEND-001',
    'FR-MCP-XAGENT-001',
    'FR-MCP-VERIFYWRAP-001',
    'FR-MCP-TRIAGEPLUGIN-001'
)

$frs = @()
foreach ($id in $frIds) {
    $frs += Invoke-PluginMethod -Method 'workflow.requirements.getFr' -ParamsYaml ("id: $id") -Label ("fr-" + $id.ToLower())
}
$trs = @()
foreach ($id in $trIds) {
    $trs += Invoke-PluginMethod -Method 'workflow.requirements.getTr' -ParamsYaml ("id: $id") -Label ("tr-" + $id.ToLower())
}
$tests = @()
foreach ($id in $testIds) {
    $tests += Invoke-PluginMethod -Method 'workflow.requirements.getTest' -ParamsYaml ("id: $id") -Label ("test-" + $id.ToLower())
}
$maps = @()
foreach ($id in $mapFrIds) {
    $maps += Invoke-PluginMethod -Method 'workflow.requirements.listMappings' -ParamsYaml ("frId: $id") -Label ("map-" + $id.ToLower())
}

function Summarize-Get {
    param($Rows)
    $out = @()
    foreach ($row in $Rows) {
        $item = $null
        $p = $row.Parsed
        if ($null -ne $p) {
            if ($p.PSObject.Properties.Name -contains 'result') {
                $r = $p.result
                if ($r -and $r.PSObject.Properties.Name -contains 'item') { $item = $r.item }
                elseif ($r -and $r.PSObject.Properties.Name -contains 'id') { $item = $r }
            } elseif ($p.PSObject.Properties.Name -contains 'item') { $item = $p.item }
            elseif ($p.PSObject.Properties.Name -contains 'id') { $item = $p }
        }
        $acs = @()
        if ($item -and $item.PSObject.Properties.Name -contains 'acceptanceCriteria' -and $null -ne $item.acceptanceCriteria) {
            $acs = @($item.acceptanceCriteria)
        }
        $out += [ordered]@{
            Label = $row.Label
            ExitCode = $row.ExitCode
            TimedOut = $row.TimedOut
            Id = $(if ($item) { [string]$item.id } else { $null })
            Title = $(if ($item -and $item.PSObject.Properties.Name -contains 'title') { [string]$item.title } else { $null })
            Status = $(if ($item -and $item.PSObject.Properties.Name -contains 'status') { [string]$item.status } else { $null })
            AcCount = @($acs).Count
            AcceptanceCriteria = @($acs | ForEach-Object {
                if ($_ -is [string]) { $_ }
                elseif ($_.PSObject.Properties.Name -contains 'text') {
                    [ordered]@{ id = $(if ($_.PSObject.Properties.Name -contains 'id') { [string]$_.id } else { $null }); text = [string]$_.text; isSatisfied = $(if ($_.PSObject.Properties.Name -contains 'isSatisfied') { $_.isSatisfied } else { $null }) }
                } else { [string]$_ }
            })
            Stderr = $row.Stderr
        }
    }
    return $out
}

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    PluginInvoke = $invoke
    Fr = @(Summarize-Get -Rows $frs)
    Tr = @(Summarize-Get -Rows $trs)
    Test = @(Summarize-Get -Rows $tests)
    Mappings = @($maps | ForEach-Object {
        $items = @()
        $p = $_.Parsed
        if ($p -and $p.PSObject.Properties.Name -contains 'result' -and $p.result.PSObject.Properties.Name -contains 'items') {
            $items = @($p.result.items)
        } elseif ($p -and $p.PSObject.Properties.Name -contains 'items') {
            $items = @($p.items)
        }
        [ordered]@{
            Label = $_.Label
            ExitCode = $_.ExitCode
            ItemCount = @($items).Count
            Items = @($items | ForEach-Object {
                [ordered]@{
                    frId = $(if ($_.PSObject.Properties.Name -contains 'frId') { [string]$_.frId } else { $null })
                    trId = $(if ($_.PSObject.Properties.Name -contains 'trId') { [string]$_.trId } else { $null })
                    testId = $(if ($_.PSObject.Properties.Name -contains 'testId') { [string]$_.testId } else { $null })
                    trIds = $(if ($_.PSObject.Properties.Name -contains 'trIds') { @($_.trIds) } else { $null })
                    testIds = $(if ($_.PSObject.Properties.Name -contains 'testIds') { @($_.testIds) } else { $null })
                }
            })
            Stderr = $_.Stderr
        }
    })
}
$obj | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} fr={1} tr={2} test={3} maps={4}" -f $out, @($obj.Fr).Count, @($obj.Tr).Count, @($obj.Test).Count, @($obj.Mappings).Count)
