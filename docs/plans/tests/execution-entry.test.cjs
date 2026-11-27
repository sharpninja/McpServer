// From the repository root (no project dependency changes):
//   deps="$(mktemp -d)"
//   npm install --prefix "$deps" --no-save --no-package-lock ajv@8.17.1 yaml@2.8.2
//   NODE_PATH="$deps/node_modules" node --test docs/plans/tests/execution-entry.test.cjs
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const Ajv2020 = require('ajv/dist/2020').default;
const { parse } = require('yaml');

const planDir = path.resolve(__dirname, '..');
const schema = JSON.parse(fs.readFileSync(path.join(planDir, 'BDPv4-QBrain-Implementation-Plan.schema.json'), 'utf8'));
const template = parse(fs.readFileSync(path.join(planDir, 'BDPv4-QBrain-Implementation-Plan-Template.yaml'), 'utf8'), { uniqueKeys: true });
// The schema also checks canonical UUID syntax with a pattern.
const validate = new Ajv2020({ strict: false, validateFormats: false }).compile(schema);

function hasGate(slice, gate) {
  return slice.gateIds.includes(gate);
}

function omitExecutionEntry(slice) {
  assert(hasGate(slice, 'execution-entry'));
  slice.gateIds = slice.gateIds.filter(id => id !== 'execution-entry');
}

function validatePlan(plan, expected, gatePath) {
  const valid = validate(plan);
  assert.equal(valid, expected, JSON.stringify(validate.errors));
  if (!expected) {
    assert(validate.errors.some(e => e.keyword === 'contains' && e.instancePath === gatePath), JSON.stringify(validate.errors));
  }
}

function explicitMixedPlan(plan) {
  const example = plan.example.artifactDependency;
  const phase = plan.delivery.phases[0];
  const defaults = phase.defaultSlice;
  delete phase.defaultSlice;
  phase.phaseId = example.phaseId;
  phase.sliceRefs = structuredClone(example.sliceRefs);
  plan.delivery.slices = phase.sliceRefs.map(sliceRef => ({ ...structuredClone(defaults), sliceRef }));
  plan.artifactDependencies.artifacts = structuredClone(example.artifacts);
  plan.artifactDependencies.versions = structuredClone(example.versions);
  plan.artifactDependencies.edges = structuredClone(example.edges);
  return plan.delivery.slices.find(slice => slice.sliceRef.kind === 'todo');
}

function toLegacy(value) {
  if (Array.isArray(value)) return value.map(toLegacy);
  if (!value || typeof value !== 'object') return value;
  const result = {};
  for (const [key, item] of Object.entries(value)) {
    if (key === 'schemaVersion') result[key] = '0.7';
    else if (key === 'taskWorkflow') continue;
    else if (key === 'gateIds') result[key] = item.filter(id => id !== 'execution-entry');
    else if (key === 'gateId' && item === 'execution-entry') result[key] = 'slice-entry';
    else if (key === 'gates') result[key] = item.filter(g => g.id !== 'execution-entry').map(g =>
      g.id === 'implementation-exit' ? { ...g, requires: ['mocks-validated'] } : g);
    else if (key === 'sliceRefs') result.sliceTodoIds = item.map(ref => ref.todoId || 'EXPORT-CONTRACT-001');
    else if (key === 'sliceRef') {
      const legacyKey = 'gateIds' in value ? 'todoId' : 'artifactVersionId' in value ? 'sliceTodoId' : 'sliceTodo';
      result[legacyKey] = item.todoId || 'EXPORT-CONTRACT-001';
    } else result[key] = toLegacy(item);
  }
  return result;
}

test('v0.8 inline implementation slice cannot omit execution-entry', () => {
  const plan = structuredClone(template);
  validatePlan(plan, true);
  omitExecutionEntry(plan.delivery.phases[0].defaultSlice);
  validatePlan(plan, false, '/delivery/phases/0/defaultSlice/gateIds');
});

test('v0.8 explicit TODO consumer of Task Group output cannot omit execution-entry', () => {
  const plan = structuredClone(template);
  const consumer = explicitMixedPlan(plan);
  validatePlan(plan, true);
  const index = plan.delivery.slices.indexOf(consumer);
  omitExecutionEntry(consumer);
  validatePlan(plan, false, '/delivery/slices/' + index + '/gateIds');
});

test('v0.8 verification-only slice need not declare an implementation gate', () => {
  const plan = structuredClone(template);
  const slice = plan.delivery.phases[0].defaultSlice;
  slice.gateIds = ['slice-entry'];
  validatePlan(plan, true);
});

test('v0.7 inline and explicit implementation slices keep their original gate list', () => {
  const inline = toLegacy(template);
  validatePlan(inline, true);
  const phase = inline.delivery.phases[0];
  const slice = phase.defaultSlice;
  delete phase.defaultSlice;
  phase.sliceTodoIds = [slice.todoId];
  inline.delivery.slices = [slice];
  validatePlan(inline, true);
});
