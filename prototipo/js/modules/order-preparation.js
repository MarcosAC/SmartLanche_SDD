import { orders } from '../data/orders.js';
import { products } from '../data/products.js';
import { customers } from '../data/customers.js';
import { notify } from '../components/notification.js';

export function init(){render();}
function render(){
 const prep=orders.filter(o=>o.status==='Em preparação');
 const c=document.getElementById('page-content');
 c.innerHTML=`<div class="grid grid-3">${prep.map(o=>`<div class="card"><div class="card-body">
 <div class="metric-row"><strong>#${o.id}</strong><span class="badge badge-warning">Em preparação</span></div>
 <p style="color:#667085;font-size:12px;margin:8px 0">${o.time} • ${customers.find(c=>c.id===o.customerId)?.name||'Consumidor'}</p>
 <div class="list">${o.items.map(i=>{const p=products.find(x=>x.id===i.productId);return `<div class="list-item"><span>${i.qty}x ${p.name}</span><strong>${money(p.price*i.qty)}</strong></div>`}).join('')}</div>
 <button class="btn btn-success" style="width:100%;margin-top:12px" data-done="${o.id}">Marcar como entregue/pago</button>
 </div></div>`).join('') || '<div class="card" style="grid-column:1/-1"><div class="empty">Nenhum pedido em preparação.</div></div>'}</div>`;
 c.querySelectorAll('[data-done]').forEach(b=>b.onclick=()=>{const o=orders.find(x=>x.id==b.dataset.done);o.status=o.payment==='Fiado'?'Entregue/Não Pago':'Entregue/Pago';render();notify(`Pedido #${o.id} atualizado.`);});
}
function money(v){return v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});}
