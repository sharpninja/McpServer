# 4. The ArchitectureDiagrams Nuke target enforces the rule

Date: 2026-10-19

## Status

Accepted

## Context

Builds and checks run as Nuke targets, and Nuke behavior is accountable only through TR-MCP-NUKE technical requirements under FR-MCP-BUILD-001. A standing code freeze applied; the operator exempted this one target (answer "Exempt the Nuke target").

## Decision

The target ArchitectureDiagrams (TR-MCP-NUKE-017) validates the workspace with structurizr/cli 2025.11.09 pinned by digest, fails on any inspection error, renders every view, removes renders without a view, and fails on Mermaid fences or .mmd files outside docs/receipts, on image links to undefined views, on views without renders, and on renders outside the page standard. It honors the Nuke parameter WhatIf (--what-if), named as PLAN-BUILDWHATIF-001 names it, because the build.ps1 pass-through of TR-MCP-NUKE-005 is not implemented yet (operator answer "Target-level --what-if").

## Consequences

The one-definition rule is checked mechanically on every run. Structurizr CLI and Chromium must be available where the target runs.
