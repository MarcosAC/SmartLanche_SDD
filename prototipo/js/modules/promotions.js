import { promotions } from '../data/promotions.js';
import { openModal, closeModal } from '../components/modal.js';
import { notify } from '../components/notification.js';

export function init(){render();}
function render(){
 const c=document.getElementById('page-content');
 c.innerHTML=`<div class="toolbar"><div class="spacer"></div><button class="btn btn-primary" id="new">+ Nova promoção</button></div>
 <div class="grid grid-3">${promotions.map(p=>`<div class="card"><div class="card-body">
 <div class="card-title"><h3>${p.name}</h3><span class="badge ${p.active?'badge-success':'badge-neutral'}">${p.active?'Ativa':'Inativa'}</span></div>
 <p style="color:#667085;font-size:13px">${p.description}</p><div class="metric-row"><strong style="font-size:20px">${money(p.price)}</strong><button class="btn btn-secondary btn-small" data-toggle="${p.id}">${p.active?'Desativar':'Ativar'}</button></div>
 </div></div>`).join('')}</div>`;
 c.querySelector('#new').onclick=()=>newPromotion();
 c.querySelectorAll('[data-toggle]').forEach(b=>b.onclick=()=>{const p=promotions.find(x=>x.id==b.dataset.toggle);p.active=!p.active;render();notify('Promoção atualizada.');});
}
function newPromotion(){
 openModal({title:'Nova promoção',body:`<div class="form-grid">
 <div class="field"><label>Nome</label><input id="name" placeholder="Ex.: Combo da Casa"></div>
 <div class="field"><label>Preço promocional</label><input id="price" type="number" step=".01"></div>
 <div class="field full"><label>Descrição</label><input id="description" placeholder="Produtos incluídos no combo"></div></div>`,
 submitText:'Criar promoção',onSubmit:el=>{
  const name=el.querySelector('#name').value.trim(); const price=Number(el.querySelector('#price').value);
  if(!name||!price){notify('Informe nome e preço.');return;}
  promotions.push({id:Date.now(),name,description:el.querySelector('#description').value,price,active:true});
  closeModal();render();notify('Promoção criada.');
 }});
}
function money(v){return v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});}
