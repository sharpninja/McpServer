# Azure DevOps Pipelines

This repository now uses `azure-pipelines.yml` as the CI/CD source of truth for the repo build, test, docs, packaging, and package-publish path.

## Scope

The Azure pipeline covers the core repository workflow only:

- Windows self-hosted build, config validation, test, version calculation, and publish artifact generation
- DocFX documentation build and docs artifact publication
- Windows MSIX packaging as a non-blocking job
- `McpServer.Client` package packing and branch-conditional package publication

It intentionally does **not** attempt to migrate or manage any separate Copilot coding agent pipeline.

## Variables

Optional Azure DevOps variables control the release-oriented steps:

- `NuGetApiKey`
  Used on `main` to push `McpServer.Client` packages to `nuget.org`.
- `AzureArtifactsFeedUrl`
  Used on non-`main` branches to push `McpServer.Client` packages to an Azure Artifacts NuGet feed.
- `DocsAzureServiceConnection`
  Azure service connection name for optional static website deployment of the generated docs artifact. Currently inert: the `docs_deploy` job that consumed this variable is commented out in `azure-pipelines.yml` (disabled to keep the pipeline YAML valid), so setting it triggers no deployment until the job is restored.
- `DocsStorageAccount`
  Azure Storage account name whose `$web` container would receive the docs artifact on `main`. Also inert while the `docs_deploy` job is disabled.
- `OCTOPUS_URL`
  Optional. Defaults to `http://PAYTON-LEGION2:8066` when omitted in the pipeline step. Points at the lab Octopus Deploy server (containerized on LEGION2).
- `OCTOPUS_API_KEY`
  Optional secret. When set, the pipeline creates an Octopus release for project `McpServer` and attempts deploy to `Development`. When unset, the Octopus step no-ops successfully.
- `OCTOPUS_SPACE`
  Optional. Defaults to `Default`.
- `SkipOctopus`
  Optional. Set to `true` to skip the Octopus step even when `OCTOPUS_API_KEY` is present.

If any optional variable is absent, the corresponding publish or deploy step is skipped rather than failing the pipeline.

## Octopus Deploy (lab)

After the main build/publish job steps, `azure-pipelines.yml` includes an **Octopus LEGION2 release** step. It uses the Octopus CLI (`C:\Program Files\Octopus CLI\octopus.exe`) on the self-hosted `Default` pool agent.

- Octopus project name: `McpServer`
- Default target on that server: deployment target **PAYTON-DESKTOP** (polling tentacle, role `app-server`)
- The step is additive to Azure Pipelines CI; it does not replace build/test/pack.

Remotes in this repo use `origin` for GitHub and `azure` for Azure DevOps when both are configured.

## Agent Hosting

This pipeline is configured for a Windows self-hosted agent so the same machine can handle the .NET build, docs generation, package publication, Azure CLI deployment, and MSIX packaging path without relying on Microsoft-hosted parallelism. The `Default` pool must contain at least one online Windows agent with current Azure Pipelines agent software.

## Retention

The former GitHub-specific cleanup workflow is retired as part of this migration. Configure Azure DevOps pipeline retention policies and artifact retention in the project settings instead of maintaining a repository cleanup YAML for stale workflow runs and artifacts.
