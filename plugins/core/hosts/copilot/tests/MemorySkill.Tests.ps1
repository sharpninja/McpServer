#Requires -Version 7.0

Describe 'Copilot memory skill and descriptor' {
    BeforeAll {
        $script:PluginRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).ProviderPath
        $script:Skill = Join-Path $script:PluginRoot 'skills/memory/SKILL.md'
        $script:Descriptor = Join-Path $script:PluginRoot 'memory-descriptor.json'
    }

    It 'loads the memory skill with required verbs and injection/fallback notes' {
        Test-Path -LiteralPath $script:Skill | Should -BeTrue
        $content = [System.IO.File]::ReadAllText($script:Skill)
        $content | Should -Match 'memory_remember'
        $content | Should -Match 'memory_recall'
        $content | Should -Match 'memory_explore'
        $content | Should -Match 'memory_consolidate'
        $content | Should -Match 'memory_promote'
        $content | Should -Match 'injection'
        $content | Should -Match 'fallback'
    }

    It 'loads the memory descriptor JSON without live cloud keys' {
        Test-Path -LiteralPath $script:Descriptor | Should -BeTrue
        $json = [System.IO.File]::ReadAllText($script:Descriptor) | ConvertFrom-Json
        $json.host | Should -Be 'copilot'
        $json.tools | Should -Contain 'memory_remember'
        $json.tools | Should -Contain 'memory_recall'
        $json.fallback.localFailsafe | Should -BeTrue
    }
}
