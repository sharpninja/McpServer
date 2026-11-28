#Requires -Version 7.0
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }

BeforeAll {
    Import-Module "$PSScriptRoot/../scripts/PlanMigrationHandoff.psm1" -Force
    $script:roster = @(0..12 | ForEach-Object {
        @{
            legacyTaskNumber = $_
            phaseId          = "PHASE-WEBREQS-$($_ + 1)"
            todoId           = 'SLICE-WEBREQS-{0:d3}' -f ($_ + 1)
            sourceAnchor     = "legacy.md#task-$_"
        }
    })
    $script:bindings = @($script:roster | ForEach-Object { @{ sliceKey = $_.phaseId; todoId = $_.todoId } })
    $script:expected = @($script:bindings | ForEach-Object { $_.sliceKey })
    $script:ledger = @($script:bindings | ForEach-Object {
        @{
            sliceKey = $_.sliceKey
            todoId   = $_.todoId
            status   = 'pending'
            proposal = 'original proposal'
            approval = 'approval-1'
            receipt  = 'receipt-1'
            blocker  = 'waiting for binding'
        }
    })
}

Describe 'Read-only slice binding reconciliation' {
    It 'clears all 13 stale blockers and preserves ledger history' {
        $before = $script:ledger | ConvertTo-Json -Depth 4 -Compress
        $result = Compare-PlanMigrationBinding -ExpectedSliceKeys $script:expected -Ledger $script:ledger -SavedBindings $script:bindings -VerifiedReadbacks $script:bindings

        $result.currentBindingBlockers.Count | Should -Be 0
        $result.supersededPendingEntries.Count | Should -Be 13
        ($result.supersededPendingEntries | ConvertTo-Json -Depth 4 -Compress) | Should -BeExactly $before
        ($script:ledger | ConvertTo-Json -Depth 4 -Compress) | Should -BeExactly $before
    }

    It 'keeps missing or conflicting readbacks blocked' {
        $readbacks = @(@{ sliceKey = $script:expected[1]; todoId = 'SLICE-OTHER-001' }) + $script:bindings[2..12]
        $result = Compare-PlanMigrationBinding -ExpectedSliceKeys $script:expected -Ledger $script:ledger -SavedBindings $script:bindings -VerifiedReadbacks $readbacks

        ($result.currentBindingBlockers.sliceKey -join ',') | Should -BeExactly ($script:expected[0..1] -join ',')
        $result.supersededPendingEntries.Count | Should -Be 11
    }

    It 'reports a required slice omitted from the ledger and readbacks' {
        $result = Compare-PlanMigrationBinding -ExpectedSliceKeys $script:expected -Ledger $script:ledger[1..12] -SavedBindings $script:bindings -VerifiedReadbacks $script:bindings[1..12]

        $result.currentBindingBlockers.Count | Should -Be 1
        $result.currentBindingBlockers[0].sliceKey | Should -BeExactly $script:expected[0]
        $result.currentBindingBlockers[0].reason | Should -BeExactly 'Expected slice is missing from the ledger'
        $result.supersededPendingEntries.Count | Should -Be 12
    }

    It 'keeps an expected slice visible even when all three evidence sets omit it' {
        $result = Compare-PlanMigrationBinding -ExpectedSliceKeys @('Phase-A') -Ledger @() -SavedBindings @() -VerifiedReadbacks @()

        $result.currentBindingBlockers.Count | Should -Be 1
        $result.currentBindingBlockers[0].sliceKey | Should -BeExactly 'Phase-A'
        $result.supersededPendingEntries.Count | Should -Be 0
    }

    It 'reports a binding with no authoritative slice and rejects ambiguous keys' {
        $saved = $script:bindings + @{ sliceKey = 'PHASE-OTHER'; todoId = 'SLICE-OTHER-001' }
        $result = Compare-PlanMigrationBinding -ExpectedSliceKeys $script:expected -Ledger $script:ledger -SavedBindings $saved -VerifiedReadbacks $script:bindings

        $result.currentBindingBlockers.Count | Should -Be 1
        $result.currentBindingBlockers[0].sliceKey | Should -BeExactly 'PHASE-OTHER'
        { Compare-PlanMigrationBinding -ExpectedSliceKeys ($script:expected + $script:expected[0]) -Ledger $script:ledger -SavedBindings $script:bindings -VerifiedReadbacks $script:bindings } |
            Should -Throw -ExpectedMessage '*Duplicate expected slice*'
        { Compare-PlanMigrationBinding -ExpectedSliceKeys $script:expected -Ledger ($script:ledger + $script:ledger[0]) -SavedBindings $script:bindings -VerifiedReadbacks $script:bindings } |
            Should -Throw -ExpectedMessage '*Duplicate ledger slice*'
    }
}

Describe 'One-based packet task projection' {
    It 'retains source identities and maps historical Task 0 to packet Task 1' {
        $crosswalk = @(ConvertTo-PlanMigrationTaskCrosswalk -Rows $script:roster[12..0] -VerifiedSourceCount 13)

        $crosswalk.Count | Should -Be 13
        foreach ($index in 0, 12) {
            $crosswalk[$index].packetTaskNumber | Should -Be ($index + 1)
            foreach ($field in 'legacyTaskNumber', 'phaseId', 'todoId', 'sourceAnchor') {
                $crosswalk[$index].$field | Should -BeExactly $script:roster[$index][$field]
            }
        }
        Resolve-PlanMigrationPacketTask -Crosswalk $crosswalk -Numbering legacy -TaskNumber 0 | Should -Be 1
        Resolve-PlanMigrationPacketTask -Crosswalk $crosswalk -Numbering packet -TaskNumber 13 | Should -Be 13
        $script:roster[0].legacyTaskNumber | Should -Be 0
    }

    It 'rejects incomplete rosters, duplicate tasks or TODOs, and unresolved references' {
        { ConvertTo-PlanMigrationTaskCrosswalk -Rows $script:roster[1..12] -VerifiedSourceCount 13 } |
            Should -Throw -ExpectedMessage '*Incomplete verified legacy roster*'
        { ConvertTo-PlanMigrationTaskCrosswalk -Rows (@($script:roster[0]) + $script:roster[0..11]) -VerifiedSourceCount 13 } |
            Should -Throw -ExpectedMessage '*Duplicate legacy task number*'
        $duplicate = $script:roster[0].Clone()
        $duplicate.todoId = $script:roster[1].todoId
        { ConvertTo-PlanMigrationTaskCrosswalk -Rows (@($duplicate) + $script:roster[1..12]) -VerifiedSourceCount 13 } |
            Should -Throw -ExpectedMessage '*Duplicate slice TODO*'
        $crosswalk = @(ConvertTo-PlanMigrationTaskCrosswalk -Rows $script:roster -VerifiedSourceCount 13)
        { Resolve-PlanMigrationPacketTask -Crosswalk $crosswalk -Numbering 'unknown' -TaskNumber 1 } |
            Should -Throw -ExpectedMessage '*numbering basis*'
        { Resolve-PlanMigrationPacketTask -Crosswalk $crosswalk -Numbering legacy -TaskNumber 0 -TodoId $script:roster[1].todoId } |
            Should -Throw -ExpectedMessage '*Unresolved*'
        { Resolve-PlanMigrationPacketTask -Crosswalk ($crosswalk + $crosswalk[0]) -Numbering legacy -TaskNumber 0 } |
            Should -Throw -ExpectedMessage '*ambiguous*'
    }
}
