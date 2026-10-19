# 3. Render with the Structurizr static export and headless Chromium

Date: 2026-10-19

## Status

Accepted

## Context

Markdown cannot render a Structurizr workspace, so documents need committed images of the views. The pinned CLI exports a static site that uses the Structurizr renderer, and it exports PlantUML; only the static site draws every view kind with the Structurizr renderer.

## Decision

Views are rendered by the Structurizr static export opened in headless Chromium over the DevTools protocol, in light mode, with structurizr.diagram.exportCurrentDiagramToSVG(false) per view (operator answer "Structurizr static + Chromium"). The workspace sets the view-set property structurizr.metadata to false.

## Consequences

Renders are SVG drawn by the Structurizr renderer. With the metadata property off, two runs produce byte-identical files; with it on, the export carries a hidden render date. The exported site loads themes from the network, so the workspace references no theme and the target fails clearly when the viewer never becomes ready.
