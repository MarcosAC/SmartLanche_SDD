import { orders } from '../data/orders.js';
import { products } from '../data/products.js';
import { notify } from '../components/notification.js';

export function init(){render();}
function render(){
 const paid=orders.filter(o=>o.status==='Entregue/Pago');
 const revenue=paid.reduce((s,o)=>s+o.total,0);
 const byPayment={}; paid.forEach(o=>byPayment[o.payment]=(byPayment[o.payment]||0)+o.total);
 const sales={}; orders.forEach(o=>o.items.forEach(i=>sales[i.productId]=(sales[i.productId]||0)+i.qty));
 const top=Object.entries(sales).sort((a,b)=>b[1]-a[1]).slice(0,6);
 const c=document.getElementById('page-content');
 c.innerHTML=`<div class="toolbar"><div class="field"><label>Período</label><select><option>Hoje</option><option>Últimos 7 dias</option><option>Este mês</option></select></div><div class="spacer"></div><button class="btn btn-secondary" id="export">Exportar relatório</button></div>
 <div class="grid grid-4">${metric('Total vendido',money(revenue)),metric('Pedidos pagos',paid.length),metric('Ticket médio',money(paid.length?revenue/paid.length:0)),metric('Pendentes',orders.filter(o=>o.status==='Entregue/Não Pago').length)}</div>
 <div class="grid grid-2 section-gap">
 <div class="card"><div class="card-body"><div class="card-title"><h3>Vendas por pagamento</h3></div><div class="list">${Object.entries(byPayment).map(([k,v])=>`<div class="list-item"><span>${k}</span><strong>${money(v)}</strong></div>`).join('')||'<div class="empty">Sem vendas pagas.</div>'}</div></div></div>
 <div class="card"><div class="card-body"><div class="card-title"><h3>Produtos mais vendidos</h3></div><div class="list">${top.map(([id,q])=>{const p=products.find(x=>x.id==id);return `<div class="list-item"><span>${p.name}</span><strong>${q} un.</strong></div>`}).join('')}</div></div></div>
 </div>
 <div class="card section-gap"><div class="card-body"><div class="card-title"><h3>Relatório demonstrativo</h3><span class="badge badge-neutral">Protótipo</span></div>
 <p style="color:#667085;font-size:13px">Nesta etapa a exportação é apenas demonstrativa. A geração real de Excel será implementada na aplicação futura.</p>
 <div class="alert alert-info"><strong>Resumo:</strong> ${paid.length} pedidos pagos totalizando ${money(revenue)} no período selecionado.</div></div></div>`;
 c.querySelector('#export').onclick=()=>notify('Exportação demonstrativa: recurso previsto para a versão completa.');
}
function metric(l,v){return `<div class="card metric"><div class="label">${l}</div><div class="value">${v}</div><div class="trend">Dados simulados</div></div>`;}
function money(v){return v.toLocaleString('pt-BR',{style:'currency',currency:'BRL'});}
