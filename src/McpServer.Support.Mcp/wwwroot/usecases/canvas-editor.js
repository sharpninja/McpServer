/* UML use-case canvas editor: palette + free canvas + drag/drop + graph serialize.
   FR-MCP-USECASE-011. Interaction model inspired by classic UML tools / JointJS demos.
   Persistence via REST graph only (wired by app.js). */
(function (global) {
  "use strict";

  const NODE_R = { actor: 28, usecase: { w: 110, h: 44 } };

  function createEditor(svgEl) {
    if (!svgEl) throw new Error("umlCanvas element required");

    const state = {
      nodes: [],
      edges: [],
      boundary: null,
      selectedId: null,
      mode: "select",
      connectFrom: null,
      edgeType: "association",
      drag: null,
      seq: 1,
    };

    const ns = "http://www.w3.org/2000/svg";

    function nextId(prefix) {
      return prefix + (state.seq++);
    }

    function clearSvg() {
      while (svgEl.firstChild) svgEl.removeChild(svgEl.firstChild);
    }

    function svg(name, attrs) {
      const el = document.createElementNS(ns, name);
      if (attrs) {
        Object.keys(attrs).forEach((k) => el.setAttribute(k, String(attrs[k])));
      }
      return el;
    }

    function render() {
      clearSvg();
      const gRoot = svg("g", { id: "canvas-root" });

      if (state.boundary) {
        const b = state.boundary;
        const rect = svg("rect", {
          x: b.x, y: b.y, width: b.width, height: b.height,
          fill: "#f8fafc", stroke: "#334155", "stroke-width": 2,
          rx: 4, class: "boundary",
        });
        const label = svg("text", {
          x: b.x + 8, y: b.y + 18, fill: "#334155", "font-size": 13, "font-family": "system-ui,sans-serif",
        });
        label.textContent = b.label || "System";
        gRoot.appendChild(rect);
        gRoot.appendChild(label);
      }

      // edges under nodes
      state.edges.forEach((e) => {
        const a = state.nodes.find((n) => n.id === e.source);
        const b = state.nodes.find((n) => n.id === e.target);
        if (!a || !b) return;
        const x1 = a.x, y1 = a.y, x2 = b.x, y2 = b.y;
        const isDashed = e.type === "include" || e.type === "extend";
        const line = svg("line", {
          x1, y1, x2, y2,
          stroke: "#0f172a",
          "stroke-width": 1.5,
          "stroke-dasharray": isDashed ? "6 4" : "none",
          class: "edge",
        });
        gRoot.appendChild(line);
        if (e.type === "include" || e.type === "extend") {
          const mx = (x1 + x2) / 2, my = (y1 + y2) / 2;
          const t = svg("text", {
            x: mx, y: my - 4, fill: "#475569", "font-size": 11, "text-anchor": "middle",
            "font-family": "system-ui,sans-serif",
          });
          t.textContent = "«" + e.type + "»";
          gRoot.appendChild(t);
        }
      });

      state.nodes.forEach((n) => {
        const g = svg("g", {
          class: "node " + n.type + (state.selectedId === n.id ? " selected" : ""),
          "data-id": n.id,
          transform: "translate(" + n.x + "," + n.y + ")",
          style: "cursor:move",
        });

        if (n.type === "actor") {
          // stick figure
          g.appendChild(svg("circle", { cx: 0, cy: -18, r: 8, fill: "#fff", stroke: "#0f172a", "stroke-width": 1.5 }));
          g.appendChild(svg("line", { x1: 0, y1: -10, x2: 0, y2: 8, stroke: "#0f172a", "stroke-width": 1.5 }));
          g.appendChild(svg("line", { x1: -12, y1: -2, x2: 12, y2: -2, stroke: "#0f172a", "stroke-width": 1.5 }));
          g.appendChild(svg("line", { x1: 0, y1: 8, x2: -10, y2: 22, stroke: "#0f172a", "stroke-width": 1.5 }));
          g.appendChild(svg("line", { x1: 0, y1: 8, x2: 10, y2: 22, stroke: "#0f172a", "stroke-width": 1.5 }));
          const t = svg("text", {
            x: 0, y: 36, "text-anchor": "middle", fill: "#0f172a", "font-size": 12,
            "font-family": "system-ui,sans-serif",
          });
          t.textContent = n.label || "Actor";
          g.appendChild(t);
        } else {
          const w = NODE_R.usecase.w, h = NODE_R.usecase.h;
          g.appendChild(svg("ellipse", {
            cx: 0, cy: 0, rx: w / 2, ry: h / 2,
            fill: "#fff", stroke: state.selectedId === n.id ? "#1f6feb" : "#0f172a",
            "stroke-width": state.selectedId === n.id ? 2.5 : 1.5,
          }));
          const t = svg("text", {
            x: 0, y: 4, "text-anchor": "middle", fill: "#0f172a", "font-size": 12,
            "font-family": "system-ui,sans-serif",
          });
          t.textContent = n.label || "Use Case";
          g.appendChild(t);
        }

        g.addEventListener("mousedown", onNodeMouseDown);
        g.addEventListener("dblclick", onNodeDblClick);
        gRoot.appendChild(g);
      });

      svgEl.appendChild(gRoot);
    }

    function setMode(mode, edgeType) {
      state.mode = mode || "select";
      if (edgeType) state.edgeType = edgeType;
      state.connectFrom = null;
      svgEl.style.cursor = mode && mode.indexOf("place") === 0 ? "crosshair" : "default";
    }

    function placeNode(type, x, y, label) {
      const id = nextId(type === "actor" ? "a" : "uc");
      const node = {
        id,
        type: type === "actor" ? "actor" : "usecase",
        label: label || (type === "actor" ? "Actor" : "Use Case"),
        x: x,
        y: y,
      };
      state.nodes.push(node);
      state.selectedId = id;
      render();
      return node;
    }

    function placeBoundary(x, y) {
      state.boundary = {
        id: "sb1",
        label: "System",
        x: x - 80,
        y: y - 40,
        width: 360,
        height: 260,
      };
      render();
      return state.boundary;
    }

    function startConnect(nodeId) {
      state.connectFrom = nodeId;
    }

    function completeConnect(targetId, edgeType) {
      if (!state.connectFrom || state.connectFrom === targetId) {
        state.connectFrom = null;
        return null;
      }
      const type = edgeType || state.edgeType || "association";
      const edge = {
        id: nextId("e"),
        type: type,
        source: state.connectFrom,
        target: targetId,
      };
      state.edges.push(edge);
      state.connectFrom = null;
      render();
      return edge;
    }

    function renameSelected(label) {
      if (!state.selectedId) return false;
      const n = state.nodes.find((x) => x.id === state.selectedId);
      if (!n) return false;
      n.label = label || n.label;
      render();
      return true;
    }

    function moveNode(id, x, y) {
      const n = state.nodes.find((x) => x.id === id);
      if (!n) return false;
      n.x = x;
      n.y = y;
      render();
      return true;
    }

    function onNodeMouseDown(ev) {
      ev.stopPropagation();
      const g = ev.currentTarget;
      const id = g.getAttribute("data-id");
      state.selectedId = id;

      if (state.mode === "connect" || state.mode.indexOf("connect-") === 0) {
        if (!state.connectFrom) {
          startConnect(id);
        } else {
          const et = state.mode === "connect-include" ? "include"
            : state.mode === "connect-extend" ? "extend"
            : state.edgeType || "association";
          completeConnect(id, et);
        }
        render();
        return;
      }

      const pt = clientToSvg(ev.clientX, ev.clientY);
      const n = state.nodes.find((x) => x.id === id);
      state.drag = { id, ox: pt.x - n.x, oy: pt.y - n.y };
      window.addEventListener("mousemove", onDragMove);
      window.addEventListener("mouseup", onDragEnd);
    }

    function onDragMove(ev) {
      if (!state.drag) return;
      const pt = clientToSvg(ev.clientX, ev.clientY);
      moveNode(state.drag.id, pt.x - state.drag.ox, pt.y - state.drag.oy);
    }

    function onDragEnd() {
      state.drag = null;
      window.removeEventListener("mousemove", onDragMove);
      window.removeEventListener("mouseup", onDragEnd);
    }

    function onNodeDblClick(ev) {
      ev.stopPropagation();
      const id = ev.currentTarget.getAttribute("data-id");
      const n = state.nodes.find((x) => x.id === id);
      if (!n) return;
      const next = window.prompt("Rename shape", n.label || "");
      if (next !== null) {
        state.selectedId = id;
        renameSelected(next.trim() || n.label);
      }
    }

    function clientToSvg(cx, cy) {
      const rect = svgEl.getBoundingClientRect();
      const vb = svgEl.viewBox && svgEl.viewBox.baseVal;
      const sx = vb && vb.width ? vb.width / rect.width : 1;
      const sy = vb && vb.height ? vb.height / rect.height : 1;
      return {
        x: (cx - rect.left) * sx + (vb ? vb.x : 0),
        y: (cy - rect.top) * sy + (vb ? vb.y : 0),
      };
    }

    function onCanvasClick(ev) {
      if (ev.target !== svgEl && ev.target.id !== "canvas-root" && ev.target.tagName !== "svg") {
        // allow clicks on empty boundary area for place mode
        if (state.mode.indexOf("place") !== 0) return;
      }
      if (state.mode.indexOf("place") !== 0) return;
      const pt = clientToSvg(ev.clientX, ev.clientY);
      if (state.mode === "place-actor") placeNode("actor", pt.x, pt.y);
      else if (state.mode === "place-usecase") placeNode("usecase", pt.x, pt.y);
      else if (state.mode === "place-boundary") placeBoundary(pt.x, pt.y);
    }

    svgEl.addEventListener("click", onCanvasClick);

    function toGraph() {
      const nodes = state.nodes.map((n) => ({
        id: n.id,
        type: n.type,
        label: n.label,
        x: n.x,
        y: n.y,
      }));
      const edges = state.edges.map((e) => ({
        id: e.id,
        type: e.type,
        source: e.source,
        target: e.target,
      }));
      return {
        schemaVersion: 1,
        kind: "uml-usecase",
        systemBoundary: state.boundary
          ? {
              id: state.boundary.id,
              label: state.boundary.label,
              x: state.boundary.x,
              y: state.boundary.y,
              width: state.boundary.width,
              height: state.boundary.height,
            }
          : null,
        nodes: nodes,
        edges: edges,
      };
    }

    function fromGraph(graph) {
      state.nodes = [];
      state.edges = [];
      state.boundary = null;
      state.selectedId = null;
      state.seq = 1;
      if (!graph) {
        render();
        return;
      }
      if (graph.systemBoundary) {
        state.boundary = {
          id: graph.systemBoundary.id || "sb1",
          label: graph.systemBoundary.label || "System",
          x: Number(graph.systemBoundary.x) || 120,
          y: Number(graph.systemBoundary.y) || 60,
          width: Number(graph.systemBoundary.width) || 360,
          height: Number(graph.systemBoundary.height) || 260,
        };
      }
      (graph.nodes || []).forEach((n) => {
        state.nodes.push({
          id: n.id,
          type: (n.type || "usecase").toLowerCase() === "actor" ? "actor" : "usecase",
          label: n.label || n.id,
          x: Number(n.x) || 100,
          y: Number(n.y) || 100,
        });
        const m = /([0-9]+)$/.exec(n.id || "");
        if (m) state.seq = Math.max(state.seq, parseInt(m[1], 10) + 1);
      });
      (graph.edges || []).forEach((e) => {
        state.edges.push({
          id: e.id,
          type: (e.type || "association").toLowerCase(),
          source: e.source,
          target: e.target,
        });
        const m = /([0-9]+)$/.exec(e.id || "");
        if (m) state.seq = Math.max(state.seq, parseInt(m[1], 10) + 1);
      });
      render();
    }

    // initial empty
    if (!svgEl.getAttribute("viewBox")) {
      svgEl.setAttribute("viewBox", "0 0 900 520");
    }
    render();

    return {
      setMode,
      placeNode,
      placeBoundary,
      startConnect,
      completeConnect,
      renameSelected,
      moveNode,
      toGraph,
      fromGraph,
      render,
      getState: () => state,
    };
  }

  global.UseCaseCanvasEditor = { createEditor };
})(window);
