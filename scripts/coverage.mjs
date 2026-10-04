#!/usr/bin/env node
// Runs the suite with coverlet and converts the cobertura report to lcov, the
// path the fleet patch-coverage gate reads. The recipe stays thin; the logic
// lives here.
import { spawnSync } from 'node:child_process';

function run(command, args) {
  return spawnSync(command, args, { stdio: 'inherit' });
}

const test = run('mise', ['exec', '--', 'dotnet', 'test', '--collect', 'XPlat Code Coverage']);
if (test.status !== 0) {
  process.exit(test.status ?? 1);
}

const convert = run('node', ['scripts/cobertura-to-lcov.mjs']);
process.exit(convert.status ?? 1);
