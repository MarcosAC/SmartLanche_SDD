export function openModal({ title, body, onSubmit, submitText='Salvar' }) {
  closeModal();
  const el = document.createElement('div');
  el.className = 'modal-backdrop open';
  el.id = 'global-modal';
  el.innerHTML = `<div class="modal">
    <div class="modal-header"><h3>${title}</h3><button class="close" data-close>×</button></div>
    <div class="modal-body">${body}</div>
    <div class="modal-footer"><button class="btn btn-secondary" data-close>Cancelar</button><button class="btn btn-primary" id="modal-submit">${submitText}</button></div>
  </div>`;
  document.body.appendChild(el);
  el.querySelectorAll('[data-close]').forEach(b => b.onclick = closeModal);
  el.onclick = e => { if (e.target === el) closeModal(); };
  el.querySelector('#modal-submit').onclick = () => {
    if (onSubmit) onSubmit(el);
  };
}
export function closeModal() {
  document.getElementById('global-modal')?.remove();
}
