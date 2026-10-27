// SPDX-License-Identifier: Apache-2.0
import { createHash, timingSafeEqual } from 'crypto';
import { JSON_SCHEMA, load } from 'js-yaml';

/** TR-QB-TRUST-001: trusted host pin; never obtain it from model arguments or ambient environment. */
export interface RequiredAgentPluginBinding {
  readonly agentName: string;
  readonly pluginName: string;
  readonly pluginVersion: string;
  /** SHA-256 of the reviewed built plugin artifact, independent of the contract digest. */
  readonly artifactSha256: string;
}

/** Metadata returned only after bootstrap authenticates the snapshot and checks its complete contract map. */
export interface VerifiedAgentPluginContract extends RequiredAgentPluginBinding {
  readonly contractDigest: string;
}

const invalid = () => new Error('MCP_UNTRUSTED: Required agent plugin contract is missing or invalid');
const fields = [
  ['sourceType', 'source_type'], ['pluginName', 'plugin_name'], ['pluginVersion', 'plugin_version'],
  ['activation', 'activation'], ['startupCommand', 'startup_command'], ['unavailableFailure', 'unavailable_failure'],
  ['requiredEnvVars', 'required_env_vars'], ['hookExpectations', 'hook_expectations'],
  ['toolExpectations', 'tool_expectations'], ['rootHints', 'root_hints'],
] as const;
const arrayFields = new Set<string>(['required_env_vars', 'hook_expectations', 'tool_expectations', 'root_hints']);
function record(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw invalid();
  return value as Record<string, unknown>;
}
function text(value: unknown): string {
  if (typeof value !== 'string') throw invalid();
  return value;
}
function line(key: string, value: string): string {
  // Match .NET ReplaceLineEndings used by MarkerFileService's canonical contract.
  return key + '=' + value.replace(/\r\n|[\r\n\f\u0085\u2028\u2029]/g, '\n') + '\n';
}

/**
 * Internal digest/schema check, following signature verification of the same immutable text.
 * Recompute the entire server contract map: selecting one entry before hashing would
 * leave changes to unselected entries outside the signed digest check.
 */
export function checkAgentPluginContractSnapshot(
  raw: string, binding: RequiredAgentPluginBinding,
): VerifiedAgentPluginContract {
  try {
    if (!binding || !binding.agentName || !binding.pluginName || !binding.pluginVersion
      || typeof binding.artifactSha256 !== 'string' || binding.artifactSha256.length !== 64
      || !/^[A-Fa-f0-9]{64}$/.test(binding.artifactSha256)) throw invalid();
    const marker = record(load(raw, { schema: JSON_SCHEMA }));
    const signature = record(marker.signature);
    if (signature.algorithm !== 'HMAC-SHA256' || signature.canonicalization !== 'marker-v1') throw invalid();
    const plugins = record(marker.agent_plugins);
    if (plugins.policy !== 'required') throw invalid();
    const stored = text(plugins.contract_digest);
    if (!/^[A-Fa-f0-9]{64}$/.test(stored)) throw invalid();
    const agents = record(plugins.agents);
    let canonical = line('policy', plugins.policy);
    for (const name of Object.keys(agents).sort()) {
      if (!/^[A-Za-z][A-Za-z0-9_-]*$/.test(name)) throw invalid();
      const contract = record(agents[name]);
      for (const [wire, key] of fields) {
        const value = contract[key];
        if (arrayFields.has(key)) {
          if (!Array.isArray(value) || value.some(item => typeof item !== 'string')) throw invalid();
          canonical += line(name + '.' + wire, value.join(','));
        } else {
          canonical += line(name + '.' + wire, text(value));
        }
      }
      // Optional for legacy profiles: their existing ten-field digest remains unchanged.
      if (contract.artifact_sha256 != null) {
        const artifact = text(contract.artifact_sha256);
        if (artifact.length !== 64 || !/^[A-Fa-f0-9]{64}$/.test(artifact)) throw invalid();
        canonical += line(name + '.artifactSha256', artifact);
      }
    }
    const actual = createHash('sha256').update(canonical).digest();
    if (!timingSafeEqual(actual, Buffer.from(stored, 'hex'))) throw invalid();
    const selected = record(agents[binding.agentName]);
    if (selected.source_type !== binding.agentName || selected.plugin_name !== binding.pluginName
      || selected.plugin_version !== binding.pluginVersion) throw invalid();
    const artifact = text(selected.artifact_sha256);
    if (artifact.length !== 64 || !/^[A-Fa-f0-9]{64}$/.test(artifact)
      || !timingSafeEqual(Buffer.from(artifact, 'hex'), Buffer.from(binding.artifactSha256, 'hex'))) throw invalid();
    return Object.freeze({ agentName: binding.agentName, pluginName: binding.pluginName,
      pluginVersion: binding.pluginVersion, artifactSha256: artifact.toUpperCase(),
      contractDigest: actual.toString('hex').toUpperCase() });
  } catch {
    // Neither YAML snippets, credentials, signatures nor server diagnostics belong in errors.
    throw invalid();
  }
}