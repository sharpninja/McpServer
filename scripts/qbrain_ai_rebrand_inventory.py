#!/usr/bin/env python3
"""TR-MCP-QBRAIN-007: Classify QBrain.AI rebrand hits before any replace.

Writes a TSV receipt. This script does not edit product source.
Existing files under docs/receipts/ are read and classified, not rewritten.
"""

from __future__ import annotations

import argparse
import os
import re
import sys
from collections import Counter
from pathlib import Path

PLAN_RELATIVE = "docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md"
INVENTORY_RELATIVE = "docs/receipts/qbrain-ai-rebrand-phase1-inventory.tsv"

SKIP_DIR_NAMES = {
    ".git",
    ".worktrees",
    "bin",
    "obj",
    "node_modules",
    "artifacts",
    "TestResults",
    ".nuke",
}

BINARY_SUFFIXES = {
    ".png",
    ".jpg",
    ".jpeg",
    ".gif",
    ".webp",
    ".ico",
    ".dll",
    ".exe",
    ".pdb",
    ".zip",
    ".nupkg",
    ".snupkg",
    ".wasm",
    ".onnx",
    ".db",
    ".sqlite",
    ".woff",
    ".woff2",
    ".ttf",
    ".eot",
    ".pdf",
    ".snk",
    ".pfx",
    ".p7b",
    ".svg",
}

# Longer patterns are recorded first. Overlapping later matches on the same span are skipped
# only when a longer pattern already claimed that start index. Every distinct pattern still
# produces its own row when its match is not a strict substring of an earlier match on the
# same start... The plan asks for one pattern at a time, including overlaps, so overlaps stay.
PATTERNS: list[tuple[str, re.Pattern[str]]] = [
    ("SharpNinja.McpServer", re.compile(r"SharpNinja\.McpServer")),
    ("SharpNinja/McpServer", re.compile(r"SharpNinja/McpServer")),
    ("sharpninja/McpServer", re.compile(r"sharpninja/McpServer")),
    ("sharpninja/mcpserver", re.compile(r"sharpninja/mcpserver")),
    ("@sharpninja/mcpserver", re.compile(r"@sharpninja/mcpserver")),
    ("@sharpninja/mcp-repl", re.compile(r"@sharpninja/mcp-repl")),
    ("MCP Server", re.compile(r"MCP Server")),
    ("MCP_SERVER", re.compile(r"MCP_SERVER")),
    ("MCP_UNTRUSTED", re.compile(r"MCP_UNTRUSTED")),
    ("MCP_", re.compile(r"MCP_")),
    ("mcp-server", re.compile(r"mcp-server")),
    ("mcpserver", re.compile(r"mcpserver")),
    ("McpServer", re.compile(r"McpServer")),
    (".mcpServer", re.compile(r"\.mcpServer")),
    ("mcp.db", re.compile(r"mcp\.db")),
    ("McpDbContext", re.compile(r"McpDbContext")),
]

CLOSED_ACTIONS = {
    "leave",
    "leave-plan-text",
    "leave-tooling",
    "rename-display",
    "rename-root",
    "rename-package",
    "rename-npm",
    "rename-docker",
    "rename-tool-command",
    "alias-route",
    "alias-env",
    "defer-phase-3",
    "defer-phase-4",
    "defer-phase-4l",
}

OPEN_ACTIONS = {"", "open", "unclassified", "review"}


def classify(relative_path: str, line: str, pattern: str, start: int, match_text: str) -> tuple[str, str]:
    """Return (class, action) for one hit. Actions in CLOSED_ACTIONS are not open."""
    path = relative_path.replace("\\", "/")
    end = start + len(match_text)
    window = line[max(0, start - 40) : end + 40]

    if path.startswith("docs/receipts/") or "/docs/receipts/" in f"/{path}":
        return "historical-receipt", "leave"
    if "docs/Project/wiki/" in path:
        return "generated-wiki", "leave"
    if path.startswith("lib/NSubstitute/"):
        return "vendored-library", "leave"
    if path == PLAN_RELATIVE:
        return "product-brand", "leave-plan-text"
    if path.startswith("scripts/qbrain_ai_rebrand_"):
        return "product-brand", "leave-tooling"
    if path == INVENTORY_RELATIVE:
        return "historical-receipt", "leave"

    if pattern in {".mcpServer", "mcp.db", "McpDbContext"}:
        return "persisted-state", "leave"

    if pattern == "MCP_UNTRUSTED" or match_text == "MCP_UNTRUSTED":
        return "protocol", "leave"

    if pattern == "MCP_" and line.startswith("MCP_UNTRUSTED", start):
        return "protocol", "leave"

    if _is_requirement_id_context(line, start, match_text):
        return "requirement-id", "leave"

    if pattern == "MCP Server":
        return "product-brand", "rename-display"

    if pattern == "mcp-server":
        if ".mcp-server.yaml" in window or ".mcp-server.json" in window:
            return "persisted-state", "leave"
        if line.startswith("mcp-server-api", start) or "mcp-server-api" in window:
            return "ambiguous", "defer-phase-4"
        return "product-brand", "rename-docker"

    if pattern in {"@sharpninja/mcpserver", "@sharpninja/mcp-repl"}:
        return "product-brand", "rename-npm"

    if pattern == "SharpNinja.McpServer":
        return "product-brand", "rename-package"

    if pattern in {"SharpNinja/McpServer", "sharpninja/McpServer", "sharpninja/mcpserver"}:
        return "product-brand", "defer-phase-3"

    if pattern == "McpServer":
        if line.startswith("McpServerTools", start) or line.startswith("McpServerManager", start):
            return "product-brand", "defer-phase-3"
        if line.startswith("McpServer_Omarchy", start):
            return "host-or-lab", "defer-phase-4l"
        if _is_repo_path_segment(line, start, end):
            return "product-brand", "defer-phase-3"
        return "product-brand", "rename-root"

    if pattern == "mcpserver":
        if path.endswith("docs/setup/install-local-service.ps1"):
            return "host-or-lab", "defer-phase-4l"
        if line.startswith("mcpserver_omarchy", start):
            return "host-or-lab", "defer-phase-4l"
        if line.startswith("mcpserver.service", start) or line.startswith("mcpserver.env", start):
            return "host-or-lab", "defer-phase-4l"
        if any(_contains_at(line, start, prefix) for prefix in ("/opt/mcpserver", "/etc/mcpserver", "/var/lib/mcpserver", "/var/log/mcpserver")):
            return "host-or-lab", "defer-phase-4l"
        if "mcpserver-" in line[start : start + 24] and _looks_like_plugin_repo(line, start):
            return "product-brand", "defer-phase-3"
        if _is_repo_path_segment(line, start, end):
            return "product-brand", "defer-phase-3"
        if line.startswith("mcpserver-repl", start):
            return "product-brand", "rename-tool-command"
        if line.startswith("mcpserver/", start) or line.startswith("mcpserver\\", start):
            return "product-brand", "alias-route"
        return "product-brand", "alias-route"

    if pattern in {"MCP_", "MCP_SERVER"}:
        return "product-brand", "alias-env"

    return "ambiguous", "defer-phase-4"


def _is_requirement_id_context(line: str, start: int, match_text: str) -> bool:
    prefix = line[max(0, start - 12) : start]
    return bool(re.search(r"(FR|TR|TEST)-MCP-$", prefix)) and match_text.startswith("MCP")


def _is_repo_path_segment(line: str, start: int, end: int) -> bool:
    if start == 0 or line[start - 1] not in "/\\":
        return False
    after = line[end : end + 4]
    if after.startswith(".git"):
        return True
    if after.startswith("."):
        # Project folders are McpServer.<Segment>. A bare repo segment is McpServer or McpServer.git.
        rest = line[end + 1 : end + 2]
        return rest == "" or not rest.isalpha() or rest.islower() and after.startswith(".git")
    return True


def _looks_like_plugin_repo(line: str, start: int) -> bool:
    snippet = line[start : start + 48]
    return "plugin" in snippet or snippet.startswith("mcpserver-grok") or snippet.startswith("mcpserver-claude") or snippet.startswith("mcpserver-cline") or snippet.startswith("mcpserver-codex") or snippet.startswith("mcpserver-copilot") or snippet.startswith("mcpserver-opencode")


def _contains_at(line: str, start: int, needle: str) -> bool:
    begin = max(0, start - len(needle))
    return needle in line[begin : start + len(needle)]


def iter_files(root: Path) -> list[Path]:
    files: list[Path] = []
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [name for name in dirnames if name not in SKIP_DIR_NAMES]
        for name in filenames:
            path = Path(dirpath) / name
            if path.suffix.lower() in BINARY_SUFFIXES:
                continue
            files.append(path)
    return files


def _covered_by_longer_pattern(line: str, pattern_name: str, start: int) -> bool:
    """Skip a hit whose span is already the suffix or prefix of a more specific pattern."""
    if pattern_name == "McpServer" and start >= len("SharpNinja.") and line.startswith("SharpNinja.", start - len("SharpNinja.")):
        return True
    if pattern_name == "McpServer" and start >= len("sharpninja/") and line.startswith("sharpninja/", start - len("sharpninja/")):
        return True
    if pattern_name == "mcpserver" and start >= 1 and line[start - 1] == "@":
        return True
    if pattern_name == "MCP_" and line.startswith("MCP_UNTRUSTED", start):
        return True
    if pattern_name == "MCP_" and line.startswith("MCP_SERVER", start):
        return True
    if pattern_name == "mcp-server" and line.startswith("mcp-server-api", start):
        return False
    return False


def scan(root: Path) -> list[tuple[str, int, str, str, str]]:
    rows: list[tuple[str, int, str, str, str]] = []
    for path in iter_files(root):
        relative = path.relative_to(root).as_posix()
        if relative == INVENTORY_RELATIVE:
            continue
        if relative.startswith("docs/receipts/") or relative.startswith("docs/Project/wiki/"):
            # Path rule wins for the whole file. Line-level rows in these trees are not replacement input.
            kind = "historical-receipt" if relative.startswith("docs/receipts/") else "generated-wiki"
            rows.append((relative, 0, "*", kind, "leave"))
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except (UnicodeDecodeError, OSError):
            continue
        for line_number, line in enumerate(text.splitlines(), start=1):
            if not any(token in line for token in ("Mcp", "MCP", "mcp")):
                continue
            for pattern_name, pattern in PATTERNS:
                for match in pattern.finditer(line):
                    if _covered_by_longer_pattern(line, pattern_name, match.start()):
                        continue
                    kind, action = classify(relative, line, pattern_name, match.start(), match.group(0))
                    rows.append((relative, line_number, pattern_name, kind, action))
    return rows


def write_tsv(path: Path, rows: list[tuple[str, int, str, str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    lines = ["path\tline\tpattern\tclass\taction"]
    for relative, line_number, pattern_name, kind, action in rows:
        lines.append(f"{relative}\t{line_number}\t{pattern_name}\t{kind}\t{action}")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def validate_rows(rows: list[tuple[str, int, str, str, str]]) -> list[str]:
    errors: list[str] = []
    for relative, line_number, pattern_name, kind, action in rows:
        if not kind or not action:
            errors.append(f"{relative}:{line_number} missing class or action for {pattern_name}")
        elif action in OPEN_ACTIONS:
            errors.append(f"{relative}:{line_number} open action {action}")
        elif action not in CLOSED_ACTIONS:
            errors.append(f"{relative}:{line_number} unknown action {action}")
    return errors


def self_test() -> None:
    samples = [
        ("README.md", "MCP Server", "MCP Server", 0, "product-brand", "rename-display"),
        ("docs/MCP-SERVER.md", "Model Context Protocol", "MCP_", 0, "product-brand", "alias-env")
        if False
        else ("src/A.cs", "namespace McpServer.Client", "McpServer", 10, "product-brand", "rename-root"),
        ("src/A.cs", "McpServer.QBAgent", "McpServer", 0, "product-brand", "rename-root"),
        ("docs/a.md", "sharpninja/McpServerTools", "McpServer", 11, "product-brand", "defer-phase-3"),
        ("docs/a.md", "https://github.com/sharpninja/McpServer", "McpServer", 31, "product-brand", "defer-phase-3"),
        ("docs/setup/install-local-service.ps1", "McpServer_Omarchy", "McpServer", 0, "host-or-lab", "defer-phase-4l"),
        ("src/A.cs", "class McpDbContext", "McpDbContext", 6, "persisted-state", "leave"),
        ("src/A.cs", "[Route(\"mcpserver/todo\")]", "mcpserver", 8, "product-brand", "alias-route"),
        ("skills/a.md", "log MCP_UNTRUSTED and stop", "MCP_UNTRUSTED", 4, "protocol", "leave"),
        ("docs/receipts/old.md", "McpServer", "McpServer", 0, "historical-receipt", "leave"),
        (PLAN_RELATIVE, "McpServer", "McpServer", 0, "product-brand", "leave-plan-text"),
        ("scripts/Setup-McpKeycloak.ps1", "mcp-server-api", "mcp-server", 0, "ambiguous", "defer-phase-4"),
    ]
    for path, line, pattern, start, expected_kind, expected_action in samples:
        match = line[start : start + len(pattern if pattern != "McpServer" else "McpServer")]
        # Use the actual matched token length from the line via the pattern name when it is the token.
        token = pattern if pattern in line else line[start:]
        if pattern == "McpServer":
            token = "McpServer"
            start = line.index(token)
        elif pattern == "mcpserver":
            token = "mcpserver"
            start = line.index(token)
        elif pattern == "MCP_UNTRUSTED":
            token = "MCP_UNTRUSTED"
            start = line.index(token)
        elif pattern == "McpDbContext":
            token = "McpDbContext"
            start = line.index(token)
        elif pattern == "mcp-server":
            token = "mcp-server"
            start = line.index(token)
        elif pattern == "MCP Server":
            token = "MCP Server"
            start = line.index(token)
        kind, action = classify(path, line, pattern, start, token)
        if (kind, action) != (expected_kind, expected_action):
            raise SystemExit(f"self-test failed for {line!r}: got {(kind, action)} expected {(expected_kind, expected_action)}")
    print("self-test ok")


def main() -> int:
    parser = argparse.ArgumentParser(description="Classify QBrain.AI rebrand hits.")
    parser.add_argument("--root", default=".")
    parser.add_argument("--output", default=INVENTORY_RELATIVE)
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--check", action="store_true", help="Validate an existing TSV.")
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return 0

    root = Path(args.root).resolve()
    output = Path(args.output)
    if not output.is_absolute():
        output = root / output

    if args.check and output.exists():
        rows = []
        for raw in output.read_text(encoding="utf-8").splitlines()[1:]:
            parts = raw.split("\t")
            if len(parts) != 5:
                print(f"bad row: {raw}", file=sys.stderr)
                return 1
            rows.append((parts[0], int(parts[1]), parts[2], parts[3], parts[4]))
    else:
        rows = scan(root)
        write_tsv(output, rows)

    errors = validate_rows(rows)
    counts = Counter((kind, action) for _, _, _, kind, action in rows)
    print(f"rows {len(rows)}")
    for key in sorted(counts):
        print(f"{counts[key]:7d}  {key[0]}  {key[1]}")
    if errors:
        print(f"errors {len(errors)}", file=sys.stderr)
        for error in errors[:20]:
            print(error, file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
