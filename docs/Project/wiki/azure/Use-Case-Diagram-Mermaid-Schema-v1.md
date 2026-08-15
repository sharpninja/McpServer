# McpServer UML Use-Case Diagram Mermaid Schema v1

**Status:** Stub for S0; goldens in TEST-MCP-USECASE-012 own the contract.  
**Header required in every export:** `%% mcp-usecase-diagram-schema:1`

## Graph JSON (source of truth)

```json
{
  "schemaVersion": 1,
  "kind": "uml-usecase",
  "systemBoundary": {
    "id": "sb1",
    "label": "System",
    "x": 200,
    "y": 80,
    "width": 420,
    "height": 320
  },
  "nodes": [
    { "id": "a1", "type": "actor", "label": "Customer", "x": 40, "y": 160 },
    { "id": "uc1", "type": "usecase", "label": "Place Order", "x": 320, "y": 180 }
  ],
  "edges": [
    { "id": "e1", "type": "association", "source": "a1", "target": "uc1" }
  ]
}
```

### Node types

- `actor`
- `usecase`

### Edge types

- `association`
- `include`
- `extend`
- `generalization`

### Rules

1. Deterministic export: sort nodes and edges by `id` ascending.  
2. Actors render outside the system boundary subgraph.  
3. Use cases with membership in boundary render inside subgraph.  
4. Empty graph: header + empty `flowchart LR` (exact empty form locked by golden test).  
5. Not a substitute for Mermaid native `usecaseDiagram` (upstream open: mermaid-js/mermaid#4628).  

## PlantUML

Parallel export uses standard PlantUML use-case syntax (`@startuml` / `@enduml`, `actor`, `usecase`, `include`, `extend`).

## Sequence diagrams

Separate path: flows/steps → `sequenceDiagram`. Not governed by this schema.
