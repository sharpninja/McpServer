# Reddit posts: Triage plugin case study, adapted

Five audience-tailored adaptations of [`docs/case-studies/Triage-Plugin-Code-Quality-Case-Study.md`](https://github.com/sharpninja/McpServer/blob/main/docs/case-studies/Triage-Plugin-Code-Quality-Case-Study.md), one per subreddit. Each post opens with a short McpServer introduction, focuses on the plugin relevant to that community, links referenced repo docs, and closes by soliciting questions.

## Subreddit to file

- r/ClaudeCode -> [`r-ClaudeCode.md`](r-ClaudeCode.md) (focus: Claude Code plugin, hook enforcement)
- r/grok -> [`r-grok.md`](r-grok.md) (focus: Grok plugin)
- r/GrokBuild -> [`r-GrokBuild.md`](r-GrokBuild.md) (focus: Grok plugin, build-process angle)
- r/OpenAIDev -> [`r-OpenAIDev.md`](r-OpenAIDev.md) (focus: Codex plugin)
- r/aiprojects -> [`r-aiprojects.md`](r-aiprojects.md) (focus: whole plugin ecosystem, all 8 plugins)

Each file has a metadata header (suggested title, flair) followed by a `PASTE BELOW` marker. Copy everything under the marker into Reddit.

## Links used (all verified present on `github/main`)

Base: `https://github.com/sharpninja/McpServer/blob/main/`

- `README.md`, `docs/case-studies/Triage-Plugin-Code-Quality-Case-Study.md`
- `docs/AGENT-PLUGIN-AVAILABILITY.md`, `docs/AGENT-PLUGIN-FEATURE-MATRIX.md`, `plugins/core/README.md`
- `docs/claude-hook-validation-skill.md`
- `docs/Development-Process-draft-v4.md`
- `docs/Project/Functional-Requirements.md`, `docs/Project/Technical-Requirements.md`, `docs/Project/Testing-Requirements.md`

Per-plugin repositories (separate repos, from `docs/AGENT-PLUGIN-AVAILABILITY.md`):

- `https://github.com/sharpninja/mcpserver-claude-code-plugin`
- `https://github.com/sharpninja/mcpserver-codex-plugin`
- `https://github.com/sharpninja/mcpserver-grok-plugin`
- `https://github.com/sharpninja/mcpserver-copilot-plugin`
- `https://github.com/sharpninja/mcpserver-cline-plugin`

## Publish checklist

1. Confirm every `blob/main/...` link resolves (200) and each plugin-repo link resolves (200). The case study reached `main` on 2026-07-07 (merge `11ca4bf2`); links before that date would have 404'd.
2. Read each subreddit's self-promotion rules before posting. These posts include an explicit "I built this" disclosure, but some subs require a flair, a ratio, or a dedicated thread.
3. Post one subreddit at a time. Do not cross-post identical text in a short window (spam filters).
4. Prefer the r/aiprojects post for the broadest audience; it is the show-and-tell version.
