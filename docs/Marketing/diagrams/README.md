# Marketing Diagrams

Mermaid source (`.mmd`) and rendered PNG images for use in the Canva website and presentation.

## Files

| Source | PNG | Description |
|---|---|---|
| `architecture.mmd` | `architecture.png` | Full architecture: agents → transports → McpServer components → workspace |
| `features.mmd` | `features.png` | Feature mindmap: 10 features grouped by category |
| `ui-tooling.mmd` | `ui-tooling.png` | UI surfaces: 6 tools and their connection to McpServer |
| `agent-workflow.mmd` | `agent-workflow.png` | Sequence diagram: agent session start → context → TODO → session log → sync |

## Regenerating

```powershell
$dir = "E:\github\McpServer\docs\Marketing\diagrams"
foreach ($d in @("architecture","features","ui-tooling","agent-workflow")) {
    mmdc -i "$dir\$d.mmd" -o "$dir\$d.png" -w 1400 -H 900 --backgroundColor white
}
```

Requires `mmdc`: `npm install -g @mermaid-js/mermaid-cli`
