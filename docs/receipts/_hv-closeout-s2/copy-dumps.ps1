#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s2'
$srcDir = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01ba2-bf1c-70b2-a1f8-43751c63f792\mcp'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$pairs = @{
    'call-676cd967-eb9f-45de-86c6-d434ae78137b-69.json' = 'fr-dump.json'
    'call-380efd39-a96a-4a14-aa44-d07c2870373a-71.json' = 'test-dump.json'
    'call-380efd39-a96a-4a14-aa44-d07c2870373a-72.json' = 'map-dump.json'
    'call-491fca6e-e74e-4784-85fb-0d4f65c45fcc-85.json' = 'tr-dump.json'
}

foreach ($name in $pairs.Keys) {
    Copy-Item -LiteralPath (Join-Path $srcDir $name) -Destination (Join-Path $outDir $pairs[$name]) -Force
}

Get-ChildItem -LiteralPath $outDir | Select-Object Name, Length | ConvertTo-Json
exit 0
