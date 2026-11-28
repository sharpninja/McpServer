#Requires -Version 7.0

function ConvertTo-HandoffSliceIndex {
    param([object[]] $Rows, [string] $Label)

    $index = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
    foreach ($row in $Rows) {
        $key = $row.sliceKey
        if ($key -isnot [string] -or [string]::IsNullOrWhiteSpace($key)) {
            throw "Missing key for $Label"
        }
        if (-not $index.TryAdd($key, $row)) {
            throw "Duplicate $($Label): $key"
        }
    }
    return ,$index
}

function Compare-PlanMigrationBinding {
    <#
    .SYNOPSIS
    Reconciles current bindings against independently verified source slices.
    .DESCRIPTION
    Reports missing or conflicting evidence and returns superseded ledger rows without editing them.
    Callers verify the scope and ownership of exact-ID TODO readbacks before supplying them.
    2026-11-28, PR #91 handoff: report a missing ledger row even when neither plan nor readback supplies it.
    .PARAMETER ExpectedSliceKeys
    Complete authoritative slice keys derived from the verified source, in source order.
    .PARAMETER Ledger
    Current ledger rows, including their historical provisioning status and receipts.
    .PARAMETER SavedBindings
    Exact bindings extracted from the saved migrated plan.
    .PARAMETER VerifiedReadbacks
    Verified bindings read back from the active QBrain workspace.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string[]] $ExpectedSliceKeys,
        [Parameter(Mandatory)] [AllowEmptyCollection()] [object[]] $Ledger,
        [Parameter(Mandatory)] [AllowEmptyCollection()] [object[]] $SavedBindings,
        [Parameter(Mandatory)] [AllowEmptyCollection()] [object[]] $VerifiedReadbacks
    )

    $expected = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($key in $ExpectedSliceKeys) {
        if ([string]::IsNullOrWhiteSpace($key)) {
            throw 'Missing expected slice key'
        }
        if (-not $expected.Add($key)) {
            throw "Duplicate expected slice: $key"
        }
    }

    $ledgerIndex = ConvertTo-HandoffSliceIndex -Rows $Ledger -Label 'ledger slice'
    $saved = ConvertTo-HandoffSliceIndex -Rows $SavedBindings -Label 'saved slice'
    $readbacks = ConvertTo-HandoffSliceIndex -Rows $VerifiedReadbacks -Label 'read-back slice'
    $blockers = [Collections.Generic.List[object]]::new()
    $superseded = [Collections.Generic.List[object]]::new()

    foreach ($key in $ExpectedSliceKeys) {
        $entry = $ledgerIndex[$key]
        $todoId = $null
        if ($null -eq $entry) {
            $reason = 'Expected slice is missing from the ledger'
        }
        else {
            $todoId = $entry.todoId
            $binding = $saved[$key]
            $readback = $readbacks[$key]
            if ($todoId -isnot [string] -or [string]::IsNullOrWhiteSpace($todoId) -or
                $binding.todoId -isnot [string] -or $binding.todoId -cne $todoId -or
                $readback.todoId -isnot [string] -or $readback.todoId -cne $todoId) {
                $reason = 'Saved binding and exact verified readback do not agree with ledger'
            }
            else {
                if ($entry.status -ceq 'pending') {
                    $superseded.Add($entry)
                }
                continue
            }
        }
        $blockers.Add([pscustomobject]@{ sliceKey = $key; todoId = $todoId; reason = $reason })
    }

    $extraKeys = [Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
    foreach ($index in $ledgerIndex, $saved, $readbacks) {
        foreach ($key in $index.Keys) {
            if (-not $expected.Contains($key)) {
                $null = $extraKeys.Add($key)
            }
        }
    }
    foreach ($key in $extraKeys) {
        $blockers.Add([pscustomobject]@{
            sliceKey = $key
            todoId   = $null
            reason   = 'Slice evidence does not match an authoritative source slice'
        })
    }

    [pscustomobject]@{
        currentBindingBlockers   = $blockers.ToArray()
        supersededPendingEntries = $superseded.ToArray()
    }
}

function ConvertTo-PlanMigrationTaskCrosswalk {
    <#
    .SYNOPSIS
    Projects a verified zero-based roster into one-based packet task numbers.
    .DESCRIPTION
    Retains legacy task numbers, canonical phase and TODO identities, and source anchors.
    2026-11-28, PR #91 handoff: retain the original numbering in the PowerShell port.
    .PARAMETER Rows
    Authoritative legacy rows in any order.
    .PARAMETER VerifiedSourceCount
    Number of legacy tasks independently verified from the source.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [AllowEmptyCollection()] [object[]] $Rows,
        [Parameter(Mandatory)] [int] $VerifiedSourceCount
    )

    if ($VerifiedSourceCount -lt 1 -or $Rows.Count -ne $VerifiedSourceCount) {
        throw 'Incomplete verified legacy roster'
    }

    $byNumber = [Collections.Generic.Dictionary[int, object]]::new()
    $todoIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($row in $Rows) {
        $number = $row.legacyTaskNumber
        if (($number -isnot [int] -and $number -isnot [long]) -or $number -lt 0 -or $number -ge $VerifiedSourceCount) {
            throw "Invalid legacy task number: $number"
        }
        if (-not $byNumber.TryAdd([int]$number, $row)) {
            throw "Duplicate legacy task number: $number"
        }
        foreach ($field in 'phaseId', 'todoId', 'sourceAnchor') {
            if ($row.$field -isnot [string] -or [string]::IsNullOrWhiteSpace($row.$field)) {
                throw "Missing $field for legacy task $number"
            }
        }
        if (-not $todoIds.Add($row.todoId)) {
            throw "Duplicate slice TODO: $($row.todoId)"
        }
    }

    for ($number = 0; $number -lt $VerifiedSourceCount; $number++) {
        $row = $byNumber[$number]
        [pscustomobject]@{
            packetTaskNumber = $number + 1
            legacyTaskNumber = $number
            phaseId          = $row.phaseId
            todoId           = $row.todoId
            sourceAnchor     = $row.sourceAnchor
        }
    }
}

function Resolve-PlanMigrationPacketTask {
    <#
    .SYNOPSIS
    Resolves a task reference with an explicit numbering basis.
    .DESCRIPTION
    Requires exactly one match and, when supplied, the exact TODO ID.
    2026-11-28, PR #91 handoff: retain ambiguity and exact-ID checks in the PowerShell port.
    .PARAMETER Crosswalk
    Rows from ConvertTo-PlanMigrationTaskCrosswalk.
    .PARAMETER Numbering
    Explicit basis, either legacy or packet.
    .PARAMETER TaskNumber
    Task number in the supplied numbering basis.
    .PARAMETER TodoId
    Optional exact TODO ID to check against the task reference.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [AllowEmptyCollection()] [object[]] $Crosswalk,
        [Parameter(Mandatory)] [string] $Numbering,
        [Parameter(Mandatory)] [int] $TaskNumber,
        [string] $TodoId
    )

    $field = switch -CaseSensitive ($Numbering) {
        'legacy' { 'legacyTaskNumber' }
        'packet' { 'packetTaskNumber' }
        default { throw 'An explicit task numbering basis is required' }
    }

    $matchTodo = $PSBoundParameters.ContainsKey('TodoId')
    $matchingRows = @($Crosswalk | Where-Object { $_.$field -eq $TaskNumber -and (-not $matchTodo -or $_.todoId -ceq $TodoId) })
    if ($matchingRows.Count -ne 1) {
        throw 'Unresolved or ambiguous task reference'
    }
    $matchingRows[0].packetTaskNumber
}

Export-ModuleMember -Function Compare-PlanMigrationBinding, ConvertTo-PlanMigrationTaskCrosswalk, Resolve-PlanMigrationPacketTask
