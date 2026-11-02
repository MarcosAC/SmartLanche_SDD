# PRD - SmartLanche

## 1. Visão Geral

O sistema será uma aplicação desktop voltada para o gerenciamento de vendas, pedidos, promoções, estoque e clientes de uma lanchonete.

Terá um banco de dados local (instalado na própria máquina) para armazenar todas as informações, garantindo funcionamento offline, sem necessidade de conexão com a internet.

O objetivo é centralizar o cadastro de produtos, criação do menu, controle de pedidos, gerenciamento de estoque, cadastro de clientes, controle de pendências e emissão de relatórios de vendas e estoque.

---

## 2. Objetivos

O sistema deve permitir que o usuário:

* Cadastrar, editar e excluir produtos
* Classificar produtos por categoria
* Criar e atualizar o menu
* Cadastrar promoções e combos
* Registrar e acompanhar pedidos
* Calcular automaticamente o valor dos pedidos
* Controlar o status dos pedidos
* Registrar formas de pagamento
* Cadastrar clientes
* Controlar pedidos realizados na modalidade fiado
* Consultar o saldo devedor dos clientes
* Controlar entradas e saídas do estoque
* Receber alertas de estoque baixo
* Consultar o histórico de movimentações do estoque
* Visualizar o resumo diário das vendas
* Gerar relatórios de vendas e estoque
* Consultar relatórios anteriores
* Enviar relatórios por e-mail quando houver internet disponível

---

## 3. Público-Alvo

* Administradores da lanchonete
* Atendentes
* Funcionários responsáveis pelo acompanhamento dos pedidos
* Funcionários responsáveis pelo controle de estoque
* Equipe responsável pela preparação dos pedidos

---

## 4. Navegação Principal

A aplicação deverá possuir as seguintes páginas:

### Pedidos e Valor Final

Permite registrar novos pedidos, montar os pedidos a partir do menu e visualizar o valor total.

### Promoções

Permite cadastrar e visualizar combos e promoções ativas.

### Pendências

Permite controlar as contas de clientes com valores a pagar.

### Pedidos em Preparação

Permite acompanhar e alterar o status dos pedidos.

### Estoque

Permite controlar a entrada e saída de produtos e consultar as movimentações.

### Resumo de Vendas

Permite visualizar os dados consolidados das vendas do dia e gerar relatórios.

---

## 5. Página Pedidos e Valor Final

A página de pedidos deverá permitir a criação de novos pedidos a partir dos produtos disponíveis no menu.

Cada pedido deverá conter:

* Data
* Hora
* Responsável pelo registro
* Produtos selecionados
* Quantidades
* Valor total
* Forma de pagamento
* Status do pedido

As formas de pagamento disponíveis deverão ser:

* Dinheiro
* Cartão
* Pix
* Fiado

O sistema deverá calcular automaticamente o valor total do pedido.

Também deverá permitir:

* Alterar o status do pedido
* Cancelar pedidos ainda não finalizados
* Vincular pedidos realizados como fiado a um cliente

## 6. Página Promoções

A página de promoções deverá permitir:

* Cadastrar promoções
* Editar promoções
* Excluir promoções
* Cadastrar combos
* Definir preços diferenciados para promoções e combos
* Visualizar promoções ativas

As promoções e combos deverão utilizar produtos previamente cadastrados no sistema.

## 7. Página Pendências

A página de pendências deverá permitir o controle dos valores devidos pelos clientes.

Cada cliente deverá possuir:

* Nome
* Telefone
* Observações
* Pedidos pendentes
* Saldo devedor

O sistema deverá permitir:

* Cadastrar clientes
* Vincular pedidos fiados a clientes
* Consultar o saldo devedor de cada cliente
* Alterar o status de pagamento

Os status de pagamento deverão ser:

* Pago
* Não Pago

## 8. Página Pedidos em Preparação

A página de pedidos em preparação deverá permitir acompanhar os pedidos registrados no sistema.

Cada pedido deverá apresentar:

* Identificação do pedido
* Data
* Hora
* Responsável pelo registro
* Produtos
* Quantidades
* Valor total
* Forma de pagamento
* Status

Os status disponíveis deverão ser:

* Em preparação
* Entregue/Pago
* Entregue/Não Pago

O sistema deverá permitir que os responsáveis atualizem o status dos pedidos durante o processo de atendimento.

## 9. Página Estoque

A página de estoque deverá permitir o gerenciamento dos produtos e suas respectivas quantidades.

Cada item de estoque deverá possuir:

* Nome
* Quantidade
* Valor unitário

O sistema deverá permitir:

* Cadastrar itens de estoque
* Atualizar quantidades
* Registrar entradas manuais
* Registrar saídas manuais
* Atualizar automaticamente o estoque conforme os pedidos são realizados
* Exibir alertas de estoque baixo
* Consultar o histórico de movimentações

O histórico deverá apresentar:

* Data
* Tipo da movimentação
* Quantidade movimentada

## 10. Página Resumo de Vendas

A página de resumo de vendas deverá apresentar os dados consolidados do dia.

O resumo deverá conter:

* Quantidade total de vendas
* Valor total vendido
* Produtos mais vendidos
* Pedidos pendentes

O sistema também deverá permitir:

* Gerar relatórios de vendas
* Gerar relatórios de estoque
* Consultar relatórios anteriores
* Filtrar o histórico de relatórios por data
* Enviar relatórios por e-mail quando houver conexão com a internet

Os relatórios deverão ser gerados em formato Excel.

---

## 11. Fonte dos Dados

Os dados do sistema serão armazenados em um banco de dados local instalado na própria máquina.

Dados previstos:

* Produtos
* Categorias
* Menu
* Promoções
* Combos
* Pedidos
* Itens dos pedidos
* Clientes
* Pendências
* Estoque
* Movimentações de estoque
* Relatórios

O sistema deverá funcionar offline e não depender de conexão com a internet para suas operações principais.

A conexão com a internet será necessária apenas para funcionalidades que dependam de serviços externos, como o envio de relatórios por e-mail.

O sistema deverá possuir backup automático do banco de dados local e também permitir a exportação manual do banco de dados.

---

## 12. Estrutura do Protótipo

O protótipo será desenvolvido como uma aplicação web utilizando HTML, CSS e JavaScript, com foco na apresentação visual do sistema e na demonstração dos principais fluxos de utilização.

Nesta primeira etapa, não será necessária uma arquitetura backend ou banco de dados real. Os dados utilizados pelo protótipo poderão ser estáticos ou armazenados em arquivos JavaScript/JSON, permitindo simular o funcionamento do sistema.

A estrutura deverá ser organizada de forma modular, facilitando uma futura migração para uma aplicação completa.

Estrutura:

```text
lanchonete-prototype/
│
├── index.html
│
├── assets/
│   ├── images/
│   └── icons/
│
├── css/
│   ├── styles.css
│   ├── components.css
│   └── responsive.css
│
├── js/
│   ├── app.js
│   │
│   ├── data/
│   │   ├── products.js
│   │   ├── customers.js
│   │   ├── orders.js
│   │   ├── promotions.js
│   │   └── inventory.js
│   │
│   ├── modules/
│   │   ├── dashboard.js
│   │   ├── orders.js
│   │   ├── promotions.js
│   │   ├── pending-payments.js
│   │   ├── order-preparation.js
│   │   ├── inventory.js
│   │   └── sales-summary.js
│   │
│   └── components/
│       ├── sidebar.js
│       ├── header.js
│       ├── modal.js
│       ├── table.js
│       └── notification.js
│
└── pages/
    ├── dashboard.html
    ├── orders.html
    ├── promotions.html
    ├── pending-payments.html
    ├── order-preparation.html
    ├── inventory.html
    └── sales-summary.html
```

## 12. Fora do Escopo

Não fazem parte da primeira versão:

* Integração com sistemas externos de vendas
* Integração automática com serviços externos de estoque
* Dependência de internet para funcionamento do sistema
* Atualização automática de dados através de serviços externos
* Funcionalidades que dependam obrigatoriamente de conexão com a internet

---

## 13. Evoluções Futuras

Possíveis melhorias:

* Ampliação dos recursos de relatórios
* Novos filtros e indicadores de vendas
* Integração com sistemas externos
* Integração com serviços de pagamento
* Integração com plataformas de delivery
* Atualização automática de informações externas
* Novos recursos de gerenciamento de estoque
* Novos recursos de gerenciamento de clientes
* Expansão das funcionalidades de usuários e permissões
