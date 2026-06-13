/**
 * Jest config for @sharpninja/mcpserver-plugin-core.
 *
 * The package ships as CommonJS ("type": "commonjs"), but the source uses
 * ESM-style explicit ".js" import suffixes (NodeNext-flavored). Under the
 * ts-jest CommonJS transform those specifiers must be rewritten back to the
 * extensionless module name so the resolver finds the .ts source. The
 * moduleNameMapper below does exactly that (mirrors the cline-v2 plugin
 * mapper, minus the @cline/core / @cline/sdk shims that only the host glue
 * needs).
 *
 * @type {import('jest').Config}
 */
module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  moduleNameMapper: {
    '^(\\.{1,2}/.*)\\.js$': '$1',
  },
  transform: {
    '^.+\\.ts$': ['ts-jest', { tsconfig: { module: 'commonjs', target: 'es2022', esModuleInterop: true } }],
  },
  testMatch: ['<rootDir>/tests/**/*.test.ts'],
};
