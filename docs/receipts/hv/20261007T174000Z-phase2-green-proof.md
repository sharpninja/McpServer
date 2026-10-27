# Phase 2 green proof (BDPv4 full-suite)

Stamp: 20261007T174000Z-phase2-green-proof
Operator rule: BDPv4 exit requires Failed 0 (no quarantine of inherited red).

## Suite results (Failed 0)

[
  {
    "repo": "mcpserver-grok-plugin",
    "total": 29,
    "fails": 0,
    "status": "PASS"
  },
  {
    "repo": "mcpserver-grok-bot-plugin",
    "total": 29,
    "fails": 0,
    "status": "PASS"
  },
  {
    "repo": "mcpserver-claude-code-plugin",
    "total": 36,
    "fails": 0,
    "status": "PASS"
  },
  {
    "repo": "mcpserver-claude-cowork-plugin",
    "total": 29,
    "fails": 0,
    "status": "PASS"
  },
  {
    "repo": "mcpserver-codex-plugin",
    "total": 41,
    "fails": 0,
    "status": "PASS"
  },
  {
    "repo": "mcpserver-copilot-plugin",
    "total": 29,
    "fails": 0,
    "status": "PASS"
  },
  {
    "repo": "mcpserver-cline-plugin",
    "total": 32,
    "fails": 0,
    "status": "PASS"
  }
]


Proof artifacts: `F:\GitHub\McpServer\docs\receipts\hv\20261007T174000Z-phase2-green-proof\`

## Root-cause fixes pushed on cursor/qbrain-ai-rebrand-phase2-91dd

- Restored `hooks/scripts/memory-context.ps1` + UPS `Add-McpRequiredMemoryToHookOutput` wiring
- Direct-marker-only workspace/start-path trust (no home AGENTS walk)
- ReplBridge accepts persisted+degraded success; session rebind on compatible mismatch
- Phantom stop-gate before sessionAgeStale; MCP_PLUGIN_REPL_RESPONSE stub seam
- UPS resets `\ = 0` before plugin-hook to avoid suite-order exit pollution
- Codex `lib/user-prompt-submit.ps1` memory injection (lib depth `..` + hooks memory-context)
- Cline `plugin-helpers` direct-marker trust; Grok Bot CORE file sync + manifest hashes

## Push tips

[
  {
    "repo": "mcpserver-grok-plugin",
    "tip": "b2a0ec7e03869f0f2863728ccdc7da75ba1b0bda",
    "short": "b2a0ec7",
    "pushed": true
  },
  {
    "repo": "mcpserver-grok-bot-plugin",
    "tip": "abb746cfdefba7cbcf06724a011b1568487ad155",
    "short": "abb746c",
    "pushed": true
  },
  {
    "repo": "mcpserver-claude-code-plugin",
    "tip": "2510cc1be501b028e59736fb638cf38df3d29ccb",
    "short": "2510cc1",
    "pushed": true
  },
  {
    "repo": "mcpserver-claude-cowork-plugin",
    "tip": "95bce132f1c71e4c4d10bf3943e0c700ed5968f8",
    "short": "95bce13",
    "pushed": true
  },
  {
    "repo": "mcpserver-codex-plugin",
    "tip": "ae35f225bd896fdc2bea2fdd6b923703beaaec39",
    "short": "ae35f22",
    "pushed": true
  },
  {
    "repo": "mcpserver-copilot-plugin",
    "tip": "a0792f4fcb1cca8e7ac8d5f20d6917218a30dbd0",
    "short": "a0792f4",
    "pushed": true
  },
  {
    "repo": "mcpserver-cline-plugin",
    "tip": "7a28d0294797bf643bdd23e3ecb7d03c3ac89473",
    "short": "7a28d02",
    "pushed": true
  }
]

PRs remain draft. No merges. No Phase 3/4-L.