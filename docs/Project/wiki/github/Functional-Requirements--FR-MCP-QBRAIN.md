# FR-MCP-QBRAIN

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-QBRAIN-001 Product display brand is QBrain.AI

Where this product is named for a person, the display brand is QBrain.AI. That includes the README title, live documentation titles, the Windows service DisplayName, and other human-readable product titles. It does not by itself rename a namespace, a package id, a path, or a repository.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A reader of the live README title sees QBrain.AI.
- [ ] The Windows service DisplayName is QBrain.AI where the service is installed under the new name.
- [ ] Live product titles no longer use "MCP Server" for this product.
- [ ] Historical receipts are not rewritten to satisfy this requirement.

## FR-MCP-QBRAIN-002 Product identity stays distinct from the Model Context Protocol

The open protocol remains the Model Context Protocol. Protocol wording and the protocol endpoint stay recognizable as MCP. The product rename must not make agents treat `/mcp-transport` or MCP JSON-RPC as a retired product name.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Live docs that describe the wire protocol still say Model Context Protocol where they mean the protocol.
- [ ] `/mcp-transport` remains the MCP endpoint and is not redirected onto a QBrain.AI path.
- [ ] Bare "MCP" in protocol sentences is classified and left in place. It is not bulk-replaced.

## FR-MCP-QBRAIN-003 Product identity stays distinct from QuadBrain and QBAgent

QuadBrain remains the four-role feature. QBAgent remains the agent tool. The command `qbagent` remains `qbagent`. The product brand QBrain.AI must not absorb those names, and those names must not be rewritten into QBrain.AI.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Live docs still describe QuadBrain as the feature and QBrain.AI as the product.
- [ ] The tool command `qbagent` still exists.
- [ ] The namespace segment `QBAgent` remains `QBAgent` after the product root changes.

## FR-MCP-QBRAIN-004 1.x callers keep a working product identity

Through the 1.x line, a caller that still uses the old product route, the old config section, the old product environment variables, or the old NuGet package ids can still operate. At 2.0 those aliases end. This is the continuity requirement. How the aliases are built is a technical requirement.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A 1.x request to the old product HTTP prefix still succeeds for the representative route covered by tests.
- [ ] A 1.x configuration that contains only the old config section still boots.
- [ ] A 1.x package reference to each published `SharpNinja.McpServer.*` id still compiles against the moved public types via the type-forward package.
- [ ] 2.0 is the version where those aliases are allowed to stop. They are not removed inside 1.x.

## FR-MCP-QBRAIN-005 Historical identity and host names stay

People can still read historical receipts, existing requirement IDs, and the host names PAYTON-LEGION2 and LAB-OMARCHY as they were written. The rebrand does not rewrite that history and does not rename those machines or that lab.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Existing files under `docs/receipts/**` are not rewritten. New inventory and hostile-validation receipts may be added. They do not replace historical receipt bodies.
- [ ] Existing `FR-MCP-*`, `TR-MCP-*`, and `TEST-MCP-*` IDs are not renamed. The new IDs in this plan are additions.
- [ ] The hostname PAYTON-LEGION2 and the lab name LAB-OMARCHY still appear where they identify that host and that lab.

