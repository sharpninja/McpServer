#!/usr/bin/env python3
"""Apply the Phase 1 QBrain.AI rename using the inventory disposition rules.

Does not edit docs/receipts, docs/Project/wiki, the plan file, or LAB-OMARCHY installer text.
"""

from __future__ import annotations

import os
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

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

SKIP_PREFIXES = (
    "docs/receipts/",
    "docs/Project/wiki/",
    "lib/NSubstitute/",
)

SKIP_FILES = {
    "docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md",
    "docs/setup/install-local-service.ps1",
    "scripts/qbrain_ai_rebrand_inventory.py",
    "scripts/qbrain_ai_rebrand_apply.py",
    "docs/receipts/qbrain-ai-rebrand-phase1-inventory.tsv",
}

BINARY_SUFFIXES = {
    ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".dll", ".exe", ".pdb",
    ".zip", ".nupkg", ".snupkg", ".wasm", ".onnx", ".db", ".sqlite", ".woff",
    ".woff2", ".ttf", ".eot", ".pdf", ".snk", ".pfx", ".p7b",
}

PACKAGE_MAP = [
    ("SharpNinja.McpServer.Cqrs.Mvvm", "QBrainAI.Cqrs.Mvvm"),
    ("SharpNinja.McpServer.Repl.Core", "QBrainAI.Repl.Core"),
    ("SharpNinja.McpServer.McpAgent", "QBrainAI.McpAgent"),
    ("SharpNinja.McpServer.QBAgent", "QBrainAI.QBAgent"),
    ("SharpNinja.McpServer.Client", "QBrainAI.Client"),
    ("SharpNinja.McpServer.Cqrs", "QBrainAI.Cqrs"),
    ("SharpNinja.McpServer.Repl", "QBrainAI.Repl"),
]

# Model Context Protocol SDK names are concatenated (McpServerTool, AddMcpServer).
# They are not the product root plus a dot segment (QBrainAi.Support).
SDK_TAIL = (
    "ServiceCollectionExtensions|PrimitiveCollection|HostedService|BuilderExtensions|OptionsSetup|"
    "ToolTypeAttribute|ToolCreateOptions|ToolAttribute|ToolType|Tool|"
    "PromptTypeAttribute|PromptCreateOptions|PromptAttribute|Prompt|"
    "ResourceTypeAttribute|ResourceCreateOptions|ResourceCollection|ResourceAttribute|Resource|"
    "Builder|Options|Filters|Handlers"
)
ROOT_TOKEN = re.compile(rf"McpServer(?!Tools|Manager|_Omarchy|{SDK_TAIL})")
HYPHEN = re.compile(r"mcp-server")
ROUTE_PREFIX = re.compile(r"mcpserver/")


def should_skip(relative: str) -> bool:
    if relative in SKIP_FILES:
        return True
    return relative.startswith(SKIP_PREFIXES)


def duplicate_routes(text: str) -> str:
    lines = text.splitlines(keepends=True)
    output: list[str] = []
    for line in lines:
        newline = ""
        body = line
        if line.endswith("\r\n"):
            newline = "\r\n"
            body = line[:-2]
        elif line.endswith("\n"):
            newline = "\n"
            body = line[:-1]
        stripped = body.lstrip()
        if stripped.startswith('[Route("mcpserver/'):
            indent = body[: len(body) - len(stripped)]
            canonical = stripped.replace('[Route("mcpserver/', '[Route("qbrainai/', 1)
            output.append(indent + canonical + newline)
        output.append(line)
    return "".join(output)


def replace_route_prefix(text: str) -> str:
    def repl(match: re.Match[str]) -> str:
        start = match.start()
        line_start = text.rfind("\n", 0, start) + 1
        line_end = text.find("\n", start)
        if line_end < 0:
            line_end = len(text)
        line = text[line_start:line_end]
        if '[Route("mcpserver/' in line:
            return match.group(0)
        if start > 0 and text[start - 1] == ".":
            return match.group(0)
        previous = text[max(0, start - 16) : start]
        if previous.endswith(("opt/", "etc/", "tmp/", "lib/", "log/")):
            return match.group(0)
        return "qbrainai/"

    return ROUTE_PREFIX.sub(repl, text)


def replace_hyphen(text: str) -> str:
    def repl(match: re.Match[str]) -> str:
        start = match.start()
        if text.startswith("mcp-server-api", start):
            return match.group(0)
        if start > 0 and text[start - 1] == ".":
            return match.group(0)
        return "qbrain-ai"

    return HYPHEN.sub(repl, text)


def replace_root_token(text: str) -> str:
    def repl(match: re.Match[str]) -> str:
        start = match.start()
        end = match.end()
        if start > 0 and text[start - 1] == "/":
            if text.startswith(".git", end):
                return match.group(0)
            if not text.startswith(".", end):
                return match.group(0)
        return "QBrainAi"

    return ROOT_TOKEN.sub(repl, text)


def replace_yaml_root(text: str) -> str:
    lines = text.splitlines(keepends=True)
    output: list[str] = []
    for line in lines:
        if line.startswith("Mcp:"):
            line = "QBrainAi:" + line[len("Mcp:") :]
        output.append(line)
    return "".join(output)


def transform(relative: str, text: str) -> str:
    for old, new in PACKAGE_MAP:
        text = text.replace(old, new)
    text = duplicate_routes(text)
    text = replace_route_prefix(text)
    text = text.replace("mcpserver-repl", "qbrain-ai-repl")
    text = text.replace("@sharpninja/mcpserver-agent-core", "@qbrainai/qbrain-ai-agent-core")
    text = text.replace("@sharpninja/mcpserver-plugin-core", "@qbrainai/qbrain-ai-plugin-core")
    text = text.replace("@sharpninja/mcp-repl", "@qbrainai/qbrain-ai-repl")
    if relative == "docker-compose.mcp.yml":
        text = text.replace("mcp-network", "qbrain-ai-network")
        text = text.replace("mcp-data:", "qbrain-ai-data:")
        text = text.replace("- mcp-data", "- qbrain-ai-data")
    text = replace_hyphen(text)
    text = text.replace("MCP Server", "QBrain.AI")
    if relative in {"Dockerfile", "docker-compose.mcp.yml"}:
        text = text.replace("Mcp__", "QBrainAi__")
    if relative.startswith("src/") or relative.startswith("build/"):
        text = text.replace(
            'Environment.GetEnvironmentVariable("MCP_',
            'McpServer.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_',
        )
    text = replace_root_token(text)
    if relative.endswith("appsettings.yaml") or relative.endswith("appsettings.Staging.yaml"):
        text = replace_yaml_root(text)
    return text


def iter_text_files() -> list[Path]:
    files: list[Path] = []
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [name for name in dirnames if name not in SKIP_DIR_NAMES]
        for name in filenames:
            path = Path(dirpath) / name
            if path.suffix.lower() in BINARY_SUFFIXES:
                continue
            relative = path.relative_to(ROOT).as_posix()
            if should_skip(relative):
                continue
            files.append(path)
    return files


def rewrite_files() -> int:
    changed = 0
    for path in iter_text_files():
        try:
            original = path.read_text(encoding="utf-8")
        except (UnicodeDecodeError, OSError):
            continue
        relative = path.relative_to(ROOT).as_posix()
        updated = transform(relative, original)
        if updated != original:
            path.write_text(updated, encoding="utf-8")
            changed += 1
    return changed


def rename_paths() -> int:
    candidates: list[Path] = []
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [name for name in dirnames if name not in SKIP_DIR_NAMES]
        relative_dir = Path(dirpath).relative_to(ROOT).as_posix()
        if relative_dir != "." and (relative_dir.startswith(SKIP_PREFIXES) or relative_dir.startswith("lib/NSubstitute")):
            dirnames[:] = []
            continue
        for name in filenames + dirnames:
            if "McpServer" not in name:
                continue
            if any(token in name for token in ("McpServerTools", "McpServerManager", "McpServer_Omarchy")):
                continue
            candidates.append(Path(dirpath) / name)
    candidates.sort(key=lambda path: len(path.as_posix()), reverse=True)
    renamed = 0
    for path in candidates:
        if not path.exists():
            continue
        new_name = path.name.replace("McpServer", "QBrainAi")
        destination = path.with_name(new_name)
        if destination.exists():
            continue
        subprocess.run(["git", "mv", str(path), str(destination)], cwd=ROOT, check=True)
        renamed += 1
    return renamed


def write_facades() -> None:
    compatibility = ROOT / "src" / "Compatibility"
    compatibility.mkdir(parents=True, exist_ok=True)
    libraries = [
        ("SharpNinja.McpServer.Client", "QBrainAI.Client", "QBrainAi.Client", "QBrainAi.Client"),
        ("SharpNinja.McpServer.Cqrs", "QBrainAI.Cqrs", "QBrainAi.Cqrs", "QBrainAi.Cqrs"),
        ("SharpNinja.McpServer.Cqrs.Mvvm", "QBrainAI.Cqrs.Mvvm", "QBrainAi.Cqrs.Mvvm", "QBrainAi.Cqrs.Mvvm"),
        ("SharpNinja.McpServer.McpAgent", "QBrainAI.McpAgent", "QBrainAi.McpAgent", "QBrainAi.McpAgent"),
        ("SharpNinja.McpServer.Repl.Core", "QBrainAI.Repl.Core", "QBrainAi.Repl.Core", "QBrainAi.Repl.Core"),
    ]
    for package_id, new_id, project, namespace in libraries:
        folder = compatibility / package_id
        folder.mkdir(parents=True, exist_ok=True)
        csproj = f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PackageId>{package_id}</PackageId>
    <Authors>SharpNinja</Authors>
    <Description>1.x compatibility package for {new_id}. Public types moved to namespace {namespace}. Removed at 2.0.</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/SharpNinja/McpServer</RepositoryUrl>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <RootNamespace>{namespace}</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\\..\\{project}\\{project}.csproj">
      <PrivateAssets>none</PrivateAssets>
    </ProjectReference>
  </ItemGroup>
  <!-- TR-MCP-QBRAIN-003: dependency facade. Namespace-preserving type forwards are impossible once the root token changes. -->
</Project>
"""
        (folder / f"{package_id}.csproj").write_text(csproj, encoding="utf-8")
        (folder / "Facade.cs").write_text(
            f"""namespace {namespace}.Compatibility;

/// <summary>
/// TR-MCP-QBRAIN-003: Marker type for the {package_id} 1.x facade package.
/// Consumers should reference namespace {namespace} from package {new_id}.
/// </summary>
public static class FacadeMarker
{{
    /// <summary>Gets the replacement package id.</summary>
    public const string ReplacementPackageId = "{new_id}";
}}
""",
            encoding="utf-8",
        )

    write_tool_facade(
        "SharpNinja.McpServer.Repl",
        "QBrainAI.Repl",
        "QBrainAi.Repl.Host",
        "mcpserver-repl",
        "1.x dotnet tool command mcpserver-repl. The canonical command is qbrain-ai-repl on QBrainAI.Repl.",
    )
    write_tool_facade(
        "SharpNinja.McpServer.QBAgent",
        "QBrainAI.QBAgent",
        "QBrainAi.QBAgent",
        "qbagent",
        "1.x package id for the qbagent tool. The command name stays qbagent.",
    )


def write_tool_facade(package_id: str, new_id: str, project: str, command: str, description: str) -> None:
    folder = ROOT / "src" / "Compatibility" / package_id
    folder.mkdir(parents=True, exist_ok=True)
    source_text = (ROOT / "src" / project / f"{project}.csproj").read_text(encoding="utf-8")
    groups = "".join(re.findall(r"  <ItemGroup>.*?</ItemGroup>\n", source_text, flags=re.S))
    groups = groups.replace("..\\", "..\\..\\")
    (folder / f"{package_id}.csproj").write_text(
        f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>{command}</ToolCommandName>
    <PackageId>{package_id}</PackageId>
    <Authors>SharpNinja</Authors>
    <Description>{description} Canonical package id is {new_id}.</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/SharpNinja/McpServer</RepositoryUrl>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <RootNamespace>{project}</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="..\\..\\{project}\\**\\*.cs" Exclude="..\\..\\{project}\\obj\\**\\*.cs;..\\..\\{project}\\bin\\**\\*.cs" />
  </ItemGroup>
{groups}
</Project>
""",
        encoding="utf-8",
    )


def patch_publish_gates() -> None:
    workflow = ROOT / ".github" / "workflows" / "build.yml"
    text = workflow.read_text(encoding="utf-8")
    needle = "          foreach ($package in $packages) {\n"
    insert = """          foreach ($package in $packages) {
            if ((Split-Path $package -Leaf) -like 'QBrainAI.*' -and $env:QBRAINAI_NUGET_PUBLISH -ne 'true') {
              Write-Host "Skipping $package until Phase 4 sets QBRAINAI_NUGET_PUBLISH=true."
              continue
            }
"""
    if "QBRAINAI_NUGET_PUBLISH" not in text and needle in text:
        text = text.replace(needle, insert, 1)
        workflow.write_text(text, encoding="utf-8")

    pipelines = ROOT / "azure-pipelines.yml"
    pipeline = pipelines.read_text(encoding="utf-8")
    pipeline = pipeline.replace(
        "condition: and(succeeded(), ne(variables['SkipOctopus'], 'true'))",
        "condition: and(succeeded(), eq(variables['AllowLegionDeploy'], 'true'))",
    )
    pipeline = pipeline.replace("$project = 'QBrainAi'", "$project = 'McpServer'")
    pipelines.write_text(pipeline, encoding="utf-8")


def main() -> None:
    changed = rewrite_files()
    renamed = rename_paths()
    write_facades()
    patch_publish_gates()
    print(f"rewrote {changed} files; renamed {renamed} paths")


if __name__ == "__main__":
    main()
