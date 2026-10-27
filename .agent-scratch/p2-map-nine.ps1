$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json'
$text = [System.IO.File]::ReadAllText($path)
$nl = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
$prefix = 'FR-MCP-SESSIONLIFE P2 cache identity metadata and outcomes.'
$tFresh = "$prefix.fresh prompt hook reports turn-opened after primary readback and turn-opened-degraded with the retained artifact"
$tConsumers = "$prefix.prompt stop-gate update and dialog consumers keep the full degraded cache omit empty query and do not claim server persistence"
$tMissing = "$prefix.missing-turn dialog recovery stays bounded and does not claim server persistence"
$tPayload = "$prefix.asserts the complete cached object and the serialized payload with MCP_PLUGIN_PERSIST_LOG unset"
$tTodo = "$prefix.rejects null empty and whitespace todoId and empty planFile on ordinary first persistence without a server row"
$tCanceled = "$prefix.stores exact None for canceled supersession when metadata is omitted and normalizes cancelled to canceled"
$csharp = @(
    'McpServer.Support.Mcp.Tests.Services.SessionLogTurnContextValidatorTests.ValidateForNewEntry_EmptyPlanFile_ThrowsArgumentException'
    'McpServer.Support.Mcp.Tests.Services.SessionLogTurnContextValidatorTests.ValidateForNewEntry_NullTodoId_ThrowsArgumentException'
    'McpServer.Support.Mcp.Tests.Services.SessionLogTurnContextValidatorTests.ValidateForNewEntry_EmptyTodoId_ThrowsArgumentException'
    'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.UpsertTurnAsync_NewTurnEmptyPlanFile_ThrowsAndDoesNotInsert'
    'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.UpsertTurnAsync_NewTurnNullTodoId_ThrowsAndDoesNotInsert'
    'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.UpsertTurnAsync_NewTurnEmptyTodoId_ThrowsAndDoesNotInsert'
    'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.UpsertTurnAsync_NewTurnWhitespaceTodoId_ThrowsAndDoesNotInsert'
    'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.UpsertTurnAsync_CanceledOmission_StoresExactNone'
    'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.UpsertTurnAsync_CancelledOmission_StoresExactNone'
    'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.SubmitAsync_EmptyPlanFileWithValidTodo_ThrowsAndInsertsNoTurn'
    'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.SubmitAsync_ValidPlanOmittedTodo_ThrowsAndInsertsNoTurn'
    'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.UpsertTurnAsync_ExistingTurnOmittingFields_PreservesStoredValues'
)
$pesterMeta = @($tTodo, $tCanceled)
$paths = @(
    'plugins/core/test-fixtures/pester/SessionLogP2Contracts.Tests.ps1'
    'tests/McpServer.Support.Mcp.Tests/Services/SessionLogTurnContextValidatorTests.cs'
    'tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTurnContextTests.cs'
)
$map = @{
    'FR-MCP-SESSIONLIFE-001-AC002' = @{ Names = @($tFresh); Mode = 'replace'; Paths = @($paths[0]) }
    'TR-MCP-SESSIONLIFE-001-AC001' = @{ Names = @($tFresh, $tConsumers); Mode = 'replace'; Paths = @($paths[0]) }
    'TR-MCP-SESSIONLIFE-001-AC002' = @{ Names = @($tConsumers, $tMissing); Mode = 'replace'; Paths = @($paths[0]) }
    'TEST-MCP-SESSIONLIFE-001-AC001' = @{ Names = @($tFresh, $tConsumers, $tMissing); Mode = 'replace'; Paths = @($paths[0]) }
    'TEST-MCP-SESSIONLIFE-001-AC002' = @{ Names = @($tPayload); Mode = 'replace'; Paths = @($paths[0]) }
    'FR-MCP-SESSIONLIFE-002-AC002' = @{ Names = @($pesterMeta + $csharp); Mode = 'append'; Paths = $paths }
    'TR-MCP-SESSIONLIFE-002-AC003' = @{ Names = @($pesterMeta + $csharp); Mode = 'append'; Paths = $paths }
    'TEST-MCP-SESSIONLIFE-002-AC001' = @{ Names = @($pesterMeta + $csharp); Mode = 'append'; Paths = $paths }
    'AC-FR-MCP-SESSIONLOGCTX-001-003' = @{ Names = @($pesterMeta + $csharp); Mode = 'append'; Paths = $paths }
}

function Get-ObjectSlice {
    param([string]$Text, [string]$Id)
    $marker = '"acceptanceCriterionId": "' + $Id + '"'
    $idx = $Text.IndexOf($marker)
    if ($idx -lt 0) { throw "missing $Id" }
    $next = $Text.IndexOf('"acceptanceCriterionId":', $idx + $marker.Length)
    if ($next -lt 0) { $next = $Text.Length }
    return @{ Start = $idx; End = $next }
}

function Replace-Array {
    param([string]$Slice, [string]$Key, [string[]]$Items)
    $keyToken = '"' + $Key + '": ['
    $start = $Slice.IndexOf($keyToken)
    if ($start -lt 0) { throw "missing $Key" }
    $open = $start + $keyToken.Length
    $depth = 1
    $i = $open
    while ($i -lt $Slice.Length -and $depth -gt 0) {
        $ch = $Slice[$i]
        if ($ch -eq '[') { $depth++ }
        elseif ($ch -eq ']') { $depth-- }
        $i++
    }
    if ($depth -ne 0) { throw "unclosed $Key" }
    $close = $i - 1
    $indentItem = '        '
    $indentClose = '      '
    $body = $nl + (($Items | ForEach-Object { $indentItem + '"' + ($_ -replace '\\','\\' -replace '"','\"') + '"' }) -join (',' + $nl)) + $nl + $indentClose
    $updated = $Slice.Substring(0, $open) + $body + $Slice.Substring($close)
    return $updated
}

function Add-Paths {
    param([string]$Slice, [string[]]$Add)
    $keyToken = '"testSourcePaths": ['
    $start = $Slice.IndexOf($keyToken)
    if ($start -lt 0) { throw 'missing testSourcePaths' }
    $open = $start + $keyToken.Length
    $depth = 1
    $i = $open
    while ($i -lt $Slice.Length -and $depth -gt 0) {
        $ch = $Slice[$i]
        if ($ch -eq '[') { $depth++ }
        elseif ($ch -eq ']') { $depth-- }
        $i++
    }
    $close = $i - 1
    $inner = $Slice.Substring($open, $close - $open)
    $existing = [regex]::Matches($inner, '"([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
    $merged = New-Object System.Collections.Generic.List[string]
    foreach ($item in @($existing)) { if ($item -and -not $merged.Contains($item)) { $merged.Add($item) } }
    foreach ($item in $Add) { if (-not $merged.Contains($item)) { $merged.Add($item) } }
    $indentItem = '        '
    $indentClose = '      '
    $body = $nl + (($merged | ForEach-Object { $indentItem + '"' + $_ + '"' }) -join (',' + $nl)) + $nl + $indentClose
    return $Slice.Substring(0, $open) + $body + $Slice.Substring($close)
}

foreach ($id in $map.Keys) {
    $sliceInfo = Get-ObjectSlice -Text $text -Id $id
    $slice = $text.Substring($sliceInfo.Start, $sliceInfo.End - $sliceInfo.Start)
    $spec = $map[$id]
    if ($spec.Mode -eq 'append') {
        $keyToken = '"criterionSpecificExistingTests": ['
        $start = $slice.IndexOf($keyToken)
        $open = $start + $keyToken.Length
        $depth = 1
        $i = $open
        while ($i -lt $slice.Length -and $depth -gt 0) {
            $ch = $slice[$i]
            if ($ch -eq '[') { $depth++ } elseif ($ch -eq ']') { $depth-- }
            $i++
        }
        $inner = $slice.Substring($open, ($i - 1) - $open)
        $existing = @([regex]::Matches($inner, '"((?:\\.|[^"\\])*)"') | ForEach-Object { $_.Groups[1].Value })
        $names = New-Object System.Collections.Generic.List[string]
        foreach ($item in $existing) { if ($item -and -not $names.Contains($item)) { $names.Add($item) } }
        foreach ($item in $spec.Names) { if (-not $names.Contains($item)) { $names.Add($item) } }
        $slice = Replace-Array -Slice $slice -Key 'criterionSpecificExistingTests' -Items @($names)
    } else {
        $slice = Replace-Array -Slice $slice -Key 'criterionSpecificExistingTests' -Items $spec.Names
    }
    $slice = Add-Paths -Slice $slice -Add $spec.Paths
    if ($slice -notmatch '"acceptanceState": "not-accepted"') { throw "$id acceptanceState drifted" }
    if ($slice -notmatch '"storeSatisfied": false') { throw "$id storeSatisfied drifted" }
    $text = $text.Substring(0, $sliceInfo.Start) + $slice + $text.Substring($sliceInfo.End)
    Write-Output ("MAPPED " + $id + " mode=" + $spec.Mode)
}

$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($path, $text, $utf8)
Write-Output 'WROTE manifest'
