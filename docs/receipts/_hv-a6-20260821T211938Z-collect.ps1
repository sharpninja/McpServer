$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-a6-20260821T211938Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$base = 'http://PAYTON-LEGION2:7147'
$key = 'tMxsITg8_OfmC8NrwlwMMCEYkr6lD0_A0zHjGU8egjg'
$ws = 'F:\GitHub\McpServer'
$headers = @{
    'X-Api-Key' = $key
    'X-Workspace-Path' = $ws
}

$nonce = [guid]::NewGuid().ToString('N')
$health = Invoke-WebRequest -Uri "$base/health?nonce=$nonce" -Headers $headers -UseBasicParsing
$health.Content | Set-Content -LiteralPath (Join-Path $outDir 'health.json') -Encoding utf8
"healthStatus=$($health.StatusCode)" | Set-Content -LiteralPath (Join-Path $outDir 'health-meta.txt') -Encoding utf8
"nonceSent=$nonce" | Add-Content -LiteralPath (Join-Path $outDir 'health-meta.txt') -Encoding utf8

function Get-Req {
    param([string]$Kind, [string]$Id)
    $uri = "$base/mcpserver/requirements/$Kind/$([uri]::EscapeDataString($Id))"
    try {
        $resp = Invoke-WebRequest -Uri $uri -Headers $headers -UseBasicParsing
        return [pscustomobject]@{
            Kind = $Kind
            Id = $Id
            StatusCode = [int]$resp.StatusCode
            Body = $resp.Content
        }
    }
    catch {
        $code = 0
        $body = $_.Exception.Message
        if ($_.Exception.Response) {
            $code = [int]$_.Exception.Response.StatusCode
            try {
                $stream = $_.Exception.Response.GetResponseStream()
                if ($stream) {
                    $reader = [System.IO.StreamReader]::new($stream)
                    $body = $reader.ReadToEnd()
                    $reader.Dispose()
                }
            } catch { }
        }
        return [pscustomobject]@{
            Kind = $Kind
            Id = $Id
            StatusCode = $code
            Body = $body
        }
    }
}

$ids = [ordered]@{
    fr = @(
        'FR-MCP-HOSTILEREVIEW-001','FR-MCP-HOSTILEREVIEW-002','FR-MCP-HOSTILEREVIEW-003','FR-MCP-HOSTILEREVIEW-004','FR-MCP-HOSTILEREVIEW-005','FR-MCP-HOSTILEREVIEW-006',
        'FR-MCP-HYGIENE-001','FR-MCP-HYGIENE-002','FR-MCP-HYGIENE-003','FR-MCP-HYGIENE-004','FR-MCP-HYGIENE-005',
        'FR-MCP-WIKIEXPORT-003','FR-MCP-WIKIEXPORT-004','FR-MCP-WIKIEXPORT-005',
        'FR-MCP-WIKIEXPORT-001','FR-MCP-WIKIEXPORT-002',
        'FR-MCP-PLUGINCORE-004','FR-MCP-REPL-009','FR-MCP-PLUGININT-001','FR-MCP-172'
    )
    tr = @(
        'TR-MCP-HOSTILEREVIEW-001','TR-MCP-HOSTILEREVIEW-002','TR-MCP-HOSTILEREVIEW-003','TR-MCP-HOSTILEREVIEW-004','TR-MCP-HOSTILEREVIEW-005','TR-MCP-HOSTILEREVIEW-006',
        'TR-MCP-HYGIENE-001','TR-MCP-HYGIENE-002','TR-MCP-HYGIENE-003','TR-MCP-HYGIENE-004','TR-MCP-HYGIENE-005',
        'TR-MCP-WIKIEXPORT-003','TR-MCP-WIKIEXPORT-004','TR-MCP-WIKIEXPORT-005',
        'TR-MCP-PLUGINCORE-004','TR-MCP-PLUGINCORE-005',
        'TR-MCP-REPL-010','TR-MCP-REPL-011','TR-MCP-REPL-012','TR-MCP-REPL-013',
        'TR-MCP-PERSIST-003','TR-MCP-PLUGININT-001'
    )
    test = @(
        'TEST-MCP-HOSTILEREVIEW-001','TEST-MCP-HOSTILEREVIEW-002','TEST-MCP-HOSTILEREVIEW-003','TEST-MCP-HOSTILEREVIEW-004','TEST-MCP-HOSTILEREVIEW-005','TEST-MCP-HOSTILEREVIEW-006',
        'TEST-MCP-HYGIENE-001','TEST-MCP-HYGIENE-002','TEST-MCP-HYGIENE-003','TEST-MCP-HYGIENE-004','TEST-MCP-HYGIENE-005',
        'TEST-MCP-WIKIEXPORT-003','TEST-MCP-WIKIEXPORT-004','TEST-MCP-WIKIEXPORT-005',
        'TEST-MCP-PLUGINCORE-004','TEST-MCP-PLUGINCORE-005',
        'TEST-MCP-REPL-025','TEST-MCP-REPL-026','TEST-MCP-REPL-027','TEST-MCP-REPL-028','TEST-MCP-REPL-041',
        'TEST-MCP-195','TEST-MCP-PLUGININT-001'
    )
    mapping = @(
        'FR-MCP-HOSTILEREVIEW-001','FR-MCP-HOSTILEREVIEW-002','FR-MCP-HOSTILEREVIEW-003','FR-MCP-HOSTILEREVIEW-004','FR-MCP-HOSTILEREVIEW-005','FR-MCP-HOSTILEREVIEW-006',
        'FR-MCP-HYGIENE-001','FR-MCP-HYGIENE-002','FR-MCP-HYGIENE-003','FR-MCP-HYGIENE-004','FR-MCP-HYGIENE-005',
        'FR-MCP-WIKIEXPORT-003','FR-MCP-WIKIEXPORT-004','FR-MCP-WIKIEXPORT-005',
        'FR-MCP-PLUGINCORE-004','FR-MCP-REPL-009','FR-MCP-PLUGININT-001','FR-MCP-172'
    )
}

$summaries = New-Object System.Collections.Generic.List[object]
foreach ($kind in @('fr','tr','test','mapping')) {
    foreach ($id in $ids[$kind]) {
        $got = Get-Req -Kind $kind -Id $id
        $safe = ($id -replace '[^A-Za-z0-9\-]', '_')
        $path = Join-Path $outDir "$kind-$safe.json"
        $got.Body | Set-Content -LiteralPath $path -Encoding utf8

        $acCount = $null
        $acTexts = @()
        $title = $null
        $status = $null
        $trIds = $null
        $testIds = $null
        $parsed = $null
        if ($got.StatusCode -eq 200) {
            try { $parsed = $got.Body | ConvertFrom-Json } catch { $parsed = $null }
        }
        if ($parsed) {
            $title = $parsed.title
            if (-not $title) { $title = $parsed.condition }
            $status = $parsed.status
            if ($parsed.PSObject.Properties.Name -contains 'acceptanceCriteria') {
                $ac = @($parsed.acceptanceCriteria)
                $acCount = $ac.Count
                $acTexts = @($ac | ForEach-Object { if ($null -eq $_) { '' } else { '{0}|sat={1}|{2}' -f $_.id, $_.isSatisfied, $_.text } })
            }
            if ($kind -eq 'mapping') {
                $trIds = @($parsed.trIds)
                $testIds = @($parsed.testIds)
            }
        }

        $summaries.Add([pscustomobject]@{
            Kind = $kind
            Id = $id
            StatusCode = $got.StatusCode
            Title = $title
            Status = $status
            AcCount = $acCount
            AcTexts = ($acTexts -join ' || ')
            TrIds = $(if ($trIds) { $trIds -join ',' } else { $null })
            TestIds = $(if ($testIds) { $testIds -join ',' } else { $null })
        })
    }
}

$summaries | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'summary.json') -Encoding utf8

# Compact TSV for review
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("Kind`tId`tHttp`tAcCount`tStatus`tTitle`tTrIds`tTestIds`tAcTexts")
foreach ($s in $summaries) {
    $lines.Add(("{0}`t{1}`t{2}`t{3}`t{4}`t{5}`t{6}`t{7}`t{8}" -f $s.Kind, $s.Id, $s.StatusCode, $s.AcCount, $s.Status, ($s.Title -replace "`t|`r|`n", ' '), $s.TrIds, $s.TestIds, ($s.AcTexts -replace "`t|`r|`n", ' ')))
}
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $outDir 'summary.tsv') -Encoding utf8

Write-Output "WROTE $outDir count=$($summaries.Count)"
