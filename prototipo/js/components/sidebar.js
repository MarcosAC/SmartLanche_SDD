export function renderSidebar(active) {
  const items = [
    ['dashboard.html', '📊', 'Dashboard'],
    ['orders.html', '🧾', 'Pedidos e Valor Final'],
    ['promotions.html', '🏷️', 'Promoções'],
    ['pending-payments.html', '💳', 'Pendências'],
    ['order-preparation.html', '🍔', 'Pedidos em Preparação'],
    ['inventory.html', '📦', 'Estoque'],
    ['sales-summary.html', '📈', 'Resumo de Vendas']
  ];
  const pathPrefix = location.pathname.includes('/pages/') ? '' : 'pages/';
  const current = location.pathname.split('/').pop() || 'dashboard.html';
  return `<aside class="sidebar">
    <div class="brand">
      <div class="brand-mark">🍔</div>
      <div><h1>SmartLanche</h1><small>Gestão da lanchonete</small></div>
    </div>
    <div class="nav-label">Menu principal</div>
    ${items.map(([href, icon, label]) => `<a class="nav-item ${current === href ? 'active' : ''}" href="${pathPrefix}${href}">
      <span class="nav-icon">${icon}</span><span>${label}</span>
    </a>`).join('')}
  </aside>`;
}
