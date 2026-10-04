#!/usr/bin/env node
// Converts a coverlet cobertura report to lcov so the fleet patch-coverage gate
// can read it. Coverlet writes one XML report per test run under TestResults;
// this finds the newest one, or takes --input, and writes coverage/lcov.info.
//
// usage:
//   node scripts/cobertura-to-lcov.mjs [--input <cobertura.xml>] [--output coverage/lcov.info]

import { existsSync, mkdirSync, readFileSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { dirname, isAbsolute, join, relative, resolve } from 'node:path';
import { EXIT } from './lib/exit.mjs';

const ROOT = resolve(process.cwd());
const COBERTURA_NAME = 'coverage.cobertura.xml';

function parseArgs(argv) {
  const options = { input: null, output: 'coverage/lcov.info', help: false };
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (arg === '-h' || arg === '--help') options.help = true;
    else if (arg === '--input') { options.input = argv[i + 1]; i += 1; }
    else if (arg === '--output') { options.output = argv[i + 1]; i += 1; }
  }
  return options;
}

function toPosix(value) {
  return value.split('\\').join('/');
}

function findCobertura(dir) {
  if (!existsSync(dir)) return null;
  const found = [];
  const walk = (current) => {
    for (const entry of readdirSync(current, { withFileTypes: true })) {
      const full = join(current, entry.name);
      if (entry.isDirectory()) walk(full);
      else if (entry.name === COBERTURA_NAME) found.push(full);
    }
  };
  walk(dir);
  if (found.length === 0) return null;
  found.sort((a, b) => statSync(b).mtimeMs - statSync(a).mtimeMs);
  return found[0];
}

// Coverlet records each <class filename> relative to a <source> root (the
// directory the project builds from). The lcov `SF:` path must be relative to
// the repository root and use forward slashes, so it lines up with the paths
// `git diff` reports.
function reportSources(xml) {
  const block = /<sources>([\s\S]*?)<\/sources>/.exec(xml);
  if (!block) return [];
  const sources = [];
  for (const match of block[1].matchAll(/<source>([\s\S]*?)<\/source>/g)) {
    sources.push(match[1].trim());
  }
  return sources;
}

function resolveSource(raw, sources, root) {
  if (isAbsolute(raw)) return toPosix(relative(root, raw));
  for (const source of sources) {
    const candidate = resolve(root, source, raw);
    if (existsSync(candidate)) return toPosix(relative(root, candidate));
  }
  if (sources.length > 0) return toPosix(relative(root, resolve(root, sources[0], raw)));
  return toPosix(raw);
}

// Coverlet repeats every line inside the per-method <lines> blocks, so only the
// class-level <lines> element (the last one in the class) is a complete record.
function classLines(classBody) {
  let body = '';
  for (const match of classBody.matchAll(/<lines>([\s\S]*?)<\/lines>/g)) body = match[1];
  const lines = [];
  for (const match of body.matchAll(/<line\b[^>]*\bnumber="(\d+)"[^>]*\bhits="(\d+)"/g)) {
    lines.push([Number(match[1]), Number(match[2])]);
  }
  return lines.sort((a, b) => a[0] - b[0]);
}

function convert(xml, root) {
  const sources = new Map();
  const sourceRoots = reportSources(xml);
  const classPattern = /<class\b[^>]*\bfilename="([^"]+)"[^>]*>([\s\S]*?)<\/class>/g;
  for (const match of xml.matchAll(classPattern)) {
    const lines = classLines(match[2]);
    if (lines.length === 0) continue;
    const source = resolveSource(match[1], sourceRoots, root);
    if (source.includes('/obj/') || source.includes('/bin/')) continue;
    const merged = sources.get(source) ?? new Map();
    for (const [number, hits] of lines) {
      merged.set(number, Math.max(merged.get(number) ?? 0, hits));
    }
    sources.set(source, merged);
  }

  const records = [];
  for (const [source, lines] of sources) {
    const sorted = [...lines.entries()].sort((a, b) => a[0] - b[0]);
    const hit = sorted.filter(([, hits]) => hits > 0).length;
    records.push([
      `SF:${source}`,
      ...sorted.map(([number, hits]) => `DA:${number},${hits}`),
      `LF:${sorted.length}`,
      `LH:${hit}`,
      'end_of_record',
    ].join('\n'));
  }
  return records.length > 0 ? `${records.join('\n')}\n` : '';
}

function main() {
  const options = parseArgs(process.argv.slice(2));
  if (options.help) {
    process.stdout.write('usage: node scripts/cobertura-to-lcov.mjs [--input <cobertura.xml>] [--output coverage/lcov.info]\n');
    process.exit(EXIT.OK);
  }

  const input = options.input ? resolve(options.input) : findCobertura(ROOT);
  if (!input || !existsSync(input)) {
    process.stderr.write('cobertura-to-lcov: no coverage.cobertura.xml found; run `just coverage` first\n');
    process.exit(EXIT.FAIL);
  }

  const output = resolve(options.output);
  mkdirSync(dirname(output), { recursive: true });
  writeFileSync(output, convert(readFileSync(input, 'utf8'), ROOT), 'utf8');
  process.stdout.write(`cobertura-to-lcov: wrote ${toPosix(relative(ROOT, output))} from ${toPosix(relative(ROOT, input))}\n`);
  process.exit(EXIT.OK);
}

main();
