# McpServer MCP Todo

Shows TODO items from the FunWasHad MCP server in the Explorer sidebar.

- **View:** Explorer → **MCP Todo**
- **Data:** Fetched from `GET /mcpserver/todo` (default base URL: `http://localhost:7147`). Ensure the MCP server is running (e.g. `.\scripts\Start-McpServer.ps1`).
- **List:** Each row is a todo item showing its **ID** at all times; items are **collapsed by default**. Expand a row to see the body (title, description, technical details, implementation tasks, etc.).
- **Copy ID:** Double-click a todo row (or use the context menu **McpServer MCP Todo: Copy ID to clipboard**) to copy that item’s ID to the clipboard.
- **Refresh:** Use the refresh icon on the view title bar or the context menu to reload the list.

## Configuration

- **`fwhMcpTodo.mcpBaseUrl`** — Base URL of the MCP server (default: `http://localhost:7147`).

## Running the extension (avoid dev host crash)

Cursor’s **Extension Development Host** (the window opened by F5 “Launch Extension”) starts the extension host as **LocalWebWorker**, which currently fails with an unspecified error in the logs. The extension itself does not load any heavy modules at startup, but the dev host window still closes.

**Use the extension in this window instead:**

1. **Compile:** From repo root, `cd extensions\fwh-mcp-todo && npm run compile`
2. **Install from folder:** Command Palette (**Ctrl+Shift+P**) → **Developer: Install Extension from Location...**
3. Select the folder: `e:\github\FunWasHad\extensions\fwh-mcp-todo`
4. Reload the window when prompted. The extension loads in the **current** window’s extension host (which works).
5. Open **Explorer → MCP Todo**, then click **Refresh (⟳)** to load items. Ensure the MCP server is running at `http://localhost:7147`.

You can still use **F5 → McpServer MCP Todo (Extension)** to debug; if that window keeps closing, use “Install from Location” above.

## Troubleshooting / logs

- **Logs:** Cursor logs are under `%APPDATA%\Cursor\logs\<timestamp>\`.  
  Each **window** has a `renderer.log`. The Extension Development Host is often **window2** (or the next free number). In that file you’ll see:
  - `Started local extension host with pid XXXXX`
  - `An unknown error occurred. Please consult the log for more details.: undefined`
  - `Error received from starting extension host (kind: LocalWebWorker)`  
  The real error is not written (undefined/empty), so the failure is in Cursor’s LocalWebWorker startup, not in this extension’s code.
