#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'

Write-Output '=== PYTHON_PROBE ==='
foreach ($cmd in @('python', 'python3', 'py')) {
    $found = Get-Command $cmd -ErrorAction SilentlyContinue
    if ($found) { Write-Output ('CMD_PRESENT {0}={1}' -f $cmd, $found.Source) }
    else { Write-Output ('CMD_ABSENT {0}' -f $cmd) }
}

Write-Output '=== LIVE_YAML ==='
$livePath = 'C:\ProgramData\McpServer\appsettings.yaml'
$liveItem = Get-Item -LiteralPath $livePath
Write-Output ('LiveYamlUtc=' + $liveItem.LastWriteTimeUtc.ToString('o'))
Write-Output ('LiveYamlLen=' + $liveItem.Length)
$liveHash = Get-FileHash -LiteralPath $livePath -Algorithm SHA256
Write-Output ('LiveYamlSha256=' + $liveHash.Hash)
$doc = Read-McpYamlObject -Path $livePath
if ($doc.Contains('AgentHelp') -and $doc['AgentHelp'] -is [System.Collections.IDictionary]) {
    $ah = $doc['AgentHelp']
    Write-Output ('AgentHelpKeys=' + (@($ah.Keys) -join ','))
    foreach ($k in @($ah.Keys)) {
        $v = $ah[$k]
        if ($v -is [bool]) { Write-Output ('AgentHelp.{0}={1} TYPE=bool' -f $k, $v) }
        else { Write-Output ('AgentHelp.{0}={1} TYPE={2}' -f $k, $v, $v.GetType().Name) }
    }
} else {
    Write-Output 'AgentHelp=MISSING'
}

Write-Output '=== EXE ==='
$exe = Get-Item -LiteralPath 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
$vi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe.FullName)
$exeHash = Get-FileHash -LiteralPath $exe.FullName -Algorithm SHA256
Write-Output ('ExeUtc=' + $exe.LastWriteTimeUtc.ToString('o'))
Write-Output ('ExeFileVersion=' + $vi.FileVersion)
Write-Output ('ExeProductVersion=' + $vi.ProductVersion)
Write-Output ('ExeSha256=' + $exeHash.Hash)

Write-Output '=== LOG_FILES ==='
Get-ChildItem -LiteralPath 'C:\ProgramData\McpServer\logs' -Filter 'mcp-*.log' |
    Sort-Object LastWriteTimeUtc |
    ForEach-Object { Write-Output ($_.LastWriteTimeUtc.ToString('o') + ' ' + $_.Length + ' ' + $_.FullName) }

$needles = @(
    'help-20260818001213-0aa9f6de59d2403296130363aa94bb75',
    'agent_help_submit_turn',
    'agent_help_create_session',
    'agent_help_get_transcript',
    '55827'
)

$paths = @(
    'C:\ProgramData\McpServer\logs\mcp-20260817.log',
    'C:\ProgramData\McpServer\logs\mcp-20260818.log'
)

foreach ($path in $paths) {
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Output ('LogMissing=' + $path)
        continue
    }
    Write-Output ('=== SCAN ' + $path + ' ===')
    $hit = 0
    $fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    $reader = [System.IO.StreamReader]::new($fs)
    $lineNo = 0
    try {
        while ($null -ne ($line = $reader.ReadLine())) {
            $lineNo++
            $keep = $false
            foreach ($n in $needles) {
                if ($line.Contains($n)) { $keep = $true; break }
            }
            if (-not $keep) { continue }
            $hit++
            $shown = $line
            if ($shown.Length -gt 1800) { $shown = $shown.Substring(0, 1800) + '...TRUNC' }
            Write-Output ('L' + $lineNo + ' ' + $shown)
            if ($hit -ge 50) {
                Write-Output 'HIT_CAP=50'
                break
            }
        }
    } finally {
        $reader.Dispose()
        $fs.Dispose()
    }
    Write-Output ('HitCount=' + $hit)
    Write-Output ('LinesRead=' + $lineNo)
}

Write-Output 'LOGSCAN_DONE'
