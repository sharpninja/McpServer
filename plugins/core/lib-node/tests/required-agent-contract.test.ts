// SPDX-License-Identifier: Apache-2.0
import { createHash, createHmac } from 'crypto';
import { mkdtempSync, readFileSync, renameSync, rmSync, writeFileSync } from 'fs';
import { tmpdir } from 'os';
import { join } from 'path';
import { dump } from 'js-yaml';
import { fullBootstrap } from '../src/discovery/marker-resolver.js';

const binding = { agentName: 'ClineQb', pluginName: '@sharpninja/mcpserver-cline-v2-plugin', pluginVersion: '1.118.0', artifactSha256: 'A'.repeat(64) };
const contract = {
  source_type: 'ClineQb', plugin_name: binding.pluginName, plugin_version: binding.pluginVersion,
  activation: 'Mandatory bundled in-process host', startup_command: '',
  unavailable_failure: 'MCP_PLUGIN_UNAVAILABLE:ClineQb', required_env_vars: [],
  hook_expectations: ['host ingress','final tool gate','completion gate'],
  tool_expectations: ['session_*'], root_hints: [], artifact_sha256: binding.artifactSha256,
};
const fields = [
  ['sourceType','source_type'],['pluginName','plugin_name'],['pluginVersion','plugin_version'],
  ['activation','activation'],['startupCommand','startup_command'],['unavailableFailure','unavailable_failure'],
  ['requiredEnvVars','required_env_vars'],['hookExpectations','hook_expectations'],['toolExpectations','tool_expectations'],['rootHints','root_hints'],
] as const;
function digest(plugins: any) {
  const lines = ['policy='+plugins.policy];
  for (const name of Object.keys(plugins.agents).sort()) { for (const [wire,key] of fields) {
    const value = plugins.agents[name][key];
    lines.push(name+'.'+wire+'='+(Array.isArray(value)?value.join(','):value).replace(/\r\n|[\r\n\f\u0085\u2028\u2029]/g,'\n'));
  }
    if(plugins.agents[name].artifact_sha256 != null) lines.push(name+'.artifactSha256='+plugins.agents[name].artifact_sha256);
  }
  return createHash('sha256').update(lines.join('\n')+'\n').digest('hex').toUpperCase();
}
let root: string, marker: string;
beforeEach(()=>{ root=mkdtempSync(join(tmpdir(),'qb-contract-'));marker=join(root,'AGENTS-README-FIRST.yaml'); });
afterEach(()=>rmSync(root,{recursive:true,force:true}));
function write(mutate?: (plugins:any)=>void, corruptContent=false) {
  const plugins:any={policy:'required',agents:{ClineQb:structuredClone(contract)}};
  mutate?.(plugins);
  plugins.contract_digest=digest(plugins);
  if(corruptContent) plugins.agents.ClineQb.plugin_version='9.9.9';
  const data:any={port:7147,baseUrl:'http://127.0.0.1:7147',apiKey:'fixture-key',workspace:'fixture',workspacePath:root,
    pid:123,startedAt:'2026-01-01T00:00:00Z',markerWrittenAtUtc:'2026-01-01T00:00:00Z',serverStartedAtUtc:'2026-01-01T00:00:00Z',
    endpoints:{health:'/health'},agent_plugins:plugins};
  const keys=['port','baseUrl','apiKey','workspace','workspacePath','pid','startedAt','markerWrittenAtUtc','serverStartedAtUtc'];
  const payload=['canonicalization=marker-v1',...keys.map(key=>key+'='+data[key]),'endpoints.health=/health','agentPlugins.policy='+plugins.policy,'agentPlugins.contractDigest='+plugins.contract_digest].join('\n')+'\n';
  data.signature={algorithm:'HMAC-SHA256',canonicalization:'marker-v1',value:createHmac('sha256',data.apiKey).update(payload).digest('hex').toUpperCase()};
  writeFileSync(marker,dump(data,{lineWidth:-1,noRefs:true}));
  return plugins.contract_digest;
}
function fetcher() {
  return jest.fn(async (url: any)=>({ok:true,json:async()=>({nonce:new URL(String(url)).searchParams.get('nonce')})})) as any;
}
function options(fetch: typeof globalThis.fetch) { return {fetch,expectedWorkspacePath:root,requiredAgentPlugin:{...binding}}; }

test('returns frozen verified contract metadata only after signature, digest and nonce checks',async()=>{
  const expected=write(),fetch=fetcher();
  const result:any=await fullBootstrap(root,options(fetch));
  expect(result.agentPluginContract).toMatchObject({...binding,contractDigest:expected});
  expect(Object.isFrozen(result.agentPluginContract)).toBe(true);
  expect(fetch).toHaveBeenCalledTimes(1);
});
test.each([
  ['missing product',(p:any)=>{p.agents.Cline=p.agents.ClineQb;delete p.agents.ClineQb;}],
  ['wrong source',(p:any)=>{p.agents.ClineQb.source_type='Cline';}],
  ['wrong plugin',(p:any)=>{p.agents.ClineQb.plugin_name='mcpserver-cline-plugin';}],
  ['wrong pinned version',(p:any)=>{p.agents.ClineQb.plugin_version='1.117.0';}],
  ['optional policy',(p:any)=>{p.policy='optional';}],
])('rejects %s before any network call',async(_name,mutate)=>{
  write(mutate as any);const fetch=fetcher();
  await expect(fullBootstrap(root,options(fetch))).rejects.toThrow(/MCP_UNTRUSTED/);
  expect(fetch).not.toHaveBeenCalled();
});
test('rejects changed nested content even when the original digest remains signed',async()=>{
  write(undefined,true);const fetch=fetcher();
  await expect(fullBootstrap(root,options(fetch))).rejects.toThrow(/MCP_UNTRUSTED/);
  expect(fetch).not.toHaveBeenCalled();
});
test('checks the entire contract map, including agents other than the selected one',async()=>{
  write(p=>{p.agents.Other={...p.agents.ClineQb,source_type:'Other',plugin_version:'2.0.0'};});
  writeFileSync(marker,readFileSync(marker,'utf8').replace('plugin_version: 2.0.0','plugin_version: 3.0.0'));
  const fetch=fetcher();await expect(fullBootstrap(root,options(fetch))).rejects.toThrow(/MCP_UNTRUSTED/);
  expect(fetch).not.toHaveBeenCalled();
});
test('requires explicit workspace binding and host fetch for product contract verification',async()=>{
  write();const fetch=fetcher();
  const unbound={fetch,requiredAgentPlugin:{...binding}};
  await expect(fullBootstrap(root,unbound)).rejects.toThrow(/MCP_UNTRUSTED/);
  expect(fetch).not.toHaveBeenCalled();
  const ambient={expectedWorkspacePath:root,requiredAgentPlugin:{...binding}};
  await expect(fullBootstrap(root,ambient)).rejects.toThrow(/MCP_UNTRUSTED/);
});
test('rejects marker replacement during the awaited health check',async()=>{
  write();const fetch=fetcher();
  fetch.mockImplementationOnce(async(url:any)=>{write(p=>{p.agents.ClineQb.plugin_version='2.0.0';});return {ok:true,json:async()=>({nonce:new URL(String(url)).searchParams.get('nonce')})};});
  await expect(fullBootstrap(root,options(fetch))).rejects.toThrow(/MCP_UNTRUSTED/);
});
test('rejects malformed YAML duplicate contract fields before probing',async()=>{
  write();writeFileSync(marker,readFileSync(marker,'utf8').replace('      activation:', '      plugin_name: replaced\n      activation:'));
  const fetch=fetcher();await expect(fullBootstrap(root,options(fetch))).rejects.toThrow(/MCP_UNTRUSTED/);expect(fetch).not.toHaveBeenCalled();
});
test('rejects nonce mismatch without returning authenticated contract metadata',async()=>{
  write();const fetch=fetcher();fetch.mockResolvedValue({ok:true,json:async()=>({nonce:'wrong'})});
  await expect(fullBootstrap(root,options(fetch))).rejects.toThrow(/MCP_UNTRUSTED/);
});
test('rejects foreign workspace before probing',async()=>{
  write();const fetch=fetcher();
  await expect(fullBootstrap(root,{...options(fetch),expectedWorkspacePath:join(root,'other')})).rejects.toThrow(/MCP_UNTRUSTED/);
  expect(fetch).not.toHaveBeenCalled();
});
test.each(['agent-contract-golden.json','agent-contract-artifact-golden.json'])('matches server golden digest %s, including normalized line endings',async(fixture)=>{
  const golden=JSON.parse(readFileSync(join(__dirname,'fixtures',fixture),'utf8'));
  expect(digest(golden)).toBe(golden.contract_digest);
  write(p=>{Object.assign(p,golden);p.agents.ClineQb.artifact_sha256=binding.artifactSha256;});
  const result:any=await fullBootstrap(root,options(fetcher()));
  expect(result.agentPluginContract.contractDigest).toBe(digest({...golden,agents:{ClineQb:{...golden.agents.ClineQb,artifact_sha256:binding.artifactSha256}}}));
});
test('refuses unsupported declared signature format before probing',async()=>{
  write();writeFileSync(marker,readFileSync(marker,'utf8').replace('canonicalization: marker-v1','canonicalization: marker-v2'));
  const fetch=fetcher();await expect(fullBootstrap(root,options(fetch))).rejects.toThrow(/MCP_UNTRUSTED/);expect(fetch).not.toHaveBeenCalled();
});
test('refuses invalid signature before probing',async()=>{
  write();writeFileSync(marker,readFileSync(marker,'utf8').replace(/value: [A-F0-9]{64}/,'value: '+'0'.repeat(64)));
  const fetch=fetcher();await expect(fullBootstrap(root,options(fetch))).rejects.toThrow(/MCP_UNTRUSTED/);expect(fetch).not.toHaveBeenCalled();
});
test('captures the required binding before awaiting health',async()=>{
  write();const fetch=fetcher(),configured=options(fetch);
  fetch.mockImplementationOnce(async(url:any)=>{configured.requiredAgentPlugin.pluginName='mutated';return {ok:true,json:async()=>({nonce:new URL(String(url)).searchParams.get('nonce')})};});
  const result:any=await fullBootstrap(root,configured);
  expect(result.agentPluginContract.pluginName).toBe(binding.pluginName);
});
test('keeps the legacy opt-out interface for other products',async()=>{
  write();const fetch=fetcher();
  const result=await fullBootstrap(root,{fetch,expectedWorkspacePath:root});
  expect(result.agentPluginContract).toBeUndefined();expect(fetch).toHaveBeenCalledTimes(1);
});
test('authenticates quoted workspace scalars without changing their path',async()=>{
  const renamed=root+" - it's # fixture";
  renameSync(root,renamed);root=renamed;marker=join(root,'AGENTS-README-FIRST.yaml');
  write();const fetch=fetcher();
  const result=await fullBootstrap(root,options(fetch));
  expect(result.workspacePath).toBe(root);expect(fetch).toHaveBeenCalledTimes(1);
});
test.each([
  ['absent', undefined], ['malformed', 'invalid'], ['wrong pin', 'B'.repeat(64)],
])('refuses %s artifact pin in an otherwise signed contract',async(_name,artifact)=>{
  write(p=>{p.agents.ClineQb.artifact_sha256=artifact;});
  const fetch=fetcher();
  await expect(fullBootstrap(root,options(fetch))).rejects.toThrow(/MCP_UNTRUSTED/);
  expect(fetch).not.toHaveBeenCalled();
});
test('refuses missing host artifact pin instead of learning it from the marker',async()=>{
  write();const fetch=fetcher(),configured:any=options(fetch);
  delete configured.requiredAgentPlugin.artifactSha256;
  await expect(fullBootstrap(root,configured)).rejects.toThrow(/MCP_UNTRUSTED/);
  expect(fetch).not.toHaveBeenCalled();
});
test('rejects artifact field changes under the original signed contract digest',async()=>{
  write();writeFileSync(marker,readFileSync(marker,'utf8').replace('artifact_sha256: '+binding.artifactSha256,'artifact_sha256: '+'B'.repeat(64)));
  const fetch=fetcher();
  await expect(fullBootstrap(root,options(fetch))).rejects.toThrow(/MCP_UNTRUSTED/);
  expect(fetch).not.toHaveBeenCalled();
});
test('accepts a matching host digest with lowercase hexadecimal spelling',async()=>{
  write();const fetch=fetcher(),configured=options(fetch);
  configured.requiredAgentPlugin.artifactSha256=binding.artifactSha256.toLowerCase();
  const result=await fullBootstrap(root,configured);
  expect((result.agentPluginContract as any).artifactSha256).toBe(binding.artifactSha256);
});