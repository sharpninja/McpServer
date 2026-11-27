# TR-WEB-UI

[Back to Technical Requirements](Technical-Requirements)

## TR-WEB-UI-001

**Host orchestration UI in McpServer.Web** — The orchestration dashboard SHALL ship in src/McpServer.Web (mcp-web), extending FR-MCP-031. If the project is absent from the solution, restore or create it. Do not implement this UI inside Director.
**Covered by:** FR: FR-WEB-001, FR-WEB-002; TEST: TEST-WEB-001, TEST-WEB-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] McpServer.sln contains McpServer.Web after Phase 1.
- [ ] The cockpit route is served by that project, not by McpServer.Support.Mcp HTML pairing pages except for login.

## TR-WEB-UI-002

**Primer layout with desktop and mobile breakpoints** — mcp-web SHALL use the existing Primer CSS / Octicons visual language and MUST satisfy FR-WEB-017 breakpoints in automated UI tests.
**Covered by:** FR: FR-WEB-001, FR-WEB-003, FR-WEB-004, FR-WEB-017; TEST: TEST-WEB-001, TEST-WEB-003, TEST-WEB-004, TEST-WEB-017
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Dashboard components reference Primer CSS or the current mcp-web design system, not a Prompter Hawk theme clone.
- [ ] Layout tests execute at 1280px and 390px widths.

