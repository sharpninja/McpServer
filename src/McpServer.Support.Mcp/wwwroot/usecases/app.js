/* First-party Use Case UI — all mutations via REST /mcpserver/usecases */
(function () {
  const $ = (id) => document.getElementById(id);
  let currentDetail = null;
  let canvasEditor = null;

  function ensureCanvas() {
    if (canvasEditor) return canvasEditor;
    const el = $("umlCanvas");
    if (!el || !window.UseCaseCanvasEditor) return null;
    canvasEditor = window.UseCaseCanvasEditor.createEditor(el);
    return canvasEditor;
  }

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
    if (!res.ok) {
      const err = (data && (data.error || data.title)) || text || res.statusText;
      throw new Error(res.status + " " + err);
    }
    return data;
  }

  function setStatus(msg) {
    $("status").textContent = typeof msg === "string" ? msg : JSON.stringify(msg, null, 2);
  }

  function requireId() {
    const id = $("ucId").value.trim();
    if (!id) throw new Error("Select or create a use case first.");
    return id;
  }

  function fillHeader(detail) {
    currentDetail = detail;
    $("ucId").value = detail.useCaseId;
    $("title").value = detail.title || "";
    $("brief").value = detail.briefDescription || "";
    $("approval").value = detail.approvalStatus || "Draft";
    $("productKey").value = detail.productKey || "";
  }

  function renderList(panelId, items, formatter) {
    const ul = $(panelId);
    ul.innerHTML = "";
    (items || []).forEach((item) => {
      const li = document.createElement("li");
      li.textContent = formatter(item);
      if (item.flowId && panelId === "flowsPanel") {
        li.style.cursor = "pointer";
        li.onclick = () => {
          $("targetFlowId").value = String(item.flowId);
          setStatus({ selectedFlowId: item.flowId });
        };
      }
      ul.appendChild(li);
    });
    if (!(items || []).length) {
      const li = document.createElement("li");
      li.className = "muted";
      li.textContent = "(none)";
      ul.appendChild(li);
    }
  }

  function refreshStructure(detail) {
    if (!detail) {
      renderList("actorsPanel", [], () => "");
      renderList("flowsPanel", [], () => "");
      renderList("stepsPanel", [], () => "");
      renderList("linksPanel", [], () => "");
      return;
    }
    currentDetail = detail;
    const actors = detail.actors || detail.useCaseActors || [];
    renderList("actorsPanel", actors, (a) => {
      const name = a.name || a.actorName || ("Actor " + (a.actorId || ""));
      const type = a.type || a.actorType || "";
      return (a.actorId || "") + ": " + name + (type ? " [" + type + "]" : "");
    });

    const flows = detail.flows || [];
    renderList("flowsPanel", flows, (f) => {
      const name = f.name || f.flowType || "flow";
      return (f.flowId || "") + ": " + name + " (" + (f.flowType || "") + ", steps=" + ((f.steps || []).length) + ")";
    });

    const steps = [];
    flows.forEach((f) => {
      (f.steps || []).forEach((s) => {
        steps.push({
          flowId: f.flowId,
          stepId: s.stepId,
          stepNumber: s.stepNumber,
          action: s.action,
          systemResponse: s.systemResponse,
        });
      });
    });
    renderList("stepsPanel", steps, (s) =>
      "flow " + s.flowId + " #" + (s.stepNumber || "?") + ": " + (s.action || "")
      + (s.systemResponse ? " → " + s.systemResponse : ""));

    const links = detail.frLinks || detail.links || [];
    renderList("linksPanel", links, (l) =>
      (l.frId || "") + " [" + (l.linkType || "Realizes") + "]");

    if (flows.length && !$("targetFlowId").value) {
      $("targetFlowId").value = String(flows[0].flowId);
    }
  }

  async function renderDiagram(source, format) {
    const view = $("diagramView");
    $("diagramSource").value = source || "";
    view.innerHTML = "";

    if (!source) {
      view.textContent = "(no diagram content)";
      return;
    }

    if ((format || "mermaid") !== "mermaid") {
      const pre = document.createElement("pre");
      pre.textContent = source;
      view.appendChild(pre);
      return;
    }

    // Wait briefly for mermaid ESM module if still loading
    let mermaid = window.__mermaid;
    for (let i = 0; i < 20 && !mermaid; i++) {
      await new Promise((r) => setTimeout(r, 50));
      mermaid = window.__mermaid;
    }
    if (!mermaid) {
      const pre = document.createElement("pre");
      pre.textContent = source + "\n\n(mermaid renderer not loaded; showing source)";
      view.appendChild(pre);
      return;
    }

    const host = document.createElement("div");
    host.className = "mermaid";
    host.textContent = source;
    view.appendChild(host);
    try {
      await mermaid.run({ nodes: [host] });
    } catch (err) {
      view.innerHTML = "";
      const pre = document.createElement("pre");
      pre.textContent = source + "\n\n(render error: " + (err && err.message ? err.message : err) + ")";
      view.appendChild(pre);
    }
  }

  async function listUseCases() {
    const items = await api("GET", "/mcpserver/usecases");
    const ul = $("list");
    ul.innerHTML = "";
    (items || []).forEach((item) => {
      const li = document.createElement("li");
      li.textContent = item.useCaseId + ": " + item.title;
      li.onclick = () => {
        Array.from(ul.children).forEach((c) => c.classList.remove("active"));
        li.classList.add("active");
        loadUseCase(item.useCaseId);
      };
      ul.appendChild(li);
    });
    setStatus({ count: (items || []).length, items });
  }

  async function loadUseCase(id) {
    const detail = await api("GET", "/mcpserver/usecases/" + id);
    fillHeader(detail);
    refreshStructure(detail);
    setStatus(detail);
    try {
      await loadDiagramGraph();
    } catch (e) {
      setStatus(String(e && e.message ? e.message : e));
    }
  }

  async function createUseCase() {
    const detail = await api("POST", "/mcpserver/usecases", {
      title: $("title").value,
      briefDescription: $("brief").value || null,
      createBasicFlow: true,
    });
    fillHeader(detail);
    refreshStructure(detail);
    setStatus(detail);
    await listUseCases();
    await loadDiagram();
  }

  async function saveHeader() {
    const id = requireId();
    const detail = await api("PUT", "/mcpserver/usecases/" + id, {
      title: $("title").value,
      briefDescription: $("brief").value || null,
    });
    fillHeader(detail);
    refreshStructure(detail);
    setStatus(detail);
    await listUseCases();
  }

  async function setApproval() {
    const id = requireId();
    const detail = await api("POST", "/mcpserver/usecases/" + id + "/approval", {
      status: $("approval").value,
    });
    fillHeader(detail);
    setStatus(detail);
  }

  async function setProduct() {
    const id = requireId();
    const detail = await api("POST", "/mcpserver/usecases/" + id + "/product", {
      productKey: $("productKey").value || null,
    });
    fillHeader(detail);
    setStatus(detail);
  }

  async function attachActor() {
    const id = requireId();
    const name = $("actorName").value.trim();
    if (!name) throw new Error("Actor name is required.");
    await api("POST", "/mcpserver/usecases/" + id + "/actors", {
      name,
      type: $("actorType").value || "Primary",
      isPrimary: ($("actorType").value || "Primary") === "Primary",
    });
    const detail = await api("GET", "/mcpserver/usecases/" + id);
    fillHeader(detail);
    refreshStructure(detail);
    setStatus(detail);
    await loadDiagram();
  }

  async function addFlow() {
    const id = requireId();
    const flow = await api("POST", "/mcpserver/usecases/" + id + "/flows", {
      flowType: $("flowType").value || "Basic",
      name: $("flowName").value || null,
    });
    $("targetFlowId").value = String(flow.flowId);
    const detail = await api("GET", "/mcpserver/usecases/" + id);
    fillHeader(detail);
    refreshStructure(detail);
    setStatus({ addedFlow: flow, detail });
    await loadDiagram();
  }

  async function addStep() {
    const id = requireId();
    const flowId = $("targetFlowId").value.trim();
    if (!flowId) throw new Error("Target flow id is required (click a flow or add one).");
    const action = $("stepAction").value.trim();
    if (!action) throw new Error("Step action is required.");
    const step = await api("POST", "/mcpserver/usecases/" + id + "/flows/" + flowId + "/steps", {
      action,
      systemResponse: $("stepResponse").value.trim() || null,
    });
    $("stepAction").value = "";
    $("stepResponse").value = "";
    const detail = await api("GET", "/mcpserver/usecases/" + id);
    fillHeader(detail);
    refreshStructure(detail);
    setStatus({ addedStep: step, detail });
    await loadDiagram();
  }

  async function linkFr() {
    const id = requireId();
    const frId = $("frId").value.trim();
    if (!frId) throw new Error("FR id is required.");
    const link = await api("POST", "/mcpserver/usecases/" + id + "/links", {
      frId,
      linkType: $("linkType").value || "Realizes",
      linkOrder: 0,
    });
    const detail = await api("GET", "/mcpserver/usecases/" + id);
    fillHeader(detail);
    refreshStructure(detail);
    setStatus({ linked: link, detail });
  }

  async function loadDiagram() {
    const id = requireId();
    const format = $("diagramFormat").value || "mermaid";
    const diagram = await api(
      "GET",
      "/mcpserver/usecases/" + id + "/diagram?kind=sequence&format=" + encodeURIComponent(format));
    await renderDiagram(diagram.content || "", diagram.format || format);
    setStatus(diagram);
  }

  async function loadDiagramGraph() {
    const id = requireId();
    const graph = await api("GET", "/mcpserver/usecases/" + id + "/diagram-graph");
    const ed = ensureCanvas();
    if (ed) ed.fromGraph(graph);
    setStatus(graph);
  }

  async function saveDiagramGraph() {
    const id = requireId();
    const ed = ensureCanvas();
    if (!ed) throw new Error("Canvas editor not ready.");
    const graph = ed.toGraph();
    const saved = await api("PUT", "/mcpserver/usecases/" + id + "/diagram-graph", graph);
    if (ed) ed.fromGraph(saved);
    setStatus(saved);
  }

  async function exportUmlDiagram(format) {
    const id = requireId();
    const diagram = await api(
      "GET",
      "/mcpserver/usecases/" + id + "/diagram?kind=usecase&format=" + encodeURIComponent(format || "mermaid"));
    await renderDiagram(diagram.content || "", diagram.format || format || "mermaid");
    setStatus(diagram);
  }

  async function reloadStructure() {
    const id = requireId();
    const detail = await api("GET", "/mcpserver/usecases/" + id);
    fillHeader(detail);
    refreshStructure(detail);
    setStatus(detail);
    try { await loadDiagramGraph(); } catch (e) { /* graph optional until created */ }
  }

  async function coverage() {
    const data = await api("GET", "/mcpserver/usecases/coverage");
    setStatus(data);
  }

  function wire(id, fn) {
    const el = $(id);
    if (!el) return;
    el.onclick = async () => {
      try { await fn(); }
      catch (e) { setStatus(String(e && e.message ? e.message : e)); }
    };
  }

  function wirePalette() {
    document.querySelectorAll(".palette button[data-mode]").forEach((btn) => {
      btn.addEventListener("click", () => {
        document.querySelectorAll(".palette button").forEach((b) => b.classList.remove("active"));
        btn.classList.add("active");
        const ed = ensureCanvas();
        if (!ed) return;
        const mode = btn.getAttribute("data-mode");
        const edge = btn.getAttribute("data-edge");
        ed.setMode(mode, edge || undefined);
      });
    });
  }

  wire("btnList", listUseCases);
  wire("btnCreate", createUseCase);
  wire("btnSave", saveHeader);
  wire("btnApproval", setApproval);
  wire("btnProduct", setProduct);
  wire("btnDiagram", loadDiagram);
  wire("btnCoverage", coverage);
  wire("btnAttachActor", attachActor);
  wire("btnAddFlow", addFlow);
  wire("btnAddStep", addStep);
  wire("btnLinkFr", linkFr);
  wire("btnRefreshStructure", reloadStructure);
  wire("btnSaveGraph", saveDiagramGraph);
  wire("btnLoadGraph", loadDiagramGraph);
  wire("btnExportUmlMermaid", () => exportUmlDiagram("mermaid"));
  wire("btnExportUmlPlantuml", () => exportUmlDiagram("plantuml"));

  wirePalette();
  ensureCanvas();

  // Expose for structural tests / debugging
  window.UseCaseUi = {
    listUseCases,
    createUseCase,
    loadDiagram,
    loadDiagramGraph,
    saveDiagramGraph,
    renderDiagram,
    refreshStructure,
    coverage,
    attachActor,
    addFlow,
    addStep,
    linkFr,
    api,
  };
})();
