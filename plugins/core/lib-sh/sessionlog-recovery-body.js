#!/usr/bin/env node
"use strict";

const fs = require("fs");

const input = fs.readFileSync(0, "utf8").trim();

function stripInlineComment(text) {
  let inSingle = false;
  let inDouble = false;
  let escaped = false;

  for (let i = 0; i < text.length; i += 1) {
    const ch = text[i];

    if (inDouble) {
      if (escaped) {
        escaped = false;
      } else if (ch === "\\") {
        escaped = true;
      } else if (ch === "\"") {
        inDouble = false;
      }
      continue;
    }

    if (inSingle) {
      if (ch === "'" && text[i + 1] === "'") {
        i += 1;
      } else if (ch === "'") {
        inSingle = false;
      }
      continue;
    }

    if (ch === "\"") {
      inDouble = true;
      continue;
    }

    if (ch === "'") {
      inSingle = true;
      continue;
    }

    if (ch === "#" && (i === 0 || /\s/.test(text[i - 1]))) {
      return text.slice(0, i).trimEnd();
    }
  }

  return text;
}

function indentOf(line) {
  const match = line.match(/^ */);
  return match ? match[0].length : 0;
}

function isBlankOrComment(line) {
  return /^\s*(#.*)?$/.test(line);
}

function findPairSeparator(text) {
  let inSingle = false;
  let inDouble = false;
  let escaped = false;
  let squareDepth = 0;
  let braceDepth = 0;

  for (let i = 0; i < text.length; i += 1) {
    const ch = text[i];

    if (inDouble) {
      if (escaped) {
        escaped = false;
      } else if (ch === "\\") {
        escaped = true;
      } else if (ch === "\"") {
        inDouble = false;
      }
      continue;
    }

    if (inSingle) {
      if (ch === "'" && text[i + 1] === "'") {
        i += 1;
      } else if (ch === "'") {
        inSingle = false;
      }
      continue;
    }

    if (ch === "\"") {
      inDouble = true;
      continue;
    }

    if (ch === "'") {
      inSingle = true;
      continue;
    }

    if (ch === "[") {
      squareDepth += 1;
      continue;
    }

    if (ch === "]") {
      squareDepth = Math.max(0, squareDepth - 1);
      continue;
    }

    if (ch === "{") {
      braceDepth += 1;
      continue;
    }

    if (ch === "}") {
      braceDepth = Math.max(0, braceDepth - 1);
      continue;
    }

    if (ch === ":" && squareDepth === 0 && braceDepth === 0 && (i === text.length - 1 || /\s/.test(text[i + 1]))) {
      return i;
    }
  }

  return -1;
}

function splitPair(text) {
  const index = findPairSeparator(text);
  if (index < 1) {
    return null;
  }

  const key = stripInlineComment(text.slice(0, index)).trim();
  if (!key) {
    return null;
  }

  return {
    key: parseKey(key),
    rest: text.slice(index + 1)
  };
}

function parseKey(text) {
  if (text.startsWith("\"") && text.endsWith("\"")) {
    try {
      return JSON.parse(text);
    } catch {
      return text.slice(1, -1);
    }
  }

  if (text.startsWith("'") && text.endsWith("'")) {
    return text.slice(1, -1).replace(/''/g, "'");
  }

  return text;
}

function parseScalar(text) {
  const value = stripInlineComment(text).trim();
  if (value === "") {
    return "";
  }

  if ((value.startsWith("{") && value.endsWith("}")) || (value.startsWith("[") && value.endsWith("]"))) {
    return JSON.parse(value);
  }

  if (value.startsWith("\"") && value.endsWith("\"")) {
    return JSON.parse(value);
  }

  if (value.startsWith("'") && value.endsWith("'")) {
    return value.slice(1, -1).replace(/''/g, "'");
  }

  if (/^(true|false)$/i.test(value)) {
    return value.toLowerCase() === "true";
  }

  if (/^(null|~)$/i.test(value)) {
    return null;
  }

  if (/^-?(?:0|[1-9]\d*)(?:\.\d+)?$/.test(value)) {
    return Number(value);
  }

  return value;
}

class YamlSubsetParser {
  constructor(rawInput) {
    this.lines = rawInput
      .replace(/\r\n?/g, "\n")
      .split("\n")
      .filter((line) => !/^\s*(?:---|\.\.\.)\s*$/.test(line));
    this.index = 0;
  }

  skipBlank() {
    while (this.index < this.lines.length && isBlankOrComment(this.lines[this.index])) {
      this.index += 1;
    }
  }

  nextContentIndex(startIndex = this.index) {
    let index = startIndex;
    while (index < this.lines.length && isBlankOrComment(this.lines[index])) {
      index += 1;
    }
    return index;
  }

  parseDocument() {
    this.skipBlank();
    if (this.index >= this.lines.length) {
      return {};
    }

    return this.parseNode(indentOf(this.lines[this.index]));
  }

  parseNode(indent) {
    this.skipBlank();
    if (this.index >= this.lines.length) {
      return null;
    }

    const line = this.lines[this.index];
    const currentIndent = indentOf(line);
    if (currentIndent < indent) {
      return null;
    }

    const trimmed = line.slice(currentIndent);
    if (currentIndent === indent && trimmed.startsWith("- ")) {
      return this.parseSequence(indent);
    }

    return this.parseMap(indent);
  }

  parseMap(indent) {
    const result = {};

    while (this.index < this.lines.length) {
      if (isBlankOrComment(this.lines[this.index])) {
        this.index += 1;
        continue;
      }

      const line = this.lines[this.index];
      const currentIndent = indentOf(line);
      if (currentIndent < indent) {
        break;
      }
      if (currentIndent > indent) {
        break;
      }

      const trimmed = line.slice(currentIndent);
      if (trimmed.startsWith("- ")) {
        break;
      }

      const pair = splitPair(trimmed);
      if (!pair) {
        throw new Error(`invalid YAML mapping line: ${trimmed}`);
      }

      this.index += 1;
      result[pair.key] = this.parseValueOrChild(pair.rest, currentIndent);
    }

    return result;
  }

  parseSequence(indent) {
    const result = [];

    while (this.index < this.lines.length) {
      if (isBlankOrComment(this.lines[this.index])) {
        this.index += 1;
        continue;
      }

      const line = this.lines[this.index];
      const currentIndent = indentOf(line);
      if (currentIndent < indent) {
        break;
      }
      if (currentIndent !== indent || !line.slice(currentIndent).startsWith("- ")) {
        break;
      }

      const rest = line.slice(currentIndent + 2);
      this.index += 1;
      const cleanRest = stripInlineComment(rest).trim();

      if (cleanRest === "") {
        const childIndex = this.nextContentIndex();
        if (childIndex >= this.lines.length || indentOf(this.lines[childIndex]) <= currentIndent) {
          result.push(null);
        } else {
          result.push(this.parseNode(indentOf(this.lines[childIndex])));
        }
        continue;
      }

      const blockMarker = cleanRest.match(/^[|>][-+]?$/);
      if (blockMarker) {
        result.push(this.readBlockScalar(currentIndent, cleanRest));
        continue;
      }

      const pair = splitPair(cleanRest);
      if (pair) {
        const item = {};
        item[pair.key] = this.parseValueOrChild(pair.rest, currentIndent);

        while (true) {
          const childIndex = this.nextContentIndex();
          if (childIndex >= this.lines.length) {
            break;
          }

          const childIndent = indentOf(this.lines[childIndex]);
          if (childIndent <= currentIndent) {
            break;
          }

          const child = this.parseMap(childIndent);
          Object.assign(item, child);
        }

        result.push(item);
        continue;
      }

      result.push(parseScalar(cleanRest));
    }

    return result;
  }

  parseValueOrChild(rest, parentIndent) {
    const cleanRest = stripInlineComment(rest).trim();
    if (cleanRest === "") {
      const childIndex = this.nextContentIndex();
      if (childIndex >= this.lines.length || indentOf(this.lines[childIndex]) <= parentIndent) {
        return null;
      }

      return this.parseNode(indentOf(this.lines[childIndex]));
    }

    if (/^[|>][-+]?$/.test(cleanRest)) {
      return this.readBlockScalar(parentIndent, cleanRest);
    }

    return parseScalar(cleanRest);
  }

  readBlockScalar(parentIndent, marker) {
    const raw = [];

    while (this.index < this.lines.length) {
      const line = this.lines[this.index];
      if (!isBlankOrComment(line) && indentOf(line) <= parentIndent) {
        break;
      }

      raw.push(line);
      this.index += 1;
    }

    const nonBlankIndents = raw
      .filter((line) => line.trim() !== "")
      .map((line) => indentOf(line));
    const blockIndent = nonBlankIndents.length > 0 ? Math.min(...nonBlankIndents) : parentIndent + 2;
    const content = raw.map((line) => {
      if (line.trim() === "") {
        return "";
      }
      return line.length >= blockIndent ? line.slice(blockIndent) : line.trimStart();
    });

    const chomp = marker.endsWith("-") ? "strip" : "clip";
    const isFolded = marker.startsWith(">");
    let result;

    if (!isFolded) {
      result = content.join("\n");
    } else {
      const pieces = [];
      let pendingBlank = false;

      for (const line of content) {
        if (line === "") {
          pendingBlank = true;
          continue;
        }

        if (pieces.length > 0) {
          pieces.push(pendingBlank ? "\n" : " ");
        }
        pieces.push(line);
        pendingBlank = false;
      }

      result = pieces.join("");
    }

    if (chomp !== "strip") {
      result += "\n";
    }

    return result;
  }
}

function parseDocument(rawInput) {
  const trimmed = rawInput.trim();
  if (trimmed === "") {
    return {};
  }

  if (trimmed.startsWith("{") || trimmed.startsWith("[")) {
    return JSON.parse(trimmed);
  }

  return new YamlSubsetParser(trimmed).parseDocument();
}

function selectSessionLog(parsed) {
  if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
    return null;
  }

  if (parsed.sessionLog && typeof parsed.sessionLog === "object" && !Array.isArray(parsed.sessionLog)) {
    return parsed.sessionLog;
  }

  if (parsed.params && parsed.params.sessionLog && typeof parsed.params.sessionLog === "object" && !Array.isArray(parsed.params.sessionLog)) {
    return parsed.params.sessionLog;
  }

  if (typeof parsed.paramsYaml === "string") {
    return selectSessionLog(parseDocument(parsed.paramsYaml));
  }

  if (parsed.session && typeof parsed.session === "object" && !Array.isArray(parsed.session)) {
    const session = parsed.session;
    if (session.sourceType || session.sessionId || session.turns) {
      return session;
    }
  }

  return parsed;
}

try {
  const parsed = parseDocument(input);
  const sessionLog = selectSessionLog(parsed);
  if (!sessionLog || typeof sessionLog !== "object" || Array.isArray(sessionLog)) {
    throw new Error("sessionLog must be a mapping/object");
  }

  process.stdout.write(JSON.stringify(sessionLog));
} catch (error) {
  console.error(`failed to parse session log recovery payload: ${error.message}`);
  process.exit(2);
}
