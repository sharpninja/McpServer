#!/usr/bin/env node
"use strict";

const fs = require("fs");

function parseScalar(value) {
  const text = String(value ?? "").trim();
  if (text === "") return "";
  if (text === "null" || text === "~") return null;
  if (text === "true") return true;
  if (text === "false") return false;
  if (/^-?(?:0|[1-9]\d*)(?:\.\d+)?$/.test(text)) return Number(text);
  if ((text.startsWith("\"") && text.endsWith("\"")) || (text.startsWith("'") && text.endsWith("'"))) {
    if (text.startsWith("\"")) return JSON.parse(text);
    return text.slice(1, -1).replace(/''/g, "'");
  }
  if (/^[\[{]/.test(text)) return JSON.parse(text);
  return text;
}

function lineIndent(line) {
  const match = line.match(/^ */);
  return match ? match[0].length : 0;
}

function parseYamlSubset(input) {
  const rawLines = String(input ?? "").replace(/\r/g, "").split("\n");
  let index = 0;

  function isIgnorable(line) {
    return !line.trim() || line.trim().startsWith("#");
  }

  function peek() {
    while (index < rawLines.length && isIgnorable(rawLines[index])) index += 1;
    if (index >= rawLines.length) return null;
    const raw = rawLines[index];
    return { raw, indent: lineIndent(raw), text: raw.trim() };
  }

  function parseLiteralBlock(parentIndent) {
    let blockIndent = null;
    const values = [];
    while (index < rawLines.length) {
      const raw = rawLines[index];
      if (!raw.trim()) {
        values.push("");
        index += 1;
        continue;
      }
      const indent = lineIndent(raw);
      if (indent <= parentIndent) break;
      if (blockIndent === null) blockIndent = indent;
      values.push(raw.slice(Math.min(blockIndent, indent)));
      index += 1;
    }
    return values.join("\n");
  }

  function parseKeyValue(text) {
    const match = text.match(/^([^:#][^:]*?):(?:\s+(.*)|\s*)$/);
    if (!match) return null;
    return { key: match[1].trim(), rest: match[2] ?? "" };
  }

  function parseNode(indent) {
    const current = peek();
    if (!current || current.indent < indent) return null;
    if (current.indent === indent && current.text.startsWith("- ")) return parseSequence(indent);
    return parseMapping(indent);
  }

  function parseMapping(indent) {
    const result = {};
    while (true) {
      const current = peek();
      if (!current || current.indent < indent) break;
      if (current.indent !== indent || current.text.startsWith("- ")) break;
      const pair = parseKeyValue(current.text);
      if (!pair) throw new Error(`Unsupported YAML mapping line: ${current.raw}`);
      index += 1;
      if (pair.rest === "|" || pair.rest === "|-" || pair.rest === "|+") {
        result[pair.key] = parseLiteralBlock(current.indent);
      } else if (pair.rest !== "") {
        result[pair.key] = parseScalar(pair.rest);
      } else {
        const child = peek();
        result[pair.key] = child && child.indent > current.indent ? parseNode(child.indent) : null;
      }
    }
    return result;
  }

  function parseSequence(indent) {
    const result = [];
    while (true) {
      const current = peek();
      if (!current || current.indent !== indent || !current.text.startsWith("- ")) break;
      const rest = current.text.slice(2).trim();
      index += 1;
      if (!rest) {
        const child = peek();
        result.push(child && child.indent > indent ? parseNode(child.indent) : null);
        continue;
      }
      const pair = parseKeyValue(rest);
      if (!pair) {
        result.push(parseScalar(rest));
        continue;
      }
      const item = {};
      if (pair.rest === "|" || pair.rest === "|-" || pair.rest === "|+") {
        item[pair.key] = parseLiteralBlock(current.indent);
      } else if (pair.rest !== "") {
        item[pair.key] = parseScalar(pair.rest);
      } else {
        const child = peek();
        item[pair.key] = child && child.indent > indent ? parseNode(child.indent) : null;
      }
      const child = peek();
      if (child && child.indent > indent) Object.assign(item, parseMapping(child.indent));
      result.push(item);
    }
    return result;
  }

  const first = peek();
  if (!first) return null;
  return parseNode(first.indent);
}

if (require.main === module) {
  const input = fs.readFileSync(0, "utf8");
  process.stdout.write(JSON.stringify(parseYamlSubset(input)));
}

module.exports = { parseYamlSubset };
