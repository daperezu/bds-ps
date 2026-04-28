#!/usr/bin/env node
// SC-015 / FR-073 verification: compare current LCP/TBT to baseline; fail if regression > 10%.
//
// Stub: pairs with capture-perf-baseline.mjs. Until both have real instrumentation,
// this script logs a deferred-gate notice and exits 0 so it does not block the pipeline.

import { readFileSync } from 'node:fs';
import { argv } from 'node:process';

const baselinePath = argv[2];
if (!baselinePath) {
  console.error('Usage: compare-perf.mjs <baseline.json>');
  process.exit(2);
}

let baseline;
try {
  baseline = JSON.parse(readFileSync(baselinePath, 'utf8'));
} catch (err) {
  console.error(`Cannot read baseline ${baselinePath}:`, err.message);
  process.exit(2);
}

let captured = false;
for (const [name, vals] of Object.entries(baseline.surfaces ?? {})) {
  if (vals.lcp_ms != null || vals.tbt_ms != null) {
    captured = true;
    break;
  }
}

if (!captured) {
  console.warn('NOTICE: perf baseline is empty (capture-perf-baseline.mjs not yet wired).');
  console.warn('SC-015 perf gate is deferred until baseline instrumentation lands.');
  process.exit(0);
}

console.log('Baseline contains values. Current-run instrumentation TBD; gate evaluation skipped.');
process.exit(0);
