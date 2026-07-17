/* =========================================================
   guests-mvc.js — Qonaqlar səhifəsi (MVC) müştəri məntiqi:
   • Filtr/axtarış (server-render olunmuş sətirlər üzərində)
   • Yeni qonaq modalı (aç/bağla, buraxılış növü, fayl önizləmə)
   • Buraxılış sənədi paneli (GUESTS_DATA + panel.js)
   Data server-render olunub; JS yalnız interaktivliyi idarə edir.
   ========================================================= */

/* ---------- Filtr / axtarış ---------- */
function applyFilters() {
  const q       = (document.getElementById('regSearch').value || '').trim().toLowerCase();
  const status  = document.getElementById('fStatus').value;
  const host    = document.getElementById('fHost').value;
  const area    = document.getElementById('fArea').value;
  const purpose = document.getElementById('fPurpose').value;

  let count = 0;
  document.querySelectorAll('#registryRows tr').forEach(tr => {
    const d = tr.dataset;
    const matchQ = !q || (d.search || '').includes(q);
    const matchS = !status  || d.status === status;
    const matchH = !host    || d.host === host;
    const matchA = !area    || (d.area || '').includes(area);
    const matchP = !purpose || (d.purpose || '').includes(purpose);
    const show = matchQ && matchS && matchH && matchA && matchP;
    tr.style.display = show ? '' : 'none';
    if (show) count++;
  });
  const c = document.getElementById('resultCount');
  if (c) c.textContent = `${count} nəticə`;
}

/* ---------- Modal ---------- */
function openModal()  { document.getElementById('guestModal').classList.add('open'); document.body.style.overflow = 'hidden'; }
function closeModal() { document.getElementById('guestModal').classList.remove('open'); document.body.style.overflow = ''; }

let passChoice = 'card';
function setPassChoice(choice) {
  passChoice = choice;
  const input = document.getElementById('passTypeInput');
  if (input) input.value = choice;
  document.querySelectorAll('.pass-choice .seg-btn').forEach(b => {
    b.classList.toggle('active', b.dataset.pass === choice);
  });
  const cardField = document.getElementById('cardSelectField');
  const qrNote    = document.getElementById('qrNoteField');
  const inCard    = document.getElementById('inCard');
  if (cardField) cardField.style.display = choice === 'card' ? '' : 'none';
  if (qrNote)    qrNote.style.display    = choice === 'qr'   ? '' : 'none';
  // QR seçimində kart tələb olunmasın
  if (inCard) inCard.disabled = choice !== 'card';
}

/* 24 saatlıq vaxt sahəsi — yalnız rəqəm, avtomatik "SS:DD" (00:00–23:59) */
function attachTime24(input) {
  input.addEventListener('input', () => {
    let d = input.value.replace(/\D/g, '').slice(0, 4);
    if (d.length >= 3) d = d.slice(0, 2) + ':' + d.slice(2);
    input.value = d;
  });
  input.addEventListener('blur', () => {
    const m = input.value.match(/^(\d{1,2}):?(\d{0,2})$/);
    if (!m) { if (input.value) input.value = ''; return; }
    let h = Math.min(parseInt(m[1] || '0', 10), 23);
    let mi = Math.min(parseInt(m[2] || '0', 10), 59);
    input.value = String(h).padStart(2, '0') + ':' + String(mi).padStart(2, '0');
  });
}

document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.time24').forEach(attachTime24);

  ['regSearch','fStatus','fHost','fArea','fPurpose'].forEach(id => {
    const el = document.getElementById(id);
    if (el) el.addEventListener(el.tagName === 'SELECT' ? 'change' : 'input', applyFilters);
  });

  /* Buraxılış sənədi paneli — goz ikonu */
  document.getElementById('registryRows').addEventListener('click', e => {
    const view = e.target.closest('.act-btn.view');
    if (!view) return;
    const g = (window.GUESTS_DATA || []).find(x => String(x.id) === view.dataset.id);
    if (g) openGuestPanel(g);
  });

  /* Modal düymələri */
  document.getElementById('btnNewGuest')?.addEventListener('click', () => { setPassChoice('card'); openModal(); });
  document.getElementById('modalClose')?.addEventListener('click', closeModal);
  document.getElementById('modalCancel')?.addEventListener('click', closeModal);
  document.getElementById('guestModal')?.addEventListener('click', e => {
    if (e.target.id === 'guestModal') closeModal();
  });
  document.addEventListener('keydown', e => { if (e.key === 'Escape') closeModal(); });

  /* Buraxılış növü seqment düymələri */
  document.querySelectorAll('.pass-choice .seg-btn').forEach(b => {
    b.addEventListener('click', () => setPassChoice(b.dataset.pass));
  });
  setPassChoice('card');

  /* Şəkil yükləmə önizləmə */
  const photoBox = document.getElementById('photoBox');
  const photoInput = document.getElementById('inPhotoFile');
  photoBox?.addEventListener('click', () => photoInput?.click());
  photoInput?.addEventListener('change', () => {
    const file = photoInput.files?.[0];
    const lbl = document.getElementById('photoBoxLabel');
    const prev = document.getElementById('photoPreview');
    if (!file) return;
    if (lbl) lbl.textContent = file.name;
    if (prev) { prev.src = URL.createObjectURL(file); prev.style.display = 'block'; }
  });

  /* Sənəd seçimi */
  const docInput = document.getElementById('inDocFile');
  document.getElementById('btnDocFile')?.addEventListener('click', () => docInput?.click());
  docInput?.addEventListener('change', () => {
    const file = docInput.files?.[0];
    const ds = document.getElementById('docStatus');
    if (file && ds) ds.textContent = file.name;
  });
});
