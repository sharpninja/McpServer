#!/usr/bin/env node
/* eslint-disable no-console */

const fs = require("fs");

const inputPath = process.argv[2];
if (!inputPath) {
  console.error("usage: pending-import-to-yaml.js <pending-import.json>");
  process.exit(2);
}

const nowIso = new Date().toISOString();
const source = JSON.parse(fs.readFileSync(inputPath, "utf8"));

function compactStatus(value) {
  const text = String(value || "completed").toLowerCase();
  if (text.startsWith("failed") || text.startsWith("interrupted")) return "failed";
  if (text.startsWith("completed") || text.startsWith("partial")) return "completed";
  return "in_progress";
}

function scalar(value) {
  if (value === undefined || value === null) return "\"\"";
  if (typeof value === "number" || typeof value === "boolean") return String(value);
  return JSON.stringify(String(value));
}

function yamlList(values, indent = "  ") {
  if (!Array.isArray(values) || values.length === 0) return " []";
  const lines = [""];
  for (const value of values) {
    if (value && typeof value === "object" && !Array.isArray(value)) {
      const keys = Object.keys(value);
      if (keys.length === 0) continue;
      lines.push(`${indent}- ${keys[0]}: ${scalar(value[keys[0]])}`);
      for (const key of keys.slice(1)) {
        lines.push(`${indent}  ${key}: ${scalar(value[key])}`);
      }
    } else {
      lines.push(`${indent}- ${scalar(value)}`);
    }
  }
  return lines.join("\n");
}

function pushUnique(target, item, keyFn) {
  const key = keyFn(item);
  if (!target.some(existing => keyFn(existing) === key)) {
    target.push(item);
  }
}

function normalizeAction(action, order) {
  return {
    order: Number(action.order || order || 1),
    type: String(action.type || "recovery_import"),
    status: compactStatus(action.status || "completed"),
    description: String(action.description || action.summary || "Recovered action"),
    filePath: String(action.filePath || "")
  };
}

function normalizeDecision(decision) {
  if (typeof decision === "string") return decision;
  if (!decision || typeof decision !== "object") return String(decision || "");
  const title = decision.title || "Decision";
  const body = decision.body || decision.description || "";
  return body ? `${title}: ${body}` : String(title);
}

function normalizeTurn(raw, index, defaults) {
  const requestId = raw.requestId || raw.id || `req-${nowIso.replace(/[-:]/g, "").replace(/\.\d+Z$/, "Z")}-import-${index}`;
  const actions = [];
  for (const action of raw.actions || []) {
    actions.push(normalizeAction(action, actions.length + 1));
  }
  for (const validation of raw.validation || raw.validations || []) {
    actions.push(normalizeAction({
      type: "test",
      status: validation.result && String(validation.result).toLowerCase().includes("fail") ? "failed" : "completed",
      description: validation.command
        ? `Validated ${validation.command}: ${validation.result || "completed"}`
        : String(validation.result || "Validation evidence"),
      filePath: validation.filePath || ""
    }, actions.length + 1));
  }
  for (const todo of raw.todoUpdates || []) {
    actions.push(normalizeAction({
      type: "todo_update",
      status: "completed",
      description: `${todo.id || "TODO"} ${todo.status || "updated"}: ${todo.summary || ""}`.trim(),
      filePath: todo.id ? `MCP TODO: ${todo.id}` : ""
    }, actions.length + 1));
  }

  const processingDialog = [];
  for (const observation of raw.observations || []) {
    processingDialog.push({
      timestamp: raw.timestamp || defaults.started || nowIso,
      role: "model",
      category: "observation",
      content: String(observation)
    });
  }

  return {
    requestId,
    timestamp: raw.timestamp || raw.createdAtUtc || defaults.started || nowIso,
    queryTitle: raw.queryTitle || raw.title || raw.summary || "Recovered turn",
    queryText: raw.queryText || raw.query || raw.summary || "Recovered session-log turn",
    response: raw.response || raw.summary || "",
    interpretation: raw.interpretation || "",
    status: compactStatus(raw.status),
    actions,
    model: raw.model || defaults.model || "unknown",
    tokenCount: Number(raw.tokenCount || 0),
    tags: raw.tags || [],
    contextList: raw.contextList || raw.context || [],
    processingDialog,
    designDecisions: (raw.designDecisions || []).map(normalizeDecision).filter(Boolean),
    requirementsDiscovered: raw.requirementsDiscovered || [],
    filesModified: raw.filesModified || raw.files || [],
    blockers: raw.blockers || []
  };
}

function sessionDefaults() {
  const session = source.targetMcpSession || source.session || {};
  return {
    sourceType: session.sourceType || source.createdBy || source.source?.agent || "Codex",
    sessionId: session.sessionId || session.id,
    title: session.title || source.workspace?.name || "Recovered MCP session",
    model: session.model || "unknown",
    started: source.createdAtUtc || source.createdAt || nowIso,
    status: "completed"
  };
}

function emitSessionImport(turns, label) {
  if (!turns.length) return null;
  const defaults = sessionDefaults();
  if (!defaults.sessionId) return null;
  const sessionLog = {
    sourceType: defaults.sourceType,
    sessionId: defaults.sessionId,
    title: defaults.title,
    model: defaults.model,
    started: defaults.started,
    lastUpdated: nowIso,
    status: defaults.status,
    turns: turns.map((turn, index) => normalizeTurn(turn, index + 1, defaults))
  };
  return {
    method: "workflow.sessionlog.importRecovery",
    label,
    paramsYaml: `sessionLog: ${JSON.stringify(sessionLog)}`
  };
}

function emitTodoCreate(payload, label) {
  const lines = [];
  for (const key of Object.keys(payload)) {
    const value = payload[key];
    if (Array.isArray(value)) {
      lines.push(`${key}:${yamlList(value)}`);
    } else if (value && typeof value === "object") {
      lines.push(`${key}: ${JSON.stringify(value)}`);
    } else {
      lines.push(`${key}: ${scalar(value)}`);
    }
  }
  return {
    method: "workflow.todo.create",
    label,
    paramsYaml: lines.join("\n")
  };
}

function emitGenericOperation(operation) {
  if (!operation || typeof operation !== "object") return null;
  const method = operation.method || operation.replMethod;
  if (!method) return null;
  let paramsYaml = "";
  if (typeof operation.paramsYamlBase64 === "string" && operation.paramsYamlBase64.length > 0) {
    paramsYaml = Buffer.from(operation.paramsYamlBase64, "base64").toString("utf8");
  } else if (typeof operation.paramsYaml === "string") {
    paramsYaml = operation.paramsYaml;
  } else if (operation.params && typeof operation.params === "object") {
    const lines = [];
    for (const [key, value] of Object.entries(operation.params)) {
      if (Array.isArray(value)) {
        lines.push(`${key}:${yamlList(value)}`);
      } else if (value && typeof value === "object") {
        lines.push(`${key}: ${JSON.stringify(value)}`);
      } else {
        lines.push(`${key}: ${scalar(value)}`);
      }
    }
    paramsYaml = lines.join("\n");
  }
  return {
    method: String(method),
    label: operation.id || operation.label || String(method),
    paramsYaml
  };
}

const commands = [];

if (Array.isArray(source.turns)) {
  const command = emitSessionImport(source.turns, "turns");
  if (command) commands.push(command);
}

if (Array.isArray(source.entries)) {
  const command = emitSessionImport(source.entries, "entries");
  if (command) commands.push(command);
}

if (Array.isArray(source.operations)) {
  const defaults = sessionDefaults();
  for (const operation of source.operations) {
    const genericCommand = emitGenericOperation(operation);
    if (genericCommand) {
      commands.push(genericCommand);
    } else if (operation.kind === "sessionlog.appendActions") {
      const actions = operation.payload?.actions || [];
      const turn = {
        requestId: source.session?.requestId || operation.requestId,
        timestamp: source.createdAt || source.createdAtUtc || nowIso,
        queryTitle: `Recovered ${operation.id || "session-log actions"}`,
        queryText: source.session?.title || defaults.title,
        response: `Recovered ${actions.length} action(s) from ${operation.id || "pending operation"}.`,
        status: "completed",
        actions
      };
      const command = emitSessionImport([turn], operation.id || "sessionlog-actions");
      if (command) commands.push(command);
    } else if (operation.kind === "todo.create" && operation.payload) {
      commands.push(emitTodoCreate(operation.payload, operation.id || operation.payload.id || "todo-create"));
    }
  }
}

for (const command of commands) {
  const encoded = Buffer.from(command.paramsYaml, "utf8").toString("base64");
  console.log(`${command.method}\t${encoded}\t${command.label || ""}`);
}
