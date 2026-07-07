#Requires -Version 7
<#
.SYNOPSIS
    Create Reddit post drafts from the adapted case-study posts by driving Microsoft Edge
    over the Chrome DevTools Protocol (CDP) remote-debugging port. PowerShell + raw CDP only.

.DESCRIPTION
    For each r-*.md post file in this directory the script:
      1. Navigates a debuggable Edge tab to https://www.reddit.com/submit
      2. Selects the target subreddit through the page's <community-picker> node
      3. Fills the post title and (text) body
      4. Clicks "Save Draft" (unless -DryRun)

    Browser model (chosen because Edge 150 / Chromium 136+ REFUSES --remote-debugging-port on the
    Default profile as an anti-cookie-theft measure): the script launches Edge with a DEDICATED
    user-data-dir (default %LOCALAPPDATA%\EdgeAutomation). Log into Reddit as kingdurin@hotmail.com
    ONCE in that window; the session then persists across runs. This is your default browser (Edge)
    signed into your account, just not the Default profile directory. On start the script opens
    https://www.reddit.com/login, waits for login to complete (polling the reddit_session cookie, with
    a manual fallback), then begins posting. On later runs the persisted session is detected and login
    is skipped.

    Transport: PowerShell owns the CDP websocket (System.Net.WebSockets.ClientWebSocket) and all
    orchestration. Reddit's composer is a web-component / shadow-DOM SPA, so the per-page DOM work is
    performed by JavaScript evaluated in the page via Runtime.evaluate. All CDP command payloads are
    built from native PowerShell objects and serialized with ConvertTo-Json (never hand-written JSON).

    SELECTOR CAVEAT: Reddit's submit composer changes often and hides elements in shadow roots. The
    selectors here are best-effort with a recursive shadow-piercing search. Run with -Inspect first
    (dumps the community-picker / title / body / button structure) and -DryRun (fills without saving)
    to confirm before committing real drafts.

.PARAMETER PostsDir
    Directory holding the r-*.md post files. Defaults to the script's own directory.

.PARAMETER DebugPort
    CDP remote-debugging port. Default 9222.

.PARAMETER UserDataDir
    Dedicated Edge user-data-dir for the debuggable session. Default %LOCALAPPDATA%\EdgeAutomation.

.PARAMETER EdgePath
    Full path to msedge.exe. Auto-detected if omitted.

.PARAMETER Only
    One or more subreddit names (without r/) to process. Default: all five.

.PARAMETER Inspect
    Dump the relevant DOM structure for each target and exit without filling anything.

.PARAMETER DryRun
    Fill title/body/community but DO NOT click Save Draft.

.PARAMETER StepDelayMs
    Base pause between UI steps (ms). Default 1200. Increase on a slow connection.

.PARAMETER LogFile
    If set, mirror ALL console output (Write-Host status + the -Inspect DOM dump) to this file via a
    transcript, so it can be reviewed or shared. The script uses Write-Host, which goes to the
    information stream (6), not stdout (1) - so plain '>' or '$x = .\script.ps1' capture nothing.
    Either pass -LogFile, or merge streams once: .\Submit-RedditDrafts.ps1 -Inspect *>&1 | Tee-Object inspect.txt

.EXAMPLE
    pwsh -NoProfile -File .\Submit-RedditDrafts.ps1 -Inspect -LogFile .\inspect.txt
    # Captures the composer DOM dump to inspect.txt (and the console) for sharing.

.EXAMPLE
    pwsh -NoProfile -File .\Submit-RedditDrafts.ps1 -Inspect
    # Launches/attaches Edge, logs you in on first run, prints the composer DOM shape per subreddit.

.EXAMPLE
    pwsh -NoProfile -File .\Submit-RedditDrafts.ps1 -DryRun -Only ClaudeCode
    # Fills the r/ClaudeCode draft but does not save it, so you can eyeball the composer.

.EXAMPLE
    pwsh -NoProfile -File .\Submit-RedditDrafts.ps1
    # Saves a draft for all five subreddits.

.NOTES
    Drafts are NOT public. Review them at https://www.reddit.com/submit (Drafts) before posting.
    The script never clicks "Post"; publishing stays a manual step.
#>
[CmdletBinding()]
param(
    [string]$PostsDir = $PSScriptRoot,
    [int]$DebugPort = 9222,
    [string]$UserDataDir = (Join-Path $env:LOCALAPPDATA 'EdgeAutomation'),
    [string]$EdgePath,
    [string[]]$Only,
    [switch]$Inspect,
    [switch]$DryRun,
    [int]$StepDelayMs = 1200,
    [string]$LogFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:CdpId = 0

# --------------------------------------------------------------------------------------------------
# Edge discovery + debug-port bootstrap
# --------------------------------------------------------------------------------------------------

function Resolve-EdgePath {
    if ($EdgePath -and (Test-Path -LiteralPath $EdgePath)) { return $EdgePath }
    $cands = @(
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
        "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
    )
    $found = $cands | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $found) { throw "msedge.exe not found. Pass -EdgePath explicitly." }
    return $found
}

function Test-DebugPort {
    param([int]$Port)
    try {
        Invoke-RestMethod -Uri "http://127.0.0.1:$Port/json/version" -TimeoutSec 3 | Out-Null
        return $true
    } catch { return $false }
}

function Start-DebugEdge {
    $edge = Resolve-EdgePath
    New-Item -ItemType Directory -Force -Path $UserDataDir | Out-Null
    $edgeArgs = @(
        "--remote-debugging-port=$DebugPort"
        "--user-data-dir=$UserDataDir"
        '--no-first-run'
        '--no-default-browser-check'
        '--new-window'
        'https://www.reddit.com/login/'
    )
    Write-Host "Launching Edge: $edge" -ForegroundColor Cyan
    Write-Host "  user-data-dir: $UserDataDir" -ForegroundColor DarkGray
    Start-Process -FilePath $edge -ArgumentList $edgeArgs | Out-Null

    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 500
        if (Test-DebugPort -Port $DebugPort) { return }
    }
    throw "Edge did not expose the debug port on $DebugPort within 20s."
}

function Get-PageTarget {
    $targets = Invoke-RestMethod -Uri "http://127.0.0.1:$DebugPort/json" -TimeoutSec 5
    $page = $targets |
        Where-Object { $_.type -eq 'page' -and $_.url -notlike 'devtools://*' } |
        Select-Object -First 1
    if (-not $page) { throw "No page target found on the debug port." }
    return $page
}

# --------------------------------------------------------------------------------------------------
# CDP websocket transport (raw)
# --------------------------------------------------------------------------------------------------

function Connect-Cdp {
    param([string]$WsUrl)
    $ws = [System.Net.WebSockets.ClientWebSocket]::new()
    # Out-Null: awaiting a void Task leaks a VoidTaskResult into the pipeline in PowerShell,
    # which would otherwise pollute this function's return value.
    $ws.ConnectAsync([Uri]$WsUrl, [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null
    return $ws
}

function Receive-CdpMessage {
    param($Ws, [int]$TimeoutSec = 60)
    $buf = [byte[]]::new(65536)
    $seg = [System.ArraySegment[byte]]::new($buf)
    $ms = [System.IO.MemoryStream]::new()
    do {
        $res = $Ws.ReceiveAsync($seg, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
        $ms.Write($buf, 0, $res.Count)
    } while (-not $res.EndOfMessage)
    $json = [Text.Encoding]::UTF8.GetString($ms.ToArray())
    return $json | ConvertFrom-Json -Depth 40
}

function Invoke-Cdp {
    param($Ws, [string]$Method, $Params, [int]$TimeoutSec = 30)
    $script:CdpId++
    $id = $script:CdpId
    $cmd = @{ id = $id; method = $Method }
    if ($null -ne $Params) { $cmd.params = $Params }
    $payload = $cmd | ConvertTo-Json -Depth 20 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($payload)
    $sendSeg = [System.ArraySegment[byte]]::new($bytes)
    $Ws.SendAsync($sendSeg, [System.Net.WebSockets.WebSocketMessageType]::Text, $true,
        [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $msg = Receive-CdpMessage -Ws $Ws -TimeoutSec $TimeoutSec
        if (($msg.PSObject.Properties.Name -contains 'id') -and ($msg.id -eq $id)) {
            if ($msg.PSObject.Properties.Name -contains 'error') {
                throw "CDP $Method error: $($msg.error.message)"
            }
            return $msg.result
        }
        # Otherwise it is a protocol event or a stale id; ignore and keep reading.
    }
    throw "CDP $Method timed out after ${TimeoutSec}s."
}

function Invoke-PageJs {
    param($Ws, [string]$Expression, [int]$TimeoutSec = 90)
    $params = @{
        expression   = $Expression
        awaitPromise = $true
        returnByValue = $true
        userGesture  = $true
    }
    $r = Invoke-Cdp -Ws $Ws -Method 'Runtime.evaluate' -Params $params -TimeoutSec $TimeoutSec
    if ($r.PSObject.Properties.Name -contains 'exceptionDetails' -and $r.exceptionDetails) {
        $ex = $r.exceptionDetails
        $desc = if ($ex.exception) { $ex.exception.description } else { $ex.text }
        throw "Page JS exception: $desc"
    }
    return $r.result.value
}

function New-IifeExpr {
    param([string]$JsFunc, $ArgObject)
    $json = if ($null -ne $ArgObject) { $ArgObject | ConvertTo-Json -Depth 12 -Compress } else { 'null' }
    return "($JsFunc)($json)"
}

function Wait-DocReady {
    param($Ws, [int]$TimeoutSec = 30)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-PageJs -Ws $Ws -Expression 'document.readyState'
            if ($state -eq 'complete') { return }
        } catch { }
        Start-Sleep -Milliseconds 400
    }
}

# --------------------------------------------------------------------------------------------------
# Injected JavaScript (runs in the page; pierces open shadow roots)
# --------------------------------------------------------------------------------------------------

# Shared helpers, prepended to every action function body.
$JS_HELPERS = @'
  const sleep = ms => new Promise(r => setTimeout(r, ms));
  function allEls(root){
    const res=[]; const st=[root];
    while(st.length){
      const n=st.pop();
      let kids=[];
      try { kids = n.querySelectorAll('*'); } catch(e){}
      for(const k of kids){ res.push(k); if(k.shadowRoot) st.push(k.shadowRoot); }
    }
    return res;
  }
  function setNativeValue(el, val){
    const proto = el.tagName === 'TEXTAREA' ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
    const desc = Object.getOwnPropertyDescriptor(proto, 'value');
    desc.set.call(el, val);
    el.dispatchEvent(new Event('input', {bubbles:true}));
    el.dispatchEvent(new Event('change', {bubbles:true}));
  }
  function txt(e){ return (e && e.textContent ? e.textContent.trim() : ''); }
'@

# Inspect: report the composer structure so selectors can be verified/tuned.
$JS_INSPECT = @"
async function(A){
  $JS_HELPERS
  const els = allEls(document);
  const tags = {};
  for(const e of els){ const t=e.tagName.toLowerCase(); if(t.includes('-')) tags[t]=(tags[t]||0)+1; }
  const picker = els.find(e => e.tagName.toLowerCase()==='community-picker');
  const inputs = els.filter(e => e.tagName==='INPUT' || e.tagName==='TEXTAREA').map(e => ({
    tag:e.tagName, name:e.getAttribute('name'), placeholder:e.getAttribute('placeholder'),
    aria:e.getAttribute('aria-label'), id:e.id
  }));
  const editable = els.filter(e => e.getAttribute && e.getAttribute('contenteditable')==='true').length;
  const buttons = els.filter(e => e.tagName==='BUTTON' || (e.getAttribute && e.getAttribute('role')==='button'))
    .map(e => txt(e)).filter(t => t.length>0 && t.length<40);
  return {
    url: location.href,
    customTags: tags,
    hasCommunityPicker: !!picker,
    pickerHasShadow: !!(picker && picker.shadowRoot),
    inputs: inputs.slice(0,25),
    contentEditableCount: editable,
    buttonTexts: Array.from(new Set(buttons)).slice(0,40)
  };
}
"@

# Detect whether we appear logged in / composer is reachable.
$JS_LOGGED_IN = @"
async function(A){
  $JS_HELPERS
  const els = allEls(document);
  const picker = els.find(e => e.tagName.toLowerCase()==='community-picker');
  const loginish = /\/login|\/account\//.test(location.href);
  return { hasPicker: !!picker, url: location.href, looksLikeLogin: loginish };
}
"@

# Select the target subreddit through the community-picker node.
$JS_SELECT_COMMUNITY = @"
async function(A){
  $JS_HELPERS
  const sub = A.subreddit;
  const wantPath = ('/r/'+sub+'/').toLowerCase();
  const wantText = ('r/'+sub).toLowerCase();
  const norm = s => (s||'').toLowerCase().replace(/^r\//,'').trim();

  // 1. Open the picker: prefer the 'Select Community' button, else the picker's own trigger.
  const picker = allEls(document).find(e => e.tagName.toLowerCase()==='community-picker');
  let opener = allEls(document).find(e =>
    (e.tagName==='BUTTON' || (e.getAttribute && e.getAttribute('role')==='button')) &&
    /^select community$/i.test(txt(e)));
  if(!opener && picker){ opener = (picker.shadowRoot||picker).querySelector('button,[role=button],input'); }
  if(opener){ opener.click(); }
  await sleep(700);

  // 2. Find the community search field. When the picker dialog opens it is a
  //    <textarea placeholder="Search communities"> (NOT an <input>), so accept both and skip title/q.
  const isField = e => (e.tagName==='INPUT' || e.tagName==='TEXTAREA');
  const notOther = e => { const n=(e.getAttribute('name')||''); return n!=='title' && n!=='q'; };
  let input = allEls(document).find(e => isField(e) && notOther(e) &&
    /communit/i.test((e.getAttribute('placeholder')||'')+(e.getAttribute('aria-label')||'')));
  if(!input && picker){ input = allEls(picker.shadowRoot||picker).find(e => isField(e) && notOther(e)); }
  if(!input){ input = allEls(document).find(e => isField(e) && notOther(e) &&
    /search/i.test((e.getAttribute('placeholder')||'')+(e.getAttribute('aria-label')||''))); }
  if(!input) return {ok:false, step:'find-input', msg:'community search input not found'};
  input.focus();
  setNativeValue(input, sub);
  // Nudge listeners that key on keyboard events.
  input.dispatchEvent(new KeyboardEvent('keydown', {bubbles:true, key:sub.slice(-1)}));
  input.dispatchEvent(new KeyboardEvent('keyup', {bubbles:true, key:sub.slice(-1)}));

  // 3. Wait for and click the exact community result.
  let clicked=false, seen=[];
  for(let i=0;i<30 && !clicked;i++){
    await sleep(300);
    const cands = allEls(document).filter(e =>
      e.tagName==='A' || e.tagName==='LI' || e.tagName==='BUTTON' ||
      (e.getAttribute && e.getAttribute('role')==='option'));
    seen = Array.from(new Set(cands.map(e=>txt(e)).filter(t=>/^r\//i.test(t)))).slice(0,15);
    const target = cands.find(e => {
      const href = ((e.getAttribute && e.getAttribute('href'))||'').toLowerCase();
      const t = txt(e).toLowerCase();
      return href.includes(wantPath) || t===wantText || t.split(/[\s·•]/)[0]===wantText;
    });
    if(target){ target.click(); clicked=true; }
  }
  if(!clicked) return {ok:false, step:'find-result', msg:'no exact match for '+wantText, seen:seen};

  // 4. Confirm via the hidden form inputs the picker populates.
  let hiddenVal='';
  for(let i=0;i<15;i++){
    await sleep(200);
    const pn = allEls(document).find(e => e.tagName==='INPUT' && e.getAttribute('name')==='prefixedName');
    const sn = allEls(document).find(e => e.tagName==='INPUT' && e.getAttribute('name')==='subredditName');
    hiddenVal = (pn&&pn.value) || (sn&&sn.value) || '';
    if(hiddenVal) break;
  }
  if(hiddenVal && norm(hiddenVal)!==sub.toLowerCase()){
    return {ok:false, step:'confirm', msg:'selected wrong community: '+hiddenVal, seen:seen};
  }
  return {ok:true, step:'selected', confirmed:hiddenVal, seen:seen};
}
"@

# Ensure the Text post type is active (best-effort).
$JS_SELECT_TEXT_TAB = @"
async function(A){
  $JS_HELPERS
  const tab = allEls(document).find(e =>
    (e.tagName==='BUTTON' || (e.getAttribute && e.getAttribute('role')==='tab')) &&
    /^text$/i.test(txt(e)));
  if(tab){ tab.click(); await sleep(500); return {ok:true, clicked:true}; }
  return {ok:true, clicked:false};
}
"@

# Fill the title field.
$JS_SET_TITLE = @"
async function(A){
  $JS_HELPERS
  const fields = allEls(document).filter(e => e.tagName==='TEXTAREA' || e.tagName==='INPUT');
  // Exact <textarea name="title"> first (confirmed by DOM inspect), then a fuzzy fallback.
  let el = fields.find(e => (e.getAttribute('name')||'')==='title');
  if(!el) el = fields.find(e => /title/i.test((e.getAttribute('name')||'')+(e.getAttribute('placeholder')||'')+(e.getAttribute('aria-label')||'')+(e.id||'')));
  if(!el) return {ok:false, msg:'title field not found'};
  el.focus();
  setNativeValue(el, A.title);
  return {ok:true, value: (el.value||'').slice(0,60)};
}
"@

# Fill the body (contenteditable rich-text editor) with plain text.
$JS_SET_BODY = @"
async function(A){
  $JS_HELPERS
  // Post bodies are Markdown, so use the Markdown editor (a real <textarea placeholder='Body text (optional)'>),
  // NOT the rich-text contenteditable, which would render [text](url) link syntax literally.
  const toMd = allEls(document).find(e =>
    (e.tagName==='BUTTON' || (e.getAttribute && e.getAttribute('role')==='button')) &&
    /switch to markdown/i.test(txt(e)));
  if(toMd){ toMd.click(); await sleep(600); }

  const ta = allEls(document).find(e => e.tagName==='TEXTAREA' &&
    /body text/i.test(e.getAttribute('placeholder')||''));
  if(ta){ ta.focus(); setNativeValue(ta, A.body); return {ok:true, mode:'markdown'}; }

  // Fallback: rich-text contenteditable (markdown will not render, but text is preserved).
  const ed = allEls(document).find(e => e.getAttribute && e.getAttribute('contenteditable')==='true');
  if(ed){
    ed.focus();
    try { document.execCommand('selectAll', false, null); document.execCommand('delete', false, null); } catch(e){}
    const ok = document.execCommand('insertText', false, A.body);
    if(!ok){ ed.textContent = A.body; ed.dispatchEvent(new Event('input', {bubbles:true})); }
    return {ok:true, mode:'contenteditable'};
  }
  return {ok:false, msg:'body editor not found'};
}
"@

# Click the Save Draft button.
$JS_SAVE_DRAFT = @"
async function(A){
  $JS_HELPERS
  const btns = allEls(document).filter(e => e.tagName==='BUTTON' || (e.getAttribute && e.getAttribute('role')==='button'));
  const b = btns.find(e => /^save\s*draft$/i.test(txt(e)));
  if(!b) return {ok:false, msg:'Save Draft button not found', seen: Array.from(new Set(btns.map(x=>txt(x)).filter(t=>t))).slice(0,30)};
  if(b.disabled) return {ok:false, msg:'Save Draft button disabled'};
  b.click();
  await sleep(1200);
  return {ok:true};
}
"@

# --------------------------------------------------------------------------------------------------
# Login flow: navigate to Reddit login, wait for completion, confirm the composer is reachable
# --------------------------------------------------------------------------------------------------

function Get-RedditCookies {
    param($Ws)
    try {
        $r = Invoke-Cdp -Ws $Ws -Method 'Network.getCookies' `
            -Params @{ urls = @('https://www.reddit.com', 'https://reddit.com') } -TimeoutSec 15
        return $r.cookies
    } catch { return @() }
}

function Test-RedditLoggedIn {
    param($Ws)
    # Strong, non-disruptive signal: the authenticated 'reddit_session' cookie (not present when logged out).
    $cookies = Get-RedditCookies -Ws $Ws
    $has = $cookies | Where-Object { $_.name -eq 'reddit_session' -and $_.value }
    return [bool]$has
}

function Confirm-ComposerReachable {
    param($Ws)
    Invoke-Cdp -Ws $Ws -Method 'Page.navigate' -Params @{ url = 'https://www.reddit.com/submit' } | Out-Null
    Wait-DocReady -Ws $Ws
    Start-Sleep -Milliseconds ($StepDelayMs * 2)
    $state = Invoke-PageJs -Ws $Ws -Expression (New-IifeExpr -JsFunc $JS_LOGGED_IN -ArgObject @{})
    return [bool]$state.hasPicker
}

function Wait-RedditLogin {
    param($Ws, [int]$TimeoutSec = 600)

    Invoke-Cdp -Ws $Ws -Method 'Page.navigate' -Params @{ url = 'https://www.reddit.com/login/' } | Out-Null
    Wait-DocReady -Ws $Ws

    # Already authenticated (persistent session from a prior run)? Confirm and go.
    if (Test-RedditLoggedIn -Ws $Ws) {
        if (Confirm-ComposerReachable -Ws $Ws) { return $true }
    }

    Write-Host ""
    Write-Host "Log into Reddit as kingdurin@hotmail.com in the Edge window." -ForegroundColor Yellow
    Write-Host "Waiting for login to complete (timeout ${TimeoutSec}s)..." -ForegroundColor Yellow

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $leftLogin = 0
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 3

        # Read current URL without navigating (non-disruptive during login).
        $url = ''
        try { $url = Invoke-PageJs -Ws $Ws -Expression 'location.href' } catch { }
        $onLogin = ($url -match '/login')

        if (Test-RedditLoggedIn -Ws $Ws) {
            Write-Host "  session cookie detected; confirming composer..." -ForegroundColor DarkGray
            if (Confirm-ComposerReachable -Ws $Ws) { return $true }
        }
        elseif ((-not $onLogin) -and ($url -match 'reddit\.com')) {
            # User left the login page (likely finished). Require two stable checks, then confirm
            # without repeatedly yanking the tab mid-login.
            $leftLogin++
            if ($leftLogin -ge 2) {
                if (Confirm-ComposerReachable -Ws $Ws) { return $true }
                $leftLogin = 0
            }
        }
        else {
            $leftLogin = 0
        }
    }
    return $false
}

# --------------------------------------------------------------------------------------------------
# Post parsing
# --------------------------------------------------------------------------------------------------

function Get-PostContent {
    param([System.IO.FileInfo]$File)
    $raw = Get-Content -LiteralPath $File.FullName -Raw

    $sub = if ($raw -match '(?m)^\#\s*r/(\S+)') { $Matches[1] }
           else { ($File.BaseName -replace '^r-', '') }

    $title = if ($raw -match '(?m)^\*\*Suggested title:\*\*\s*(.+?)\s*$') { $Matches[1].Trim() } else { $null }

    $body = $null
    $marker = 'PASTE BELOW THIS LINE'
    $idx = $raw.IndexOf($marker)
    if ($idx -ge 0) {
        $after = $raw.Substring($idx + $marker.Length)
        $after = $after -replace '^\s*', ''      # drop leading blank lines
        $after = $after -replace '^\-\-\-\s*', '' # drop the leading --- separator
        $body = $after.Trim()
    }

    [pscustomobject]@{
        File      = $File.Name
        Subreddit = $sub
        Title     = $title
        Body      = $body
    }
}

# --------------------------------------------------------------------------------------------------
# Per-post flow
# --------------------------------------------------------------------------------------------------

function Invoke-DraftForPost {
    param($Ws, $Post)

    Write-Host ""
    Write-Host "== r/$($Post.Subreddit)  ($($Post.File)) ==" -ForegroundColor Green

    Invoke-Cdp -Ws $Ws -Method 'Page.navigate' -Params @{ url = 'https://www.reddit.com/submit' } | Out-Null
    Wait-DocReady -Ws $Ws
    Start-Sleep -Milliseconds ($StepDelayMs * 2)   # SPA hydration

    if ($Inspect) {
        $info = Invoke-PageJs -Ws $Ws -Expression (New-IifeExpr -JsFunc $JS_INSPECT -ArgObject @{})
        Write-Host "  URL: $($info.url)"
        Write-Host "  community-picker present: $($info.hasCommunityPicker) (shadow: $($info.pickerHasShadow))"
        Write-Host "  contenteditable regions: $($info.contentEditableCount)"
        Write-Host "  inputs/texted fields:"
        foreach ($f in $info.inputs) { Write-Host "    [$($f.tag)] name=$($f.name) ph=$($f.placeholder) aria=$($f.aria)" }
        Write-Host "  button texts: $([string]::Join(' | ', $info.buttonTexts))"
        Write-Host "  custom tags: $([string]::Join(', ', ($info.customTags.PSObject.Properties | ForEach-Object { $_.Name })))"
        Write-Host "  ---inspect-json (r/$($Post.Subreddit))---"
        Write-Host ($info | ConvertTo-Json -Depth 8)
        return [pscustomobject]@{ Subreddit = $Post.Subreddit; Result = 'inspected' }
    }

    $sel = Invoke-PageJs -Ws $Ws -Expression (New-IifeExpr -JsFunc $JS_SELECT_COMMUNITY -ArgObject @{ subreddit = $Post.Subreddit })
    if (-not $sel.ok) {
        $seen = if ($sel.PSObject.Properties.Name -contains 'seen') { " seen: $([string]::Join(', ', $sel.seen))" } else { '' }
        throw "community select failed at '$($sel.step)': $($sel.msg).$seen"
    }
    Write-Host "  community: confirmed='$($sel.confirmed)'" -ForegroundColor DarkGray
    Start-Sleep -Milliseconds $StepDelayMs

    Invoke-PageJs -Ws $Ws -Expression (New-IifeExpr -JsFunc $JS_SELECT_TEXT_TAB -ArgObject @{}) | Out-Null
    Start-Sleep -Milliseconds ([Math]::Min($StepDelayMs, 600))

    $t = Invoke-PageJs -Ws $Ws -Expression (New-IifeExpr -JsFunc $JS_SET_TITLE -ArgObject @{ title = $Post.Title })
    if (-not $t.ok) { throw "title fill failed: $($t.msg)" }
    Write-Host "  title set: $($t.value)" -ForegroundColor DarkGray

    $b = Invoke-PageJs -Ws $Ws -Expression (New-IifeExpr -JsFunc $JS_SET_BODY -ArgObject @{ body = $Post.Body })
    if (-not $b.ok) { throw "body fill failed: $($b.msg)" }
    Write-Host "  body set ($($b.mode))" -ForegroundColor DarkGray

    if ($DryRun) {
        Write-Host "  DRY RUN: left filled, not saved" -ForegroundColor Yellow
        return [pscustomobject]@{ Subreddit = $Post.Subreddit; Result = 'dry-run-filled' }
    }

    $s = Invoke-PageJs -Ws $Ws -Expression (New-IifeExpr -JsFunc $JS_SAVE_DRAFT -ArgObject @{})
    if (-not $s.ok) {
        $seen = if ($s.PSObject.Properties.Name -contains 'seen') { " Buttons seen: $([string]::Join(' | ', $s.seen))" } else { '' }
        throw "save draft failed: $($s.msg).$seen"
    }
    Write-Host "  draft saved" -ForegroundColor Green
    return [pscustomobject]@{ Subreddit = $Post.Subreddit; Result = 'draft-saved' }
}

# --------------------------------------------------------------------------------------------------
# Main
# --------------------------------------------------------------------------------------------------

# 0. Optional transcript so every stream (Write-Host included) is captured to a file.
$script:TranscriptOn = $false
if ($LogFile) {
    try {
        Start-Transcript -LiteralPath $LogFile -Force | Out-Null
        $script:TranscriptOn = $true
        Write-Host "Transcript: $LogFile" -ForegroundColor DarkGray
    } catch {
        Write-Warning "Could not start transcript ($($_.Exception.Message)); continuing without it."
    }
}

try {

# 1. Gather posts.
$files = Get-ChildItem -LiteralPath $PostsDir -Filter 'r-*.md' | Sort-Object Name
if ($Only) {
    $files = $files | Where-Object {
        $s = ($_.BaseName -replace '^r-', '')
        $Only -contains $s
    }
}
if (-not $files) { throw "No matching r-*.md post files in $PostsDir" }

$posts = $files | ForEach-Object { Get-PostContent -File $_ }
foreach ($p in $posts) {
    if (-not $p.Title -or -not $p.Body) { throw "Could not parse title/body from $($p.File)" }
}
Write-Host "Posts to process: $([string]::Join(', ', ($posts | ForEach-Object { 'r/' + $_.Subreddit })))" -ForegroundColor Cyan

# 2. Ensure a debuggable Edge is running on the dedicated profile.
if (-not (Test-DebugPort -Port $DebugPort)) {
    Start-DebugEdge
} else {
    Write-Host "Attaching to existing debug session on port $DebugPort." -ForegroundColor Cyan
}

# 3. Connect CDP and enable domains.
$target = Get-PageTarget
$ws = Connect-Cdp -WsUrl $target.webSocketDebuggerUrl
try {
    Invoke-Cdp -Ws $ws -Method 'Page.enable' -Params @{} | Out-Null
    Invoke-Cdp -Ws $ws -Method 'Runtime.enable' -Params @{} | Out-Null
    Invoke-Cdp -Ws $ws -Method 'Network.enable' -Params @{} | Out-Null

    # 4. Navigate to Reddit login and wait for login to complete before posting.
    if (-not (Wait-RedditLogin -Ws $ws -TimeoutSec 600)) {
        Write-Host "Automatic login detection timed out." -ForegroundColor Yellow
        [void](Read-Host "If you ARE logged in and see the submit page, press Enter to continue anyway")
        if (-not (Confirm-ComposerReachable -Ws $ws)) {
            throw "Cannot see the community-picker. Confirm login and Reddit's submit-page layout."
        }
    }
    Write-Host "Login confirmed; composer reachable. Starting drafts..." -ForegroundColor Green

    # 5. Process each post.
    $results = foreach ($p in $posts) { Invoke-DraftForPost -Ws $ws -Post $p }

    Write-Host ""
    Write-Host "Summary:" -ForegroundColor Cyan
    foreach ($r in $results) { Write-Host "  r/$($r.Subreddit): $($r.Result)" }
    if (-not $DryRun -and -not $Inspect) {
        Write-Host ""
        Write-Host "Review drafts before posting: https://www.reddit.com/submit  (Drafts)" -ForegroundColor Cyan
    }
}
finally {
    if ($ws -is [System.Net.WebSockets.ClientWebSocket]) {
        try { $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'done', [Threading.CancellationToken]::None).GetAwaiter().GetResult() | Out-Null } catch { }
        $ws.Dispose()
    }
    # Edge is left running so you can review the drafts.
}

} # end main try
finally {
    if ($script:TranscriptOn) { try { Stop-Transcript | Out-Null } catch { } }
}
