# Use Case Canvas Engine Spike (S1)

**TimestampUtc:** 2026-08-08T10:25:00Z  
**Decision:** **JointJS (community, MPL-2.0)** as default in-product canvas engine for S5.

## Criteria evaluated

| Criterion | JointJS | draw.io embed | React Flow + custom shapes |
|-----------|---------|---------------|----------------------------|
| UML use-case demo / shapes | Official use-case demo | Full UML library | DIY actors/ovals |
| Embed in `/usecases/` static host | Yes (JS/SVG) | Heavy iframe / app | Yes (React stack not current UI) |
| Self-host assets | Yes | Large package | Yes |
| License | MPL-2.0 (review copyleft for shipped assets) | Apache-2.0 | MIT |
| Drag place / connect / rename | Demo proves | Proven | Build-from-scratch cost |
| Graph JSON serialize | Natural | Draw.io XML intermediate | Natural |

## Rationale

1. Matches classic UML UC interaction without draw.io format lock-in.  
2. Avoids React rewrite of existing vanilla `/usecases/` shell.  
3. Export remains our Mermaid/PlantUML pure service (S2), not vendor format.  
4. Spike does **not** claim S5 complete; only freezes engine choice for implementation.

## Rejected for primary

- **draw.io embed:** gold UX, but intermediate XML and heavy hosting.  
- **React Flow:** fine library; wrong stack cost for current wwwroot vanilla UI.

## Next

S3 storage + CQRS graph, then S5 JointJS canvas wiring to PUT/GET diagram-graph.
