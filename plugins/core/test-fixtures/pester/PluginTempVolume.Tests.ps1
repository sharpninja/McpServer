# TEST-MCP-TEMPVOL-001: plugin TEMP/TMP alignment helper (FR-MCP-TEMPVOL-001 / TR-MCP-TEMPVOL-001).
#Requires -Version 7.0

Describe 'TEST-MCP-TEMPVOL-001 plugin same-volume TEMP alignment' {
    BeforeAll {
        $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        $script:LibRoot = Join-Path $script:RepoRoot 'plugins\core\lib-ps'
        $script:ResolveCachePath = Join-Path $script:LibRoot 'resolve-cache-dir.ps1'
        $script:PluginHookPath = Join-Path $script:LibRoot 'plugin-hook.ps1'
        $script:WrapperTemplatePath = Join-Path $script:RepoRoot 'plugins\core\hooks-templates\wrapper.ps1.template'
        $script:PromptTemplatePath = Join-Path $script:RepoRoot 'templates\prompt-templates.yaml'
        . $script:ResolveCachePath

        function script:Get-UnusedDriveLetter {
            foreach ($letter in [char[]](90..70)) {
                $root = '{0}:\' -f $letter
                if (-not (Test-Path -LiteralPath $root)) {
                    return [string]$letter
                }
            }

            return $null
        }
    }

    It 'defines Set-McpPluginSameVolumeTemp after sourcing resolve-cache-dir.ps1' {
        Get-Command Set-McpPluginSameVolumeTemp -ErrorAction Stop | Should -Not -BeNullOrEmpty
        Get-Command Invoke-McpPluginReplacementMove -ErrorAction Stop | Should -Not -BeNullOrEmpty
        Get-Command Get-McpPathVolumeRoot -ErrorAction Stop | Should -Not -BeNullOrEmpty
    }

    It 'leaves TEMP and TMP unchanged when they already share the target volume' {
        $workspace = Join-Path $TestDrive 'same-volume-ws'
        New-Item -ItemType Directory -Path $workspace | Out-Null
        $savedTemp = $env:TEMP
        $savedTmp = $env:TMP
        $processTemp = Join-Path $TestDrive 'process-temp'
        New-Item -ItemType Directory -Path $processTemp | Out-Null
        try {
            $env:TEMP = $processTemp
            $env:TMP = $processTemp
            $result = Set-McpPluginSameVolumeTemp -TargetPath $workspace
            $result.Succeeded | Should -BeTrue
            $result.Changed | Should -BeFalse
            $result.Error | Should -BeNullOrEmpty
            $env:TEMP | Should -Be $processTemp
            $env:TMP | Should -Be $processTemp
        } finally {
            $env:TEMP = $savedTemp
            $env:TMP = $savedTmp
        }
    }

    It 'TEMPVOL cross-volume target per platform: Windows moves TEMP and TMP to the target drive; a single-root system leaves them unchanged' {
        $savedTemp = $env:TEMP
        $savedTmp = $env:TMP
        $foreignTemp = Join-Path $TestDrive 'foreign-temp'
        New-Item -ItemType Directory -Path $foreignTemp | Out-Null
        if ($IsWindows) {
            $letter = Get-UnusedDriveLetter
            $letter | Should -Not -BeNullOrEmpty
            $workspaceHost = Join-Path $TestDrive 'cross-volume-ws'
            New-Item -ItemType Directory -Path $workspaceHost | Out-Null
            subst "$letter`:" $workspaceHost
            try {
                $env:TEMP = $foreignTemp
                $env:TMP = $foreignTemp
                $target = '{0}:\ws' -f $letter
                New-Item -ItemType Directory -Path $target | Out-Null
                $result = Set-McpPluginSameVolumeTemp -TargetPath $target
                $result.Succeeded | Should -BeTrue
                $result.Changed | Should -BeTrue
                $result.Error | Should -BeNullOrEmpty
                $alignedRoot = Get-McpPathVolumeRoot -Path $env:TEMP
                $targetRoot = Get-McpPathVolumeRoot -Path $target
                $alignedRoot | Should -Be $targetRoot
                (Get-McpPathVolumeRoot -Path $env:TMP) | Should -Be $targetRoot
                Test-Path -LiteralPath $env:TEMP -PathType Container | Should -BeTrue
                $probe = Join-Path $env:TEMP 'tempvol-write-probe.txt'
                Set-Content -LiteralPath $probe -Value 'ok'
                Test-Path -LiteralPath $probe -PathType Leaf | Should -BeTrue
            } finally {
                $env:TEMP = $savedTemp
                $env:TMP = $savedTmp
                subst "$letter`:" /d | Out-Null
            }
        }
        else {
            # TR-MCP-TEMPVOL-001 AC-001: every path shares the root '/', so TEMP and TMP stay where they are.
            $target = Join-Path $TestDrive 'single-root-ws'
            New-Item -ItemType Directory -Path $target | Out-Null
            try {
                $env:TEMP = $foreignTemp
                $env:TMP = $foreignTemp
                (Get-McpPathVolumeRoot -Path $target) | Should -Be (Get-McpPathVolumeRoot -Path $foreignTemp)
                $result = Set-McpPluginSameVolumeTemp -TargetPath $target
                $result.Succeeded | Should -BeTrue
                $result.Changed | Should -BeFalse
                $result.Error | Should -BeNullOrEmpty
                $env:TEMP | Should -Be $foreignTemp
                $env:TMP | Should -Be $foreignTemp
            } finally {
                $env:TEMP = $savedTemp
                $env:TMP = $savedTmp
            }
        }
    }

    It 'TEMPVOL uncreatable target per platform: Windows leaves TEMP unchanged with a visible error; a single-root system leaves it unchanged without one' {
        $savedTemp = $env:TEMP
        $savedTmp = $env:TMP
        $foreignTemp = Join-Path $TestDrive 'unchanged-temp'
        New-Item -ItemType Directory -Path $foreignTemp | Out-Null
        if ($IsWindows) {
            $letter = Get-UnusedDriveLetter
            $letter | Should -Not -BeNullOrEmpty
            $missingTarget = '{0}:\no-such-workspace' -f $letter
        }
        else {
            $missingTarget = Join-Path $TestDrive 'no-such-workspace'
        }
        try {
            $env:TEMP = $foreignTemp
            $env:TMP = $foreignTemp
            $result = Set-McpPluginSameVolumeTemp -TargetPath $missingTarget
            $result.Changed | Should -BeFalse
            $env:TEMP | Should -Be $foreignTemp
            $env:TMP | Should -Be $foreignTemp
            if ($IsWindows) {
                $result.Succeeded | Should -BeFalse
                $result.Error | Should -Not -BeNullOrEmpty
            }
            else {
                # TR-MCP-TEMPVOL-001 AC-001: the missing target shares the root '/', so nothing is attempted and no error is raised.
                $result.Succeeded | Should -BeTrue
                $result.Error | Should -BeNullOrEmpty
            }
        } finally {
            $env:TEMP = $savedTemp
            $env:TMP = $savedTmp
        }
    }

    It 'treats a failed replacement move as a visible error and leaves the destination unchanged' {
        $sourceDir = Join-Path $TestDrive 'move-src'
        $destDir = Join-Path $TestDrive 'move-dst'
        New-Item -ItemType Directory -Path $sourceDir | Out-Null
        New-Item -ItemType Directory -Path $destDir | Out-Null
        $source = Join-Path $sourceDir 'replacement.txt'
        $destination = Join-Path $destDir 'target.txt'
        Set-Content -LiteralPath $source -Value 'new-content'
        Set-Content -LiteralPath $destination -Value 'original-content'

        $letter = Get-UnusedDriveLetter
        $letter | Should -Not -BeNullOrEmpty
        $crossDest = '{0}:\target.txt' -f $letter
        $result = Invoke-McpPluginReplacementMove -SourcePath $source -DestinationPath $crossDest
        $result.Succeeded | Should -BeFalse
        $result.Error | Should -Not -BeNullOrEmpty
        $result.DestinationUnchanged | Should -BeTrue
        Get-Content -LiteralPath $destination -Raw | Should -Match 'original-content'
        Test-Path -LiteralPath $source -PathType Leaf | Should -BeTrue
    }

    It 'session-start and wrapper entrypoints call Set-McpPluginSameVolumeTemp' {
        $hookSource = Get-Content -LiteralPath $script:PluginHookPath -Raw
        $wrapperSource = Get-Content -LiteralPath $script:WrapperTemplatePath -Raw
        ($hookSource -match 'Set-McpPluginSameVolumeTemp') | Should -BeTrue
        ($hookSource -match '(?ms)function Start-PluginSession \{[\s\S]*Set-McpPluginSameVolumeTemp') | Should -BeTrue
        ($wrapperSource -match 'Set-McpPluginSameVolumeTemp') | Should -BeTrue
        ($wrapperSource -match 'resolve-cache-dir\.ps1') | Should -BeTrue
    }

    It 'keeps prompt-template same-volume TEMP and verify-after-edit guidance' {
        $prompt = Get-Content -LiteralPath $script:PromptTemplatePath -Raw
        $routing = [regex]::Match($prompt, '(?ms)## PowerShell\.Mcp Command Routing.*?(?=## )').Value
        $routing | Should -Not -BeNullOrEmpty
        ($routing -match 'same volume') | Should -BeTrue
        ($routing -match 'TEMP') | Should -BeTrue
        ($routing -match 'TMP') | Should -BeTrue
        ($routing -match 'verify the edit landed') | Should -BeTrue
        ($routing.Contains([char]0x2014)) | Should -BeFalse
        ($routing.Contains([char]0x2013)) | Should -BeFalse
    }

    It 'does not patch PSGallery PowerShell.MCP internals' {
        $helperSource = Get-Content -LiteralPath $script:ResolveCachePath -Raw
        $wrapperSource = Get-Content -LiteralPath $script:WrapperTemplatePath -Raw
        ($helperSource -match 'Add-LinesToFile') | Should -BeFalse
        ($helperSource -match 'Update-LinesInFile') | Should -BeFalse
        ($helperSource -match 'PSGallery') | Should -BeFalse
        ($wrapperSource -match 'Add-LinesToFile') | Should -BeFalse
        ($wrapperSource -match 'Update-LinesInFile') | Should -BeFalse
    }
}
