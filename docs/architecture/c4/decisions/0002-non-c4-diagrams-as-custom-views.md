# 2. Activity, entity, state and topic diagrams as custom views

Date: 2026-10-19

## Status

Accepted

## Context

C4 has notation for context, container, component, deployment and dynamic views, but the documents also hold activity diagrams (one per unit of work), entity diagrams, state diagrams and a topic map. Structurizr supports custom elements and custom views, and the pinned CLI validates them.

## Decision

Activity, entity, state and topic diagrams are custom views over tagged custom elements in the same workspace (operator answer "Custom views"). Activity diagrams use elements tagged Activity with Start, Action, Decision (Diamond shape), Error and End; branch conditions are relationship descriptions. Entity diagrams use one element per entity tagged Entity, with keys and attributes as properties and cardinality on relationships. State diagrams use elements tagged State with transitions named by their trigger. Sequences are dynamic views; each alternative or optional branch is its own dynamic view, and each loop body is a nested dynamic view placed directly after its parent.

## Consequences

No second diagram language is needed. Imported diagram IDs (for example AD-TXN-001) are view keys, and branch IDs are element and relationship properties, so test mappings keep working (TR-MCP-TXNDIAGRAMS-001).
