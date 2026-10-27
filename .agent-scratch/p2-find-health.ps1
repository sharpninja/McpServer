$ErrorActionPreference = 'Continue'
$dirs = @(
    'F:\GitHub\mcpserver-grok-plugin\lib',
    'F:\GitHub\mcpserver-grok-plugin\hooks'
)
foreach ($dir in $dirs) {
    Get-ChildItem -LiteralPath $dir -Recurse -Include *.ps1 -File -ErrorAction SilentlyContinue |
        Select-String -Pattern 'nonce|Test-MarkerSignature|sessionlog.bootstrap|Health' |
        Select-Object -First 40 |
        ForEach-Object { $_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim() }
    Write-Output '---'
}
