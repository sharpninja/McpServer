/** TEST-QB-007: independent Node process for journal contention and crash boundaries. */
const { promises: fs } = require('fs');
const [modulePath, rootDirectory, mode, countText] = process.argv.slice(2);
const { AuditOutbox } = require(modulePath);
const context = { workspacePath: rootDirectory, agentIdentity: 'ClineQb', mcpSessionId: 'ClineQb-20261009T120000Z-process' };
const box = new AuditOutbox({ rootDirectory, context });

async function run() {
  if (mode === 'publish-crash') {
    const rename = fs.rename.bind(fs);
    fs.rename = async (source, target) => {
      if (String(target).endsWith('.event.yaml')) process.exit(73);
      return rename(source, target);
    };
  }
  for (let i = 0; i < Number(countText); i++) {
    const result = await box.enqueue('req-process', 'action', { index: i, worker: process.pid });
    if (result.code !== 'queued') throw new Error('enqueue was not durable');
    process.stdout.write(JSON.stringify(result.event) + '\n');
  }
  if (mode === 'submit-crash') {
    await box.flush({ submit: async () => process.exit(74), read: async () => { throw new Error('unexpected read'); } });
  }
}
run().catch(error => { process.stderr.write(String(error)); process.exitCode = 1; });
