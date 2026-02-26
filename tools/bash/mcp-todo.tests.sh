#!/usr/bin/env bash
# Tests for mcp-todo.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TESTS=0 PASSED=0 FAILED=0
TEST_TMPDIR=""

setup_suite() {
    TEST_TMPDIR=$(mktemp -d /tmp/mcp-todo-test-XXXXXX)
    cat > "$TEST_TMPDIR/AGENTS-README-FIRST.yaml" <<EOF
owner: test-owner
baseUrl: http://localhost:9999
apiKey: todo-key-xyz
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

# -- mcp_todo_init --

describe "mcp_todo_init"

it "reads baseUrl from marker file" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        curl() { return 0; }
        mcp_todo_init '$TEST_TMPDIR/AGENTS-README-FIRST.yaml'
        [[ \"\$MCP_TODO_BASE_URL\" == 'http://localhost:9999' ]]
    "

it "reads apiKey from marker file" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        curl() { return 0; }
        mcp_todo_init '$TEST_TMPDIR/AGENTS-README-FIRST.yaml'
        [[ \"\$MCP_TODO_API_KEY\" == 'todo-key-xyz' ]]
    "

it "fails when marker file not found" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        ! mcp_todo_init '/nonexistent/marker.yaml' 2>/dev/null
    "

it "discovers marker by walking up directories" \
    bash -c "
        mkdir -p '$TEST_TMPDIR/d1/d2/d3'
        cd '$TEST_TMPDIR/d1/d2/d3'
        source '$SCRIPT_DIR/mcp-todo.sh'
        curl() { return 0; }
        mcp_todo_init
        [[ \"\$MCP_TODO_BASE_URL\" == 'http://localhost:9999' ]]
    "

# -- mcp_todo_create --

describe "mcp_todo_create"

it "creates todo with required fields" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        CAPTURED_BODY=''
        curl() {
            local arg capture_next=false
            for arg in \"\$@\"; do
                if \$capture_next; then CAPTURED_BODY=\"\$arg\"; capture_next=false; fi
                if [[ \"\$arg\" == '-d' ]]; then capture_next=true; fi
            done
            echo '{\"id\":\"new-todo\"}'
        }
        MCP_TODO_BASE_URL='http://test:9999'
        MCP_TODO_API_KEY='key'
        mcp_todo_create 'new-todo' 'New Todo' 'Backend' 'high' > /dev/null
        echo \"\$CAPTURED_BODY\" | jq -e '.id == \"new-todo\"' > /dev/null &&
        echo \"\$CAPTURED_BODY\" | jq -e '.title == \"New Todo\"' > /dev/null &&
        echo \"\$CAPTURED_BODY\" | jq -e '.section == \"Backend\"' > /dev/null &&
        echo \"\$CAPTURED_BODY\" | jq -e '.priority == \"high\"' > /dev/null
    "

it "merges extra JSON fields" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        CAPTURED_BODY=''
        curl() {
            local arg capture_next=false
            for arg in \"\$@\"; do
                if \$capture_next; then CAPTURED_BODY=\"\$arg\"; capture_next=false; fi
                if [[ \"\$arg\" == '-d' ]]; then capture_next=true; fi
            done
            echo '{}'
        }
        MCP_TODO_BASE_URL='http://test:9999'
        MCP_TODO_API_KEY='key'
        mcp_todo_create 'x' 't' 's' 'low' '{\"estimate\":\"2h\",\"note\":\"test\"}' > /dev/null
        echo \"\$CAPTURED_BODY\" | jq -e '.estimate == \"2h\"' > /dev/null &&
        echo \"\$CAPTURED_BODY\" | jq -e '.note == \"test\"' > /dev/null
    "

# -- mcp_todo_create_full --

describe "mcp_todo_create_full"

it "passes complete JSON body to POST" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        CAPTURED_BODY=''
        curl() {
            local arg capture_next=false
            for arg in \"\$@\"; do
                if \$capture_next; then CAPTURED_BODY=\"\$arg\"; capture_next=false; fi
                if [[ \"\$arg\" == '-d' ]]; then capture_next=true; fi
            done
            echo '{}'
        }
        MCP_TODO_BASE_URL='http://test:9999'
        MCP_TODO_API_KEY='key'
        body='{\"id\":\"full\",\"title\":\"Full\",\"section\":\"FE\",\"priority\":\"critical\"}'
        mcp_todo_create_full \"\$body\" > /dev/null
        echo \"\$CAPTURED_BODY\" | jq -e '.id == \"full\"' > /dev/null
    "

# -- mcp_todo_update --

describe "mcp_todo_update"

it "sends PUT with JSON body" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        CAPTURED_BODY=''
        curl() {
            local arg capture_next=false
            for arg in \"\$@\"; do
                if \$capture_next; then CAPTURED_BODY=\"\$arg\"; capture_next=false; fi
                if [[ \"\$arg\" == '-d' ]]; then capture_next=true; fi
            done
            echo '{}'
        }
        MCP_TODO_BASE_URL='http://test:9999'
        MCP_TODO_API_KEY='key'
        mcp_todo_update 'fix-auth' '{\"priority\":\"critical\"}' > /dev/null
        echo \"\$CAPTURED_BODY\" | jq -e '.priority == \"critical\"' > /dev/null
    "

# -- mcp_todo_complete --

describe "mcp_todo_complete"

it "sends done=true with summary and date" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        CAPTURED_BODY=''
        curl() {
            local arg capture_next=false
            for arg in \"\$@\"; do
                if \$capture_next; then CAPTURED_BODY=\"\$arg\"; capture_next=false; fi
                if [[ \"\$arg\" == '-d' ]]; then capture_next=true; fi
            done
            echo '{}'
        }
        MCP_TODO_BASE_URL='http://test:9999'
        MCP_TODO_API_KEY='key'
        mcp_todo_complete 'fix-auth' 'Auth fixed with JWT' > /dev/null
        echo \"\$CAPTURED_BODY\" | jq -e '.done == true' > /dev/null &&
        echo \"\$CAPTURED_BODY\" | jq -e '.doneSummary == \"Auth fixed with JWT\"' > /dev/null &&
        echo \"\$CAPTURED_BODY\" | jq -e 'has(\"completedDate\")' > /dev/null
    "

it "sets completedDate to ISO 8601 UTC format" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        CAPTURED_BODY=''
        curl() {
            local arg capture_next=false
            for arg in \"\$@\"; do
                if \$capture_next; then CAPTURED_BODY=\"\$arg\"; capture_next=false; fi
                if [[ \"\$arg\" == '-d' ]]; then capture_next=true; fi
            done
            echo '{}'
        }
        MCP_TODO_BASE_URL='http://test:9999'
        MCP_TODO_API_KEY='key'
        mcp_todo_complete 'x' 'done' > /dev/null
        date_val=\$(echo \"\$CAPTURED_BODY\" | jq -r '.completedDate')
        [[ \"\$date_val\" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}T ]]
    "

# -- mcp_todo_delete --

describe "mcp_todo_delete"

it "calls DELETE on the correct endpoint" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        CAPTURED_URL=''
        curl() {
            local arg
            for arg in \"\$@\"; do
                if [[ \"\$arg\" == http* ]]; then CAPTURED_URL=\"\$arg\"; fi
            done
        }
        MCP_TODO_BASE_URL='http://test:9999'
        MCP_TODO_API_KEY='key'
        mcp_todo_delete 'old-todo' > /dev/null
        [[ \"\$CAPTURED_URL\" == 'http://test:9999/mcp/todo/old-todo' ]]
    "

# -- mcp_todo_add_requirements --

describe "mcp_todo_add_requirements"

it "posts requirements to correct endpoint" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        CAPTURED_BODY='' CAPTURED_URL=''
        curl() {
            local arg capture_next=false
            for arg in \"\$@\"; do
                if \$capture_next; then CAPTURED_BODY=\"\$arg\"; capture_next=false; fi
                if [[ \"\$arg\" == '-d' ]]; then capture_next=true; fi
                if [[ \"\$arg\" == http* ]]; then CAPTURED_URL=\"\$arg\"; fi
            done
            echo '{}'
        }
        MCP_TODO_BASE_URL='http://test:9999'
        MCP_TODO_API_KEY='key'
        body='{\"functionalRequirements\":[\"FR-001\"],\"technicalRequirements\":[\"TR-001\"]}'
        mcp_todo_add_requirements 'api' \"\$body\" > /dev/null
        [[ \"\$CAPTURED_URL\" == *'/mcp/todo/api/requirements' ]] &&
        echo \"\$CAPTURED_BODY\" | jq -e '.functionalRequirements[0] == \"FR-001\"' > /dev/null
    "

# -- mcp_todo_prompt --

describe "mcp_todo_prompt"

it "calls GET /mcp/todo/{id}/prompt/{type}" \
    bash -c "
        source '$SCRIPT_DIR/mcp-todo.sh'
        CAPTURED_URL=''
        curl() {
            for arg in \"\$@\"; do
                if [[ \"\$arg\" == http* ]]; then CAPTURED_URL=\"\$arg\"; fi
            done
            echo 'prompt text'
        }
        MCP_TODO_BASE_URL='http://test:9999'
        MCP_TODO_API_KEY='key'
        mcp_todo_prompt 'fix-auth' 'implement' > /dev/null
        [[ \"\$CAPTURED_URL\" == 'http://test:9999/mcp/todo/fix-auth/prompt/implement' ]]
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