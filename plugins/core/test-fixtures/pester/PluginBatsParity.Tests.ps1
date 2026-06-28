#Requires -Version 7.0

BeforeAll {
    $script:ParityPath = Join-Path $PSScriptRoot 'bats-pester-parity.generated.json'
}

Describe 'TEST-MCP-PLUGIN-PSONLY-001 Bats parity matrix' {
    BeforeAll {
        $script:ParityRows = @([System.IO.File]::ReadAllText($script:ParityPath) | ConvertFrom-Json)
    }

    It 'TEST-MCP-PLUGIN-PSONLY-001 has one traceable Pester parity row per Bats scenario' {
        $script:ParityRows.Count | Should -BeGreaterThan 0
        ($script:ParityRows.pesterId | Select-Object -Unique).Count | Should -Be $script:ParityRows.Count
        foreach ($row in $script:ParityRows) {
            $row.testRequirement | Should -Be 'TEST-MCP-PLUGIN-PSONLY-001'
            $row.batsFile | Should -Match '^plugins/core/test-fixtures/(legacy-bats/)?[^/]+\.bats$'
            $row.pesterId | Should -Match '^PSONLY-[A-Z0-9-]+-\d{3}$'
        }
    }
}
