#Requires -Version 7.0

BeforeAll {
    $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
}

Describe 'PowerShell-only plugin migration requirements' {
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
