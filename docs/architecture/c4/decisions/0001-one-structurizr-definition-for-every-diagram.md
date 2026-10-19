# 1. One Structurizr definition for every diagram

Date: 2026-10-19

## Status

Accepted

## Context

The first C4 model of QBrain.AI (PR 84) shipped a Structurizr workspace beside Mermaid exports of its views and Mermaid entity diagrams, and the architecture, plan and marketing documents carried several hundred Mermaid diagrams of their own. The operator ruled on PR 84 (comment 6269382182): "Mermaid was supposed to be replaced by a single Structurizr C4 model encompassing the whole codebase." and then: "Every diagram in every doc should come from the same C4 definition."

## Decision

docs/architecture/c4/workspace.dsl is the only diagram source in the repository. It may include fragment files, but it is one workspace. Documents link rendered views; they never carry diagram source. Historical receipts under docs/receipts keep their recorded content.

## Consequences

Every element is defined once and reused by every view that shows it, so a change to a component appears in every diagram that includes it. Product features that emit Mermaid or PlantUML for users (use-case diagram export) are product output, not documentation, and are unaffected. The plan is PLAN-C4MODEL-001.
