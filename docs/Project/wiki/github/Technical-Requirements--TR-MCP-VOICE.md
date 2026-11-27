# TR-MCP-VOICE

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-VOICE-001

**Voice Conversation Service** — `VoiceConversationService` manages the full voice session lifecycle: session creation with `CopilotInteractiveSession` spawned via `DesktopProcessLauncher` (or standard `Process.Start`), turn processing with tool-call loop (max `MaxToolSteps` iterations), in-memory transcript storage, tool-call record tracking, and session cleanup. Configurable via `VoiceConversationOptions` bound from `Mcp:Voice` configuration section (model, timeouts, rate limits for writes/deletes per turn, transcript context limit).
**Covered by:** `VoiceConversationService`, `VoiceConversationOptions`, `CopilotInteractiveSession`
**Status:** pending
Scope: layer-1+

## TR-MCP-VOICE-002

**Voice Controller REST API** — `VoiceController` at `/mcpserver/voice/session/*` exposes 8 endpoints: `POST /` (create session with `DeviceId`/`Language`/`ClientName`), `GET /?deviceId=` (find by device), `POST /{id}/turn` (synchronous turn), `POST /{id}/turn/stream` (SSE streaming turn), `POST /{id}/interrupt` (cancel active turn), `POST /{id}/escape` (send ESC chars to Copilot stdin), `GET /{id}` (session status), `GET /{id}/transcript` (transcript entries), `DELETE /{id}` (destroy session). DTOs: `VoiceSessionCreateRequest/Response`, `VoiceTurnRequest/Response`, `VoiceInterruptResponse`, `VoiceSessionStatusDto`, `VoiceTranscriptEntryDto/Response`, `VoiceToolCallRecordDto`, `VoiceTurnStreamEvent`.
**Covered by:** `VoiceController`, `VoiceConversationContracts`
**Status:** pending
Scope: layer-1+

## TR-MCP-VOICE-003

**Voice Session Lifecycle Management** — One active session per device enforced via `DeviceId` lookup; creating a new session for a device with an active session returns the existing session. Idle timeout (`SessionIdleTimeoutMinutes`, default 15) triggers the configured idle-shutdown prompt sent to Copilot, waits for the configured sentinel response, then terminates the session. `UseDesktopLaunch` option (default true) selects `CreateProcessAsUser` for Windows service context.
**Covered by:** `VoiceConversationService.OnIdleCleanupTick` / `CleanupIdleSessionsAsync` (60s timer-driven idle-shutdown orchestrator), `VoiceConversationOptions.SessionIdleTimeoutMinutes`, `VoiceConversationOptions.IdleShutdownCommand` (config string sent to Copilot), `VoiceConversationOptions.IdleShutdownSentinel` (config string awaited as the shutdown confirmation). Note: `IdleShutdownCommand` and `IdleShutdownSentinel` are configured strings on `VoiceConversationOptions`, not CLR message types.
**Status:** pending
Scope: layer-1+

## TR-MCP-VOICE-004

**Interactive Presence Signals on Stream State Changes** — On interactive stream disconnect, the runtime SHALL send `User is AFK.` to the associated interactive agent session.
On interactive stream reconnect, after response stream establishment, the runtime SHALL send `User is here.` to the associated interactive agent session.

Presence signaling SHALL be excluded from one-shot sessions.

**Status:** 🔴 Planned

**Covered by:** `VoiceConversationService` *(planned extension)*, `AgentPoolStreamService` *(planned)*
Scope: layer-1+

