# 5. Element metadata stays visible; rendered text is 8 to 16 pt

Date: 2026-10-19

## Status

Accepted

## Context

The documentation page standard asked for rendered text between 8 and 12 pt on a letter page. Structurizr draws element names at 1.4 times the style font size and the element metadata line at 0.7 times, a fixed 2:1 spread, and hiding the metadata line is itself a Structurizr inspection error (views.styles.element.metadata).

## Decision

The metadata line stays visible, and the page standard for Structurizr renders is 8 pt minimum with element names up to 16 pt (10.67 to 21.33 px at 96 dpi), on one letter page (operator answer "Show it; widen AC-5 to 2:1"). TR-MCP-NUKE-017 AC-5 states the rule.

## Consequences

Every inspection stays enforced. Views too large for one letter page at that text size are split per functional group.
