-- Sanitized OpenCode SQLite snapshot schema fixture. Tests must copy the database before reading.
CREATE TABLE session (id TEXT PRIMARY KEY, workspace_path TEXT, created_at TEXT, model TEXT);
CREATE TABLE message (id TEXT PRIMARY KEY, session_id TEXT NOT NULL, role TEXT NOT NULL, created_at TEXT NOT NULL, content_json TEXT NOT NULL);
CREATE TABLE tool_event (id TEXT PRIMARY KEY, session_id TEXT NOT NULL, message_id TEXT, tool_name TEXT, status TEXT, payload_json TEXT);
