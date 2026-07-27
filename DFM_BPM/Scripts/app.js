/* DFM_BPM app.js – minimal JS helpers (most logic is inline on each page) */

/* ── Sidebar toggle ── */
(function () {
    var btn = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('sidebar');
    var wrapper = document.getElementById('pageWrapper');
    if (btn && sidebar) {
        btn.addEventListener('click', function () {
            sidebar.classList.toggle('collapsed');
            if (wrapper) wrapper.classList.toggle('sidebar-collapsed');
        });
    }
})();

/* ── Auto-dismiss alerts after 5s ── */
setTimeout(function () {
    var alerts = document.querySelectorAll('.alert-auto-dismiss');
    alerts.forEach(function (el) { el.style.display = 'none'; });
}, 5000);

/* ── Confirm on dangerous buttons ── */
document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-confirm]');
    if (btn) {
        if (!confirm(btn.getAttribute('data-confirm'))) {
            e.preventDefault();
            e.stopPropagation();
            return false;
        }
    }
});

/* ── Number formatting helper (used by PET line items) ── */
window.fmt = function (v) {
    if (isNaN(v) || v === '' || v === null) return '0.00';
    return parseFloat(v).toLocaleString('en-AE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
};

/* ── Recalc PET line totals ── */
window.recalcLine = function (rates) {
    var qty   = parseFloat(document.getElementById('ctl00_MainContent_txtLineUnits')?.value   || 0);
    var price = parseFloat(document.getElementById('ctl00_MainContent_txtLineUnitPrice')?.value || 0);
    var ccy   = document.getElementById('ctl00_MainContent_ddlLineCcy')?.value;
    var cont  = parseFloat(document.getElementById('ctl00_MainContent_txtLineConting')?.value || 0);
    var rate  = (rates && ccy && rates[ccy]) ? rates[ccy] : 1;
    var fcy   = qty * price;
    var lcy   = fcy * rate;
    var final = lcy * (1 + cont / 100);

    var elFcy   = document.getElementById('ctl00_MainContent_litFCY');
    var elLcy   = document.getElementById('ctl00_MainContent_litLCY');
    var elFinal = document.getElementById('ctl00_MainContent_litFinalLCY');
    if (elFcy)   elFcy.innerText   = fmt(fcy);
    if (elLcy)   elLcy.innerText   = fmt(lcy);
    if (elFinal) elFinal.innerText = fmt(final);
};
