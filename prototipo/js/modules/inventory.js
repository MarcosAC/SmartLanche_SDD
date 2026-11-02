import { products } from '../data/products.js';
import { inventoryMovements } from '../data/inventory.js';
import { openModal, closeModal } from '../components/modal.js';
import { notify } from '../components/notification.js';

export function init(){render();}
function render(){
 const c=document.getElementById('page-content');
 const low=products.filter(p=>p.stock<=p.minStock);
 c.innerHTML=`<div class="toolbar"><div class="spacer"></div><button class="btn btn-primary" id="move">+ Nova movimentação</button></div>
 ${low.length?`<div class="alert alert-warning"><strong>Atenção:</strong> ${low.length} item(ns) estão com estoque baixo.</div>`:''}
 <div class="card"><div class="card-body"><div class="card-title"><h3>Estoque atual</h3></div>
 <div class="table-wrap"><table><thead><tr><th>Item</th><th>Categoria</th><th>Quantidade</th><th>Mínimo</th><th>Valor unitário</th><th>Status</th></tr></thead><tbody>${products.map(p=>`<tr><td><strong>${p.name}</strong></td><td>${p.category}</td><td>${p.stock}</td><td>${p.minStock}</td><td>${money(p.price)}</td><td><span class="badge ${p.stock<=p.minStock?'badge-warning':'badge-success'}">${p.stock<=p.minStock?'Estoque baixo':'Normal'}</span></td></tr>`).join('')}</tbody></table></div>
 </div></div>
 <div class="card section-gap"><div class="card-body"><div class="card-title"><h3>Histórico de movimentações</h3></div>
 <div class="table-wrap"><table><thead><tr><th>Data</th><th>Tipo</th><th>Item</th><th>Quantidade</th><th>Valor unitário</th></tr></thead><tbody>${inventoryMovements.slice().reverse().map(m=>`<tr><td>${m.date}</td><td><span class="badge ${m.type==='Entrada'?'badge-success':'badge-danger'}">${m.type}</span></td><td>${m.item}</td><td>${m.quantity}</td><td>${money(m.unitValue)}</td></tr>`).join('')}</tbody></table></div>
 </div></div>`;
 c.querySelector('#move').onclick=newMovement;
}
function newMovement(){
 openModal({title:'Nova movimentação',body:`<div class="form-grid">
 <div class="field"><label>Item</label><select id="item">${products.map(p=>`<option value="${p.id}">${p.name}</option>`).join('')}</select></div>
 <div class="field"><label>Tipo</label><select id="type"><option>Entrada</option><option>Saída</option></select></div>
 <div class="field"><label>Quantidade</label><input id="qty" type="number" min="1" value="1"></div></div>`,
 submitText:'Registrar',onSubmit:el=>{
  const p=products.find(x=>x.id==el.querySelector('#item').value);const type=el.querySelector('#type').value;const qty=Number(el.querySelector('#qty').value);
  if(!qty||qty<1){notify('Informe uma quantidade válida.');return;}
  if(type==='Saída'&&p.stock<qty){notify('Quantidade insuficiente em estoque.');return;}
  p.stock += type==='Entrada'?qty:-qty;
  inventoryMovements.push({id:Date.now(),date:'2026-09-13',type,item:p.name,quantity:qty,unitValue:p.price});
  closeModal();render();notify('Movimentação registrada.');
 }});
}
function money(v){return v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});}
