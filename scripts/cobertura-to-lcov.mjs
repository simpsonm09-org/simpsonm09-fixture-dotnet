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

function attribute(attributes, name) {
  const match = new RegExp(`\\b${name}="([^"]*)"`).exec(attributes);
  return match ? match[1] : null;
}

// Coverlet escapes method names in XML, so `<Main>$` arrives as `&lt;Main&gt;$`.
function decodeEntities(value) {
  return value
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'")
    .replace(/&amp;/g, '&');
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
// Branch lines carry a condition-coverage attribute like "50% (1/2)".
function classLines(classBody) {
  let body = '';
  for (const match of classBody.matchAll(/<lines>([\s\S]*?)<\/lines>/g)) body = match[1];
  const lines = [];
  const branches = new Map();
  for (const match of body.matchAll(/<line\b([^>]*)>/g)) {
    const attributes = match[1];
    const number = Number(attribute(attributes, 'number'));
    const hits = Number(attribute(attributes, 'hits'));
    if (!Number.isFinite(number) || !Number.isFinite(hits)) continue;
    lines.push([number, hits]);
    if (attribute(attributes, 'branch') !== 'True') continue;
    const coverage = /\((\d+)\/(\d+)\)/.exec(attribute(attributes, 'condition-coverage') ?? '');
    if (coverage) branches.set(number, { covered: Number(coverage[1]), total: Number(coverage[2]) });
  }
  return { lines: lines.sort((a, b) => a[0] - b[0]), branches };
}

// Each <method> has no line attribute, so the function line is the first line
// in its own <lines> block. The method is "hit" when any of those lines ran.
function classMethods(classBody) {
  const block = /<methods>([\s\S]*?)<\/methods>/.exec(classBody);
  if (!block) return [];
  const methods = [];
  for (const match of block[1].matchAll(/<method\b([^>]*)>([\s\S]*?)<\/method>/g)) {
    const name = attribute(match[1], 'name');
    const lines = /<lines>([\s\S]*?)<\/lines>/.exec(match[2]);
    if (!name || !lines) continue;
    let first = Infinity;
    let executed = false;
    for (const line of lines[1].matchAll(/<line\b([^>]*)>/g)) {
      const number = Number(attribute(line[1], 'number'));
      const hits = Number(attribute(line[1], 'hits'));
      if (Number.isFinite(number)) first = Math.min(first, number);
      if (hits > 0) executed = true;
    }
    if (!Number.isFinite(first)) continue;
    methods.push({ name: decodeEntities(name), line: first, hits: executed ? 1 : 0 });
  }
  return methods;
}

function convert(xml, root) {
  const sources = new Map();
  const sourceRoots = reportSources(xml);
  const classPattern = /<class\b[^>]*\bfilename="([^"]+)"[^>]*>([\s\S]*?)<\/class>/g;
  for (const match of xml.matchAll(classPattern)) {
    const { lines, branches } = classLines(match[2]);
    if (lines.length === 0) continue;
    const source = resolveSource(match[1], sourceRoots, root);
    if (source.includes('/obj/') || source.includes('/bin/')) continue;
    const merged = sources.get(source) ?? { lines: new Map(), branches: new Map(), methods: [], names: new Set() };
    for (const [number, hits] of lines) {
      merged.lines.set(number, Math.max(merged.lines.get(number) ?? 0, hits));
    }
    for (const [number, branch] of branches) {
      const current = merged.branches.get(number);
      if (current) {
        current.covered = Math.max(current.covered, branch.covered);
        current.total = Math.max(current.total, branch.total);
      } else {
        merged.branches.set(number, { ...branch });
      }
    }
    for (const method of classMethods(match[2])) {
      if (merged.names.has(method.name)) continue;
      merged.names.add(method.name);
      merged.methods.push(method);
    }
    sources.set(source, merged);
  }

  const records = [];
  for (const [source, data] of sources) {
    const sorted = [...data.lines.entries()].sort((a, b) => a[0] - b[0]);
    const hit = sorted.filter(([, hits]) => hits > 0).length;
    const branchesCovered = [...data.branches.values()].reduce((sum, branch) => sum + branch.covered, 0);
    const branchesTotal = [...data.branches.values()].reduce((sum, branch) => sum + branch.total, 0);
    const record = [`SF:${source}`];
    for (const method of data.methods) record.push(`FN:${method.line},${method.name}`);
    for (const method of data.methods) record.push(`FNDA:${method.hits},${method.name}`);
    for (const [number, hits] of sorted) record.push(`DA:${number},${hits}`);
    for (const [number, branch] of [...data.branches.entries()].sort((a, b) => a[0] - b[0])) {
      for (let index = 0; index < branch.total; index += 1) {
        record.push(`BRDA:${number},0,${index},${index < branch.covered ? 1 : 0}`);
      }
    }
    record.push(`LF:${sorted.length}`);
    record.push(`LH:${hit}`);
    record.push(`BRF:${branchesTotal}`);
    record.push(`BRH:${branchesCovered}`);
    record.push(`FNF:${data.methods.length}`);
    record.push(`FNH:${data.methods.filter((method) => method.hits > 0).length}`);
    record.push('end_of_record');
    records.push(record.join('\n'));
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
