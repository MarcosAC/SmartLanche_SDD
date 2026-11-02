# PRD - SmartLanche

## 1. Visão Geral

O SmartLanche é um sistema de gerenciamento de lanchonete destinado ao controle de vendas, pedidos, promoções, estoque e clientes.

A primeira etapa do projeto será desenvolvida como um **protótipo web**, utilizando HTML, CSS e JavaScript, com foco na apresentação visual do sistema e na demonstração dos principais fluxos de utilização.

O protótipo não utilizará backend ou banco de dados real. Os dados utilizados na demonstração serão fictícios e poderão ser armazenados em arquivos JavaScript/JSON e, quando necessário, no `localStorage` do navegador.

O objetivo do protótipo é validar a experiência de uso, a navegação, a organização das informações e os principais fluxos do sistema antes da implementação da versão completa.

A versão futura poderá evoluir para uma aplicação completa com backend, banco de dados local, autenticação, controle de permissões e demais recursos necessários para utilização em ambiente de produção.

---

## 2. Objetivos

O protótipo deve permitir demonstrar os seguintes fluxos:

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
* Demonstrar alertas de estoque baixo
* Consultar o histórico de movimentações do estoque
* Visualizar o resumo diário das vendas
* Demonstrar a geração de relatórios de vendas e estoque
* Consultar relatórios anteriores
* Demonstrar o fluxo de envio de relatórios por e-mail

> Os recursos acima representam os fluxos que deverão ser demonstrados no protótipo. A implementação real dessas funcionalidades dependerá da versão definitiva do sistema.

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

### Dashboard

Página inicial do sistema, apresentando um resumo das principais informações da lanchonete.

Deverá apresentar indicadores como:

* Vendas do dia
* Quantidade de pedidos
* Pedidos em preparação
* Pendências de pagamento
* Produtos com estoque baixo

### Pedidos e Valor Final

Permite registrar novos pedidos, montar pedidos a partir do menu e visualizar o valor total.

### Promoções

Permite cadastrar e visualizar combos e promoções ativas.

### Pendências

Permite controlar as contas de clientes com valores a pagar.

### Pedidos em Preparação

Permite acompanhar e alterar o status dos pedidos.

### Estoque

Permite controlar a entrada e saída de produtos e consultar as movimentações.

### Resumo de Vendas

Permite visualizar os dados consolidados das vendas e demonstrar a geração de relatórios.

---

## 5. Página Dashboard

A Dashboard deverá funcionar como página inicial do protótipo.

Deverá apresentar:

### Indicadores

* Total de vendas do dia
* Quantidade de pedidos
* Ticket médio
* Pedidos em preparação
* Total de pendências

### Produtos Mais Vendidos

Exibir os produtos com maior quantidade de vendas.

### Pedidos em Preparação

Exibir os pedidos que estão aguardando preparação ou entrega.

### Estoque Baixo

Exibir produtos que atingiram ou estão abaixo do nível mínimo configurado.

### Ações Rápidas

Disponibilizar atalhos para:

* Novo pedido
* Ver pedidos em preparação
* Consultar estoque
* Consultar pendências

---

## 6. Página Pedidos e Valor Final

A página de pedidos deverá permitir a criação de novos pedidos a partir dos produtos disponíveis no menu.

Cada pedido deverá conter:

* Identificação do pedido
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

O protótipo deverá calcular automaticamente o valor total do pedido.

### Interações

O protótipo deverá permitir demonstrar:

* Adicionar produtos ao pedido
* Alterar quantidades
* Remover produtos
* Calcular subtotal
* Calcular total
* Selecionar forma de pagamento
* Selecionar cliente para pedidos fiados
* Confirmar pedido
* Cancelar pedido
* Alterar status do pedido

---

## 7. Página Promoções

A página de promoções deverá permitir demonstrar:

* Cadastro de promoções
* Edição de promoções
* Exclusão de promoções
* Cadastro de combos
* Definição de preços diferenciados
* Visualização de promoções ativas

As promoções e combos deverão utilizar produtos previamente cadastrados no protótipo.

---

## 8. Página Pendências

A página de pendências deverá permitir o controle visual dos valores devidos pelos clientes.

Cada cliente deverá possuir:

* Nome
* Telefone
* Observações
* Pedidos pendentes
* Saldo devedor

O protótipo deverá permitir demonstrar:

* Cadastro de clientes
* Associação de pedidos fiados
* Consulta do saldo devedor
* Visualização dos pedidos pendentes
* Alteração do status de pagamento

Os status de pagamento deverão ser:

* Pago
* Não Pago

---

## 9. Página Pedidos em Preparação

A página de pedidos em preparação deverá permitir acompanhar os pedidos registrados.

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

O protótipo deverá permitir demonstrar a alteração do status dos pedidos.

### Fluxo

```text
Novo Pedido
     ↓
Em preparação
     ↓
Entregue/Pago
```

ou:

```text
Novo Pedido
     ↓
Em preparação
     ↓
Entregue/Não Pago
     ↓
Pagamento da pendência
     ↓
Pago
```

---

## 10. Página Estoque

A página de estoque deverá permitir demonstrar o gerenciamento dos produtos e suas respectivas quantidades.

Cada item de estoque deverá possuir:

* Nome
* Quantidade
* Valor unitário
* Quantidade mínima

O protótipo deverá permitir demonstrar:

* Cadastro de itens
* Atualização de quantidades
* Entrada manual
* Saída manual
* Atualização automática após realização de pedidos
* Alertas de estoque baixo
* Consulta do histórico de movimentações

O histórico deverá apresentar:

* Data
* Tipo da movimentação
* Quantidade movimentada

### Tipos de movimentação

* Entrada
* Saída

---

## 11. Página Resumo de Vendas

A página de resumo de vendas deverá apresentar os dados consolidados.

O resumo deverá conter:

* Quantidade total de vendas
* Valor total vendido
* Ticket médio
* Produtos mais vendidos
* Pedidos pendentes

### Relatórios

O protótipo deverá demonstrar:

* Geração de relatório de vendas
* Geração de relatório de estoque
* Consulta de relatórios anteriores
* Filtro por data
* Ação de envio do relatório por e-mail

A geração do arquivo Excel poderá ser simulada no protótipo ou implementada utilizando uma biblioteca JavaScript apropriada.

O envio de e-mail poderá ser representado por uma interação simulada, sem necessidade de integração real com um servidor de e-mail.

---

## 12. Fonte dos Dados

O protótipo utilizará dados fictícios para representar o funcionamento do sistema.

Os dados poderão ser organizados em arquivos JavaScript:

```text
js/data/
├── products.js
├── customers.js
├── orders.js
├── promotions.js
└── inventory.js
```

Os dados deverão representar situações realistas para facilitar a apresentação do sistema.

Exemplos:

* Produtos cadastrados
* Categorias
* Clientes
* Pedidos
* Promoções
* Estoque
* Movimentações
* Relatórios

### Persistência do Protótipo

Quando necessário, o protótipo poderá utilizar:

```text
JavaScript
    ↓
localStorage
```

O `localStorage` será utilizado apenas para simular persistência durante a demonstração.

Não deverá ser tratado como banco de dados definitivo da aplicação.

### Dados Iniciais

O protótipo deverá possuir dados pré-carregados para que, ao ser executado, já apresente informações suficientes para demonstrar as funcionalidades.

---

## 13. Estrutura do Protótipo

O protótipo será desenvolvido como uma aplicação web utilizando:

* HTML5
* CSS3
* JavaScript

A arquitetura deverá manter separação entre:

* Dados
* Lógica dos módulos
* Componentes reutilizáveis
* Apresentação

A estrutura deverá ser modular e preparada para uma futura evolução da aplicação.

### Estrutura de Pastas

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

### index.html

Será o ponto de entrada da aplicação.

Deverá carregar:

* Layout principal
* Menu de navegação
* Scripts
* Estilos
* Página inicial

### assets/

Deverá armazenar recursos estáticos:

```text
assets/
├── images/
└── icons/
```

### css/

Deverá armazenar os estilos da aplicação:

```text
css/
├── styles.css
├── components.css
└── responsive.css
```

#### styles.css

Estilos gerais:

* Tipografia
* Layout
* Variáveis
* Cores
* Espaçamentos

#### components.css

Estilos dos componentes reutilizáveis:

* Cards
* Botões
* Tabelas
* Modais
* Formulários
* Badges
* Alertas

#### responsive.css

Regras de adaptação para diferentes tamanhos de tela.

### js/data/

Responsável pelos dados utilizados na demonstração.

### js/modules/

Responsável pela lógica específica de cada módulo.

### js/components/

Responsável pelos componentes reutilizáveis da interface.

---

## 14. Fora do Escopo

Não fazem parte desta versão de protótipo:

* Backend
* Banco de dados real
* API
* Autenticação real
* Controle de permissões real
* Integração com sistemas externos de vendas
* Integração automática com serviços externos de estoque
* Integração real com plataformas de delivery
* Integração real com serviços de pagamento
* Atualização automática através de APIs externas
* Envio real de e-mails
* Funcionamento em ambiente de produção

O objetivo desta versão é exclusivamente demonstrar a interface, navegação e principais fluxos do sistema.

---

## 15. Evoluções Futuras

Após a validação do protótipo, o sistema poderá evoluir para uma aplicação completa.

Possíveis evoluções:

* Backend
* Banco de dados local
* API
* Autenticação
* Controle de usuários e permissões
* Persistência definitiva dos dados
* Backup automático
* Exportação de dados
* Geração real de relatórios Excel
* Envio real de relatórios por e-mail
* Ampliação dos recursos de relatórios
* Novos filtros e indicadores
* Integração com sistemas externos
* Integração com serviços de pagamento
* Integração com plataformas de delivery
* Atualização automática de informações externas
* Novos recursos de gerenciamento de estoque
* Novos recursos de gerenciamento de clientes
* Dashboard avançado

---

## 16. Critérios de Aceitação do Protótipo

O protótipo será considerado concluído quando:

* [ ] A aplicação abrir através do `index.html`.
* [ ] A Dashboard apresentar dados fictícios.
* [ ] A navegação entre os módulos estiver funcionando.
* [ ] For possível visualizar produtos.
* [ ] For possível simular a criação de um pedido.
* [ ] O total do pedido for calculado automaticamente.
* [ ] For possível selecionar a forma de pagamento.
* [ ] For possível simular um pedido fiado.
* [ ] For possível consultar pendências.
* [ ] O saldo devedor for apresentado corretamente.
* [ ] For possível alterar o status de pagamento.
* [ ] For possível visualizar pedidos em preparação.
* [ ] For possível alterar o status de um pedido.
* [ ] For possível visualizar o estoque.
* [ ] For possível simular entradas e saídas de estoque.
* [ ] O alerta de estoque baixo for apresentado.
* [ ] O histórico de movimentações estiver disponível.
* [ ] O resumo de vendas apresentar dados coerentes.
* [ ] Os produtos mais vendidos forem apresentados.
* [ ] O protótipo demonstrar a geração de relatórios.
* [ ] A interface for visualmente consistente.
* [ ] A interface for responsiva.
* [ ] Os componentes reutilizáveis forem utilizados quando aplicável.
* [ ] Os dados estiverem separados da lógica dos módulos.
* [ ] A estrutura estiver preparada para futura evolução.

---

## 17. Princípios do Protótipo

O desenvolvimento deverá seguir os seguintes princípios:

```text
Simplicidade
Modularidade
Reutilização
Responsividade
Boa experiência de uso
Separação de responsabilidades
Dados fictícios
Offline First
Facilidade de demonstração
Facilidade de evolução
```

O protótipo deverá priorizar a qualidade visual e a experiência de navegação, evitando complexidade técnica desnecessária nesta etapa.

A implementação deverá demonstrar os principais fluxos do negócio sem criar dependências de infraestrutura que somente serão necessárias na versão definitiva.