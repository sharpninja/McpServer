// Host-neutral structural tool shape (was the MCP SDK Tool type in cline v1;
// the core package must not depend on a specific host SDK).
interface Tool {
  name: string;
  inputSchema?: unknown;
}

type Schema = {
  type?: string | string[];
  properties?: Record<string, Schema>;
  required?: string[];
  additionalProperties?: boolean | Schema;
  items?: Schema;
  enum?: unknown[];
  const?: unknown;
  minItems?: number;
  anyOf?: Schema[];
  oneOf?: Schema[];
  allOf?: Schema[];
};

function schemaFor(toolName: string, tools: Tool[]): Schema {
  const tool = tools.find((candidate) => candidate.name === toolName);
  if (!tool) throw new Error(`Unknown tool: ${toolName}`);
  return (tool.inputSchema ?? { type: 'object' }) as Schema;
}

function typeOf(value: unknown): string {
  if (Array.isArray(value)) return 'array';
  if (value === null) return 'null';
  if (Number.isInteger(value)) return 'integer';
  return typeof value;
}

function acceptsType(value: unknown, expected: string): boolean {
  if (expected === 'number') return typeof value === 'number' && Number.isFinite(value);
  if (expected === 'integer') return Number.isInteger(value);
  if (expected === 'array') return Array.isArray(value);
  if (expected === 'object') return typeof value === 'object' && value !== null && !Array.isArray(value);
  if (expected === 'null') return value === null;
  return typeof value === expected;
}

function sameValue(left: unknown, right: unknown): boolean {
  return Object.is(left, right) || JSON.stringify(left) === JSON.stringify(right);
}

function childPath(path: string, key: string): string {
  return path === '$' ? `$.${key}` : `${path}.${key}`;
}

function validateSchema(value: unknown, schema: Schema, path: string, errors: string[]): void {
  if (schema.const !== undefined && !sameValue(value, schema.const)) {
    errors.push(`${path} must be ${JSON.stringify(schema.const)}`);
  }

  if (schema.enum && !schema.enum.some((item) => sameValue(item, value))) {
    errors.push(`${path} must be one of ${schema.enum.map((item) => JSON.stringify(item)).join(', ')}`);
  }

  if (schema.anyOf) {
    const matched = schema.anyOf.some((candidate) => {
      const nested: string[] = [];
      validateSchema(value, candidate, path, nested);
      return nested.length === 0;
    });
    if (!matched) errors.push(`${path} must match at least one allowed schema`);
  }

  if (schema.oneOf) {
    const matches = schema.oneOf.filter((candidate) => {
      const nested: string[] = [];
      validateSchema(value, candidate, path, nested);
      return nested.length === 0;
    }).length;
    if (matches !== 1) errors.push(`${path} must match exactly one allowed schema`);
  }

  if (schema.allOf) {
    for (const candidate of schema.allOf) {
      validateSchema(value, candidate, path, errors);
    }
  }

  const expectedTypes = Array.isArray(schema.type) ? schema.type : schema.type ? [schema.type] : [];
  if (expectedTypes.length > 0 && !expectedTypes.some((expected) => acceptsType(value, expected))) {
    errors.push(`${path} must be ${expectedTypes.join(' or ')}, got ${typeOf(value)}`);
    return;
  }

  if (Array.isArray(value)) {
    if (schema.minItems !== undefined && value.length < schema.minItems) {
      errors.push(`${path} must contain at least ${schema.minItems} item(s)`);
    }
    if (schema.items) {
      value.forEach((item, index) => validateSchema(item, schema.items!, `${path}[${index}]`, errors));
    }
    return;
  }

  if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
    const record = value as Record<string, unknown>;
    for (const required of schema.required ?? []) {
      if (record[required] === undefined) errors.push(`${childPath(path, required)} is required`);
    }

    for (const [key, propertySchema] of Object.entries(schema.properties ?? {})) {
      if (record[key] !== undefined) validateSchema(record[key], propertySchema, childPath(path, key), errors);
    }

    if (schema.additionalProperties === false) {
      const known = new Set(Object.keys(schema.properties ?? {}));
      for (const key of Object.keys(record)) {
        if (!known.has(key)) errors.push(`${childPath(path, key)} is not allowed`);
      }
    } else if (typeof schema.additionalProperties === 'object') {
      const known = new Set(Object.keys(schema.properties ?? {}));
      for (const [key, item] of Object.entries(record)) {
        if (!known.has(key)) validateSchema(item, schema.additionalProperties, childPath(path, key), errors);
      }
    }
  }
}

export function validateToolArguments(toolName: string, args: Record<string, unknown>, tools: Tool[]): void {
  const errors: string[] = [];
  validateSchema(args, schemaFor(toolName, tools), '$', errors);
  if (errors.length > 0) {
    throw new Error(`schema_validation_failed: ${toolName}: ${errors.join('; ')}`);
  }
}
