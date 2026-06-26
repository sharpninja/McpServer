#Requires -Version 7.0

BeforeAll {
    $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
    $script:FixtureRoot = Join-Path $script:RepoRoot 'plugins\core\test-fixtures\legacy-bats'
    if (-not (Test-Path -LiteralPath $script:FixtureRoot)) {
        $script:FixtureRoot = Join-Path $script:RepoRoot 'plugins\core\test-fixtures'
    }
    $script:ParityPath = Join-Path $PSScriptRoot 'bats-pester-parity.generated.json'

    function Get-BatsScenarios {
        $rows = [System.Collections.Generic.List[object]]::new()
        foreach ($file in Get-ChildItem -LiteralPath $script:FixtureRoot -Filter '*.bats' -File | Sort-Object Name) {
            $lines = [System.IO.File]::ReadAllLines($file.FullName)
            for ($index = 0; $index -lt $lines.Length; $index++) {
                if ($lines[$index] -match '^@test\s+"(?<name>.*)"\s+\{') {
                    $rows.Add([pscustomobject]@{
                        batsFile = ([System.IO.Path]::GetRelativePath($script:RepoRoot, $file.FullName).Replace('\', '/'))
                        batsLine = $index + 1
                        batsName = $matches['name']
                    })
                }
            }
        }

        return $rows
    }
}

Describe 'PowerShell-only plugin migration requirements' {
    It 'TEST-MCP-PLUGIN-PSONLY-001 inventories every current Bats scenario in the generated parity map' {
        $bats = @(Get-BatsScenarios)
        Test-Path -LiteralPath $script:ParityPath | Should -BeTrue

        $map = @([System.IO.File]::ReadAllText($script:ParityPath) | ConvertFrom-Json)
        $map.Count | Should -Be $bats.Count

        foreach ($scenario in $bats) {
            $match = @($map | Where-Object {
                    $sameFile = $_.batsFile -eq $scenario.batsFile
                    $sameLine = $_.batsLine -eq $scenario.batsLine
                    $sameName = $_.batsName -eq $scenario.batsName
                    $sameFile -and $sameLine -and $sameName
                })
            $match.Count | Should -Be 1
            $match[0].testRequirement | Should -Be 'TEST-MCP-PLUGIN-PSONLY-001'
            $match[0].pesterFile | Should -Be 'plugins/core/test-fixtures/pester/PluginBatsParity.Tests.ps1'
            $match[0].pesterName | Should -Match 'TEST-MCP-PLUGIN-PSONLY-001'
        }
    }

    It 'TEST-MCP-PLUGIN-PSONLY-002 rejects forbidden Bash and Node runtime files from shipped plugin packages' {
        $parentRoot = Split-Path -Parent $script:RepoRoot
        $siblingNames = @(
            'mcpserver-claude-code-plugin',
            'mcpserver-claude-cowork-plugin',
            'mcpserver-cline-plugin',
            'mcpserver-cline-v2-plugin',
            'mcpserver-codex-plugin',
            'mcpserver-copilot-plugin',
            'mcpserver-grok-plugin',
            'mcpserver-opencode-plugin'
        )
        $packageRoots = @((Join-Path $script:RepoRoot 'plugins\core\.staged-plugin')) + @($siblingNames | ForEach-Object { Join-Path $parentRoot $_ })
        $packageRoots = $packageRoots | Where-Object { Test-Path -LiteralPath $_ }

        $forbidden = [System.Collections.Generic.List[string]]::new()
        foreach ($root in $packageRoots) {
            foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) {
                $relative = [System.IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
                $isInstallerBootstrap = $relative -eq 'bootstrap/install-powershell.sh'
                if ($isInstallerBootstrap) { continue }
                if ($relative -match '(^|/)(\.git|node_modules|bin|obj|tests)(/|$)') { continue }
                if ($relative -notmatch '(^|/)(lib|hooks|skills)/' -and $relative -ne 'plugin.json' -and $relative -ne 'CORE-MANIFEST.yaml') { continue }

                $hasForbiddenPath = $relative -match '(^|/)(lib-sh|lib-node)(/|$)' `
                    -or $relative -match '\.(sh|bash)$' `
                    -or $relative -match '\.js$'

                if ($hasForbiddenPath) {
                    $forbidden.Add($relative)
                    continue
                }

                $textExtensions = @('.json', '.md', '.ps1', '.psm1', '.psd1', '.yaml', '.yml', '.txt')
                if ($textExtensions -contains $file.Extension.ToLowerInvariant()) {
                    $content = [System.IO.File]::ReadAllText($file.FullName)
                    if ($content -match '(?i)\b(bash|lib-sh|node\s|node\.exe|\.js)\b') {
                        $forbidden.Add($relative)
                    }
                }
            }
        }

        $forbidden | Should -BeNullOrEmpty
    }
}
