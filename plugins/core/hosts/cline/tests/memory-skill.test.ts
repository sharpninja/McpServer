import * as fs from 'fs';
import * as path from 'path';

const root = path.resolve(__dirname, '..');
const skillPath = path.join(root, 'skills', 'memory', 'SKILL.md');
const descriptorPath = path.join(root, 'memory-descriptor.json');

describe('cline memory skill and descriptor', () => {
  test('skill loads with required verbs and injection/fallback notes', () => {
    const content = fs.readFileSync(skillPath, 'utf8');
    expect(content.trim().length).toBeGreaterThan(0);
    expect(content).toMatch(/^---/m);
    expect(content).toContain('memory_remember');
    expect(content).toContain('memory_recall');
    expect(content).toContain('memory_explore');
    expect(content).toContain('memory_consolidate');
    expect(content).toContain('memory_promote');
    expect(content).toMatch(/injection/i);
    expect(content).toMatch(/fallback/i);
  });

  test('descriptor loads without live cloud keys', () => {
    const descriptor = JSON.parse(fs.readFileSync(descriptorPath, 'utf8')) as {
      host: string;
      tools: string[];
      injection: { requiredMemoriesPrefix: string };
      fallback: { localFailsafe: boolean };
    };
    expect(descriptor.host).toBe('cline');
    expect(descriptor.tools).toEqual(expect.arrayContaining([
      'memory_remember',
      'memory_recall',
      'memory_explore',
      'memory_consolidate',
      'memory_promote',
    ]));
    expect(descriptor.injection.requiredMemoriesPrefix).toContain('REQUIRED MEMORIES');
    expect(descriptor.fallback.localFailsafe).toBe(true);
    expect(JSON.stringify(descriptor)).not.toMatch(/sk-[A-Za-z0-9]{10,}/);
  });
});
