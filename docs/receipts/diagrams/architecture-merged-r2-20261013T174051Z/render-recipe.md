<!-- SPDX-License-Identifier: Apache-2.0 -->
# Render recipe: architecture diagrams, round 2

These pages are the rendered form of every Mermaid diagram in `docs/architecture/SessionTurn-Review-AgentContext-Architecture.md` at the commit that adds this receipt. `manifest.json` names the architecture file's SHA-256, the render page's SHA-256, the survey of every diagram, and the SHA-256 of every page image. The render page `mermaid-render.html` holds all the diagram sources it rendered (the `blocks` array), in document order.

Renderer
- Mermaid imported from `https://cdn.jsdelivr.net/npm/mermaid@12/dist/mermaid.esm.min.mjs`. On 2026-10-13 that resolved to Mermaid 12.2.0 (the `package.json` jsDelivr serves for `mermaid@12`).
- Configuration: `theme: default`, `htmlLabels: false`, flowchart `curve: linear` (straight segments) and `useMaxWidth: false`, sequence `wrap: true` and `useMaxWidth: false`, ER and state `useMaxWidth: false`.
- Layout per diagram, written as frontmatter (`config: layout:`) before each source:
  - `elk` for `flowchart`, `graph`, and `erDiagram`. Mermaid 12.2.0 honors it with no registered loader: a probe flowchart rendered with `elk` differs from the same source rendered with `dagre`, the ELK output carries ELK markers, and no warning is logged.
  - `dagre` for every other kind.
- Sequence diagrams ignore the layout setting. Mermaid draws them with its own sequence layout. A probe sequence rendered with `elk`, with `dagre`, and with no layout gives byte-identical SVG once Mermaid's auto-incremented element ids are normalized. The `dagre` frontmatter on a sequence diagram therefore has no effect, and no other layout applies to sequences.

Pages
- US letter at 96 CSS px per inch:
  - landscape 1056 x 816 px (11 x 8.5 in);
  - portrait 816 x 1056 px;
  - margin 24 px;
  - a 26 px header line naming the diagram number, source line, heading, orientation, tile, text point range, and layout.
- Orientation:
  - every sequence diagram is landscape;
  - any other diagram is landscape when its width is at least its height, otherwise portrait.
- Text size: the scale keeps every text element between 8 pt and 12 pt (10.67 px and 16 px at 96 px per inch). The diagram is fit to one page when that keeps its smallest text at 8 pt or larger, otherwise scaled to 8 pt and tiled into columns and rows of pages.
- No diagram needs more than three tiles. A tile that holds no drawn element is not exported. The survey's tile count therefore includes three blank third tiles (diagrams 65, 72, and 94) that the export skips.
- Each page is exported as PNG at 1.5 times the page size: 1584 x 1224 px landscape, 1224 x 1584 px portrait.

Reproducing
1. Serve the folder that holds `mermaid-render.html` over HTTP. The scratchpad server `serve-pages.ps1` listens on 127.0.0.1 and accepts `POST /save/<name>.png`.
2. Open the page in a browser.
3. Run `await window.survey()` for the per-diagram survey string recorded in `manifest.json` (diagram number, `L` or `P`, tiles, text point range).
4. Run `await window.exportRange(1, 213)` to write the pages.
5. The query parameter `?mm=<version>` pins another Mermaid version.
