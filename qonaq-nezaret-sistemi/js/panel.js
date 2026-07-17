/* =========================================================
   panel.js — "Buraxılış sənədi" paneli (goz ikonu ilə açılır)
   Kart ilə buraxılan qonaqda müvəqqəti kart, QR ilə
   buraxılanda isə QR kod göstərilir. Çap etmək mümkündür.
   Həm Ana səhifədə, həm Qonaqlar səhifəsində istifadə olunur.
   ========================================================= */

let _panelQR = null; // aktiv QR obyekti (çap üçün data-URL almaq məqsədilə)

function passCardBlock(g) {
  if (g.passType === 'qr') {
    return `
      <div class="pass-block qr">
        <div class="pass-kicker">QR KOD İLƏ BURAXILIŞ</div>
        <div class="qr-holder" id="panelQrBox"></div>
        <div class="pass-hint">Girişdə skan edin</div>
      </div>`;
  }
  return `
    <div class="pass-block card">
      <div class="pass-top">
        <span class="pass-kicker">MÜVƏQQƏTİ KART</span>
        <span class="pass-nfc">${ICONS.nfc}</span>
      </div>
      <div class="pass-cardno">${esc(g.cardNo || '—')}</div>
    </div>`;
}

function buildPanel(g) {
  const isQr = g.passType === 'qr';
  const title = isQr ? 'QR kod ilə buraxılış sənədi' : 'Kartla buraxılış sənədi';
  const s = STATUS[g.status] || STATUS.out;

  return `
    <div class="pass-overlay open" id="passOverlay">
      <div class="pass-doc" role="dialog" aria-label="${esc(title)}">
        <div class="pass-head">
          <h2>${esc(title)}</h2>
          <button class="modal-close" id="passClose" title="Bağla">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
          </button>
        </div>

        <div class="pass-body">
          <div class="pass-main">
            <div class="pass-photo">${avatarHTML(g)}</div>
            <div class="pass-info">
              <h3>${esc(g.name)}</h3>
              <div class="pass-line"><span>Şəxsiyyət vəsiqəsi:</span> <b>${esc(g.doc)}</b></div>
              <div class="pass-line"><span>Qəbul edən:</span> <b>${esc(g.host)}</b></div>
              <div class="pass-line"><span>Məqsədlər:</span> <b>${esc(g.purpose)}</b></div>
              <div class="pass-line"><span>Ərazilər:</span> <b>${esc(g.area)}</b></div>
            </div>
            ${passCardBlock(g)}
          </div>

          <div class="pass-dates">
            <div class="pass-date">
              <div class="pd-lbl">Gəliş tarixi</div>
              <div class="pd-val">${esc(g.arrival)}</div>
            </div>
            <div class="pass-date">
              <div class="pd-lbl">Çıxış tarixi (proqnoz)</div>
              <div class="pd-val">${esc(g.exit)}</div>
            </div>
          </div>

          <div class="pass-status chip-lg ${s.cls}">${s.label}</div>
        </div>

        <div class="pass-foot">
          <button class="btn btn-primary-soft" id="passPrint">${ICONS.print} Çap et</button>
          <button class="btn btn-ghost" id="passCloseBtn">Bağla</button>
        </div>
      </div>
    </div>`;
}

function openGuestPanel(g) {
  closeGuestPanel();
  document.body.insertAdjacentHTML('beforeend', buildPanel(g));
  document.body.style.overflow = 'hidden';

  const overlay = document.getElementById('passOverlay');

  // Düymələrin listener-lərini ƏVVƏL bağlayırıq ki, QR yaradılmasında
  // baş verə biləcək xəta "Çap et" / "Bağla" düymələrini bloklamasın.
  overlay.addEventListener('click', e => { if (e.target === overlay) closeGuestPanel(); });
  document.getElementById('passClose').addEventListener('click', closeGuestPanel);
  document.getElementById('passCloseBtn').addEventListener('click', closeGuestPanel);
  document.getElementById('passPrint').addEventListener('click', () => printPass(g));

  // QR kodu çək (yalnız QR növü üçün) — xəta olsa belə panel işləməlidir
  _panelQR = null;
  if (g.passType === 'qr') {
    const box = document.getElementById('panelQrBox');
    if (box && window.QRCode) {
      try {
        _panelQR = renderQR(box, guestQrPayload(g), 168);
      } catch (err) {
        _panelQR = null;
        box.innerHTML = '<div class="qr-fallback">QR kod yaradıla bilmədi</div>';
      }
    }
  }
}

function closeGuestPanel() {
  const overlay = document.getElementById('passOverlay');
  if (overlay) overlay.remove();
  document.body.style.overflow = '';
}

/* Buraxılış sənədini ayrıca pəncərədə çap edir */
function printPass(g) {
  const isQr = g.passType === 'qr';
  const s = STATUS[g.status] || STATUS.out;
  const statusColor = { in:'#16a34a', out:'#5b6472', late:'#dc2626' }[s.cls] || '#5b6472';

  let passHtml;
  if (isQr) {
    const src = _panelQR ? qrDataURL(document.getElementById('panelQrBox')) : '';
    passHtml = `
      <div class="pass qr">
        <div class="kick">QR KOD İLƏ BURAXILIŞ</div>
        ${src ? `<img src="${src}" width="180" height="180" alt="QR" />` : ''}
        <div class="hint">Girişdə skan edin</div>
      </div>`;
  } else {
    passHtml = `
      <div class="pass card">
        <div class="kick">MÜVƏQQƏTİ KART</div>
        <div class="cardno">${esc(g.cardNo || '—')}</div>
      </div>`;
  }

  const win = window.open('', '_blank', 'width=720,height=900');
  if (!win) { alert('Çap pəncərəsi bloklandı. Zəhmət olmasa pop-up-lara icazə verin.'); return; }
  win.document.write(`<!DOCTYPE html><html lang="az"><head><meta charset="UTF-8">
    <title>Buraxılış sənədi — ${esc(g.name)}</title>
    <style>
      *{box-sizing:border-box;margin:0;padding:0;font-family:'Inter',system-ui,'Segoe UI',sans-serif}
      body{padding:32px;color:#1e2a3a}
      h1{font-size:20px;margin-bottom:4px}
      .sub{color:#6b7688;font-size:13px;margin-bottom:24px}
      .grid{display:flex;gap:28px;align-items:flex-start;border:1px solid #eaecf1;border-radius:16px;padding:24px}
      .info h2{font-size:22px;margin-bottom:12px}
      .info p{font-size:14px;color:#374151;margin:5px 0}
      .info p span{color:#6b7688}
      .pass{width:230px;flex-shrink:0;border-radius:16px;padding:22px;text-align:center}
      .pass.card{background:linear-gradient(135deg,#6d28d9,#4c1d95);color:#fff}
      .pass.card .kick{font-size:11px;letter-spacing:.08em;font-weight:700;opacity:.9}
      .pass.card .cardno{font-size:26px;font-weight:800;margin-top:26px;letter-spacing:.02em}
      .pass.qr{background:#fff;border:1px solid #eaecf1}
      .pass.qr .kick{font-size:11px;letter-spacing:.06em;font-weight:700;color:#6b7688;margin-bottom:14px}
      .pass.qr img{border-radius:8px}
      .pass.qr .hint{font-size:12px;color:#9aa4b3;margin-top:12px}
      .dates{display:flex;gap:16px;margin-top:20px}
      .d{flex:1;border:1px solid #eaecf1;border-radius:12px;padding:14px 16px}
      .d .l{font-size:12px;color:#6b7688}
      .d .v{font-size:15px;font-weight:700;margin-top:4px}
      .st{margin-top:18px;text-align:center;padding:12px;border-radius:10px;font-weight:700;color:#fff;background:${statusColor}}
      @media print{body{padding:0}}
    </style></head><body>
      <h1>${isQr ? 'QR kod ilə buraxılış sənədi' : 'Kartla buraxılış sənədi'}</h1>
      <div class="sub">Giriş-çıxışa Nəzarət Sistemi</div>
      <div class="grid">
        <div class="info">
          <h2>${esc(g.name)}</h2>
          <p><span>Şəxsiyyət vəsiqəsi:</span> <b>${esc(g.doc)}</b></p>
          <p><span>Qəbul edən:</span> <b>${esc(g.host)}</b></p>
          <p><span>Məqsədlər:</span> <b>${esc(g.purpose)}</b></p>
          <p><span>Ərazilər:</span> <b>${esc(g.area)}</b></p>
        </div>
        ${passHtml}
      </div>
      <div class="dates">
        <div class="d"><div class="l">Gəliş tarixi</div><div class="v">${esc(g.arrival)}</div></div>
        <div class="d"><div class="l">Çıxış tarixi (proqnoz)</div><div class="v">${esc(g.exit)}</div></div>
      </div>
      <div class="st">${s.label}</div>
    </body></html>`);
  win.document.close();
  win.focus();
  setTimeout(() => { win.print(); }, 300);
}

/* Escape ilə bağlama */
document.addEventListener('keydown', e => { if (e.key === 'Escape') closeGuestPanel(); });
