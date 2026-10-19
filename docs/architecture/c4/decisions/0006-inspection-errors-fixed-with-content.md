# 6. Structurizr inspection errors are fixed with content, never suppressed

Date: 2026-10-19

## Status

Accepted

## Context

The pinned CLI reported 18 inspection errors on the first model: no workspace scope, no documentation or decisions for the software system, and 15 deployment instances without relationships because the deployment environments were declared before the container relationships they replicate.

## Decision

Each error is fixed at its cause (operator answer "Real docs and ADRs"): the workspace scope is softwaresystem; the software system carries documentation and these decision records; deployment environments are declared after all relationships; and the Windows deployment shows the optional Subscriber process that is the only caller of the standalone Key Server. No inspection is set to ignore.

## Consequences

AC-1 of TR-MCP-NUKE-017 holds without exceptions, and new inspection errors fail the target.
