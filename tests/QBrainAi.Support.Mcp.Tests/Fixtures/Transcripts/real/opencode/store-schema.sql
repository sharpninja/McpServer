-- Sanitized schema inventory from OpenCode's current SQLite-backed store.
-- Integration tests must copy the source database before reading so WAL content is consistent.
CREATE TABLE session (
  id TEXT PRIMARY KEY,
  title TEXT,
  version TEXT,
  time_created INTEGER,
  time_updated INTEGER
);

CREATE TABLE message (
  id TEXT PRIMARY KEY,
  session_id TEXT NOT NULL,
  role TEXT NOT NULL,
  model_id TEXT,
  provider_id TEXT,
  time_created INTEGER,
  time_completed INTEGER
);

CREATE TABLE part (
  id TEXT PRIMARY KEY,
  message_id TEXT NOT NULL,
  session_id TEXT NOT NULL,
  type TEXT NOT NULL,
  json TEXT NOT NULL
);