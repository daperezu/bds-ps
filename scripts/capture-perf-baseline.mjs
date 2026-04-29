#!/usr/bin/env node
// FR-073: Capture LCP / TBT baseline for the four wow-moment surfaces + layout shell.
//
// IMPORTANT: This is a stub. A full implementation requires:
//   1. A running Aspire-orchestrated AppHost on a known port.
//   2. A seeded user account that can reach each surface.
//   3. Playwright + chromium with tracing enabled, or Lighthouse-CI.
//
// The script writes to specs/011-warm-modern-facelift/perf-baseline.json.
// Until instrumentation lands, T129 (perf comparison) MUST surface this gap.

import { writeFileSync } from 'node:fs';
import { join } from 'node:path';

const out = join(process.cwd(), 'specs/011-warm-modern-facelift/perf-baseline.json');
const baseline = {
  $schema: 'perf-baseline schema v1',
  captured_at: new Date().toISOString(),
  method: 'stub-not-yet-instrumented',
  note: 'Run this script when Playwright tracing or Lighthouse-CI is wired into the Aspire test fixture.',
  surfaces: {
    applicant_dashboard: { lcp_ms: null, tbt_ms: null },
    application_journey: { lcp_ms: null, tbt_ms: null },
    signing_ceremony: { lcp_ms: null, tbt_ms: null },
    reviewer_queue: { lcp_ms: null, tbt_ms: null },
    layout_shell: { lcp_ms: null, tbt_ms: null },
  },
};

writeFileSync(out, JSON.stringify(baseline, null, 2));
console.log(`Wrote ${out}`);
