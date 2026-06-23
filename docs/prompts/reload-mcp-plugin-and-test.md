# Prompt: Reload McpServer Plugin and Run Server-Truth Validation (Claude Desktop / Codex Desktop)

Copy the entire block below and paste it as a new message into **Claude Desktop** or **Codex Desktop**.

---

You are now running inside **Claude Desktop** (or **Codex Desktop**) with the McpServer plugin.

The McpServer core has been updated and the plugins have been synced. The shims now build all envelopes as proper objects and serialize them to JSON (instead of hand-crafting YAML text with manual indentation). This fixes the "Malformed YAML envelope" and `invalid_envelope` errors that previously caused non-filePath actions (design_decision, commit, verify, test) and multiline responses to be lost or only appear in failsafes.

## 1. Reload the plugin (do this first)

Perform a full reload so the desktop app picks up the latest code:

- **Claude Desktop**:
  - Quit Claude Desktop completely and restart it.
  - If the McpServer plugin provides a reload command, status script, or "reload extensions" option, run it.
  - Confirm you are using the updated version from your local plugin checkout (typically `F:\GitHub\mcpserver-claude-code-plugin`).

- **Codex Desktop**:
  - Quit Codex completely and restart it.
  - Use any available plugin reload / refresh mechanism (settings, cache flush, or restart hooks).
  - Confirm you are using the updated version from your local plugin checkout (typically `F:\GitHub\mcpserver-codex-plugin`).

After reload, run a quick health or status check using the McpServer tools to verify the plugin is active and using the new shims.

## 2. Run the server-truth validation in your workspace

Switch to (or confirm you are in) a workspace that has an `AGENTS-README-FIRST.yaml` (recommended: `F:\GitHub\vice-sharp` or any registered McpServer workspace).

Read `AGENTS-README-FIRST.yaml` now.

**Use only the official plugin shims** for mutations (`workflow.sessionlog.*` or the equivalent McpServer session tools). Do **not** use raw REST for `appendActions` or `completeTurn`.

### Test payloads (use exactly these or the closest structured form your interface accepts)

**appendActions** (must include a design_decision whose description mentions the word "filePath", plus a real filePath action):
```yaml
actions:
  - type: design_decision
    description: "Decision about correctly handling the filePath word in descriptions without bumping codeEdits"
    status: completed
  - type: edit
    description: "Real file modification as part of the fix"
    filePath: "src/fix-validation.cs"
    status: completed
  - type: commit
    description: "Commit the changes for BUG-6"
    status: completed
```

**completeTurn** (must be a multiline block scalar containing a colon and list items):
```yaml
response: |
  Server-truth validation run for BUG-6 (after plugin reload).

  This response contains:
    - A list item
    - Another line with a colon: value inside it

  All appended actions (including the design_decision that mentioned "filePath") must appear in the persisted turn on the server.
```

### Steps (execute in order)
1. Start a fresh turn with a clear title containing "BUG-6 server-truth validation - [Claude/Codex] - [timestamp] - post-reload".

2. Call `appendActions` (via the shim) with the payload above. Capture the full local output.

3. Call `completeTurn` (via the shim) with the payload above. Capture the full local output.

4. Perform **server-truth verification** (this is mandatory — local "ok: true" is not enough):
   - Use your normal session query tool (`workflow.sessionlog.queryHistory`, `getHistory`, or equivalent).
   - Also run (or output the exact authenticated command for) a direct GET:
     ```
     GET {baseUrl}/mcpserver/sessionlog?agent={your-sourceType}&sessionId={sessionId}&limit=5
     Headers:
       X-Api-Key: {apiKey}
       X-Workspace-Path: {workspacePath}
     ```
   - Confirm from the raw server response:
     - Turn exists and has `status: completed`
     - `actions` array contains all three items (the `design_decision` must be present even though its text mentioned "filePath")
     - The real `filePath` edit is present
     - `codeEdits` semantics are correct (exactly 1 from the real filePath; text mentions of "filePath" did not inflate it)
     - Full multiline `response` is preserved exactly (including the list and the line containing a colon)
     - `turnCount` increased
     - No new failsafes were created for this turn
     - No "invalid_envelope" or "Malformed YAML envelope" errors for this turn

5. Append your results to the shared file (do **not** overwrite):
   `F:\GitHub\vice-sharp\docs\mcpserver-bug6-server-truth-results.md`

   Use exactly this section format (replace placeholders with your real data):

```markdown
## [Claude / Codex] - [ISO Timestamp] (post-reload)

**Workspace**: F:\GitHub\vice-sharp
**SessionId**: ...
**RequestId**: ...
**SourceType**: [Claude / Codex]

### Local Results
- appendActions local output: `...`
- completeTurn local output: `...`
- Local codeEdits reported: ...

### Server Truth (raw GET excerpt)
```json
[paste the relevant turn object here]
```

### Verification Checklist
- [ ] Turn present on server with status completed
- [ ] actions array contains the design_decision (even though its text mentioned "filePath")
- [ ] actions array contains the real edit with filePath
- [ ] codeEdits == 1 on server
- [ ] Multiline response is fully present and intact
- [ ] No new failsafes left for this turn
- [ ] No "invalid_envelope" or deserialization errors for this turn
- [ ] turnCount for session increased appropriately

**Raw server GET URL used**: `...`

**Notes / Observations**:
[reload success/failure notes, whether the object+JSON fixes worked, comparison to previous runs, any remaining issues]

---
```

### 3. Final output
After appending, reply with a one-line summary:
`[Claude / Codex] post-reload validation: [PASSED / PARTIAL / FAILED] — server actions count = X, codeEdits semantics correct = Y, no malformed envelopes = Z`

Include the exact section you appended (or a link/path to it) and any raw server excerpts.

Start with the reload step, then execute the test exactly as described. Use server-truth GETs for verification — local "ok: true" is not sufficient.

Begin now.