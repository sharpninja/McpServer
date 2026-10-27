$ErrorActionPreference = 'Stop'
$gh = Get-Command gh -ErrorAction SilentlyContinue
if (-not $gh) { Write-Output 'GH_MISSING'; exit 0 }
& gh pr view 72 --repo sharpninja/McpServer --json number,state,isDraft,mergedAt,headRefOid,url,title
Write-Output ('GH_EXIT=' + $LASTEXITCODE)
