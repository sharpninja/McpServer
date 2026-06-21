@{
    RootModule        = 'McpRepl.psm1'
    ModuleVersion     = '1.0.0'
    GUID              = 'a8c4e2f1-9b3d-4e7a-8f2c-1d5e6f7a8b9c'
    Author            = 'SharpNinja'
    CompanyName       = 'SharpNinja'
    Copyright         = '(c) 2026 SharpNinja. All rights reserved.'
    Description       = 'Consolidated PowerShell module for McpServer REPL protocol, typed message entities, YAML serialization, and shared plugin helpers. Canonical home: tools/powershell/McpRepl in the main McpServer repository. Published to the PowerShell Gallery.'
    PowerShellVersion = '7.0'
    FunctionsToExport = @(
        'ConvertTo-McpYaml',
        'ConvertFrom-McpYaml',
        'New-McpRequest',
        'New-McpResult',
        'New-McpError',
        'New-McpEvent',
        'Invoke-McpRepl',
        'Invoke-McpReplRaw',
        'Resolve-McpCacheDir'
    )
    PrivateData = @{
        PSData = @{
            Tags         = @('McpServer', 'REPL', 'MCP', 'YAML', 'AgentPlugin', 'Grok', 'Claude', 'Codex')
            LicenseUri   = 'https://github.com/sharpninja/McpServer/blob/main/LICENSE'
            ProjectUri   = 'https://github.com/sharpninja/McpServer'
        }
    }
}
