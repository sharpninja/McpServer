/**
 * New coverage for discovery/marker-resolver.ts. The cline-v2 plugin shipped
 * no marker-resolver spec (marker trust was only exercised end-to-end in the
 * host repos). The canonical core owns the resolver, so its file walk and
 * field-parsing contract (mirrors lib/marker-resolver.sh) gets direct tests.
 * Signature verification and fullBootstrap's live /health nonce check stay
 * uncovered here on purpose: they require a running server and belong to the
 * host repos' integration suites.
 */
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { findMarkerFile, parseMarkerField } from '../src/discovery/marker-resolver.js';

const MARKER = 'AGENTS-README-FIRST.yaml';

describe('findMarkerFile', () => {
  let root: string;

  beforeEach(() => {
    root = fs.mkdtempSync(path.join(os.tmpdir(), 'core-marker-'));
  });

  afterEach(() => {
    fs.rmSync(root, { recursive: true, force: true });
  });

  test('returns the marker path when present in the start directory', () => {
    const marker = path.join(root, MARKER);
    fs.writeFileSync(marker, 'baseUrl: http://127.0.0.1:8765\n');
    expect(findMarkerFile(root)).toBe(marker);
  });

  test('walks up parent directories until it finds the marker', () => {
    const marker = path.join(root, MARKER);
    fs.writeFileSync(marker, 'baseUrl: http://127.0.0.1:8765\n');
    const nested = path.join(root, 'a', 'b', 'c');
    fs.mkdirSync(nested, { recursive: true });
    expect(findMarkerFile(nested)).toBe(marker);
  });

  test('returns null when no marker exists up the tree', () => {
    const nested = path.join(root, 'x', 'y');
    fs.mkdirSync(nested, { recursive: true });
    // The repo root itself has a marker; resolve through a path that cannot
    // reach it by using an isolated temp dir (above) with no marker written.
    expect(findMarkerFile(nested)).toBeNull();
  });
});

describe('parseMarkerField', () => {
  let root: string;
  let marker: string;

  beforeEach(() => {
    root = fs.mkdtempSync(path.join(os.tmpdir(), 'core-marker-field-'));
    marker = path.join(root, MARKER);
    fs.writeFileSync(
      marker,
      [
        'baseUrl: http://127.0.0.1:8765',
        "apiKey: 'secret-key-123'",
        'workspace: McpServer',
        'workspacePath: F:\\GitHub\\McpServer',
        'port: 8765',
        'endpoints:',
        '  todo: /mcpserver/todo',
        '  sessionlog: /mcpserver/sessionlog',
        'signature:',
        '  value: ABC123',
        '',
      ].join('\n'),
    );
  });

  afterEach(() => {
    fs.rmSync(root, { recursive: true, force: true });
  });

  test('reads top-level fields and strips surrounding quotes', () => {
    expect(parseMarkerField(marker, 'baseUrl')).toBe('http://127.0.0.1:8765');
    expect(parseMarkerField(marker, 'apiKey')).toBe('secret-key-123');
    expect(parseMarkerField(marker, 'workspace')).toBe('McpServer');
    expect(parseMarkerField(marker, 'workspacePath')).toBe('F:\\GitHub\\McpServer');
    expect(parseMarkerField(marker, 'port')).toBe('8765');
  });

  test('reads fields nested under endpoints:', () => {
    expect(parseMarkerField(marker, 'todo')).toBe('/mcpserver/todo');
    expect(parseMarkerField(marker, 'sessionlog')).toBe('/mcpserver/sessionlog');
  });

  test('returns null for fields that are not present', () => {
    expect(parseMarkerField(marker, 'doesNotExist')).toBeNull();
  });
});
