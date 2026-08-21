$ErrorActionPreference = 'Stop'
$hist = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\chat_history.jsonl'
$wanted = @(510,545,501,505,507,508,517,523)
$i = 0
Get-Content -LiteralPath $hist | ForEach-Object {
    $i++
    if ($wanted -contains $i) {
        $snip = $_.Substring(0, [Math]::Min(500, $_.Length))
        Write-Output "===== L$i ====="
        Write-Output $snip
        Write-Output ''
    }
}
