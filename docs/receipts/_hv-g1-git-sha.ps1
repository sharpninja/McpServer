$ErrorActionPreference = 'Continue'
Set-Location 'F:\GitHub\McpServer'
Write-Output ('HEAD=' + (git rev-parse HEAD))
git log -1 --format='HEAD_MSG=%H %ci %s'
Write-Output 'LIVE_MARKER_SHA=f4060f037e62e64974026aff9d24e11b2f481952'
git log -1 --format='LIVE_MSG=%H %ci %s' f4060f037e62e64974026aff9d24e11b2f481952
git merge-base --is-ancestor f4060f037e62e64974026aff9d24e11b2f481952 HEAD
Write-Output ('LIVE_IS_ANCESTOR_OF_HEAD=' + $LASTEXITCODE)
git merge-base --is-ancestor 0620078259d0be441d953fbaf457b0fdb670dbbc f4060f037e62e64974026aff9d24e11b2f481952
Write-Output ('HEAD_IS_ANCESTOR_OF_LIVE=' + $LASTEXITCODE)
Write-Output 'COMMITS_LIVE_TO_HEAD'
git log --oneline f4060f037e62e64974026aff9d24e11b2f481952..HEAD | Select-Object -First 30
Write-Output 'C81_STORE'
git log -1 --format='%H %ci %s' c81abaf0
git merge-base --is-ancestor c81abaf0 HEAD
Write-Output ('C81_IS_ANCESTOR_OF_HEAD=' + $LASTEXITCODE)
git merge-base --is-ancestor c81abaf0 f4060f037e62e64974026aff9d24e11b2f481952
Write-Output ('C81_IS_ANCESTOR_OF_LIVE=' + $LASTEXITCODE)
Write-Output 'SHOW_APPLY_ON_HEAD'
git grep -n "IsSupersededHookPersist" 0620078259d0be441d953fbaf457b0fdb670dbbc -- src/McpServer.Services/Services/SessionLogService.cs
Write-Output 'SHOW_APPLY_ON_LIVE'
git grep -n "IsSupersededHookPersist" f4060f037e62e64974026aff9d24e11b2f481952 -- src/McpServer.Services/Services/SessionLogService.cs
