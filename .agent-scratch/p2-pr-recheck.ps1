$ErrorActionPreference = 'Stop'
Start-Sleep -Seconds 5
gh pr view 72 --repo sharpninja/McpServer --json headRefOid,isDraft,mergedAt,state,url,baseRefName
Write-Output 'UTC'
Write-Output ([DateTime]::UtcNow.ToString('o'))
