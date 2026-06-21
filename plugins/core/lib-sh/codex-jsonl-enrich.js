#!/usr/bin/env node
"use strict";
/**
 * codex-jsonl-enrich.js - Emit completeTurn YAML params enriched from Codex JSONL.
 *
 * Usage: node codex-jsonl-enrich.js <jsonl-path> [response-text]
 *
 * Reads the JSONL, extracts the most recent (or in-progress) turn's rich fields,
 * and prints a YAML block suitable for piping to: repl-invoke.sh workflow.sessionlog.completeTurn
 *
 * Exits non-zero on failure so final-response.sh can fall back to plain completeTurn.
 */

const fs = require("fs");
const path = require("path");

const jsonlPath = process.argv[2] || "";
const responseText = process.argv[3] || "Turn completed.";

if (!jsonlPath || !fs.existsSync(jsonlPath)) {
  process.exit(1);
}

// Reuse codex-jsonl.js parse logic by requiring it inline - but it has a main()
// call at the end, so we reproduce just the needed functions here.

function redact(text) {
  if (typeof text !== "string") return text;
  return text
    .replace(/X-Api-Key:\s*[^\s"'\r\n]{8,}/gi, "X-Api-Key: [REDACTED]")
    .replace(/"X-Api-Key":\s*"[^"]{8,}"/gi, '"X-Api-Key": "[REDACTED]"')
    .replace(/apiKey['":\s]+["']?[A-Za-z0-9_\-]{20,}["']?/gi, "apiKey: [REDACTED]")
    .replace(/Bearer\s+[A-Za-z0-9_.\-]{20,}/gi, "Bearer [REDACTED]")
    .replace(/Authorization:\s*[^\s"'\r\n]{8,}/gi, "Authorization: [REDACTED]");
}

function readJsonl(filePath) {
  const events = [];
  for (const line of fs.readFileSync(filePath, "utf8").split(/\r?\n/)) {
    const t = line.trim();
    if (!t) continue;
    try { events.push(JSON.parse(t)); } catch {}
  }
  return events;
}

function groupByTurns(events) {
  const turns = [];
  let current = null;
  for (const event of events) {
    const payload = event.payload || {};
    const subType = payload.type;
    if (event.type === "event_msg" && subType === "task_started") {
      if (current) turns.push(current);
      current = { turnId: payload.turn_id || "", startedAt: event.timestamp || "", status: "in_progress", events: [event] };
    } else if (current) {
      current.events.push(event);
      if (event.type === "event_msg" && subType === "task_complete") {
        current.status = "completed";
        current.completedAt = event.timestamp;
      } else if (event.type === "event_msg" && subType === "turn_aborted") {
        current.status = "failed";
        current.reason = payload.reason || "aborted";
      }
    }
  }
  if (current) turns.push(current);
  return turns;
}

const FILE_WRITE_TOOLS = new Set(["write_file","create_file","overwrite_file","WriteFile","edit_block","patch_apply","apply_patch"]);
const FILE_READ_TOOLS = new Set(["read_file","cat","head","tail","nl","sed","get_file_info","read","ReadFile"]);

function extractTurnFields(turn) {
  let queryText = "";
  let interpretation = "";
  const processingDialog = [];
  const actions = [];
  const filesModified = [];
  const contextList = [];
  const blockers = [];
  const designDecisions = [];
  const requirementsDiscovered = [];
  let actionOrder = 1;

  for (const event of turn.events) {
    const payload = event.payload || {};
    const subType = payload.type;
    const ts = event.timestamp || turn.startedAt;

    if (event.type === "event_msg" && subType === "user_message") {
      if (!queryText) queryText = redact(payload.message || "");
    }

    if (event.type === "event_msg" && subType === "agent_message") {
      const msg = redact(payload.message || "");
      const phase = payload.phase || "completion";
      if (!interpretation && msg) interpretation = msg.slice(0, 500);
      if (msg) {
        let cat = "observation";
        if (/decision:|chose |approach:|rationale:/i.test(msg)) cat = "decision";
        else if (phase === "context") cat = "reasoning";
        processingDialog.push({ timestamp: ts, role: "model", category: cat, content: msg });
        if (/decision:|chose |approach:|rationale:/i.test(msg)) designDecisions.push(msg.slice(0, 200));
        const reqIds = msg.match(/\b(?:FR|TR|TEST)-[A-Z]+-\d+\b/g) || [];
        for (const id of reqIds) {
          if (!requirementsDiscovered.includes(id)) requirementsDiscovered.push(id);
        }
      }
    }

    if (event.type === "event_msg" && subType === "turn_aborted") {
      blockers.push(`Turn aborted: ${payload.reason || "interrupted"}`);
    }

    if (event.type === "event_msg" && subType === "patch_apply_end") {
      const stdout = payload.stdout || "";
      for (const line of stdout.split(/\r?\n/)) {
        const m = line.match(/^[AMRDC\s]*([A-Za-z]:[\\/][^\r\n]+|\/[^\r\n\s]+\.[A-Za-z]{1,10})/);
        if (m) {
          const f = redact(m[1].trim());
          if (f && !filesModified.includes(f)) filesModified.push(f);
          actions.push({ order: actionOrder++, type: "edit", status: "completed", description: `Patch: ${path.basename(f)}`, filePath: f });
        }
      }
    }

    if (event.type === "response_item" && subType === "function_call") {
      const toolName = payload.name || "";
      let argsObj = {};
      try { argsObj = JSON.parse(payload.arguments || "{}"); } catch {}
      if (FILE_WRITE_TOOLS.has(toolName)) {
        const f = redact(argsObj.path || argsObj.file || argsObj.filename || "");
        if (f) {
          if (!filesModified.includes(f)) filesModified.push(f);
          actions.push({ order: actionOrder++, type: "edit", status: "completed", description: `${toolName}: ${path.basename(f)}`, filePath: f });
        }
      } else if (FILE_READ_TOOLS.has(toolName)) {
        const f = redact(argsObj.path || argsObj.file || argsObj.filename || "");
        if (f && !contextList.includes(f)) contextList.push(f);
      }
    }

    if (event.type === "response_item" && subType === "custom_tool_call") {
      let inputObj = payload.input || {};
      if (typeof inputObj === "string") { try { inputObj = JSON.parse(inputObj); } catch {} }
      const f = redact(inputObj.path || inputObj.file || "");
      if (f) {
        if (!filesModified.includes(f)) filesModified.push(f);
        actions.push({ order: actionOrder++, type: "edit", status: "completed", description: `${payload.name || "tool"}: ${path.basename(f)}`, filePath: f });
      }
    }
  }

  if (actions.length === 0) {
    actions.push({ order: 1, type: "session_log", status: "completed", description: "Session turn captured from Codex JSONL", filePath: "" });
  }

  return { queryText, interpretation: interpretation || "Turn captured from Codex JSONL transcript", processingDialog, actions, filesModified, contextList, blockers, designDecisions, requirementsDiscovered };
}

function yamlStr(s) {
  if (!s) return '""';
  // Use block literal if multi-line, else quote
  if (typeof s !== "string") s = String(s);
  if (s.includes("\n")) return null; // caller uses block
  return JSON.stringify(s);
}

function emitYaml(key, value, indent) {
  const pad = " ".repeat(indent);
  if (typeof value === "string") {
    if (value.includes("\n")) {
      const lines = [pad + key + ": |"];
      for (const line of value.split("\n")) lines.push(pad + "  " + line);
      return lines.join("\n");
    }
    return `${pad}${key}: ${JSON.stringify(value)}`;
  }
  if (typeof value === "number" || typeof value === "boolean") return `${pad}${key}: ${value}`;
  return `${pad}${key}: ${JSON.stringify(value)}`;
}

function emitList(key, items, indent, itemFn) {
  const pad = " ".repeat(indent);
  if (!items || items.length === 0) return `${pad}${key}: []`;
  const lines = [pad + key + ":"];
  for (const item of items) lines.push(itemFn(item, indent + 2));
  return lines.join("\n");
}

try {
  const events = readJsonl(jsonlPath);
  const turns = groupByTurns(events);
  if (!turns.length) {
    process.stderr.write("codex-jsonl-enrich.js: no turns found in JSONL\n");
    process.exit(1);
  }
  // Use last (most recent) turn
  const turn = turns[turns.length - 1];
  const fields = extractTurnFields(turn);

  const lines = [];
  lines.push(emitYaml("response", responseText, 0));
  lines.push(emitYaml("interpretation", fields.interpretation, 0));

  // contextList
  if (fields.contextList.length > 0) {
    lines.push("contextList:");
    for (const f of fields.contextList) lines.push("  - " + JSON.stringify(f));
  } else {
    lines.push("contextList: []");
  }

  // actions
  if (fields.actions.length > 0) {
    lines.push("actions:");
    for (const a of fields.actions) {
      lines.push(`  - order: ${a.order}`);
      lines.push(`    type: ${JSON.stringify(a.type)}`);
      lines.push(`    status: ${JSON.stringify(a.status)}`);
      lines.push(`    description: ${JSON.stringify(a.description)}`);
      lines.push(`    filePath: ${JSON.stringify(a.filePath || "")}`);
    }
  } else {
    lines.push("actions: []");
  }

  // filesModified
  if (fields.filesModified.length > 0) {
    lines.push("filesModified:");
    for (const f of fields.filesModified) lines.push("  - " + JSON.stringify(f));
  } else {
    lines.push("filesModified: []");
  }

  // blockers
  if (fields.blockers.length > 0) {
    lines.push("blockers:");
    for (const b of fields.blockers) lines.push("  - " + JSON.stringify(b));
  } else {
    lines.push("blockers: []");
  }

  // designDecisions
  if (fields.designDecisions.length > 0) {
    lines.push("designDecisions:");
    for (const d of fields.designDecisions) lines.push("  - " + JSON.stringify(d));
  } else {
    lines.push("designDecisions: []");
  }

  // requirementsDiscovered
  if (fields.requirementsDiscovered.length > 0) {
    lines.push("requirementsDiscovered:");
    for (const r of fields.requirementsDiscovered) lines.push("  - " + JSON.stringify(r));
  } else {
    lines.push("requirementsDiscovered: []");
  }

  // processingDialog
  if (fields.processingDialog.length > 0) {
    lines.push("processingDialog:");
    for (const d of fields.processingDialog) {
      lines.push("  - timestamp: " + JSON.stringify(d.timestamp));
      lines.push("    role: " + JSON.stringify(d.role));
      lines.push("    category: " + JSON.stringify(d.category));
      const content = d.content.length > 300 ? d.content.slice(0, 300) + "..." : d.content;
      if (content.includes("\n")) {
        lines.push("    content: |");
        for (const cl of content.split("\n")) lines.push("      " + cl);
      } else {
        lines.push("    content: " + JSON.stringify(content));
      }
    }
  } else {
    lines.push("processingDialog: []");
  }

  process.stdout.write(lines.join("\n") + "\n");
} catch (e) {
  process.stderr.write(`codex-jsonl-enrich.js: ${e.message}\n`);
  process.exit(1);
}
