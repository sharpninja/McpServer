#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-wrapup-git-20260818.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function W([string]$s) { $script:lines.Add($s); Write-Output $s }

Set-Location 'F:\GitHub\McpServer'
W "UTC=$(Get-Date -AsUTC -Format o)"
W "GIT_HEAD=$(git rev-parse HEAD)"
W "GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD)"
W "GIT_STATUS=$(git status -sb)"
W "GIT_REMOTES"
git remote -v | ForEach-Object { W $_ }
W "ORIGIN_DEVELOP=$(git ls-remote origin refs/heads/develop)"
W "AZURE_DEVELOP=$(git ls-remote azure refs/heads/develop)"
W "ORIGIN_MAIN=$(git ls-remote origin refs/heads/main)"
W "AZURE_MAIN=$(git ls-remote azure refs/heads/main)"
W "WIKI_HEAD=$(git ls-remote https://github.com/sharpninja/McpServer.wiki.git HEAD)"
W "WIKI_MASTER=$(git ls-remote https://github.com/sharpninja/McpServer.wiki.git refs/heads/master)"
W "WIKI_MAIN=$(git ls-remote https://github.com/sharpninja/McpServer.wiki.git refs/heads/main)"

git diff --check
W "GIT_DIFF_CHECK_WT=$LASTEXITCODE"
git diff --check HEAD
W "GIT_DIFF_CHECK_HEAD=$LASTEXITCODE"

W "COMMIT_ONELINE=$(git log -1 --format='%H %D %s')"
W "COMMIT_PARENTS=$(git rev-parse HEAD^)"
W "COMMIT_NAME_STATUS_SUMMARY"
git show --name-status --format=fuller --stat bf000bb7fc495b6011eb5888a8c9293c992eb305 | Select-Object -First 80 | ForEach-Object { W $_ }
W "COMMIT_FILE_COUNT=$(git show --name-only --pretty=format: bf000bb7 | Where-Object { $_ } | Measure-Object | Select-Object -ExpandProperty Count)"
W "COMMIT_DELETED"
git show --name-status --format= --diff-filter=D bf000bb7 | ForEach-Object { W $_ }
W "WIKI_YAML_PAGES_IN_COMMIT"
git show bf000bb7:docs/wiki.yaml | Select-String -Pattern 'source:|id:|schema:' | ForEach-Object { W $_.Line }

# Does commit include product/handoff source?
W "COMMIT_PRODUCT_CS"
git show --name-only --pretty=format: bf000bb7 | Where-Object { $_ -match 'Product|Handoff' } | ForEach-Object { W $_ }

# Azure wiki git repo?
W "AZURE_WIKI_LSREMOTE_TRY"
git ls-remote "https://dev.azure.com/McpServer/McpServer/_git/McpServer.wiki" 2>&1 | ForEach-Object { W "$_" }
W "AZURE_WIKI_LSREMOTE_EXIT=$LASTEXITCODE"

# az wiki list if available
if (Get-Command az -ErrorAction SilentlyContinue) {
    W "AZ_WIKI_LIST"
    az devops wiki list --organization https://dev.azure.com/McpServer --project McpServer -o json 2>&1 | ForEach-Object { W "$_" }
    W "AZ_WIKI_LIST_EXIT=$LASTEXITCODE"
} else {
    W "AZ_NOT_FOUND"
}

W "UTC_END=$(Get-Date -AsUTC -Format o)"
$lines | Set-Content -LiteralPath $out -Encoding utf8
W "WROTE=$out"
