# McpServer MCP Todo – Visual Studio extension (VSIX)

This is the **Visual Studio VSIX project** for the MCP Todo tool window. It uses the legacy (non-SDK) project format so that building in **Visual Studio** produces a valid VSIX.

## Building

**Recommended: build in Visual Studio**

1. Open `FunWasHad.sln` in Visual Studio 2022.
2. Set configuration to **Debug** or **Release**.
3. Build the project **McpServer.VsExtension.McpTodo.Vsix** (or build the solution).
4. The VSIX is produced at `bin\Debug\McpServer.VsExtension.McpTodo.vsix` or `bin\Release\...`.
5. Double-click the `.vsix` to install, or use **Extensions > Manage Extensions** to install from disk.

**From command line**

- Run `.\scripts\Build-AndInstall-Vsix.ps1` from the repo root (uses MSBuild; may require a first build from Visual Studio so that NuGet restore generates the VSSDK targets).

## After installing

In Visual Studio: **View > Other Windows > MCP Todo**. Ensure the MCP server is running (e.g. `.\scripts\Start-McpServer.ps1`) so the tool can load TODO items.
