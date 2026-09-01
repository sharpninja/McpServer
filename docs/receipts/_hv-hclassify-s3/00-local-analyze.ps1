#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$outDir = Join-Path $workspace 'docs\receipts\_hv-hclassify-s3'
$scratch = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-498d465c218e\implementer'
$receiptMatrix = Join-Path $workspace 'docs\receipts\todo-audit-20260820T101500Z\s3-matrix.json'
$scratchMatrix = Join-Path $scratch 's3-matrix.json'
$receiptInv = Join-Path $workspace 'docs\receipts\todo-audit-20260820T101500Z\s0-inventory.json'
$scratchInv = Join-Path $scratch 's0-inventory.json'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Set-Location -LiteralPath $workspace

function Get-Sha256File {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Write-JsonOut {
    param([string]$Name, [object]$Value)
    $path = Join-Path $outDir $Name
    ($Value | ConvertTo-Json -Depth 40) | Set-Content -LiteralPath $path -Encoding utf8
    return $path
}

# --- git / trust ---
. (Join-Path $pluginRoot 'lib\marker-resolver.ps1')
$sigOk = [bool](Test-MarkerSignature -MarkerFile $marker)
$baseUrl = Get-MarkerField -MarkerFile $marker -FieldName 'baseUrl'
$nonce = [guid]::NewGuid().ToString('N')
$health = $null
$nonceOk = $false
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health?nonce=$nonce" -TimeoutSec 10
    $nonceOk = ($health.nonce -eq $nonce)
} catch {
    $health = @{ error = $_.Exception.Message }
}

$head = (git rev-parse HEAD).Trim()
$branch = (git rev-parse --abbrev-ref HEAD).Trim()
$statusShort = @(git status --short)

Write-JsonOut '01-trust-git.json' ([ordered]@{
    timestampUtc = [datetime]::UtcNow.ToString('o')
    signatureOk = $sigOk
    nonce = $nonce
    nonceOk = $nonceOk
    health = $health
    head = $head
    branch = $branch
    statusShort = $statusShort
    pluginVersion = (Get-Content -LiteralPath (Join-Path $pluginRoot '.grok-plugin\plugin.json') -Raw | ConvertFrom-Json).version
})

# --- hashes ---
$hashReceiptMatrix = Get-Sha256File $receiptMatrix
$hashScratchMatrix = Get-Sha256File $scratchMatrix
$hashReceiptInv = Get-Sha256File $receiptInv
$hashScratchInv = Get-Sha256File $scratchInv

$receiptMatrixExists = Test-Path -LiteralPath $receiptMatrix
$scratchMatrixExists = Test-Path -LiteralPath $scratchMatrix
$s4GetsExists = Test-Path -LiteralPath (Join-Path $scratch 's4-todo-gets.json')

# --- parse ---
$inv = Get-Content -LiteralPath $receiptInv -Raw | ConvertFrom-Json
$mat = Get-Content -LiteralPath $receiptMatrix -Raw | ConvertFrom-Json
$matScratch = Get-Content -LiteralPath $scratchMatrix -Raw | ConvertFrom-Json

$openIds = @($inv.openTodoIds)
$itemIds = @($mat.items | ForEach-Object { [string]$_.id })
$missing = @($openIds | Where-Object { $_ -notin $itemIds })
$extra = @($itemIds | Where-Object { $_ -notin $openIds })
$dupIds = @($itemIds | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })

$staleOnly = @()
$noHeadCite = @()
$mentions0711 = @()
$doneTruePatches = @()
$donePropertyPresent = @()
$orphanNoReason = @()
$frIds = [System.Collections.Generic.HashSet[string]]::new()
$trIds = [System.Collections.Generic.HashSet[string]]::new()
$itemSummaries = @()

foreach ($it in @($mat.items)) {
    $id = [string]$it.id
    $rem = [string]$it.remaining
    $fr = @($it.functionalRequirements | ForEach-Object { [string]$_ })
    $tr = @($it.technicalRequirements | ForEach-Object { [string]$_ })
    $props = @($it.PSObject.Properties.Name)
    $hasDone = $props -contains 'done'
    $doneVal = $null
    if ($hasDone) { $doneVal = $it.done; $donePropertyPresent += $id }
    if ($hasDone -and [bool]$it.done) { $doneTruePatches += $id }

    $hasHeadSha = $rem.Contains('20db61aa0dd70f2d4f94da06d2a133ecfe6967a8')
    $hasAuditDate = $rem.Contains('2026-08-20T101500Z')
    $has0711 = $rem.Contains('2026-07-11')
    if ($has0711) { $mentions0711 += $id }
    if ($has0711 -and -not $hasAuditDate -and -not $hasHeadSha) { $staleOnly += $id }
    if (-not $hasHeadSha -and -not $hasAuditDate) { $noHeadCite += $id }

    $emptyLinks = ($fr.Count -eq 0) -and ($tr.Count -eq 0)
    $hasOrphan = $rem.Contains('OrphanReason')
    if ($emptyLinks -and -not $hasOrphan) { $orphanNoReason += $id }

    foreach ($x in $fr) { if ($x) { [void]$frIds.Add($x) } }
    foreach ($x in $tr) { if ($x) { [void]$trIds.Add($x) } }

    $itemSummaries += [ordered]@{
        id = $id
        class = [string]$it.class
        priority = [string]$it.priority
        section = [string]$it.section
        hasDoneProperty = $hasDone
        done = $doneVal
        fr = $fr
        tr = $tr
        hasHeadSha = $hasHeadSha
        hasAuditDate = $hasAuditDate
        has0711 = $has0711
        hasOrphanReason = $hasOrphan
        remainingLength = $rem.Length
        remainingPreview = if ($rem.Length -gt 280) { $rem.Substring(0, 280) } else { $rem }
    }
}

$e1 = @($mat.items | Where-Object { $_.id -eq 'PLAN-QUADBRAIN-E1-001' }) | Select-Object -First 1
$file1 = @($mat.items | Where-Object { $_.id -eq 'PLAN-FILETOOLS-001' }) | Select-Object -First 1
$del3 = @($mat.items | Where-Object { $_.id -eq 'PLAN-DELETECOMPLIANCE-003' }) | Select-Object -First 1
$keepOpenIds = @(
    'PLAN-QUADBRAIN-001','PLAN-QUADBRAIN-I1-001','PLAN-QUADBRAIN-E1-001','PLAN-QUADBRAIN-C1-001','PLAN-QUADBRAIN-C2-001','PLAN-QUADBRAIN-C3-001','PLAN-QUADBRAIN-T1-001',
    'PLAN-FILETOOLS-001','PLAN-FILETOOLS-002','PLAN-FILETOOLS-003','PLAN-FILETOOLS-004',
    'MCP-HANDOFF-001','MCP-HANDOFFPLAN-001','MCP-HANDOFFREVIEW-001',
    'BUG-TRIAGE-160','BUG-TRIAGE-161','BUG-TRIAGE-162','BUG-TRIAGE-163'
)
$keepOpenMissing = @($keepOpenIds | Where-Object { $_ -notin $itemIds })
$keepOpenDone = @($itemSummaries | Where-Object { $_.id -in $keepOpenIds -and $_.done -eq $true })
$keepOpenNoStoreClose = @()
foreach ($kid in $keepOpenIds) {
    $row = @($mat.items | Where-Object { $_.id -eq $kid }) | Select-Object -First 1
    if ($null -eq $row) { continue }
    $rem = [string]$row.remaining
    $keepOpenNoStoreClose += [ordered]@{
        id = $kid
        saysDoNotStoreClose = ($rem -match 'Do not store-close|Keep open|Do not set done:true')
        remainingPreview = if ($rem.Length -gt 220) { $rem.Substring(0,220) } else { $rem }
    }
}

$reqPatches = @($mat.requirementPatches)
$reqActions = @($reqPatches | ForEach-Object { [string]$_.action } | Sort-Object -Unique)
$reqIds = @($reqPatches | ForEach-Object { [string]$_.id })
$handoffInDefer = @($reqIds | Where-Object { $_ -like '*HANDOFF*' })
$deleteActions = @($reqPatches | Where-Object { [string]$_.action -eq 'delete' })
$expectedDefer = @('[]','TR-02','TR-03','TR-04','TR-05','TR-06','TR-07','TR-08','TR-09','TR-10','TR-11','TR-12','TR-13','TR-14')
$missingDefer = @($expectedDefer | Where-Object { $_ -notin $reqIds })
$extraDefer = @($reqIds | Where-Object { $_ -notin $expectedDefer })

# HEAD greps
$renameHits = @(Select-String -Path (Join-Path $workspace 'src\*\Migrations\20260720170000_RenameQuadBrainRolesToCreativityLogic.cs') -Pattern 'RenameQuadBrainRolesToCreativityLogic' -SimpleMatch -ErrorAction SilentlyContinue)
$renameFiles = @(Get-ChildItem -Path (Join-Path $workspace 'src') -Recurse -Filter '20260720170000_RenameQuadBrainRolesToCreativityLogic.cs' | ForEach-Object { $_.FullName })
$repoFileService = Test-Path -LiteralPath (Join-Path $workspace 'src\McpServer.Services\Services\RepoFileService.cs')
$repoFileTests = Test-Path -LiteralPath (Join-Path $workspace 'tests\McpServer.Support.Mcp.Tests\Services\RepoFileServiceTests.cs')
$mcpToolReadFile = @(Select-String -Path (Join-Path $workspace 'src\**\*.cs') -Pattern 'read_file' -SimpleMatch -ErrorAction SilentlyContinue | Where-Object { $_.Path -match 'Tools' } | Select-Object -First 30 | ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim() })

$e1Rem = if ($e1) { [string]$e1.remaining } else { '' }
$fileRem = if ($file1) { [string]$file1.remaining } else { '' }
$delFr = if ($del3) { @($del3.functionalRequirements) } else { @('MISSING') }

Write-JsonOut '02-hashes.json' ([ordered]@{
    receiptMatrixExists = $receiptMatrixExists
    scratchMatrixExists = $scratchMatrixExists
    hashReceiptMatrix = $hashReceiptMatrix
    hashScratchMatrix = $hashScratchMatrix
    matricesEqual = ($hashReceiptMatrix -eq $hashScratchMatrix)
    hashReceiptInv = $hashReceiptInv
    hashScratchInv = $hashScratchInv
    inventoriesEqual = ($hashReceiptInv -eq $hashScratchInv)
    s4TodoGetsExists = $s4GetsExists
    scratchFiles = @(Get-ChildItem -LiteralPath $scratch -File | ForEach-Object { $_.Name })
})

Write-JsonOut '03-matrix-coverage.json' ([ordered]@{
    invOpenCount = $inv.openTodoCount
    invOpenIdsCount = $openIds.Count
    matrixCountField = $mat.count
    matrixItemsCount = $itemIds.Count
    scratchMatrixCount = $matScratch.count
    scratchItemsCount = @($matScratch.items).Count
    missingFromMatrix = $missing
    extraInMatrix = $extra
    duplicateIds = $dupIds
    leftoverInOpen = ($openIds -contains 'PLAN-TRIAGELEFTOVER-001')
    leftoverInMatrix = ($itemIds -contains 'PLAN-TRIAGELEFTOVER-001')
    leftoverDoneFlagInInv = $inv.leftoverDone
})

Write-JsonOut '04-remaining-attacks.json' ([ordered]@{
    staleOnly0711 = $staleOnly
    noHeadCite = $noHeadCite
    mentions0711 = $mentions0711
    doneTruePatches = $doneTruePatches
    donePropertyPresent = $donePropertyPresent
    orphanNoReason = $orphanNoReason
    keepOpenMissing = $keepOpenMissing
    keepOpenDone = $keepOpenDone
    keepOpenNoStoreClose = $keepOpenNoStoreClose
    deleteComplianceFr = $delFr
    e1AdmitsRename = ($e1Rem -match 'RenameQuadBrainRolesToCreativityLogic')
    e1Remaining = $e1Rem
    filetoolsHasRepoFileService = ($fileRem -match 'RepoFileService')
    filetoolsSaysReadFileMissing = ($fileRem -match 'read_file') -and ($fileRem -match 'absent')
    filetoolsRemaining = $fileRem
})

Write-JsonOut '05-req-patches.json' ([ordered]@{
    count = $reqPatches.Count
    ids = $reqIds
    actions = $reqActions
    deleteActions = @($deleteActions | ForEach-Object { $_.id })
    handoffInDefer = $handoffInDefer
    expectedDefer = $expectedDefer
    missingDefer = $missingDefer
    extraDefer = $extraDefer
    patches = $reqPatches
})

Write-JsonOut '06-item-summaries.json' ([ordered]@{
    uniqueFr = @($frIds)
    uniqueTr = @($trIds)
    items = $itemSummaries
})

Write-JsonOut '07-head-code.json' ([ordered]@{
    renameFiles = $renameFiles
    repoFileServiceExists = $repoFileService
    repoFileTestsExist = $repoFileTests
    mcpToolReadFileHits = $mcpToolReadFile
})

Write-Output 'LOCAL_OK'
Write-Output ('HEAD=' + $head)
Write-Output ('SIG=' + $sigOk)
Write-Output ('NONCE=' + $nonceOk)
Write-Output ('MATRIX_EQ=' + ($hashReceiptMatrix -eq $hashScratchMatrix))
Write-Output ('COUNT=' + $itemIds.Count)
Write-Output ('MISSING=' + ($missing -join ','))
Write-Output ('STALE_ONLY=' + ($staleOnly -join ','))
Write-Output ('NO_HEAD=' + ($noHeadCite -join ','))
Write-Output ('DONE_TRUE=' + ($doneTruePatches -join ','))
Write-Output ('ORPHAN_BAD=' + ($orphanNoReason -join ','))
Write-Output ('LEFTOVER_IN_MATRIX=' + ($itemIds -contains 'PLAN-TRIAGELEFTOVER-001'))
Write-Output ('E1_RENAME=' + ($e1Rem -match 'RenameQuadBrainRolesToCreativityLogic'))
Write-Output ('FILE_REPO=' + ($fileRem -match 'RepoFileService'))
Write-Output ('FILE_READ_ABSENT=' + (($fileRem -match 'read_file') -and ($fileRem -match 'absent')))
Write-Output ('DEL_FR=' + ($delFr -join ','))
Write-Output ('REQ_DELETE=' + @($deleteActions).Count)
Write-Output ('HANDOFF_DEFER=' + ($handoffInDefer -join ','))
