/* =========================================================
   users-mvc.js — İstifadəçi redaktə modalı (MVC).
   Sətirdəki "Redaktə" düyməsindən data-* oxunub modal doldurulur.
   ========================================================= */

function openEditModal(data) {
  document.getElementById('eId').value = data.id;
  document.getElementById('eFirst').value = data.first || '';
  document.getElementById('eLast').value = data.last || '';
  document.getElementById('eEmail').value = data.email || '';
  document.getElementById('ePass').value = '';
  const role = document.getElementById('eRole');
  if (role) role.value = data.role;
  document.getElementById('editModal').classList.add('open');
  document.body.style.overflow = 'hidden';
}

function closeEditModal() {
  document.getElementById('editModal').classList.remove('open');
  document.body.style.overflow = '';
}

document.addEventListener('DOMContentLoaded', () => {
  document.querySelector('.users-table')?.addEventListener('click', e => {
    const btn = e.target.closest('button[data-act="edit"]');
    if (!btn) return;
    openEditModal({
      id: btn.dataset.id, first: btn.dataset.first, last: btn.dataset.last,
      email: btn.dataset.email, role: btn.dataset.role,
    });
  });

  document.getElementById('editClose')?.addEventListener('click', closeEditModal);
  document.getElementById('editCancel')?.addEventListener('click', closeEditModal);
  document.getElementById('editModal')?.addEventListener('click', e => {
    if (e.target.id === 'editModal') closeEditModal();
  });
  document.addEventListener('keydown', e => { if (e.key === 'Escape') closeEditModal(); });
});
