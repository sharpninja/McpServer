/**
 * Package-surface honesty coverage for @sharpninja/mcpserver-plugin-core.
 *
 * Ruling: the QuadBrain removal deleted three public exports from the package
 * index (brainSlotTools, canHandleBrainSlotTool, handleBrainSlotTool). That is
 * a breaking change to a published shared surface, so the manifest version must
 * move off 0.1.0 by more than a patch (under semver, a 0.x breaking change
 * bumps the minor), and the package's own documentation must record what was
 * removed so consumers can see why their import broke.
 *
 * Fixtures: the real package.json and README.md on disk, read as text.
 *
 * Validates: the manifest version is a valid semver greater than 0.1.0 and not
 * a mere patch bump, and README.md names all three removed exports.
 */
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

/** A parsed semver triple, patch-level only (no prerelease handling needed here). */
interface SemVer {
  major: number;
  minor: number;
  patch: number;
}

/** Parses "x.y.z" into its numeric parts, failing the test on any other shape. */
function parseSemVer(value: string): SemVer {
  const match = /^(\d+)\.(\d+)\.(\d+)$/.exec(value);
  if (!match) {
    throw new Error(`package.json version is not a plain semver triple: "${value}"`);
  }
  return { major: Number(match[1]), minor: Number(match[2]), patch: Number(match[3]) };
}

/** Returns a negative/zero/positive ordering of two semver triples. */
function compareSemVer(left: SemVer, right: SemVer): number {
  return (
    left.major - right.major || left.minor - right.minor || left.patch - right.patch
  );
}

const packageRoot = join(__dirname, '..');
const manifest = JSON.parse(readFileSync(join(packageRoot, 'package.json'), 'utf8')) as {
  name: string;
  version: string;
};
const readme = readFileSync(join(packageRoot, 'README.md'), 'utf8');

/** The public exports deleted from src/index.ts by the QuadBrain removal. */
const removedExports = ['brainSlotTools', 'canHandleBrainSlotTool', 'handleBrainSlotTool'];

describe('@sharpninja/mcpserver-plugin-core package surface honesty', () => {
  test('the manifest is the expected package', () => {
    expect(manifest.name).toBe('@sharpninja/mcpserver-plugin-core');
  });

  test('the version has moved past the pre-removal 0.1.0', () => {
    const version = parseSemVer(manifest.version);
    expect(compareSemVer(version, { major: 0, minor: 1, patch: 0 })).toBeGreaterThan(0);
  });

  test('the version bump is not a patch bump, because exports were removed', () => {
    const version = parseSemVer(manifest.version);
    // Under semver a 0.x breaking change bumps the minor; 1.x+ bumps the major.
    expect(version.major > 0 || version.minor > 1).toBe(true);
  });

  test('README.md records every removed public export', () => {
    const missing = removedExports.filter((name) => !readme.includes(name));
    expect(missing).toEqual([]);
  });
});
