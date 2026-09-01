$ErrorActionPreference = 'Continue'
$pidMarker = 34520
$cim = Get-CimInstance Win32_Process -Filter "ProcessId=$pidMarker" -ErrorAction SilentlyContinue
$out = [ordered]@{}
if ($null -eq $cim) {
    $out.process = 'NOT_RUNNING'
} else {
    $path = [string]$cim.ExecutablePath
    $out.process = [ordered]@{
        Id = $cim.ProcessId
        Name = $cim.Name
        ExecutablePath = $path
        CommandLine = $cim.CommandLine
        CreationDate = [string]$cim.CreationDate
    }
    if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path -LiteralPath $path)) {
        $info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($path)
        $out.fileVersion = [ordered]@{
            FileVersion = $info.FileVersion
            ProductVersion = $info.ProductVersion
            OriginalFilename = $info.OriginalFilename
        }
        $dir = Split-Path -Parent $path
        foreach ($name in @('McpServer.Services.dll', 'McpServer.Support.Mcp.dll')) {
            $dll = Join-Path $dir $name
            if (Test-Path -LiteralPath $dll) {
                $dinfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll)
                $item = Get-Item -LiteralPath $dll
                $out[$name] = [ordered]@{
                    FullName = $item.FullName
                    LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
                    Length = $item.Length
                    FileVersion = $dinfo.FileVersion
                    ProductVersion = $dinfo.ProductVersion
                }
            }
        }
        $svcDll = Join-Path $dir 'McpServer.Services.dll'
        if (Test-Path -LiteralPath $svcDll) {
            $bytes = [System.IO.File]::ReadAllBytes($svcDll)
            $text = [System.Text.Encoding]::UTF8.GetString($bytes)
            $out.servicesDllContainsIsSupersededHookPersist = $text.Contains('IsSupersededHookPersist')
        }
    }
}
$dest = 'F:\GitHub\McpServer\docs\receipts\_hv-g1-live-process.json'
($out | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $dest -Encoding UTF8
$out | ConvertTo-Json -Depth 8
