/* =========================================================
   common-mvc.js — MVC (Razor) səhifələri üçün köməkçi funksiyalar.
   Statik data və auth-guard YOXDUR (server tərəfdə idarə olunur).
   panel.js buradakı funksiyalardan istifadə edir.
   ========================================================= */

const STATUS = {
  in:        { label: 'Binadadır',       cls: 'in'        },
  onfloor:   { label: 'Mərtəbədə',       cls: 'onfloor'   },
  checkedin: { label: 'Kart verilib',    cls: 'checkedin' },
  planned:   { label: 'Planlaşdırılmış', cls: 'planned'   },
  out:       { label: 'Çıxıb',           cls: 'out'       },
  late:      { label: 'Gecikib',         cls: 'late'      },
};

const ICONS = {
  nfc:   '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 8.32a7.43 7.43 0 0 1 0 7.36"/><path d="M9.46 6.21a11.76 11.76 0 0 1 0 11.58"/><path d="M12.91 4.1a15.91 15.91 0 0 1 .01 15.8"/><path d="M16.37 2a20.16 20.16 0 0 1 0 20"/></svg>',
  print: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 6 2 18 2 18 9"/><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/><rect x="6" y="14" width="12" height="8"/></svg>',
  person:'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>',
};

/* HTML escape */
function esc(str) {
  return String(str ?? '').replace(/[&<>"']/g, c =>
    ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;' }[c]));
}

/* Avatar HTML — şəkil yolu varsa foto, yoxdursa placeholder ikon */
function avatarHTML(g) {
  if (!g.photo) return `<div class="g-avatar empty">${ICONS.person}</div>`;
  if (/[\/.]/.test(g.photo) && /\.(jpe?g|png|webp|gif|svg)$/i.test(g.photo)) {
    return `<div class="g-avatar"><img src="${esc(g.photo)}" alt="${esc(g.name || '')}" loading="lazy" /></div>`;
  }
  return `<div class="g-avatar">${esc(g.photo)}</div>`;
}

function statusChip(status) {
  const s = STATUS[status] || STATUS.out;
  return `<span class="chip ${s.cls}">${s.label}</span>`;
}

/* Azərbaycan hərflərini ASCII-yə çevirir (qrcodejs UTF-8 problemini həll edir) */
function toAscii(s) {
  const map = { 'ə':'e','Ə':'E','ı':'i','İ':'I','ö':'o','Ö':'O','ü':'u','Ü':'U',
                'ç':'c','Ç':'C','ş':'s','Ş':'S','ğ':'g','Ğ':'G' };
  return String(s ?? '').replace(/[əƏıİöÖüÜçÇşŞğĞ]/g, ch => map[ch] || ch);
}

function guestQrPayload(g) {
  // Cihaza yazılan vahid nömrə (employeeNo = cardNo). QR oxutduqda cihaz bunu tanıyır.
  return String(g.accessNumber || g.cardNo || g.doc || '');
}

/* QR kodunu konteynerə çəkir (qrcode.min.js tələb olunur) */
function renderQR(container, text, size) {
  container.innerHTML = '';
  const qr = new QRCode(container, {
    text: text, width: size || 168, height: size || 168,
    colorDark: '#111827', colorLight: '#ffffff', correctLevel: QRCode.CorrectLevel.M,
  });
  const canvas = container.querySelector('canvas');
  const img = container.querySelector('img');
  if (canvas && img) { img.style.display = 'none'; canvas.style.display = 'block'; }
  return qr;
}

function qrDataURL(container) {
  const canvas = container.querySelector('canvas');
  if (canvas) return canvas.toDataURL('image/png');
  const img = container.querySelector('img');
  return img ? img.src : '';
}

/* Mobil sidebar */
function openSidebar()  { document.getElementById('sidebar').classList.add('open');  document.getElementById('backdrop').classList.add('show'); }
function closeSidebar() { document.getElementById('sidebar').classList.remove('open'); document.getElementById('backdrop').classList.remove('show'); }
