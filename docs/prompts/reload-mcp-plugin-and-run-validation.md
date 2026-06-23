# Prompt: Reload McpServer Plugin and Run Server-Truth Validation (for Claude Desktop and Codex Desktop)

Copy the entire section below this line and paste it into a fresh chat with Claude Desktop or Codex Desktop.

---

You are running in **Claude Desktop** (or **Codex Desktop**) with the McpServer plugin installed.

The McpServer core has been updated and the plugin code has been synced (object-first + JSON serialization for all envelopes in bash/pwsh/node shims, eliminating previous "Malformed YAML envelope" / invalid_envelope errors on rich sessionlog payloads).

### 1. Reload the plugin
Perform a full plugin reload so the desktop app picks up the latest code:

- **Claude Desktop**:
  - Restart Claude Desktop completely.
  - If a plugin reload command or "reload extensions" option is available in settings or the McpServer plugin UI, use it.
  - Confirm you are now using the updated version from your local plugin checkout (typically F:\GitHub\mcpserver-claude-code-plugin or equivalent).

- **Codex Desktop**:
  - Restart Codex completely.
  - Use any available plugin reload / refresh mechanism (check Codex settings, plugin panel, or run the plugin's reload hook if exposed).
  - Confirm the latest synced core (from F:\GitHub\mcpserver-codex-plugin or equivalent).

After reload, run a quick health/status check using the McpServer tools to verify the plugin is active and using the new shims.

### 2. Switch to your test workspace and run the validation
Switch to (or confirm you are in) a registered McpServer workspace that has an `AGENTS-README-FIRST.yaml` (example: F:\GitHub\vice-sharp).

Read `AGENTS-README-FIRST.yaml` now.

Verify the server is healthy (use /health with nonce if possible).

Run the **BUG-6 server-truth validation** using the official plugin shims only (workflow.sessionlog.* or the equivalent McpServer session tools). Do **not** use raw REST for mutations.

Use this exact test case (structured input with a non-filePath action whose description mentions the word "filePath", plus a real filePath action, plus a commit):

**appendActions payload (YAML or the closest structured form your interface accepts):**
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

**completeTurn payload (must be multiline and contain colons + lists):**
```yaml
response: |
  Server-truth validation run for BUG-6 (after plugin reload).

  This response contains:
    - A list item
    - Another line with a colon: value inside it

  All appended actions (including the design_decision that mentioned "filePath") must appear in the persisted turn on the server.
```

**Steps to execute:**
1. Start a fresh turn with a clear title that includes "BUG-6 server-truth validation - [Claude/Codex] - [timestamp] - post-reload".

2. Call appendActions (via the plugin shim) with the payload above. Capture the full local output.

3. Call completeTurn (via the plugin shim) with the payload above. Capture the full local output.

4. Perform **server-truth verification** (this is mandatory):
   - Use your normal session query tool (workflow.sessionlog.queryHistory, getHistory, or equivalent).
   - Also run (or output the exact authenticated command for) a direct GET:
     ```
     GET {baseUrl}/mcpserver/sessionlog?agent={your-sourceType}&sessionId={sessionId}&limit=5
     Headers:
       X-Api-Key: {apiKey}
       X-Workspace-Path: {workspacePath}
     ```
   - Inspect the specific turn and confirm:
     - status: completed
     - actions array contains all 3 items (the design_decision must be present even though its description mentioned "filePath")
     - the real filePath edit is present
     - codeEdits == 1 on the server (only the real filePath counted; text mentions of "filePath" must not inflate it)
     - full multiline response is intact (including the list and the line with a colon inside)
     - turnCount increased
     - no new failsafes were left for this turn
     - no "invalid_envelope" or "Malformed YAML envelope" errors appeared for this turn

5. Append your results to the shared file (do not overwrite):
   `F:\GitHub\vice-sharp\docs\mcpserver-bug6-server-truth-results.md`

   Use exactly this section format (replace placeholders with your actual data):

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
[reload success notes, whether the object+JSON fixes eliminated the envelope errors, any remaining issues, comparison to previous runs, etc.]

---
```

### 3. Final report
After appending, give a one-line summary in this chat:
`[Claude / Codex] post-reload validation: [PASSED / PARTIAL / FAILED] — server actions count = X, codeEdits semantics correct = Y, no malformed envelopes = Z`

Include the link or path to the appended section in the results file.

Start with the reload step, then the test. Use only the official plugin shims for mutations and server-truth GETs for verification. Be precise and capture the raw server response.

---

**End of prompt** (copy from the line above the first `---` to the line below the last `---`). 

Save this file in your local McpServer checkout for future reference if needed.