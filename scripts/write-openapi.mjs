#!/usr/bin/env node
// Regenerates docs/openapi.json through the drift test with WRITE_OPENAPI=1.
// The test owns the generator, so the writer and the drift check share one
// source. This script only sets the flag and selects the test.
import { spawnSync } from 'node:child_process';

const result = spawnSync(
  'mise',
  ['exec', '--', 'dotnet', 'test', '--filter', 'FullyQualifiedName~OpenApiDocumentTests'],
  { stdio: 'inherit', env: { ...process.env, WRITE_OPENAPI: '1' } },
);

process.exit(result.status ?? 1);
