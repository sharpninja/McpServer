#!/usr/bin/env node
"use strict";
/**
 * complete-turn-to-recovery.js - Convert flat completeTurn YAML params into
 * a sessionLog importRecovery YAML block when rich fields are present.
 *
 * Usage: node complete-turn-to-recovery.js <sessionId> <sourceType> <model>
 *          <started> <reqId> <title> <openedAt>
 * Reads flat completeTurn YAML from stdin.
 *
 * Exits 0 + writes sessionLog YAML to stdout when rich fields detected.
 * Exits 1 (no output) when params are plain (no interpretation field).
 */

const fs = require("fs");

const raw = fs.readFileSync(0, "utf8");

// Session context from argv (preferred) or env vars (fallback for compat)
const sessionId  = process.argv[2] || process.env.CT2R_SESSION_ID  || "";
const sourceType = process.argv[3] || process.env.CT2R_SOURCE_TYPE || "Codex";
const model      = process.argv[4] || process.env.CT2R_MODEL       || "codex";
const started    = process.argv[5] || process.env.CT2R_STARTED     || new Date().toISOString();
const reqId      = process.argv[6] || process.env.CT2R_REQ_ID      || "";
const title      = process.argv[7] || process.env.CT2R_TITLE       || "Codex turn";
const openedAt   = process.argv[8] || process.env.CT2R_OPENED_AT   || new Date().toISOString();

// Minimal YAML parser: handles flat scalars and sequences we emit.
function parseFlat(text) {
  const obj = {};
  const lines = text.split(/\r?\n/);
  let i = 0;

  function peek() { return i < lines.length ? lines[i] : null; }
  function advance() { return lines[i++]; }

  while (i < lines.length) {
    const line = lines[i];
    if (!line || /^\s*#/.test(line)) { advance(); continue; }
    const m = line.match(/^([A-Za-z_][A-Za-z0-9_]*):\s*(.*)/);
    if (!m) { advance(); continue; }
    const key = m[1];
    const rest = m[2].trim();
    advance();

    // Block literal |
    if (rest === "|" || rest === "|+" || rest === "|-") {
      const baseIndent = line.match(/^(\s*)/)[1].length;
      const blockLines = [];
      while (i < lines.length) {
        const bl = lines[i];
        if (!bl.trim()) { blockLines.push(""); i++; continue; }
        const blIndent = bl.match(/^(\s*)/)[1].length;
        if (blIndent <= baseIndent && bl.trim()) break;
        blockLines.push(bl.slice(blIndent)); i++;
      }
      obj[key] = blockLines.join("\n");
      continue;
    }

    // Inline sequence []
    if (rest.startsWith("[")) {
      try { obj[key] = JSON.parse(rest); } catch { obj[key] = []; }
      continue;
    }

    // Empty (null value, check next lines for sequence items)
    if (rest === "" || rest === "~" || rest === "null") {
      const items = [];
      while (i < lines.length) {
        const nl = lines[i];
        if (!nl || /^\s*#/.test(nl)) { i++; continue; }
        const sm = nl.match(/^(\s+)-\s+(.*)/);
        if (!sm) break;
        i++;
        const val = sm[2].trim();
        // Sub-object (sequence of mappings)
        if (!val || val === "") {
          const obj2 = {};
          const subIndent = sm[1].length + 2;
          while (i < lines.length) {
            const sl = lines[i];
            if (!sl.trim()) { i++; continue; }
            const sIndent = sl.match(/^(\s*)/)[1].length;
            if (sIndent < subIndent) break;
            const sm2 = sl.match(/^\s+([A-Za-z_][A-Za-z0-9_]*):\s*(.*)/);
            if (!sm2) { i++; continue; }
            i++;
            const sv = sm2[2].trim();
            obj2[sm2[1]] = sv.startsWith('"') ? JSON.parse(sv) : (sv === "" ? null : parseScalar(sv));
          }
          items.push(obj2);
        } else {
          items.push(val.startsWith('"') ? JSON.parse(val) : val);
        }
      }
      obj[key] = items.length > 0 ? items : null;
      continue;
    }

    // Quoted scalar
    if (rest.startsWith('"')) {
      try { obj[key] = JSON.parse(rest); } catch { obj[key] = rest; }
      continue;
    }

    obj[key] = rest;
  }
  return obj;
}

function parseScalar(v) {
  if (v === "true") return true;
  if (v === "false") return false;
  if (v === "null" || v === "~") return null;
  if (/^-?\d+(\.\d+)?$/.test(v)) return Number(v);
  return v;
}

// Parse flat params
const flat = parseFlat(raw);

// Only proceed if interpretation present (indicates rich params)
if (!flat.interpretation || String(flat.interpretation).trim() === "") {
  process.exit(1);
}

if (!sessionId || !reqId) {
  process.exit(1);
}

function toArray(v) {
  if (!v) return [];
  if (Array.isArray(v)) return v;
  return [];
}

const turn = {
  requestId: reqId,
  timestamp: openedAt,
  queryTitle: title,
  queryText: String(flat.queryText || title),
  response: String(flat.response || flat.interpretation || ""),
  interpretation: String(flat.interpretation),
  status: "completed",
  actions: toArray(flat.actions),
  filesModified: toArray(flat.filesModified),
  contextList: toArray(flat.contextList),
  blockers: toArray(flat.blockers),
  designDecisions: toArray(flat.designDecisions),
  requirementsDiscovered: toArray(flat.requirementsDiscovered),
  processingDialog: toArray(flat.processingDialog),
  tags: (process.env.CT2R_TAGS || process.env.PLUGIN_TAG || "codex")
    .split(",")
    .map((tag) => tag.trim())
    .filter(Boolean),
  model,
  tokenCount: 0,
};

const sessionLog = {
  sourceType,
  sessionId,
  title,
  model,
  started,
  lastUpdated: openedAt,
  status: "in_progress",
  turns: [turn],
};

// Emit YAML with embedded JSON for the sessionLog value
process.stdout.write("sessionLog: " + JSON.stringify(sessionLog) + "\n");
