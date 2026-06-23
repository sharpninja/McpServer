# BUG-6 Post-Reload: repl-invoke.sh Shell Quoting Defect (node-script heredoc boundary)

**Date**: 2026-06-23  
**Reporter(s)**: Claude (syntax error at :3433), Codex (patched at :3294 in their plugin copy; achieved PASS after)

## Summary
After the object+JSON envelope refactor (to fix previous "Malformed YAML envelope" / invalid_envelope for design_decision + multiline responses containing ":" and lists), the bash shims in `plugins/core/lib-sh/repl-invoke.sh` (and synced copies) contained fragile inline `node -e '...'` scripts.

The actions-block text-to-JSON parser used quote-stripping regexes that required embedding `'` characters inside single-quoted shell strings:

```sh
... .replace(/^["' + "'" + ']|["' + "'" + ']$/g,"")
# and the worse glued variant in persist:
... .replace(/^["'+"'"'+'"]|["'+"'"'+'"]$/g,"")
```

When placed inside `node -e ' multi-line JS '`, the `'` count in the bash parser became unbalanced (especially the glued ` '+"'"'+' " ` version). Bash prematurely closed the single-quoted argument, causing subsequent JS tokens like `else if(cur&&line.includes(":"))...` (and later code) to be parsed as top-level shell syntax → `syntax error`.

This made the entire `repl-invoke.sh` unloadable by bash in the official plugin shims. No methods could dispatch. This was itself a BUG-6 manifestation visible only on reload / rich action tests.

Codex workaround: patched their active `F:\GitHub\mcpserver-codex-plugin/lib/repl-invoke.sh` locally, re-ran validation, got PASS (actions=3, codeEdits correct, no malformed).

Claude hit the 3433 variant.

## Root Cause Locations (pre-fix)
- `_repl_turns_block` (~3296)
- `_repl_turn_upsert_params` / similar (~3360)
- `_repl_persist_turn` (envelope path, ~3432-3433) — the one using base64 + env + full UpsertTurnAsync envelope

Contributing: missing `eol=lf` in `.gitattributes` for `plugins/core/lib-sh/*.sh` (CRLF on Windows checkout broke some bashes further, e.g. WSL `bash -n` saw `\r` in tokens).

## Fix Applied
1. Normalized `lib-sh/repl-invoke.sh` and `.staged-plugin` copy to LF endings.
2. Replaced the three inline parsers with safe construction using quoted heredocs (`<<'NODEEOF'`) + `$(cat ...)` or direct heredoc-to-node. This isolates all JS (including `'` in `/["']/` regexes and strings) so bash never counts quotes inside the script body.

   Example (pipe + -e case):
   ```sh
   ... | node -e "$(cat <<'NODEEOF'
   ... clean JS with normal " and ' ...
   NODEEOF
   )" ...
   ```

   Direct heredoc-to-node (for env-only persist case, no stdin conflict):
   ```sh
   ... node <<'NODEEOF'
   const ...  # no -e, heredoc is the script
   NODEEOF
   ```

3. Updated `.gitattributes`:
   ```
   plugins/core/lib-sh/*.sh text eol=lf
   plugins/core/.staged-plugin/lib/*.sh text eol=lf
   ```

4. Verified: `bash -n` (both Git bash and WSL bash) now clean. No more `else if...` leakage.

## Impact / Verification
- The "object + JSON" approach for envelopes (and actions normalization) is retained.
- No more manual YAML text + indent hacks for the wire envelopes.
- PS shim (`lib-ps/repl-invoke.ps1`) was already using `ConvertTo-Json` on objects + its own lighter parser; unaffected or less fragile.
- Future syncs (`sync-plugin-core.ps1 --IncludePs` or .sh) to `mcpserver-*-plugin` will carry the fixed shims.
- Matches the test case: `design_decision` (description containing the word "filePath") + real `filePath` action + multiline `response:` containing `:` and list items. Server must see exactly 1 codeEdit, full actions, intact response, no failsafes, no invalid_envelope.

Codex post-patch result (via their shim after manual patch, before this root fix landed): PASSED. After this change, official synced plugins should no longer require local patches for the same reason.

## Files Changed (relative to McpServer workspace)
- `plugins/core/lib-sh/repl-invoke.sh`
- `plugins/core/.staged-plugin/lib/repl-invoke.sh` (for dev parity; ignored in git)
- `.gitattributes`

## Next
- Sync to plugin repos if needed (run from here with absolute plugin root).
- Re-run full validation from clean (no local patches) in Claude/Codex/Cline desktops against vice-sharp or other.
- Monitor for any other inline node -e '...' that embed ' or complex quotes.

This closes the quoting-boundary regression in the BUG-6 shim hardening.
