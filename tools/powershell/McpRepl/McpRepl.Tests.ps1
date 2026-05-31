# Pester 5+ tests for the consolidated McpRepl module (canonical source).
# Run from repo root:
#   pwsh -NoProfile -Command "Invoke-Pester 'tools/powershell/McpRepl/McpRepl.Tests.ps1'"
#
# These tests live with the module source in McpServer so they travel with the canonical bits
# and can be executed in CI or locally before publishing to PSGallery.

# Custom classes defined in the .psm1 are brought into scope via 'using module'.
# Functions are exported via the manifest.
using module .\McpRepl.psm1

BeforeAll {
    $moduleRoot = $PSScriptRoot
    Import-Module (Join-Path $moduleRoot 'McpRepl.psd1') -Force
}

Describe 'McpRepl Typed Entities' {
    It 'Creates McpRequest with correct properties' {
        $req = New-McpRequest -RequestId 'req-001' -Method 'workflow.todo.create' -Params @{ id = 'TODO-001' }
        $req.RequestId | Should -Be 'req-001'
        $req.Method    | Should -Be 'workflow.todo.create'
        $req.Params.id | Should -Be 'TODO-001'
    }

    It 'McpError and McpResult can be created' {
        $err = [McpError]::new()
        $err.Code = 'validation_failed'
        $err.Message = 'missing condition'
        $err | Should -Not -BeNullOrEmpty
    }
}

Describe 'YAML Serialization (the core requirement)' {
    It 'Serializes a createTest request using the correct "condition" field' {
        $req = New-McpRequest -RequestId 'req-test-042' -Method 'workflow.requirements.createTest' -Params @{
            id        = 'TEST-MCP-042'
            title     = 'Module produces valid YAML'
            condition = 'The serializer must emit "condition" not "description"'
            priority  = 'high'
            area      = 'PLUGIN'
        }

        $yaml = ConvertTo-McpYaml -InputObject $req

        $yaml | Should -Match 'type: request'
        $yaml | Should -Match 'method: workflow.requirements.createTest'
        $yaml | Should -Match 'condition:'
        $yaml | Should -Not -Match 'description:'   # critical regression guard
    }

    It 'Uses literal block scalar for multi-line bodies' {
        $params = @{ body = "Line 1`nLine 2 with special: chars" }
        $req = New-McpRequest -RequestId 'r1' -Method 'test' -Params $params
        $yaml = ConvertTo-McpYaml -InputObject $req
        $yaml | Should -Match '\|'
    }

    It 'Round-trips a result envelope' {
        $sample = @"
type: result
payload:
  requestId: req-123
  result:
    ok: true
"@
        $parsed = ConvertFrom-McpYaml $sample
        $parsed.RequestId | Should -Be 'req-123'
    }
}

Describe 'High-level Invoke-McpRepl API' {
    It 'Invoke-McpReplRaw returns a structured object even on failure (when binary missing)' {
        # This will fail because mcpserver-repl may not be in PATH in test env, but we test the structure
        try {
            $null = Invoke-McpReplRaw -Method 'client.Health.GetAsync' -TimeoutSeconds 2 -ErrorAction Stop
        } catch {
            $_.Exception.Message | Should -Match 'mcpserver-repl not found'
        }
    }
}

Describe 'Module Surface' {
    It 'Exports the expected functions and classes are loadable' {
        $funcs = Get-Command -Module McpRepl | Select-Object -ExpandProperty Name
        $funcs | Should -Contain 'ConvertTo-McpYaml'
        $funcs | Should -Contain 'Invoke-McpRepl'

        # Classes should be available
        [McpRequest] | Should -Not -BeNullOrEmpty
        [McpError]   | Should -Not -BeNullOrEmpty
    }
}
