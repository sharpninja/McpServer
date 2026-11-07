@{
    requestId = 'req-20261107T002200Z-unitgreen-r14-relaunch'
    queryTitle = 'Independent hostile validation of IMPL-UNITGREEN-001 round 14 relaunch'
    queryText = 'Perform one independent read-only hostile validation, unitgreen-r14, at records head 0980a52b016386cfa6e131bd3efe9fc32e1c6e78 and code head da11faa2. Full request: docs/receipts/hv/20261106T181559-unitgreen-r14.request.jsonl. Supplied TRX policy prohibits builds, restores and tests. Judge all UG1-UG59 claims, slice Z16, 61 tasks, requirement mappings and historical closure, with no edits except reviewer receipts and this audit turn. Preserve first-launch receipts. Reviewer is Codex CLI, live rollout model gpt-6-astra, effort max, thread 01a1b35c-ba09-7e80-9cba-06848c138e69.'
    interpretation = 'Review product and retained evidence independently; do not remediate implementation or alter live requirements, tasks, memories, triage or other sessions.'
    tags = @('unitgreen-r14', 'IMPL-UNITGREEN-001', 'hostile-validator', 'read-only', 'gpt-6-astra', 'max')
    contextList = @('docs/receipts/hv/20261106T181559-unitgreen-r14.request.jsonl', 'docs/receipts/hv/unitgreen-r14-trx-da11faa2/manifest.json', 'docs/receipts/hv/unitgreen-r14-tasks-da11faa2.json')
}
