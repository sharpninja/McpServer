# YAML Object Mutation

Agents must never update YAML by appending, replacing, or removing text lines.
Every YAML update must follow this flow:

1. Deserialize the complete document into an object.
2. Mutate the object by changing keys, arrays, or nested maps.
3. Serialize the object back to YAML.
4. Save the serialized document.

For PowerShell work, dot-source `plugins/core/lib-ps/yaml-object-mutation.ps1`
and use `Set-McpYamlObjectValue` or `Update-McpYamlObject`.

```powershell
. .\plugins\core\lib-ps\yaml-object-mutation.ps1

Set-McpYamlObjectValue `
    -Path .\appsettings.yaml `
    -KeyPath Triage,AgentPath `
    -Value 'codex' `
    -Create
```

When a change requires multiple fields, build the nested value as an ordered
PowerShell object and assign it as one object mutation. Do not construct YAML
snippets in strings, and do not use line-oriented edit helpers for YAML files.
