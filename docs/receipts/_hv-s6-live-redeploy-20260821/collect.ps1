#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s6-live-redeploy-20260821'
if (-not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

function Write-Section {
    param([string]$Name)
    Write-Output ('===== ' + $Name + ' =====')
}

Write-Section 'UTC'
$utc = [DateTime]::UtcNow
Write-Output ('TimestampUtc=' + $utc.ToString('yyyy-MM-ddTHH:mm:ssZ'))
Write-Output ('TimestampUtcO=' + $utc.ToString('o'))

Write-Section 'Test-MarkerSignature and Invoke-FullBootstrap'
$pluginRoot = 'C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f'
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
. (Join-Path $pluginRoot 'lib\marker-resolver.ps1')
$marker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$sig = $false
try {
    $sig = [bool](Test-MarkerSignature -MarkerFile $marker)
} catch {
    Write-Output ('Test-MarkerSignature_ERROR=' + $_.Exception.Message)
}
Write-Output ('Test-MarkerSignature=' + $sig)
$boot = $false
try {
    $boot = [bool](Invoke-FullBootstrap -StartDir 'F:\GitHub\McpServer')
} catch {
    Write-Output ('Invoke-FullBootstrap_ERROR=' + $_.Exception.Message)
}
Write-Output ('Invoke-FullBootstrap=' + $boot)

Write-Section 'Get-Service McpServer'
$svc = Get-Service -Name McpServer -ErrorAction SilentlyContinue
if ($null -eq $svc) {
    Write-Output 'SERVICE_MISSING'
} else {
    Write-Output ('Name=' + $svc.Name)
    Write-Output ('Status=' + $svc.Status)
    Write-Output ('StartType=' + $svc.StartType)
    Write-Output ('DisplayName=' + $svc.DisplayName)
}

Write-Section 'FileVersionInfo'
$exe = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
if (Test-Path -LiteralPath $exe) {
    $vi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
    $item = Get-Item -LiteralPath $exe
    Write-Output ('FullPath=' + $item.FullName)
    Write-Output ('LastWriteTimeUtc=' + $item.LastWriteTimeUtc.ToString('o'))
    Write-Output ('Length=' + $item.Length)
    Write-Output ('FileVersion=' + $vi.FileVersion)
    Write-Output ('ProductVersion=' + $vi.ProductVersion)
    Write-Output ('FileDescription=' + $vi.FileDescription)
    Write-Output ('ProductName=' + $vi.ProductName)
} else {
    Write-Output 'EXE_MISSING'
}

Write-Section 'Process'
Get-CimInstance Win32_Process -Filter "Name='McpServer.Support.Mcp.exe'" | ForEach-Object {
    Write-Output ('ProcessId=' + $_.ProcessId)
    Write-Output ('CreationDate=' + $_.CreationDate)
    Write-Output ('ExecutablePath=' + $_.ExecutablePath)
    Write-Output ('CommandLine=' + $_.CommandLine)
}

Write-Section 'MarkerTail'
Get-Content -LiteralPath $marker -Tail 20 | ForEach-Object { Write-Output $_ }

Write-Section 'MarkerMeta'
$m = Get-Item -LiteralPath $marker
Write-Output ('LastWriteTimeUtc=' + $m.LastWriteTimeUtc.ToString('o'))
Write-Output ('Length=' + $m.Length)

Write-Section 'MarkerPidStarted'
Select-String -LiteralPath $marker -Pattern '^(pid|startedAt|markerWrittenAtUtc|serverStartedAtUtc|port|baseUrl):' | ForEach-Object { Write-Output $_.Line }

Write-Section 'Health'
try {
    $nonce = 'hv-' + [guid]::NewGuid().ToString('N')
    $uri = 'http://PAYTON-LEGION2:7147/health?nonce=' + $nonce
    $resp = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 10
    Write-Output ('StatusCode=' + [int]$resp.StatusCode)
    Write-Output ('NonceSent=' + $nonce)
    Write-Output ('Body=' + $resp.Content)
} catch {
    Write-Output ('HEALTH_ERROR=' + $_.Exception.Message)
}

Write-Section 'BackupZip'
$zip = 'C:\Users\kingd\McpServer-Backups\McpServer-backup-20260821-051736195.zip'
if (Test-Path -LiteralPath $zip) {
    $z = Get-Item -LiteralPath $zip
    Write-Output ('Exists=True')
    Write-Output ('FullName=' + $z.FullName)
    Write-Output ('Length=' + $z.Length)
    Write-Output ('LastWriteTimeUtc=' + $z.LastWriteTimeUtc.ToString('o'))
} else {
    Write-Output 'BACKUP_MISSING'
}

Write-Section 'GitVersionStaged'
Set-Location 'F:\GitHub\McpServer'
git diff --cached -- GitVersion.yml
Write-Output '--- porcelain ---'
git status --porcelain -- GitVersion.yml
Write-Output '--- HEAD GitVersion next-version ---'
git show HEAD:GitVersion.yml | Select-String -Pattern 'next-version'
Write-Output '--- working tree next-version ---'
Select-String -LiteralPath 'F:\GitHub\McpServer\GitVersion.yml' -Pattern 'next-version' | ForEach-Object { Write-Output $_.Line }

Write-Section 'HEAD'
git rev-parse HEAD
git log -1 --format='%H %cI %s'

Write-Section 'HMACSHA256_in_updateservice_receipt'
$implDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s6-updateservice-20260821T101630Z'
if (Test-Path -LiteralPath $implDir) {
    Get-ChildItem -LiteralPath $implDir -Recurse -File | ForEach-Object {
        Write-Output ('FILE=' + $_.FullName + ' Length=' + $_.Length + ' LastWriteUtc=' + $_.LastWriteTimeUtc.ToString('o'))
    }
    $hmacHits = Select-String -Path (Join-Path $implDir '*') -Pattern 'HMACSHA256' -ErrorAction SilentlyContinue
    if ($hmacHits) {
        $hmacHits | ForEach-Object { Write-Output ('HMAC_HIT=' + $_.Path + ':' + $_.LineNumber + ':' + $_.Line) }
    } else {
        Write-Output 'HMAC_HIT=NONE_IN_RECEIPT_DIR'
    }
} else {
    Write-Output 'IMPL_RECEIPT_DIR_MISSING'
}

Write-Section 'Done'
Write-Output 'COLLECT_OK'
