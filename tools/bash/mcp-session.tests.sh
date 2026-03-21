#!/usr/bin/env bash
# Tests for mcp-session.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TESTS=0 PASSED=0 FAILED=0
TEST_TMPDIR=""

setup_suite() {
    TEST_TMPDIR=$(mktemp -d /tmp/mcp-session-test-XXXXXX)
    cat > "$TEST_TMPDIR/AGENTS-README-FIRST.yaml" <<EOF
owner: test-owner
baseUrl: http://localhost:9999
apiKey: test-api-key-abc123
workspace: test-workspace
EOF
}

teardown_suite() {
    rm -rf "$TEST_TMPDIR"
}

describe() {
    echo ""
    echo "=== $1 ==="
}

it() {
    local desc="$1"; shift
    TESTS=$((TESTS + 1))
    local output
    if output=$("$@" 2>&1); then
        echo "  OK $desc"
        PASSED=$((PASSED + 1))
    else
        echo "  FAIL $desc"
        echo "    output: $output"
        FAILED=$((FAILED + 1))
    fi
}

setup_suite

# -- mcp_session_init --

describe "mcp_session_init"

it "reads baseUrl from marker file" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { echo '{\"status\":\"ok\"}'; }
        mcp_session_init 'Copilotcli' 'gpt-5.3-codex' '$TEST_TMPDIR/AGENTS-README-FIRST.yaml'
        [[ \"\$MCP_BASE_URL\" == 'http://localhost:9999' ]]
    "

it "reads apiKey from marker file" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { echo '{\"status\":\"ok\"}'; }
        mcp_session_init 'Copilotcli' 'gpt-5.3-codex' '$TEST_TMPDIR/AGENTS-README-FIRST.yaml'
        [[ \"\$MCP_API_KEY\" == 'test-api-key-abc123' ]]
    "

it "creates session temp file" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { echo '{\"status\":\"ok\"}'; }
        mcp_session_init 'Copilotcli' 'gpt-5.3-codex' '$TEST_TMPDIR/AGENTS-README-FIRST.yaml'
        [[ -n \"\$MCP_SESSION_FILE\" && \"\$MCP_SESSION_FILE\" == /tmp/mcp-session-*.json ]]
    "

it "fails when agent/model are missing" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        ! mcp_session_init '' '' '$TEST_TMPDIR/AGENTS-README-FIRST.yaml' 2>/dev/null
    "

it "fails when marker file not found" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        ! mcp_session_init 'Copilotcli' 'gpt-5.3-codex' '/nonexistent/path/marker.yaml' 2>/dev/null
    "

it "discovers marker by walking up directories" \
    bash -c "
        mkdir -p '$TEST_TMPDIR/a/b/c'
        cd '$TEST_TMPDIR/a/b/c'
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { echo '{\"status\":\"ok\"}'; }
        mcp_session_init 'Copilotcli' 'gpt-5.3-codex'
        [[ \"\$MCP_BASE_URL\" == 'http://localhost:9999' ]]
    "

# -- mcp_session_create --

describe "mcp_session_create"

it "creates session JSON with correct sourceType" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'Copilot' 'Test session' 'gpt-4' 'sess-001' > /dev/null
        jq -e '.sourceType == \"Copilot\"' \"\$MCP_SESSION_FILE\" > /dev/null
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "sets session title correctly" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'Agent' 'My Title' 'model' 'sid' > /dev/null
        jq -e '.title == \"My Title\"' \"\$MCP_SESSION_FILE\" > /dev/null
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "sets status to in_progress" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        jq -e '.status == \"in_progress\"' \"\$MCP_SESSION_FILE\" > /dev/null
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "initializes empty turns array" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        count=\$(jq '.turns | length' \"\$MCP_SESSION_FILE\")
        [[ \"\$count\" == '0' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "returns the session ID" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        result=\$(mcp_session_create 'A' 't' 'm' 'my-id-42')
        [[ \"\$result\" == 'my-id-42' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

# -- mcp_session_add_turn --

describe "mcp_session_add_turn"

it "adds turn with correct requestId and queryTitle" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'req-001' 'Fix bug' 'Fix the auth bug' 'in_progress'
        rid=\$(jq -r '.turns[0].requestId' \"\$MCP_SESSION_FILE\")
        qt=\$(jq -r '.turns[0].queryTitle' \"\$MCP_SESSION_FILE\")
        [[ \"\$rid\" == 'req-001' && \"\$qt\" == 'Fix bug' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "sets turn status correctly" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'r1' 'title' 'text' 'completed'
        st=\$(jq -r '.turns[0].status' \"\$MCP_SESSION_FILE\")
        [[ \"\$st\" == 'completed' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "initializes empty collections on turn" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'r1' 'title' 'text' 'in_progress'
        jq -e '.turns[0].actions | length == 0' \"\$MCP_SESSION_FILE\" > /dev/null &&
        jq -e '.turns[0].filesModified | length == 0' \"\$MCP_SESSION_FILE\" > /dev/null &&
        jq -e '.turns[0].designDecisions | length == 0' \"\$MCP_SESSION_FILE\" > /dev/null
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "appends multiple turns" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'r1' 'First' 'text1' 'in_progress'
        mcp_session_add_turn 'r2' 'Second' 'text2' 'completed'
        count=\$(jq '.turns | length' \"\$MCP_SESSION_FILE\")
        [[ \"\$count\" == '2' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

# -- mcp_session_update_turn --

describe "mcp_session_update_turn"

it "updates turn response field" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'r1' 'title' 'text' 'in_progress'
        mcp_session_update_turn 'r1' 'response' 'All done!'
        resp=\$(jq -r '.turns[0].response' \"\$MCP_SESSION_FILE\")
        [[ \"\$resp\" == 'All done!' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "updates turn status field" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'r1' 'title' 'text' 'in_progress'
        mcp_session_update_turn 'r1' 'status' 'completed'
        st=\$(jq -r '.turns[0].status' \"\$MCP_SESSION_FILE\")
        [[ \"\$st\" == 'completed' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

# -- mcp_session_add_action --

describe "mcp_session_add_action"

it "adds action with correct fields" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'r1' 'title' 'text' 'in_progress'
        mcp_session_add_action 'r1' 'Created file' 'create' 'new.cs' 'completed'
        desc=\$(jq -r '.turns[0].actions[0].description' \"\$MCP_SESSION_FILE\")
        atype=\$(jq -r '.turns[0].actions[0].type' \"\$MCP_SESSION_FILE\")
        fp=\$(jq -r '.turns[0].actions[0].filePath' \"\$MCP_SESSION_FILE\")
        [[ \"\$desc\" == 'Created file' && \"\$atype\" == 'create' && \"\$fp\" == 'new.cs' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "auto-increments action order" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'r1' 'title' 'text' 'in_progress'
        mcp_session_add_action 'r1' 'First' 'edit' 'a.cs'
        mcp_session_add_action 'r1' 'Second' 'edit' 'b.cs'
        o1=\$(jq '.turns[0].actions[0].order' \"\$MCP_SESSION_FILE\")
        o2=\$(jq '.turns[0].actions[1].order' \"\$MCP_SESSION_FILE\")
        [[ \"\$o1\" == '1' && \"\$o2\" == '2' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "pushes immediately after adding an action" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        call_log=\$(mktemp /tmp/mcp-curl-log-XXXXXX)
        curl() { echo called >> \"\$call_log\"; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'r1' 'title' 'text' 'in_progress'
        mcp_session_add_action 'r1' 'Tracked change' 'edit' 'src/a.cs'
        calls=\$(wc -l < \"\$call_log\")
        [[ \"\$calls\" -ge 3 ]]
        rm -f \"\$MCP_SESSION_FILE\" \"\$call_log\"
    "

# -- mcp_session_add_file / mcp_session_add_tag --

describe "mcp_session_add_file and mcp_session_add_tag"

it "appends file to filesModified" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'r1' 'title' 'text' 'in_progress'
        mcp_session_add_file 'r1' 'src/main.cs'
        mcp_session_add_file 'r1' 'src/test.cs'
        count=\$(jq '.turns[0].filesModified | length' \"\$MCP_SESSION_FILE\")
        first=\$(jq -r '.turns[0].filesModified[0]' \"\$MCP_SESSION_FILE\")
        [[ \"\$count\" == '2' && \"\$first\" == 'src/main.cs' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "appends tag to tags array" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_turn 'r1' 'title' 'text' 'in_progress'
        mcp_session_add_tag 'r1' 'bugfix'
        tag=\$(jq -r '.turns[0].tags[0]' \"\$MCP_SESSION_FILE\")
        [[ \"\$tag\" == 'bugfix' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

# -- mcp_session_update --

describe "mcp_session_update"

it "updates session status when provided" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_update 'completed'
        st=\$(jq -r '.status' \"\$MCP_SESSION_FILE\")
        [[ \"\$st\" == 'completed' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "updates title when provided" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 'Old Title' 'm' 'sid' > /dev/null
        mcp_session_update '' 'New Title'
        t=\$(jq -r '.title' \"\$MCP_SESSION_FILE\")
        [[ \"\$t\" == 'New Title' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

# -- mcp_session_complete --

describe "mcp_session_complete"

it "sets status to completed" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_complete
        st=\$(jq -r '.status' \"\$MCP_SESSION_FILE\")
        [[ \"\$st\" == 'completed' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "deletes .mcpServer/session.yaml on completion" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        workspace=\$(mktemp -d /tmp/mcp-workspace-XXXXXX)
        mkdir -p \"\$workspace/.mcpServer\"
        echo '{}' > \"\$workspace/.mcpServer/session.yaml\"

        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_WORKSPACE_PATH=\"\$workspace\"
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)

        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_complete

        [[ ! -f \"\$workspace/.mcpServer/session.yaml\" ]]
        rm -f \"\$MCP_SESSION_FILE\"
        rm -rf \"\$workspace\"
    "

# -- Summary --

teardown_suite

echo ""
echo "----------------------------------------"
echo "Results: $PASSED passed, $FAILED failed (of $TESTS)"
echo "----------------------------------------"

if [[ $FAILED -gt 0 ]]; then
    exit 1
fi
