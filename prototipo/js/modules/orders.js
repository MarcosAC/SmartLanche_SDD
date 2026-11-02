import { products } from '../data/products.js';
import { customers } from '../data/customers.js';
import { orders } from '../data/orders.js';
import { openModal, closeModal } from '../components/modal.js';
import { notify } from '../components/notification.js';

let cart = [];

export function init() {
  render();
}
function render() {
  const content=document.getElementById('page-content');
  content.innerHTML=`<div class="toolbar"><div class="spacer"></div><button class="btn btn-primary" id="new-order">+ Novo pedido</button></div>
  <div class="order-builder">
    <div class="card"><div class="card-body">
      <div class="card-title"><h3>Cardápio</h3><span class="badge badge-neutral">${products.length} produtos</span></div>
      <div class="product-grid">${products.map(p=>`<button class="product-card" data-product="${p.id}">
        <h4>${p.name}</h4><p>${p.category}</p><span class="product-price">${money(p.price)}</span>
      </button>`).join('')}</div>
    </div></div>
    <div class="card"><div class="card-body">
      <div class="card-title"><h3>Novo pedido</h3><span class="badge badge-info">#${nextId()}</span></div>
      <div id="cart">${cartHtml()}</div>
      <div class="total"><span>Total</span><span>${money(total())}</span></div>
      <button class="btn btn-primary" style="width:100%;margin-top:16px" id="finish">Finalizar pedido</button>
    </div></div>
  </div>
  <div class="card section-gap"><div class="card-body">
    <div class="card-title"><h3>Pedidos recentes</h3></div>
    ${ordersTable()}
  </div></div>`;
  content.querySelectorAll('[data-product]').forEach(b=>b.onclick=()=>add(+b.dataset.product));
  document.getElementById('finish').onclick=finish;
  content.querySelectorAll('[data-minus]').forEach(b=>b.onclick=()=>change(+b.dataset.minus,-1));
  content.querySelectorAll('[data-plus]').forEach(b=>b.onclick=()=>change(+b.dataset.plus,1));
}
function add(id){ const item=cart.find(i=>i.productId===id); if(item)item.qty++; else cart.push({productId:id,qty:1}); render(); }
function change(id,delta){ const i=cart.find(x=>x.productId===id); if(!i)return; i.qty+=delta; if(i.qty<=0)cart=cart.filter(x=>x!==i); render(); }
function cartHtml(){ if(!cart.length)return '<div class="empty">Selecione produtos do cardápio.</div>'; return cart.map(i=>{const p=products.find(x=>x.id===i.productId);return `<div class="cart-item"><div><strong>${p.name}</strong><br><small>${money(p.price)} cada</small></div><div class="qty"><button data-minus="${p.id}">−</button><strong>${i.qty}</strong><button data-plus="${p.id}">+</button></div><strong>${money(p.price*i.qty)}</strong></div>`}).join(''); }
function total(){return cart.reduce((s,i)=>s+products.find(p=>p.id===i.productId).price*i.qty,0);}
function nextId(){return Math.max(...orders.map(o=>o.id))+1;}
function finish(){
 if(!cart.length){notify('Adicione pelo menos um produto.');return;}
 openModal({title:'Finalizar pedido',submitText:'Registrar pedido',body:`<div class="form-grid">
 <div class="field"><label>Responsável</label><input id="responsible" value="Atendente"></div>
 <div class="field"><label>Forma de pagamento</label><select id="payment"><option>Dinheiro</option><option>Cartão</option><option>Pix</option><option>Fiado</option></select></div>
 <div class="field full"><label>Cliente (necessário para Fiado)</label><select id="customer"><option value="">Consumidor não identificado</option>${customers.map(c=>`<option value="${c.id}">${c.name}</option>`).join('')}</select></div>
 <div class="field full"><label>Valor final</label><input value="${money(total())}" disabled></div></div>`,
 onSubmit: el=>{
   const payment=el.querySelector('#payment').value;
   const customerId=el.querySelector('#customer').value ? +el.querySelector('#customer').value : null;
   if(payment==='Fiado'&&!customerId){notify('Selecione um cliente para o pedido fiado.');return;}
   const now=new Date();
   orders.unshift({id:nextId(),date:'2026-09-13',time:now.toLocaleTimeString('pt-BR',{hour:'2-digit',minute:'2-digit'}),responsible:el.querySelector('#responsible').value,customerId,items:[...cart],total:total(),payment,status:'Em preparação'});
   if(payment==='Fiado'){const c=customers.find(x=>x.id===customerId);c.balance+=total();}
   cart=[]; closeModal(); render(); notify('Pedido registrado com sucesso.');
 }
 });
}
function ordersTable(){
 return `<div class="table-wrap"><table><thead><tr><th>Pedido</th><th>Horário</th><th>Cliente</th><th>Pagamento</th><th>Total</th><th>Status</th></tr></thead><tbody>${orders.map(o=>`<tr><td><strong>#${o.id}</strong></td><td>${o.time}</td><td>${customers.find(c=>c.id===o.customerId)?.name||'Consumidor'}</td><td>${o.payment}</td><td>${money(o.total)}</td><td><span class="badge ${o.status==='Em preparação'?'badge-warning':o.status==='Entregue/Pago'?'badge-success':'badge-danger'}">${o.status}</span></td></tr>`).join('')}</tbody></table></div>`;
}
function money(v){return v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});}
