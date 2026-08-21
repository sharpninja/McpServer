$ErrorActionPreference = 'Stop'
Set-Location 'F:\GitHub\McpServer'
$live = 'f4060f037e62e64974026aff9d24e11b2f481952'
git show "${live}:src/McpServer.Services/Services/SessionLogService.cs" |
    Select-String -Pattern 'ApplyTurnContext|ValidateForNewEntry|NoneSentinel|canceled|cancelled|IsSuperseded' |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line }
Write-Output '----- SNIPPET -----'
$lines = git show "${live}:src/McpServer.Services/Services/SessionLogService.cs"
$arr = $lines -split "`n"
# print around first ValidateForNewEntry
$idx = 0
for ($i=0; $i -lt $arr.Length; $i++) {
    if ($arr[$i] -match 'ValidateForNewEntry') {
        $start = [Math]::Max(0, $i-25)
        $end = [Math]::Min($arr.Length-1, $i+20)
        Write-Output ("AROUND_LINE=" + ($i+1))
        for ($j=$start; $j -le $end; $j++) {
            Write-Output (('{0,5}|{1}' -f ($j+1), $arr[$j]))
        }
        $idx++
        if ($idx -ge 2) { break }
    }
}
