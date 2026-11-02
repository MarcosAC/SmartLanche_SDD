import { customers } from '../data/customers.js';
import { notify } from '../components/notification.js';

export function init(){render();}
function render(){
 const c=document.getElementById('page-content');
 const debtors=customers.filter(x=>x.balance>0);
 const total=customers.reduce((s,x)=>s+x.balance,0);
 c.innerHTML=`<div class="grid grid-3">
 <div class="card metric"><div class="label">Total pendente</div><div class="value">${money(total)}</div><div class="trend" style="color:#d97706">Pagamentos em aberto</div></div>
 <div class="card metric"><div class="label">Clientes devedores</div><div class="value">${debtors.length}</div><div class="trend">Com saldo em aberto</div></div>
 <div class="card metric"><div class="label">Maior pendência</div><div class="value">${money(Math.max(0,...customers.map(x=>x.balance)))}</div><div class="trend">Por cliente</div></div></div>
 <div class="card section-gap"><div class="card-body"><div class="card-title"><h3>Contas pendentes</h3><span class="badge badge-danger">${debtors.length}</span></div>
 ${debtors.length?`<div class="table-wrap"><table><thead><tr><th>Cliente</th><th>Telefone</th><th>Observações</th><th>Saldo</th><th>Ação</th></tr></thead><tbody>${debtors.map(x=>`<tr><td><strong>${x.name}</strong></td><td>${x.phone}</td><td>${x.observations||'—'}</td><td><strong>${money(x.balance)}</strong></td><td><button class="btn btn-success btn-small" data-pay="${x.id}">Registrar pagamento</button></td></tr>`).join('')}</tbody></table></div>`:'<div class="empty">Nenhuma pendência encontrada.</div>'}
 </div></div>`;
 c.querySelectorAll('[data-pay]').forEach(b=>b.onclick=()=>{const x=customers.find(c=>c.id==b.dataset.pay);x.balance=0;render();notify(`Pagamento de ${x.name} registrado.`);});
}
function money(v){return v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});}
