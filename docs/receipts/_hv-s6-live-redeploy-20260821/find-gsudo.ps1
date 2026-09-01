#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

Write-Output '===== UTC ====='
Write-Output ([DateTime]::UtcNow.ToString('o'))

$roots = @(
    'C:\Users\kingd\.grok\sessions'
    'C:\Users\kingd\.grok'
)
Write-Output '===== name match 01f6458b ====='
Get-ChildItem -Path $roots -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like '*01f6458b*' } |
    Select-Object -First 30 FullName, Length, LastWriteTimeUtc |
    ForEach-Object { Write-Output ($_.FullName + ' ' + $_.Length + ' ' + $_.LastWriteTimeUtc.ToString('o')) }

Write-Output '===== content match 01f6458b (limited) ====='
$termRoot = 'C:\Users\kingd\.grok\sessions'
if (Test-Path -LiteralPath $termRoot) {
    Get-ChildItem -LiteralPath $termRoot -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        $term = Join-Path $_.FullName 'terminal'
        if (-not (Test-Path -LiteralPath $term)) {
            $nested = Get-ChildItem -LiteralPath $_.FullName -Directory -ErrorAction SilentlyContinue
            foreach ($n in $nested) {
                $t2 = Join-Path $n.FullName 'terminal'
                if (Test-Path -LiteralPath $t2) {
                    Get-ChildItem -LiteralPath $t2 -File -ErrorAction SilentlyContinue |
                        Where-Object { $_.Name -like '*01f6458b*' -or $_.LastWriteTimeUtc -gt [datetime]'2026-08-21T10:10:00Z' } |
                        ForEach-Object { Write-Output ('TERMFILE=' + $_.FullName + ' Len=' + $_.Length + ' Utc=' + $_.LastWriteTimeUtc.ToString('o')) }
                }
            }
        } else {
            Get-ChildItem -LiteralPath $term -File -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTimeUtc -gt [datetime]'2026-08-21T10:10:00Z' } |
                ForEach-Object { Write-Output ('TERMFILE=' + $_.FullName + ' Len=' + $_.Length + ' Utc=' + $_.LastWriteTimeUtc.ToString('o')) }
        }
    }
}

Write-Output '===== current session terminal listing ====='
$cur = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a023d7-b8d5-7c90-9492-8e948d165698\terminal'
Get-ChildItem -LiteralPath $cur -File | ForEach-Object {
    Write-Output ($_.Name + ' Len=' + $_.Length + ' Utc=' + $_.LastWriteTimeUtc.ToString('o'))
}

Write-Output '===== grep gsudo in current session terminals ====='
Select-String -Path (Join-Path $cur '*') -Pattern 'gsudo|UpdateService|run-update-service' -ErrorAction SilentlyContinue |
    Select-Object -First 80 |
    ForEach-Object { Write-Output ($_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim()) }

Write-Output '===== HMACSHA256 in receipts dated 20260821 after 10:00 ====='
Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\docs\receipts' -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTimeUtc -gt [datetime]'2026-08-21T10:00:00Z' -and $_.LastWriteTimeUtc -lt [datetime]'2026-08-21T10:25:00Z' } |
    ForEach-Object {
        Write-Output ('RECENT=' + $_.FullName + ' Utc=' + $_.LastWriteTimeUtc.ToString('o'))
        $hit = Select-String -LiteralPath $_.FullName -Pattern 'HMACSHA256' -ErrorAction SilentlyContinue
        if ($hit) { $hit | ForEach-Object { Write-Output ('HMAC=' + $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim()) } }
    }

Write-Output '===== live exe hash vs manifest ====='
$exe = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
$sha = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Output ('LiveExeSha256=' + $sha)
$man = Get-Content -LiteralPath 'C:\ProgramData\McpServer\.mcpservice-deployment.json' -Raw | ConvertFrom-Json
$manHash = ($man.executableHashes | Where-Object { $_.name -eq 'McpServer.Support.Mcp.exe' }).sha256
Write-Output ('ManifestSha256=' + $manHash)
Write-Output ('HashMatch=' + ($sha -eq $manHash.ToLowerInvariant()))
Write-Output ('generatedBy=' + $man.generatedBy)
Write-Output ('generatedUtc=' + $man.generatedUtc)
Write-Output ('operation=' + $man.operation)

Write-Output '===== appsettings restored presence ====='
$cfg = 'C:\ProgramData\McpServer\appsettings.yaml'
if (Test-Path -LiteralPath $cfg) {
    $c = Get-Item -LiteralPath $cfg
    Write-Output ('appsettings.yaml Length=' + $c.Length + ' Utc=' + $c.LastWriteTimeUtc.ToString('o'))
} else {
    Write-Output 'APPSETTINGS_MISSING'
}
$data = 'C:\ProgramData\McpServer-Data'
if (Test-Path -LiteralPath $data) {
    Get-ChildItem -LiteralPath $data | ForEach-Object { Write-Output ('DATA=' + $_.Name + ' Utc=' + $_.LastWriteTimeUtc.ToString('o')) }
} else {
    Write-Output 'DATA_DIR_MISSING'
}

Write-Output 'DONE'
