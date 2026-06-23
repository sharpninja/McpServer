# Prompt: Reload McpServer Plugin and Run Server-Truth Validation Test (Claude Desktop / Codex Desktop)

**Instructions for the user running this:**
- Copy the entire block below (starting from "You are now running in...") and paste it as a new message into Claude Desktop or Codex Desktop.
- Run this in a fresh session in the target workspace (recommended: F:\GitHub\vice-sharp).
- This prompt assumes the McpServer plugin core has been updated and synced (latest shims use proper object construction + JSON serialization for envelopes, fixing previous "Malformed YAML envelope" / invalid_envelope issues for rich sessionlog data like actions + multiline responses).
- After reload, the agent must use the **plugin shims** (workflow.sessionlog.* or equivalent) for mutations, and verify via server-truth GET (not just local "ok: true").

---

You are now running in **Claude Desktop** (or **Codex Desktop**) with the McpServer plugin.

## Step 1: Reload / Refresh the McpServer Plugin

The plugin core has been updated with critical fixes for session logging shims (replacing fragile manual YAML text construction with object-first + reliable JSON serialization for envelopes). This fixes BUG-6 issues where appendActions + completeTurn with non-filePath actions (e.g. design_decision whose description mentions "filePath") + multiline responses containing colons/lists were producing "invalid_envelope" / "Malformed YAML envelope" errors. Data was landing only in failsafes instead of the server.

**Reload instructions (do this first):**
- **For Claude Desktop:**
  - Fully restart Claude Desktop application.
  - If the plugin supports it, trigger a plugin reload via any available menu/command (e.g., PostToolUse or session restart hooks).
  - Ensure it is loading from the latest synced checkout: `F:\GitHub\mcpserver-claude-code-plugin` (lib/ should have the updated repl-invoke.sh / .ps1 with JSON object handling).
  - Verify by checking the active plugin version or running a simple status tool (e.g., workflow or mcp status).
  - If cache is involved, clear relevant plugin cache dirs under your user profile if needed (e.g., ~/.claude or plugin cache).

- **For Codex Desktop:**
  - Fully restart Codex Desktop.
  - Trigger plugin reload if available (via Codex settings, session restart, or hook).
  - Ensure loading from latest: `F:\GitHub\mcpserver-codex-plugin`.
  - Confirm via plugin status or by noting the core version in any manifest (synced from McpServer core 58b68e2+).
  - Clear Codex plugin cache if stale (e.g., under C:\Users\you\.codex\plugins\cache).

After reload/restart, confirm the plugin is active and using the updated shims (you should no longer hit the old YAML envelope bugs on rich payloads).

## Step 2: Run Server-Truth Validation Test in Your Workspace

Use the workspace at `F:\GitHub\vice-sharp` (or confirm the current one has the McpServer marker and is registered).

**Goal:** Prove the shim fixes work end-to-end for complex sessionlog data:
- appendActions with mixed structured actions (including non-filePath like design_decision/verify/commit whose text may contain the word "filePath", plus a real filePath edit).
- completeTurn with a rich multiline response (containing lists and colons).
- Verify **server truth** via GET to /mcpserver/sessionlog (not local return codes).
- Append your results to the shared file: `F:\GitHub\vice-sharp\docs\mcpserver-bug6-server-truth-results.md` (use your file tools / repo_write / shell append to add a new section without overwriting).

**Preconditions (run these):**
- Read `AGENTS-README-FIRST.yaml` in the workspace root.
- Verify `/health` (with nonce) succeeds and marker signature is trusted.
- Use **only plugin shims** for mutations (workflow.sessionlog.appendActions, completeTurn, etc. – or your agent's equivalent session tools). Do **not** bypass with raw REST for writes.
- Record your sourceType (Claude / Codex), sessionId, requestId, and timestamp.

**Test payloads (use exactly these or the closest structured form your interface accepts):**

appendActions:
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

completeTurn:
```yaml
response: |
  Server-truth validation run for BUG-6 (after plugin reload).

  This response contains:
    - A list item
    - Another line with a colon: value inside it

  All appended actions (including the design_decision that mentioned "filePath") must appear in the persisted turn on the server.
```

**Execution steps:**
1. Start a fresh turn with a clear title containing: "BUG-6 server-truth validation - [Claude/Codex] - [ISO timestamp] - after-reload".

2. Call appendActions (via shim) with the payload above. Capture full local output.

3. Call completeTurn (via shim) with the payload above. Capture full local output.

4. Perform **server-truth verification**:
   - Use your normal query tool (sessionlog.queryHistory / getHistory / equivalent).
   - Also construct and run (or output the exact curl/command for) an authenticated GET:
     ```
     GET {baseUrl}/mcpserver/sessionlog?agent={your-sourceType}&sessionId={sessionId}&limit=5
     Headers:
       X-Api-Key: {apiKey}
       X-Workspace-Path: {workspacePath}
     ```
   - Extract and inspect the specific turn:
     - status: completed
     - actions array has all 3 items (design_decision present even with "filePath" in text; real edit with filePath; commit)
     - codeEdits semantics correct (exactly 1 from real filePath)
     - full multiline response intact (with lists + internal colon)
     - turnCount increased
     - No new failsafes or "invalid_envelope" errors for this turn

5. Append results to the shared file `F:\GitHub\vice-sharp\docs\mcpserver-bug6-server-truth-results.md` using this exact structure (replace placeholders):

```markdown
## [Claude/Codex] - [ISO Timestamp] (after plugin reload)

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
[paste relevant turn object]
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
[any differences vs previous runs, success/failure details, whether reload picked up the object+JSON fixes, etc.]

---
```

**How to append:** Use your available tools (repo write/edit, shell cat >>, or equivalent). Do **not** overwrite the file.

## Step 3: Report Back
After completing:
- Summarize: Did reload succeed? Did the test pass server-truth checks (full actions persisted, no malformed envelopes)?
- One-line: `[Claude/Codex] after-reload validation: [PASSED / FAILED] - server actions count=X, codeEdits semantics correct=Y`
- Confirm the section was appended to the shared results file.
- Note any remaining issues or observations.

Run the full procedure now. Be precise and use server GETs for truth. Start with the reload step.