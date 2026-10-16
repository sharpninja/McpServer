/**
 * Type-level 1.x alias coverage for the QBrainAi rename of the shared Node
 * plugin core.
 *
 * Requirement: TR-MCP-QBRAIN-003 (package and tool migration): the renamed
 * npm package keeps its 1.x public names as deprecated aliases through 1.x
 * (BUG-PLUGINCORE-001). Types do not exist at run time, so this file checks
 * the alias by compiling against it: ts-jest fails the suite when the
 * published index does not export McpServerPluginCoreConfig.
 *
 * Fixtures: the real published surface of ../src/index.js; no runtime state.
 *
 * Validates: a McpServerPluginCoreConfig value is accepted wherever a
 * QBrainAiPluginCoreConfig is, and the reverse.
 */
import type { McpServerPluginCoreConfig, QBrainAiPluginCoreConfig } from '../src/index.js';

describe('1.x config type alias (TR-MCP-QBRAIN-003, BUG-PLUGINCORE-001)', () => {
  it('accepts McpServerPluginCoreConfig and QBrainAiPluginCoreConfig interchangeably', () => {
    const legacy: McpServerPluginCoreConfig = {
      agentName: 'OpenCode',
      pluginId: 'opencode',
      autoBootstrap: false,
      autoFlushCache: false,
    };
    const current: QBrainAiPluginCoreConfig = legacy;
    const roundTrip: McpServerPluginCoreConfig = current;
    expect(roundTrip).toEqual({
      agentName: 'OpenCode',
      pluginId: 'opencode',
      autoBootstrap: false,
      autoFlushCache: false,
    });
  });
});
