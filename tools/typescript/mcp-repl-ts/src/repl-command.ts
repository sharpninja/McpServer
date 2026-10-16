import { existsSync } from 'fs';
import { delimiter, join } from 'path';

/**
 * Prefer an explicit override, then qbrain-ai-repl, then the 1.x mcpserver-repl command.
 * Does not download either tool.
 */
export function resolveReplCommand(): string {
  const explicit = (process.env.MCPSERVER_REPL_BIN || process.env.MCPSERVER_REPL_COMMAND || '').trim();
  if (explicit) return explicit;
  return findInstalledRepl('qbrain-ai-repl') || findInstalledRepl('mcpserver-repl') || 'qbrain-ai-repl';
}

function findInstalledRepl(name: string): string | undefined {
  const entries = (process.env.PATH || '').split(delimiter).filter(Boolean);
  const extensions = process.platform === 'win32' ? ['.cmd', '.exe', '.bat', ''] : [''];
  for (const dir of entries) {
    for (const ext of extensions) {
      if (existsSync(join(dir, name + ext))) return name;
    }
  }
  return undefined;
}
