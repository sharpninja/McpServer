#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
SCRIPT_DIR="$(core_stage)"
SKILLS_DIR="$SCRIPT_DIR/skills"

# skills/ is host-bound plugin content: the staged root borrows it from a
# host plugin checkout (MCP_PLUGIN_SKILLS_SOURCE). Skip when unavailable.
setup() {
    [ -d "$SKILLS_DIR" ] || skip "skills source unavailable (set MCP_PLUGIN_SKILLS_SOURCE)"
}

# ──────────────────────────────────────────────
# Helper: extract raw YAML frontmatter block
# ──────────────────────────────────────────────
get_frontmatter() {
    local file="$1"
    # Extract content between the first and second '---' lines
    awk '/^---$/{count++; if(count==2) exit; next} count==1{print}' "$file"
}

# ──────────────────────────────────────────────
# Helper: extract body (everything after second ---)
# ──────────────────────────────────────────────
get_body() {
    local file="$1"
    awk '/^---$/{count++; next} count>=2{print}' "$file"
}

# ══════════════════════════════════════════════
# File existence and non-empty checks
# ══════════════════════════════════════════════

@test "skills/todo/SKILL.md exists and is non-empty" {
    [ -f "$SKILLS_DIR/todo/SKILL.md" ]
    [ -s "$SKILLS_DIR/todo/SKILL.md" ]
}

@test "skills/session/SKILL.md exists and is non-empty" {
    [ -f "$SKILLS_DIR/session/SKILL.md" ]
    [ -s "$SKILLS_DIR/session/SKILL.md" ]
}

@test "skills/requirements/SKILL.md exists and is non-empty" {
    [ -f "$SKILLS_DIR/requirements/SKILL.md" ]
    [ -s "$SKILLS_DIR/requirements/SKILL.md" ]
}

@test "skills/graphrag/SKILL.md exists and is non-empty" {
    [ -f "$SKILLS_DIR/graphrag/SKILL.md" ]
    [ -s "$SKILLS_DIR/graphrag/SKILL.md" ]
}

@test "skills/workspace/SKILL.md exists and is non-empty" {
    [ -f "$SKILLS_DIR/workspace/SKILL.md" ]
    [ -s "$SKILLS_DIR/workspace/SKILL.md" ]
}

# ══════════════════════════════════════════════
# Frontmatter: starts with --- delimiter
# ══════════════════════════════════════════════

@test "skills/todo/SKILL.md has YAML frontmatter delimiters" {
    head -1 "$SKILLS_DIR/todo/SKILL.md" | grep -q "^---$"
}

@test "skills/session/SKILL.md has YAML frontmatter delimiters" {
    head -1 "$SKILLS_DIR/session/SKILL.md" | grep -q "^---$"
}

@test "skills/requirements/SKILL.md has YAML frontmatter delimiters" {
    head -1 "$SKILLS_DIR/requirements/SKILL.md" | grep -q "^---$"
}

@test "skills/graphrag/SKILL.md has YAML frontmatter delimiters" {
    head -1 "$SKILLS_DIR/graphrag/SKILL.md" | grep -q "^---$"
}

# ══════════════════════════════════════════════
# Frontmatter: name field present
# ══════════════════════════════════════════════

@test "skills/todo/SKILL.md frontmatter has name field" {
    get_frontmatter "$SKILLS_DIR/todo/SKILL.md" | grep -q "^name:"
}

@test "skills/session/SKILL.md frontmatter has name field" {
    get_frontmatter "$SKILLS_DIR/session/SKILL.md" | grep -q "^name:"
}

@test "skills/requirements/SKILL.md frontmatter has name field" {
    get_frontmatter "$SKILLS_DIR/requirements/SKILL.md" | grep -q "^name:"
}

@test "skills/graphrag/SKILL.md frontmatter has name field" {
    get_frontmatter "$SKILLS_DIR/graphrag/SKILL.md" | grep -q "^name:"
}

# ══════════════════════════════════════════════
# Frontmatter: description field present
# ══════════════════════════════════════════════

@test "skills/todo/SKILL.md frontmatter has description field" {
    get_frontmatter "$SKILLS_DIR/todo/SKILL.md" | grep -q "^description:"
}

@test "skills/session/SKILL.md frontmatter has description field" {
    get_frontmatter "$SKILLS_DIR/session/SKILL.md" | grep -q "^description:"
}

@test "skills/requirements/SKILL.md frontmatter has description field" {
    get_frontmatter "$SKILLS_DIR/requirements/SKILL.md" | grep -q "^description:"
}

@test "skills/graphrag/SKILL.md frontmatter has description field" {
    get_frontmatter "$SKILLS_DIR/graphrag/SKILL.md" | grep -q "^description:"
}

# ══════════════════════════════════════════════
# Description: contains at least 3 quoted trigger phrases
# ══════════════════════════════════════════════

@test "skills/todo/SKILL.md description contains at least 3 quoted trigger phrases" {
    local desc
    desc=$(get_frontmatter "$SKILLS_DIR/todo/SKILL.md" | grep "^description:" | sed 's/^description: *//')
    local count
    count=$(echo "$desc" | grep -o '"[^"]*"' | wc -l)
    [ "$count" -ge 3 ]
}

@test "skills/session/SKILL.md description contains at least 3 quoted trigger phrases" {
    local desc
    desc=$(get_frontmatter "$SKILLS_DIR/session/SKILL.md" | grep "^description:" | sed 's/^description: *//')
    local count
    count=$(echo "$desc" | grep -o '"[^"]*"' | wc -l)
    [ "$count" -ge 3 ]
}

@test "skills/requirements/SKILL.md description contains at least 3 quoted trigger phrases" {
    local desc
    desc=$(get_frontmatter "$SKILLS_DIR/requirements/SKILL.md" | grep "^description:" | sed 's/^description: *//')
    local count
    count=$(echo "$desc" | grep -o '"[^"]*"' | wc -l)
    [ "$count" -ge 3 ]
}

@test "skills/graphrag/SKILL.md description contains at least 3 quoted trigger phrases" {
    local desc
    desc=$(get_frontmatter "$SKILLS_DIR/graphrag/SKILL.md" | grep "^description:" | sed 's/^description: *//')
    local count
    count=$(echo "$desc" | grep -o '"[^"]*"' | wc -l)
    [ "$count" -ge 3 ]
}

# ══════════════════════════════════════════════
# Body: imperative form — first 5 sentences must not start with "You"
# ══════════════════════════════════════════════

check_imperative() {
    local file="$1"
    local body
    body=$(get_body "$file")

    # Extract sentences from non-code-block lines: lines starting with a capital letter
    # that look like prose (not YAML/code fenced lines)
    local prose_lines
    prose_lines=$(echo "$body" | grep -v '^\s*```' | grep -v '^\s*#' | grep -v '^\s*-' | grep -v '^\s*|' | grep -v '^\s*$' | grep -v '^\s*type:' | grep -v '^\s*payload:' | grep -v '^\s*requestId:' | grep -v '^\s*method:' | grep -v '^\s*params:')

    # Check the first 5 prose lines do not start with "You"
    local fail_count=0
    local checked=0
    while IFS= read -r line; do
        if [ $checked -ge 5 ]; then
            break
        fi
        # Skip blank lines
        [ -z "$line" ] && continue
        if echo "$line" | grep -q "^You[[:space:]]"; then
            fail_count=$((fail_count + 1))
        fi
        checked=$((checked + 1))
    done <<< "$prose_lines"

    [ "$fail_count" -eq 0 ]
}

@test "skills/todo/SKILL.md body uses imperative form (first 5 prose lines do not start with 'You')" {
    check_imperative "$SKILLS_DIR/todo/SKILL.md"
}

@test "skills/session/SKILL.md body uses imperative form (first 5 prose lines do not start with 'You')" {
    check_imperative "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/requirements/SKILL.md body uses imperative form (first 5 prose lines do not start with 'You')" {
    check_imperative "$SKILLS_DIR/requirements/SKILL.md"
}

@test "skills/graphrag/SKILL.md body uses imperative form (first 5 prose lines do not start with 'You')" {
    check_imperative "$SKILLS_DIR/graphrag/SKILL.md"
}

# ══════════════════════════════════════════════
# Content: body references mcpserver-repl --agent-stdio
# ══════════════════════════════════════════════

@test "skills/todo/SKILL.md references mcpserver-repl --agent-stdio" {
    grep -q "mcpserver-repl --agent-stdio" "$SKILLS_DIR/todo/SKILL.md"
}

@test "skills/session/SKILL.md references mcpserver-repl --agent-stdio" {
    grep -q "mcpserver-repl --agent-stdio" "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/requirements/SKILL.md references mcpserver-repl --agent-stdio" {
    grep -q "mcpserver-repl --agent-stdio" "$SKILLS_DIR/requirements/SKILL.md"
}

@test "skills/graphrag/SKILL.md references mcpserver-repl --agent-stdio" {
    grep -q "mcpserver-repl --agent-stdio" "$SKILLS_DIR/graphrag/SKILL.md"
}

# ══════════════════════════════════════════════
# Content: body contains YAML envelope examples
# ══════════════════════════════════════════════

@test "skills/todo/SKILL.md contains YAML envelope example (type: request)" {
    grep -q "type: request" "$SKILLS_DIR/todo/SKILL.md"
}

@test "skills/session/SKILL.md contains YAML envelope example (type: request)" {
    grep -q "type: request" "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/requirements/SKILL.md contains YAML envelope example (type: request)" {
    grep -q "type: request" "$SKILLS_DIR/requirements/SKILL.md"
}

@test "skills/graphrag/SKILL.md contains YAML envelope example (type: request)" {
    grep -q "type: request" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/workspace/SKILL.md documents workspace initialization commands" {
    grep -q "client.Workspace.ListAsync" "$SKILLS_DIR/workspace/SKILL.md"
    grep -q "client.Workspace.CreateAsync" "$SKILLS_DIR/workspace/SKILL.md"
    grep -q "client.Workspace.InitAsync" "$SKILLS_DIR/workspace/SKILL.md"
    grep -q "type: request" "$SKILLS_DIR/workspace/SKILL.md"
}

# ══════════════════════════════════════════════
# Content: skill-specific method namespace present
# ══════════════════════════════════════════════

@test "skills/todo/SKILL.md references workflow.todo namespace" {
    grep -q "workflow\.todo\." "$SKILLS_DIR/todo/SKILL.md"
}

@test "skills/session/SKILL.md references workflow.sessionlog namespace" {
    grep -q "workflow\.sessionlog\." "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/requirements/SKILL.md references workflow.requirements namespace" {
    grep -q "workflow\.requirements\." "$SKILLS_DIR/requirements/SKILL.md"
}

@test "skills/graphrag/SKILL.md references workflow.graphrag namespace" {
    grep -q "workflow\.graphrag\." "$SKILLS_DIR/graphrag/SKILL.md"
}

# ══════════════════════════════════════════════
# Content: key trigger phrases appear in description
# ══════════════════════════════════════════════

@test "skills/todo/SKILL.md description contains 'create a todo' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/todo/SKILL.md" | grep "^description:" | grep -q '"create a todo"'
}

@test "skills/todo/SKILL.md description contains 'list todos' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/todo/SKILL.md" | grep "^description:" | grep -q '"list todos"'
}

@test "skills/todo/SKILL.md description contains 'mark todo done' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/todo/SKILL.md" | grep "^description:" | grep -q '"mark todo done"'
}

@test "skills/session/SKILL.md description contains 'start session' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/session/SKILL.md" | grep "^description:" | grep -q '"start session"'
}

@test "skills/session/SKILL.md description contains 'begin turn' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/session/SKILL.md" | grep "^description:" | grep -q '"begin turn"'
}

@test "skills/session/SKILL.md description contains 'complete turn' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/session/SKILL.md" | grep "^description:" | grep -q '"complete turn"'
}

@test "skills/requirements/SKILL.md description contains 'create FR' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/requirements/SKILL.md" | grep "^description:" | grep -q '"create FR"'
}

@test "skills/requirements/SKILL.md description contains 'list requirements' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/requirements/SKILL.md" | grep "^description:" | grep -q '"list requirements"'
}

@test "skills/requirements/SKILL.md description contains 'ingest requirements' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/requirements/SKILL.md" | grep "^description:" | grep -q '"ingest requirements"'
}

@test "skills/graphrag/SKILL.md description contains 'create entity' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/graphrag/SKILL.md" | grep "^description:" | grep -q '"create entity"'
}

@test "skills/graphrag/SKILL.md description contains 'query knowledge graph' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/graphrag/SKILL.md" | grep "^description:" | grep -q '"query knowledge graph"'
}

@test "skills/graphrag/SKILL.md description contains 'delete document' trigger phrase" {
    get_frontmatter "$SKILLS_DIR/graphrag/SKILL.md" | grep "^description:" | grep -q '"delete document"'
}

# ══════════════════════════════════════════════
# Content: TODO skill covers required commands
# ══════════════════════════════════════════════

@test "skills/todo/SKILL.md covers workflow.todo.query command" {
    grep -q "workflow\.todo\.query" "$SKILLS_DIR/todo/SKILL.md"
}

@test "skills/todo/SKILL.md covers workflow.todo.create command" {
    grep -q "workflow\.todo\.create" "$SKILLS_DIR/todo/SKILL.md"
}

@test "skills/todo/SKILL.md covers workflow.todo.update command" {
    grep -q "workflow\.todo\.update" "$SKILLS_DIR/todo/SKILL.md"
}

@test "skills/todo/SKILL.md covers workflow.todo.delete command" {
    grep -q "workflow\.todo\.delete" "$SKILLS_DIR/todo/SKILL.md"
}

@test "skills/todo/SKILL.md covers streamStatus command" {
    grep -q "workflow\.todo\.streamStatus" "$SKILLS_DIR/todo/SKILL.md"
}

@test "skills/todo/SKILL.md covers ISSUE-NEW special create ID" {
    grep -q "ISSUE-NEW" "$SKILLS_DIR/todo/SKILL.md"
}

@test "skills/todo/SKILL.md documents the TODO ID regex pattern" {
    grep -q "\^\\[A-Z\\]" "$SKILLS_DIR/todo/SKILL.md"
}

# ══════════════════════════════════════════════
# Content: Session skill covers required commands
# ══════════════════════════════════════════════

@test "skills/session/SKILL.md covers openSession command" {
    grep -q "workflow\.sessionlog\.openSession" "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/session/SKILL.md covers beginTurn command" {
    grep -q "workflow\.sessionlog\.beginTurn" "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/session/SKILL.md covers updateTurn command" {
    grep -q "workflow\.sessionlog\.updateTurn" "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/session/SKILL.md covers completeTurn command" {
    grep -q "workflow\.sessionlog\.completeTurn" "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/session/SKILL.md covers failTurn command" {
    grep -q "workflow\.sessionlog\.failTurn" "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/session/SKILL.md covers appendDialog command" {
    grep -q "workflow\.sessionlog\.appendDialog" "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/session/SKILL.md covers appendActions command" {
    grep -q "workflow\.sessionlog\.appendActions" "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/session/SKILL.md covers queryHistory command" {
    grep -q "workflow\.sessionlog\.queryHistory" "$SKILLS_DIR/session/SKILL.md"
}

@test "skills/session/SKILL.md documents session ID naming convention" {
    grep -q "yyyyMMddTHHmmssZ" "$SKILLS_DIR/session/SKILL.md"
}

# ══════════════════════════════════════════════
# Content: Requirements skill covers required commands
# ══════════════════════════════════════════════

@test "skills/requirements/SKILL.md covers listFr command" {
    grep -q "workflow\.requirements\.listFr" "$SKILLS_DIR/requirements/SKILL.md"
}

@test "skills/requirements/SKILL.md covers createFr command" {
    grep -q "workflow\.requirements\.createFr" "$SKILLS_DIR/requirements/SKILL.md"
}

@test "skills/requirements/SKILL.md covers createTr command" {
    grep -q "workflow\.requirements\.createTr" "$SKILLS_DIR/requirements/SKILL.md"
}

@test "skills/requirements/SKILL.md covers listTest command" {
    grep -q "workflow\.requirements\.listTest" "$SKILLS_DIR/requirements/SKILL.md"
}

@test "skills/requirements/SKILL.md covers createMapping command" {
    grep -q "workflow\.requirements\.createMapping" "$SKILLS_DIR/requirements/SKILL.md"
}

@test "skills/requirements/SKILL.md covers generateDocument command" {
    grep -q "workflow\.requirements\.generateDocument" "$SKILLS_DIR/requirements/SKILL.md"
}

@test "skills/requirements/SKILL.md covers ingestDocument command" {
    grep -q "workflow\.requirements\.ingestDocument" "$SKILLS_DIR/requirements/SKILL.md"
}

# ══════════════════════════════════════════════
# Content: GraphRAG skill covers required commands
# ══════════════════════════════════════════════

@test "skills/graphrag/SKILL.md covers status command" {
    grep -q "workflow\.graphrag\.status" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers index command" {
    grep -q "workflow\.graphrag\.index" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers query command" {
    grep -q "workflow\.graphrag\.query" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers ingest command" {
    grep -q "workflow\.graphrag\.ingest" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers documents.list command" {
    grep -q "workflow\.graphrag\.documents\.list" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers documents.chunks command" {
    grep -q "workflow\.graphrag\.documents\.chunks" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers documents.delete command" {
    grep -q "workflow\.graphrag\.documents\.delete" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers entities.create command" {
    grep -q "workflow\.graphrag\.entities\.create" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers entities.list command" {
    grep -q "workflow\.graphrag\.entities\.list" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers entities.delete command" {
    grep -q "workflow\.graphrag\.entities\.delete" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers relationships.create command" {
    grep -q "workflow\.graphrag\.relationships\.create" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers relationships.list command" {
    grep -q "workflow\.graphrag\.relationships\.list" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md covers relationships.delete command" {
    grep -q "workflow\.graphrag\.relationships\.delete" "$SKILLS_DIR/graphrag/SKILL.md"
}

@test "skills/graphrag/SKILL.md documents local/global/drift query modes" {
    grep -q "local" "$SKILLS_DIR/graphrag/SKILL.md"
    grep -q "global" "$SKILLS_DIR/graphrag/SKILL.md"
    grep -q "drift" "$SKILLS_DIR/graphrag/SKILL.md"
}

# ══════════════════════════════════════════════
# Workflow skill acceptance criteria
# ══════════════════════════════════════════════

WORKFLOW_SKILLS=(sync-logs commit-sync wrap-up)

@test "workflow skills satisfy AC-SKILLS-001 and AC-SKILLS-002" {
    for skill in "${WORKFLOW_SKILLS[@]}"; do
        local skill_file="$SKILLS_DIR/$skill/SKILL.md"
        [ -s "$skill_file" ]
        get_frontmatter "$skill_file" | grep -Eq "^name:[[:space:]]*.+"
        get_frontmatter "$skill_file" | grep -Eq "^description:[[:space:]]*.+"
    done
}

@test "sync-logs skill documents AC-SKILLS-003" {
    local skill_file="$SKILLS_DIR/sync-logs/SKILL.md"
    grep -Eiq "status check|mcp.*status|Status" "$skill_file"
    grep -Eq "workflow\.sessionlog\.(openSession|beginTurn)|session/turn|turn handling" "$skill_file"
    grep -q "workflow.sessionlog.appendDialog" "$skill_file"
    grep -q "workflow.sessionlog.appendActions" "$skill_file"
    grep -Eiq "background.*session|session.*background" "$skill_file"
    grep -Eiq "factual summary|factual.*summary" "$skill_file"
    grep -Eiq "raw[[:space:]-]*REST" "$skill_file"
}

@test "commit-sync skill documents AC-SKILLS-004" {
    local skill_file="$SKILLS_DIR/commit-sync/SKILL.md"
    grep -Eiq "pause" "$skill_file"
    grep -Eiq "repo-scope|repo scope|dirty tree|dirty-tree" "$skill_file"
    grep -Eiq "acknowledg" "$skill_file"
    grep -Fq "git add -A -- ." "$skill_file"
    grep -Eiq "commit SHA|git rev-parse HEAD" "$skill_file"
    grep -Eiq "push result|git push" "$skill_file"
    grep -Eiq "force|rewrite" "$skill_file"
}

@test "wrap-up skill documents AC-SKILLS-005" {
    local skill_file="$SKILLS_DIR/wrap-up/SKILL.md"
    grep -Eiq "marker trust|trust.*marker" "$skill_file"
    grep -Eiq "requirement reconciliation|requirements.*reconcile|reconcile.*requirements" "$skill_file"
    grep -Eiq "wiki|generateDocument" "$skill_file"
    grep -Eiq "validation" "$skill_file"
    grep -Eiq "commit" "$skill_file"
    grep -Eiq "push" "$skill_file"
    grep -Eiq "session-log reconciliation|session log reconciliation|reconcile.*session" "$skill_file"
    grep -Eq "workflow\.sessionlog\.(completeTurn|failTurn)" "$skill_file"
}

@test "workflow skills are exposed by plugin metadata for AC-SKILLS-006" {
    if [ -f "$SCRIPT_DIR/.codex-plugin/plugin.json" ]; then
        grep -q '"skillsPath": "skills"' "$SCRIPT_DIR/.codex-plugin/plugin.json"
    else
        [ -d "$SKILLS_DIR" ]
    fi
}
