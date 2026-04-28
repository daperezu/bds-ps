// facelift-init.js — wires up KPI tickers, filter chip reflow, and journey clicks
// on DOMContentLoaded. Token-driven motion only.

(function () {
  'use strict';

  function init() {
    if (window.ForgeMotion) {
      window.ForgeMotion.mountTickers(document);
      window.ForgeMotion.mountJourney(document);
      window.ForgeMotion.bindJourneyEventClicks(document);
    }

    // Filter chip strip (US4, FR-054). Click → fetch QueueRows partial → swap tbody.
    document.querySelectorAll('[data-chip-strip]').forEach((strip) => {
      const endpoint = strip.getAttribute('data-chip-endpoint');
      const targetSel = strip.getAttribute('data-chip-target');
      if (!endpoint || !targetSel) return;
      const target = document.querySelector(targetSel);
      if (!target) return;

      strip.querySelectorAll('[data-chip-value]').forEach((chip) => {
        chip.addEventListener('click', async (ev) => {
          ev.preventDefault();
          strip.querySelectorAll('[data-chip-value]').forEach((c) => c.setAttribute('aria-pressed', 'false'));
          chip.setAttribute('aria-pressed', 'true');
          const value = chip.getAttribute('data-chip-value');
          try {
            const res = await fetch(`${endpoint}?filter=${encodeURIComponent(value)}`, {
              headers: { 'X-Requested-With': 'fetch' },
            });
            if (!res.ok) return;
            const html = await res.text();
            target.innerHTML = html;
            if (window.ForgeMotion) {
              window.ForgeMotion.mountJourney(target);
            }
          } catch { /* network error — keep current rows */ }
        });
      });
    });

    // Row click — navigate to data-row-href (US4, FR-057)
    document.querySelectorAll('[data-row-href]').forEach((row) => {
      row.addEventListener('click', (ev) => {
        if (ev.target && ev.target.closest('a, button, form, [data-no-row-nav]')) return;
        const href = row.getAttribute('data-row-href');
        if (href) window.location.assign(href);
      });
    });

    // Ceremony hooks if present
    const ceremony = document.querySelector('[data-ceremony-config]');
    if (ceremony && window.ForgeMotion) {
      try {
        const opts = JSON.parse(ceremony.getAttribute('data-ceremony-config'));
        window.ForgeMotion.mountCeremony(opts);
      } catch { /* malformed — skip */ }
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
