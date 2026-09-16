#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$boot = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6-bootstrap.ps1'
$c = Get-Content -LiteralPath $boot -Raw
$c = $c.Replace("        if (`$null -eq `$Value) { }`r`n", '')
$c = $c.Replace("        if (`$null -eq `$Value) { }`n", '')
Set-Content -LiteralPath $boot -Value $c -Encoding utf8 -NoNewline
Write-Output 'BOOT_PATCHED'

$col = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6-collect.ps1'
$t = Get-Content -LiteralPath $col -Raw
$old = @"
`$rgOut = Join-Path `$out 'rg-adapter-captures.txt'
& rg.exe -n --glob '*.cs' --glob '*.csproj' 'Adapter_CapturesExecutable' 'F:\GitHub\McpServer\tests' 'F:\GitHub\McpServer\src' | Out-File -FilePath `$rgOut -Encoding utf8
if (-not (Test-Path -LiteralPath `$rgOut) -or ((Get-Item -LiteralPath `$rgOut).Length -eq 0)) {
    Set-Content -LiteralPath `$rgOut -Value 'NO_MATCH' -Encoding utf8
}
"@
$new = @"
`$rgOut = Join-Path `$out 'rg-adapter-captures.txt'
`$hits = @(Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\tests','F:\GitHub\McpServer\src' -Recurse -Include '*.cs','*.csproj' -File -ErrorAction SilentlyContinue | Select-String -Pattern 'Adapter_CapturesExecutable' -SimpleMatch)
if (`$hits.Count -eq 0) {
    Set-Content -LiteralPath `$rgOut -Value 'NO_MATCH' -Encoding utf8
} else {
    `$hits | ForEach-Object { `$_.Path + ':' + `$_.LineNumber + ':' + `$_.Line } | Set-Content -LiteralPath `$rgOut -Encoding utf8
}
"@
if ($t.Contains('rg.exe')) {
    $t2 = $t.Replace($old, $new)
    if ($t2.Contains('rg.exe')) { throw 'rg.exe still present after replace' }
    Set-Content -LiteralPath $col -Value $t2 -Encoding utf8 -NoNewline
    Write-Output 'COLLECT_PATCHED'
} else {
    Write-Output 'COLLECT_NO_RG'
}
