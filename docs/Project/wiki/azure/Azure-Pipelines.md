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

If any optional variable is absent, the corresponding publish or deploy step is skipped rather than failing the pipeline.

## Agent Hosting

This pipeline is configured for a Windows self-hosted agent so the same machine can handle the .NET build, docs generation, package publication, Azure CLI deployment, and MSIX packaging path without relying on Microsoft-hosted parallelism. The `Default` pool must contain at least one online Windows agent with current Azure Pipelines agent software.

## Retention

The former GitHub-specific cleanup workflow is retired as part of this migration. Configure Azure DevOps pipeline retention policies and artifact retention in the project settings instead of maintaining a repository cleanup YAML for stale workflow runs and artifacts.
