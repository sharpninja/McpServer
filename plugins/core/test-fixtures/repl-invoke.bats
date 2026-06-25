#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
SCRIPT_DIR="$(core_stage)"

@test "repl_invoke constructs valid JSON envelope with type, method, params, and requestId" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    # Create a mock mcpserver-repl that echoes stdin so we can inspect the envelope
    TMPBIN="$(mktemp -d)"
    cat > "$TMPBIN/mcpserver-repl" << 'MOCK'
#!/bin/bash
cat  # echo stdin to stdout
MOCK
    chmod +x "$TMPBIN/mcpserver-repl"
    export PATH="$TMPBIN:$PATH"

    source "$SCRIPT_DIR/lib/repl-invoke.sh"
    output=$(repl_invoke "sessionlog.addTurn" "sessionId: abc-123
turnIndex: 1")

    printf '%s' "$output" | node -e '
const fs = require("fs");
const env = JSON.parse(fs.readFileSync(0, "utf8"));
if (env.type !== "request") process.exit(1);
if (env.payload.method !== "sessionlog.addTurn") process.exit(1);
if (!/^req-\d{8}T\d{6}Z-[0-9a-f]{4}$/.test(env.payload.requestId)) process.exit(1);
if (env.payload.params.sessionId !== "abc-123") process.exit(1);
if (env.payload.params.turnIndex !== 1) process.exit(1);
'

    rm -rf "$TMPBIN"
}

@test "repl_invoke outputs the constructed JSON envelope correctly" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    TMPBIN="$(mktemp -d)"
    cat > "$TMPBIN/mcpserver-repl" << 'MOCK'
#!/bin/bash
cat
MOCK
    chmod +x "$TMPBIN/mcpserver-repl"
    export PATH="$TMPBIN:$PATH"

    source "$SCRIPT_DIR/lib/repl-invoke.sh"
    output=$(repl_invoke "todo.list")

    printf '%s' "$output" | node -e '
const fs = require("fs");
const env = JSON.parse(fs.readFileSync(0, "utf8"));
if (env.type !== "request") process.exit(1);
if (env.payload.method !== "todo.list") process.exit(1);
if (!env.payload.requestId) process.exit(1);
if (Object.prototype.hasOwnProperty.call(env.payload, "params")) process.exit(1);
'

    rm -rf "$TMPBIN"
}

@test "repl_invoke requestId contains a timestamp in ISO format" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    TMPBIN="$(mktemp -d)"
    cat > "$TMPBIN/mcpserver-repl" << 'MOCK'
#!/bin/bash
cat
MOCK
    chmod +x "$TMPBIN/mcpserver-repl"
    export PATH="$TMPBIN:$PATH"

    source "$SCRIPT_DIR/lib/repl-invoke.sh"
    output=$(repl_invoke "health.check")

    # Extract requestId value
    request_id=$(printf '%s' "$output" | node -e 'const fs=require("fs"); process.stdout.write(JSON.parse(fs.readFileSync(0,"utf8")).payload.requestId)')

    # Should match pattern: req-YYYYMMDDTHHMMSSz-XXXX
    [[ "$request_id" =~ ^req-[0-9]{8}T[0-9]{6}Z-[0-9a-f]{4}$ ]]

    rm -rf "$TMPBIN"
}

@test "repl_build_envelope parses YAML params as an object" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    source "$SCRIPT_DIR/lib/repl-invoke.sh"

    output=$(repl_build_envelope "workflow.requirements.createFr" "id: FR-MCP-901
title: Requirements shim
priority: high
area: MCP")

    printf '%s' "$output" | node -e '
const fs = require("fs");
const env = JSON.parse(fs.readFileSync(0, "utf8"));
if (env.payload.method !== "workflow.requirements.createFr") process.exit(1);
if (typeof env.payload.params !== "object" || Array.isArray(env.payload.params)) process.exit(1);
if (env.payload.params.id !== "FR-MCP-901") process.exit(1);
    if (env.payload.params.priority !== "high") process.exit(1);
'
}

@test "repl_build_envelope parses nested YAML without installed js-yaml" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    isolated_root="$(mktemp -d)"
    mkdir -p "$isolated_root/lib" "$isolated_root/work"
    cp "$SCRIPT_DIR/lib/repl-invoke.sh" "$SCRIPT_DIR/lib/yaml-subset-parser.js" "$isolated_root/lib/"

    output="$(
        cd "$isolated_root/work"
        export MCP_PLUGIN_ROOT="$isolated_root"
        source "$isolated_root/lib/repl-invoke.sh"
        repl_build_envelope "workflow.requirements.createFrBatch" "records:
  - id: FR-MCP-902
    title: Requirements fallback parser
    priority: high
    acceptanceCriteria:
      - text: YAML params parse without js-yaml.
        evidence: fallback-parser"
    )"

    printf '%s' "$output" | node -e '
const fs = require("fs");
const env = JSON.parse(fs.readFileSync(0, "utf8"));
const record = env.payload.params.records && env.payload.params.records[0];
if (!record) process.exit(1);
if (record.id !== "FR-MCP-902") process.exit(1);
if (record.priority !== "high") process.exit(1);
if (!Array.isArray(record.acceptanceCriteria)) process.exit(1);
if (record.acceptanceCriteria[0].text !== "YAML params parse without js-yaml.") process.exit(1);
'

    rm -rf "$isolated_root"
}

@test "repl_build_envelope fallback parser accepts document path keys" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    isolated_root="$(mktemp -d)"
    mkdir -p "$isolated_root/lib" "$isolated_root/work"
    cp "$SCRIPT_DIR/lib/repl-invoke.sh" "$SCRIPT_DIR/lib/yaml-subset-parser.js" "$isolated_root/lib/"

    output="$(
        cd "$isolated_root/work"
        export MCP_PLUGIN_ROOT="$isolated_root"
        source "$isolated_root/lib/repl-invoke.sh"
        repl_build_envelope "workflow.requirements.ingestDocument" "documents:
  github/Functional-Requirements.md:
    content: |
      # Functional Requirements
    lastModifiedUtc: 2026-06-25T01:00:00Z"
    )"

    printf '%s' "$output" | node -e '
const fs = require("fs");
const env = JSON.parse(fs.readFileSync(0, "utf8"));
const doc = env.payload.params.documents && env.payload.params.documents["github/Functional-Requirements.md"];
if (!doc) process.exit(1);
if (doc.lastModifiedUtc !== "2026-06-25T01:00:00Z") process.exit(1);
if (!doc.content.includes("# Functional Requirements")) process.exit(1);
'

    rm -rf "$isolated_root"
}

@test "script entrypoint exits non-zero for type error envelope" {
    TMPBIN="$(mktemp -d)"
    cat > "$TMPBIN/mcpserver-repl" << 'MOCK'
#!/bin/bash
cat >/dev/null
printf 'type: error\npayload:\n  code: boom\n  message: failed\n'
MOCK
    chmod +x "$TMPBIN/mcpserver-repl"
    export PATH="$TMPBIN:$PATH"

    run "$SCRIPT_DIR/lib/repl-invoke.sh" "client.Bad"

    [ "$status" -eq 1 ]
    [[ "$output" == *"type: error"* ]]

    rm -rf "$TMPBIN"
}

@test "repl_invoke returns exit 1 when mcpserver-repl is not available" {
    # Run in a subshell with restricted PATH containing only essential tools
    run bash -c '
        export PATH="/usr/bin:/bin"
        # Remove any mcpserver-repl from discoverable locations
        hash -r 2>/dev/null
        source "'"$SCRIPT_DIR"'/lib/repl-invoke.sh" 2>/dev/null
        repl_invoke "test.method" 2>&1
    '
    # Should fail because mcpserver-repl is not in /usr/bin or /bin
    if command -v mcpserver-repl >/dev/null 2>&1; then
        skip "mcpserver-repl is installed globally — cannot test unavailable path"
    fi
    [ "$status" -eq 1 ]
}

@test "repl-invoke.sh is syntactically valid bash" {
    run bash -n "$SCRIPT_DIR/lib/repl-invoke.sh"
    [ "$status" -eq 0 ]
}
