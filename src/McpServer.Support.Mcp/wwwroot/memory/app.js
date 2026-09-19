/* First-party Memory UI — mutations via REST /mcpserver/memory only */
(function () {
  const $ = (id) => document.getElementById(id);
  let selectedVersion = null;
  let currentId = "";

  function headers() {
    const h = { Accept: "application/json", "Content-Type": "application/json" };
    const key = $("apiKey").value.trim();
    const ws = $("workspace").value.trim();
    if (key) h["X-Api-Key"] = key;
    if (ws) h["X-Workspace-Path"] = ws;
    return h;
  }

  function base() {
    const b = $("baseUrl").value.trim().replace(/\/$/, "");
    return b || window.location.origin;
  }

  async function api(method, path, body) {
    const res = await fetch(base() + path, {
      method,
      headers: headers(),
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    const text = await res.text();
    let data = null;
    try { data = text ? JSON.parse(text) : null; } catch { data = text; }
    return { res, data };
  }

  function setStatus(msg) {
    $("status").textContent = typeof msg === "string" ? msg : JSON.stringify(msg, null, 2);
  }

  function show(el, visible) {
    el.hidden = !visible;
  }

  function closePanel() {
    show($("detail-panel"), false);
    show($("version-panel"), false);
    show($("fail-closed"), false);
    selectedVersion = null;
    currentId = "";
  }

  function parseTags(value) {
    return value.split(",").map((item) => item.trim()).filter(Boolean);
  }

  function renderTagChips(tags) {
    const host = $("tagChips");
    host.innerHTML = "";
    tags.forEach((tag) => {
      const chip = document.createElement("span");
      chip.className = "tag-chip";
      chip.textContent = tag;
      host.appendChild(chip);
    });
  }

  function renderList(items, queryActive) {
    const ul = $("list");
    ul.innerHTML = "";
    const rows = items || [];
    show($("empty-state"), rows.length === 0 && !queryActive);
    show($("empty-results"), rows.length === 0 && queryActive);
    rows.forEach((item) => {
      const li = document.createElement("li");
      li.textContent = (item.id || "") + " " + (item.title || item.content || item.text || "");
      li.onclick = () => openMemory(item.id);
      ul.appendChild(li);
    });
  }

  async function listEffective() {
    const { res, data } = await api("GET", "/mcpserver/memory?scope=Effective");
    if (!res.ok) {
      setStatus("List request failed: " + res.status);
      return;
    }
    renderList(data.items || data.Items || [], false);
  }

  async function search() {
    const query = $("query").value.trim();
    const tags = parseTags($("tagFilter").value);
    renderTagChips(tags);
    if (!query && tags.length === 0) {
      await listEffective();
      return;
    }
    const { res, data } = await api("POST", "/mcpserver/memory/recall", { query: query || "*", tags });
    if (!res.ok) {
      setStatus("Search request failed: " + res.status);
      return;
    }
    renderList(data.items || data.Items || [], true);
  }

  function fillDetail(item) {
    currentId = item.id || "";
    $("memoryId").value = currentId;
    $("editTitle").value = item.title || "";
    $("editContent").value = item.content || item.text || "";
    show($("fail-closed"), false);
    show($("detail-panel"), true);
  }

  async function openMemory(id) {
    if (!id) return;
    const { res, data } = await api("GET", "/mcpserver/memory/" + encodeURIComponent(id));
    if (res.status === 403 || res.status === 404) {
      show($("detail-panel"), true);
      show($("fail-closed"), true);
      $("memoryId").value = id;
      $("editTitle").value = "";
      $("editContent").value = "";
      setStatus("Opened foreign or unknown id fail-closed (" + res.status + ").");
      return;
    }
    if (!res.ok) {
      setStatus("Open request failed: " + res.status);
      return;
    }
    fillDetail(data.memory || data);
  }

  async function createMemory() {
    const content = $("content").value.trim();
    if (!content) {
      setStatus("Content is required");
      return;
    }
    const { res, data } = await api("POST", "/mcpserver/memory/remember", {
      title: $("title").value.trim(),
      content,
      type: "fact",
      tags: parseTags($("tags").value),
    });
    if (!res.ok) {
      setStatus("Create request failed: " + res.status + " " + (data && data.error || ""));
      return;
    }
    setStatus(data);
    $("content").value = "";
    await listEffective();
    const createdId = data.memoryId || (data.memory && data.memory.id);
    if (createdId) await openMemory(createdId);
  }

  async function saveMemory() {
    if (!currentId) return;
    const content = $("editContent").value.trim();
    if (!content) {
      setStatus("Content is required");
      return;
    }
    const { res, data } = await api("PUT", "/mcpserver/memory/" + encodeURIComponent(currentId), {
      title: $("editTitle").value.trim(),
      content,
    });
    if (!res.ok) {
      setStatus("Save request failed: " + res.status);
      return;
    }
    setStatus(data);
    await listEffective();
  }

  async function loadVersions() {
    if (!currentId) return;
    const { res, data } = await api("GET", "/mcpserver/memory/" + encodeURIComponent(currentId) + "/versions");
    if (!res.ok) {
      setStatus("Versions request failed: " + res.status);
      return;
    }
    const items = data.items || data.Items || [];
    const ul = $("version-list");
    ul.innerHTML = "";
    selectedVersion = null;
    items.forEach((item) => {
      const li = document.createElement("li");
      li.textContent = "v" + item.versionNumber + " " + (item.title || "");
      li.onclick = () => {
        selectedVersion = item.versionNumber;
        Array.from(ul.children).forEach((node) => node.classList.remove("active"));
        li.classList.add("active");
      };
      ul.appendChild(li);
    });
    show($("version-panel"), true);
  }

  async function revertMemory() {
    if (!currentId || !selectedVersion) {
      setStatus("Select a version, then revert.");
      return;
    }
    const { res, data } = await api("POST", "/mcpserver/memory/" + encodeURIComponent(currentId) + "/revert", {
      versionNumber: selectedVersion,
    });
    if (!res.ok) {
      setStatus("Revert request failed: " + res.status);
      return;
    }
    setStatus(data);
    await openMemory(currentId);
    await loadVersions();
  }

  function deepLink() {
    const path = location.pathname || "";
    const marker = "/memory/";
    const idx = path.toLowerCase().indexOf(marker);
    if (idx < 0) return;
    const id = decodeURIComponent(path.slice(idx + marker.length).split("/")[0]);
    if (id && id.toUpperCase().startsWith("MEMORY-"))
      openMemory(id);
  }

  $("btnList").onclick = listEffective;
  $("btnSearch").onclick = search;
  $("btnCreate").onclick = createMemory;
  $("btnSave").onclick = saveMemory;
  $("btnVersions").onclick = loadVersions;
  $("btnRevert").onclick = revertMemory;
  $("btnClose").onclick = closePanel;
  $("tagFilter").addEventListener("input", () => renderTagChips(parseTags($("tagFilter").value)));
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closePanel();
  });

  renderTagChips([]);
  deepLink();
})();
