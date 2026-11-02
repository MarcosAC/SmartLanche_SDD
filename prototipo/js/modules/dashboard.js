import { products } from '../data/products.js';
import { orders } from '../data/orders.js';
import { customers } from '../data/customers.js';

export function init() {
  const content = document.getElementById('page-content');
  const paid = orders.filter(o => o.status === 'Entregue/Pago');
  const revenue = paid.reduce((s,o)=>s+o.total,0);
  const avg = paid.length ? revenue / paid.length : 0;
  const preparing = orders.filter(o=>o.status==='Em preparação').length;
  const pending = customers.reduce((s,c)=>s+c.balance,0);
  const low = products.filter(p=>p.stock <= p.minStock);
  const sales = {};
  orders.forEach(o => o.items.forEach(i => sales[i.productId]=(sales[i.productId]||0)+i.qty));
  const top = Object.entries(sales).sort((a,b)=>b[1]-a[1]).slice(0,5);

  content.innerHTML = `
    <div class="grid grid-4">
      ${metric('Vendas do dia', money(revenue), '↗ Dados demonstrativos')}
      ${metric('Pedidos', orders.length, 'Total registrado hoje', 'stat-icon-info')}
      ${metric('Ticket médio', money(avg), 'Entre pedidos pagos', 'stat-icon-success')}
      ${metric('Em preparação', preparing, 'Aguardando produção', 'stat-icon-warning')}
    </div>
    <div class="grid grid-2 section-gap">
      <div class="card"><div class="card-body">
        <div class="card-title"><h3>Produtos mais vendidos</h3><span class="badge badge-info">Hoje</span></div>
        <div class="list">${top.map(([id,qty]) => {
          const p=products.find(x=>x.id==id); return `<div class="list-item"><div><strong>${p.name}</strong><br><small style="color:#667085">${p.category}</small></div><strong>${qty} un.</strong></div>`;
        }).join('')}</div>
      </div></div>
      <div class="card"><div class="card-body">
        <div class="card-title"><h3>Alertas</h3><span class="badge badge-warning">${low.length}</span></div>
        ${low.map(p=>`<div class="alert alert-warning"><strong>${p.name}</strong> está com ${p.stock} unidades. Mínimo: ${p.minStock}.</div>`).join('') || '<div class="empty">Nenhum produto com estoque baixo.</div>'}
        <div class="alert alert-info"><strong>Pendências:</strong> ${money(pending)} em pagamentos fiados.</div>
      </div></div>
    </div>
    <div class="card section-gap"><div class="card-body">
      <div class="card-title"><h3>Ações rápidas</h3></div>
      <div class="quick-actions">
        <a class="btn btn-primary" href="orders.html">+ Novo pedido</a>
        <a class="btn btn-secondary" href="inventory.html">📦 Ver estoque</a>
        <a class="btn btn-secondary" href="pending-payments.html">💳 Pendências</a>
        <a class="btn btn-secondary" href="sales-summary.html">📈 Relatório de vendas</a>
      </div>
    </div></div>`;
}
function metric(label,value,trend,icon='') {
 return `<div class="card metric"><div class="metric-row"><div><div class="label">${label}</div><div class="value">${value}</div><div class="trend">${trend}</div></div><div class="kpi-icon ${icon}">●</div></div></div>`;
}
function money(v){return v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});}
