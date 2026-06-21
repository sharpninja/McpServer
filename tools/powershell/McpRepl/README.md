# McpRepl - Shared PowerShell Module for McpServer Agent Plugins

This is the canonical home of the shared PowerShell module used by all PowerShell-based McpServer agent plugins (Grok, Claude, Codex, Copilot, etc.).

## Location

- Source: `tools/powershell/McpRepl` in the main McpServer repository
- Published: [PowerShell Gallery - McpRepl](https://www.powershellgallery.com/packages/McpRepl)

## What it provides

- Typed entities for REPL messages (`McpRequest`, `McpResult`, `McpError`, `McpEvent`)
- Proper YAML serialization (`ConvertTo-McpYaml` / `ConvertFrom-McpYaml`)
- High-level invocation helpers (`Invoke-McpRepl`, `Invoke-McpReplRaw`)
- Common utilities (`Resolve-McpCacheDir`, etc.)

## Usage in Plugins

Plugins should use the `Ensure-McpRepl.ps1` helper:

```powershell
$ensure = 'F:\GitHub\McpServer\tools\powershell\Ensure-McpRepl.ps1'
if (Test-Path $ensure) { . $ensure }
Import-Module McpRepl -MinimumVersion 1.0.0
```

## Publishing

The module is published automatically by the `publish_shared_modules` job in `azure-pipelines.yml` when changes land on `main`.

Manual publish (from this folder):

```powershell
Publish-Module -Path . -NuGetApiKey $env:PSGALLERY_API_KEY
```

## Testing

Pester 5.5+ is required.

```powershell
pwsh -NoProfile -Command "Invoke-Pester 'tools/powershell/McpRepl/McpRepl.Tests.ps1'"
```

All 7 tests (typed entities, correct `condition:` / `body:` emission for requirements resources, literal block scalars, round-tripping, module surface) must pass before publishing.

## Versioning

Follow semantic versioning. Update `ModuleVersion` in `McpRepl.psd1` for releases. (Bump when serializer or API behavior changes.)
