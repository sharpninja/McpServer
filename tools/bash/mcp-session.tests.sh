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
        mcp_session_init '$TEST_TMPDIR/AGENTS-README-FIRST.yaml'
        [[ \"\$MCP_BASE_URL\" == 'http://localhost:9999' ]]
    "

it "reads apiKey from marker file" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { echo '{\"status\":\"ok\"}'; }
        mcp_session_init '$TEST_TMPDIR/AGENTS-README-FIRST.yaml'
        [[ \"\$MCP_API_KEY\" == 'test-api-key-abc123' ]]
    "

it "creates session temp file" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { echo '{\"status\":\"ok\"}'; }
        mcp_session_init '$TEST_TMPDIR/AGENTS-README-FIRST.yaml'
        [[ -n \"\$MCP_SESSION_FILE\" && \"\$MCP_SESSION_FILE\" == /tmp/mcp-session-*.json ]]
    "

it "fails when marker file not found" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        ! mcp_session_init '/nonexistent/path/marker.yaml' 2>/dev/null
    "

it "discovers marker by walking up directories" \
    bash -c "
        mkdir -p '$TEST_TMPDIR/a/b/c'
        cd '$TEST_TMPDIR/a/b/c'
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { echo '{\"status\":\"ok\"}'; }
        mcp_session_init
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

it "initializes empty entries array" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        count=\$(jq '.entries | length' \"\$MCP_SESSION_FILE\")
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

# -- mcp_session_add_entry --

describe "mcp_session_add_entry"

it "adds entry with correct requestId and queryTitle" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_entry 'req-001' 'Fix bug' 'Fix the auth bug' 'in_progress'
        rid=\$(jq -r '.entries[0].requestId' \"\$MCP_SESSION_FILE\")
        qt=\$(jq -r '.entries[0].queryTitle' \"\$MCP_SESSION_FILE\")
        [[ \"\$rid\" == 'req-001' && \"\$qt\" == 'Fix bug' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "sets entry status correctly" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_entry 'r1' 'title' 'text' 'completed'
        st=\$(jq -r '.entries[0].status' \"\$MCP_SESSION_FILE\")
        [[ \"\$st\" == 'completed' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "initializes empty collections on entry" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_entry 'r1' 'title' 'text' 'in_progress'
        jq -e '.entries[0].actions | length == 0' \"\$MCP_SESSION_FILE\" > /dev/null &&
        jq -e '.entries[0].filesModified | length == 0' \"\$MCP_SESSION_FILE\" > /dev/null &&
        jq -e '.entries[0].designDecisions | length == 0' \"\$MCP_SESSION_FILE\" > /dev/null
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "appends multiple entries" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_entry 'r1' 'First' 'text1' 'in_progress'
        mcp_session_add_entry 'r2' 'Second' 'text2' 'completed'
        count=\$(jq '.entries | length' \"\$MCP_SESSION_FILE\")
        [[ \"\$count\" == '2' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

# -- mcp_session_update_entry --

describe "mcp_session_update_entry"

it "updates entry response field" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_entry 'r1' 'title' 'text' 'in_progress'
        mcp_session_update_entry 'r1' 'response' 'All done!'
        resp=\$(jq -r '.entries[0].response' \"\$MCP_SESSION_FILE\")
        [[ \"\$resp\" == 'All done!' ]]
        rm -f \"\$MCP_SESSION_FILE\"
    "

it "updates entry status field" \
    bash -c "
        source '$SCRIPT_DIR/mcp-session.sh'
        curl() { :; }
        MCP_BASE_URL='http://test:9999'
        MCP_API_KEY='key'
        MCP_SESSION_FILE=\$(mktemp /tmp/mcp-test-XXXXXX.json)
        mcp_session_create 'A' 't' 'm' 'sid' > /dev/null
        mcp_session_add_entry 'r1' 'title' 'text' 'in_progress'
        mcp_session_update_entry 'r1' 'status' 'completed'
        st=\$(jq -r '.entries[0].status' \"\$MCP_SESSION_FILE\")
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
        mcp_session_add_entry 'r1' 'title' 'text' 'in_progress'
        mcp_session_add_action 'r1' 'Created file' 'create' 'new.cs' 'completed'
        desc=\$(jq -r '.entries[0].actions[0].description' \"\$MCP_SESSION_FILE\")
        atype=\$(jq -r '.entries[0].actions[0].type' \"\$MCP_SESSION_FILE\")
        fp=\$(jq -r '.entries[0].actions[0].filePath' \"\$MCP_SESSION_FILE\")
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
        mcp_session_add_entry 'r1' 'title' 'text' 'in_progress'
        mcp_session_add_action 'r1' 'First' 'edit' 'a.cs'
        mcp_session_add_action 'r1' 'Second' 'edit' 'b.cs'
        o1=\$(jq '.entries[0].actions[0].order' \"\$MCP_SESSION_FILE\")
        o2=\$(jq '.entries[0].actions[1].order' \"\$MCP_SESSION_FILE\")
        [[ \"\$o1\" == '1' && \"\$o2\" == '2' ]]
        rm -f \"\$MCP_SESSION_FILE\"
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
        mcp_session_add_entry 'r1' 'title' 'text' 'in_progress'
        mcp_session_add_file 'r1' 'src/main.cs'
        mcp_session_add_file 'r1' 'src/test.cs'
        count=\$(jq '.entries[0].filesModified | length' \"\$MCP_SESSION_FILE\")
        first=\$(jq -r '.entries[0].filesModified[0]' \"\$MCP_SESSION_FILE\")
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
        mcp_session_add_entry 'r1' 'title' 'text' 'in_progress'
        mcp_session_add_tag 'r1' 'bugfix'
        tag=\$(jq -r '.entries[0].tags[0]' \"\$MCP_SESSION_FILE\")
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

# -- Summary --

teardown_suite

echo ""
echo "----------------------------------------"
echo "Results: $PASSED passed, $FAILED failed (of $TESTS)"
echo "----------------------------------------"

if [[ $FAILED -gt 0 ]]; then
    exit 1
fi