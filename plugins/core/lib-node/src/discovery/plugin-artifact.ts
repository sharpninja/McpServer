// SPDX-License-Identifier: Apache-2.0
import { createHash, timingSafeEqual } from 'crypto';
import * as fs from 'fs';
import { isAbsolute } from 'path';
import type { RequiredAgentPluginBinding } from './agent-plugin-contract.js';

/** A host-owned review pin, never a value learned from a marker or model input. */
export interface PinnedPluginArtifactOptions {
  readonly artifactPath: string;
  readonly binding: RequiredAgentPluginBinding;
  /** Built JavaScript plugin archive limit; REPL distributions are separate artifacts. */
  readonly maxBytes?: number;
}

/** Consume copies of these verified bytes for extraction; do not reopen the source path. */
export interface VerifiedPluginArtifact {
  readonly binding: Readonly<RequiredAgentPluginBinding>;
  readonly byteLength: number;
  readBytes(): Buffer;
}

/**
 * TR-QB-TRUST-001: verify an exact reviewed archive before any extraction or execution.
 * Capturing bytes prevents a subsequent path replacement from changing what the
 * installer consumes. This does not validate an already-extracted installation.
 */
export function verifyPinnedPluginArtifact(options: PinnedPluginArtifactOptions): VerifiedPluginArtifact {
  let descriptor: number | undefined;
  try {
    const pin = options.binding;
    if (!pin || typeof pin.agentName !== 'string' || !pin.agentName
      || typeof pin.pluginName !== 'string' || !pin.pluginName
      || typeof pin.pluginVersion !== 'string' || !pin.pluginVersion
      || typeof pin.artifactSha256 !== 'string' || pin.artifactSha256.length !== 64
      || !/^[A-Fa-f0-9]{64}$/.test(pin.artifactSha256)) throw new Error();
    const binding = Object.freeze({ agentName: pin.agentName, pluginName: pin.pluginName,
      pluginVersion: pin.pluginVersion, artifactSha256: pin.artifactSha256.toUpperCase() });
    const maximum = options.maxBytes ?? 64 * 1024 * 1024;
    if (!Number.isSafeInteger(maximum) || maximum < 1 || maximum > 512 * 1024 * 1024
      || typeof options.artifactPath !== 'string' || !isAbsolute(options.artifactPath)) throw new Error();
    // Refuse special files before opening; nonblocking/no-follow also bound replacement races.
    if (!fs.lstatSync(options.artifactPath).isFile()) throw new Error();
    descriptor = fs.openSync(options.artifactPath, fs.constants.O_RDONLY
      | (fs.constants.O_NOFOLLOW ?? 0) | (fs.constants.O_NONBLOCK ?? 0));
    const stat = fs.fstatSync(descriptor);
    if (!stat.isFile() || !Number.isSafeInteger(stat.size) || stat.size < 1 || stat.size > maximum) throw new Error();
    const bytes = Buffer.alloc(stat.size);
    let offset = 0;
    while (offset < bytes.length) {
      const read = fs.readSync(descriptor, bytes, offset, bytes.length - offset, null);
      if (read === 0) throw new Error();
      offset += read;
    }
    if (fs.readSync(descriptor, Buffer.alloc(1), 0, 1, null) !== 0) throw new Error();
    const actual = createHash('sha256').update(bytes).digest();
    if (!timingSafeEqual(actual, Buffer.from(binding.artifactSha256, 'hex'))) throw new Error();
    return Object.freeze({ binding, byteLength: bytes.length, readBytes: () => Buffer.from(bytes) });
  } catch {
    // No path contents, credentials or parser/native diagnostic details in caller-facing failures.
    throw new Error('MCP_UNTRUSTED: Bundled plugin artifact does not match its reviewed pin');
  } finally {
    if (descriptor !== undefined) fs.closeSync(descriptor);
  }
}
