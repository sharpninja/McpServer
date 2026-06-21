#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
SCRIPT_DIR="$(core_stage)"

setup() {
    # Create a temp workspace with a marker file for testing
    TEST_WORKSPACE="$(mktemp -d)"
    cat > "$TEST_WORKSPACE/AGENTS-README-FIRST.yaml" << 'MARKER'
port: 7147
baseUrl: http://testhost:7147
apiKey: test-api-key-12345
workspace: TestWorkspace
workspacePath: /tmp/test-workspace
pid: 99999
startedAt: "2026-04-07T10:00:00Z"
markerWrittenAtUtc: "2026-04-07T10:00:00Z"
serverStartedAtUtc: "2026-04-07T09:59:55Z"
endpoints:
  health: /health
  swagger: /swagger/v1/swagger.json
  swaggerUi: /swagger
  mcpTransport: /mcp-transport
  sessionLog: /mcpserver/sessionlog
  sessionLogDialog: /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog
  contextSearch: /mcpserver/context/search
  contextPack: /mcpserver/context/pack
  contextSources: /mcpserver/context/sources
  todo: /mcpserver/todo
  repo: /mcpserver/repo
  desktop: /mcpserver/desktop
  gitHub: /mcpserver/gh
  tools: /mcpserver/tools
  workspace: /mcpserver/workspace
  serverStartupUtc: /server-startup-utc
  markerFileTimestamp: /marker-file-timestamp?repoPath={workspacePath}
signature:
  algorithm: HMAC-SHA256
  canonicalization: marker-v1
  verifier: workspace_api_key
  value: PLACEHOLDER
MARKER
}

teardown() {
    rm -rf "$TEST_WORKSPACE"
}

@test "find_marker_file finds marker in current directory" {
    source "$SCRIPT_DIR/lib/marker-resolver.sh"
    cd "$TEST_WORKSPACE"
    run find_marker_file
    [ "$status" -eq 0 ]
    [[ "$output" == *"AGENTS-README-FIRST.yaml"* ]]
}

@test "find_marker_file walks up to parent directories" {
    source "$SCRIPT_DIR/lib/marker-resolver.sh"
    mkdir -p "$TEST_WORKSPACE/sub/deep"
    cd "$TEST_WORKSPACE/sub/deep"
    run find_marker_file
    [ "$status" -eq 0 ]
    [[ "$output" == *"AGENTS-README-FIRST.yaml"* ]]
}

@test "find_marker_file returns exit 1 when no marker exists" {
    source "$SCRIPT_DIR/lib/marker-resolver.sh"
    EMPTY_DIR="$(mktemp -d)"
    cd "$EMPTY_DIR"
    run find_marker_file "$EMPTY_DIR"
    [ "$status" -eq 1 ]
    rm -rf "$EMPTY_DIR"
}

@test "parse_marker_field extracts baseUrl correctly" {
    source "$SCRIPT_DIR/lib/marker-resolver.sh"
    result=$(parse_marker_field "$TEST_WORKSPACE/AGENTS-README-FIRST.yaml" "baseUrl")
    [ "$result" = "http://testhost:7147" ]
}

@test "parse_marker_field extracts apiKey correctly" {
    source "$SCRIPT_DIR/lib/marker-resolver.sh"
    result=$(parse_marker_field "$TEST_WORKSPACE/AGENTS-README-FIRST.yaml" "apiKey")
    [ "$result" = "test-api-key-12345" ]
}

@test "parse_marker_field extracts workspacePath correctly" {
    source "$SCRIPT_DIR/lib/marker-resolver.sh"
    result=$(parse_marker_field "$TEST_WORKSPACE/AGENTS-README-FIRST.yaml" "workspacePath")
    [ "$result" = "/tmp/test-workspace" ]
}

@test "marker-resolver.sh is syntactically valid bash" {
    run bash -n "$SCRIPT_DIR/lib/marker-resolver.sh"
    [ "$status" -eq 0 ]
}
