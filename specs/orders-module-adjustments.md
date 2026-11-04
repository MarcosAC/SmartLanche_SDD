# Spec: Ajustes no Módulo de Pedidos

## Visão Geral
Ajustes no módulo de Pedidos para corrigir fluxo de status, funcionalidade de busca, botões de ação, seleção de produtos e carregamento do cardápio.

---

## Requisitos

### 1. Ajustar Fluxo de Status do Pedido
**Atual**: Fluxo de status desconhecido/incorreto  
**Requerido**: Cinco status em ordem de fluxo:
- `Pendente` - Estado inicial quando pedido criado
- `Em Preparo` - Cozinha iniciou preparação
- `Pronto` - Pedido pronto para retirada/entrega
- `Finalizado` - Pedido entregue/retirado, pagamento confirmado
- `Cancelado` - Pedido cancelado em qualquer momento

**Transições**:
- Pendente → Em Preparo → Pronto → Finalizado
- Qualquer status → Cancelado (exceto Finalizado)

### 2. Corrigir Busca de Pedidos por Cliente
**Problema**: Busca por cliente não retorna pedidos do cliente pesquisado  
**Causa Raiz**: Provavelmente predicado de filtro incorreto ou join/include ausente para propriedade de navegação Customer  
**Correção**: Garantir que filtro `CustomerId` funcione corretamente em `OrderListViewModel.LoadAsync()` e query do repositório

### 3. Corrigir Ações do Pedido Não Funcionando
**Problema**: Botões de ação (transições de status, cancelar, etc.) não funcionam  
**Possíveis Causas**:
- Commands não vinculados corretamente na View
- Commands da ViewModel não implementados/problemas async
- Métodos de serviço ausentes ou incorretos
- Parâmetros de command ausentes

**Ações a verificar/corrigir**:
- Alterar status (Pendente → Em Preparo → Pronto → Finalizado)
- Cancelar pedido (qualquer status não finalizado)
- Ver detalhes do pedido
- Imprimir/exportar pedido

### 4. Remover Botão "Selecionar Produto"
**Atual**: Botão existe para abrir seletor de produtos  
**Requerido**: Remover botão completamente  
**Alternativa**: Produtos devem ser carregados diretamente no formulário do pedido (ver requisito 5)

### 5. Carregar Cardápio com Produtos ao Navegar para Novo Pedido
**Atual**: Produtos carregados via botão separado "Selecionar Produto"  
**Requerido**: Carregar produtos automaticamente ao navegar para tela "Novo Pedido"
- Carregar todos produtos ativos agrupados por categoria
- Exibir em grid/lista de produtos dentro do formulário do pedido
- Suportar adicionar produtos ao pedido diretamente deste grid

### 6. Adicionar Opção "Todos" no ComboBox de Categoria
**Atual**: Filtro de categoria mostra apenas categorias específicas  
**Requerido**: Adicionar "Todos" como primeira opção no ComboBox de categoria
- Seleção padrão: "Todos"
- Quando "Todos" selecionado, mostrar produtos de todas categorias
- Quando categoria específica selecionada, filtrar produtos por essa categoria

---

## Detalhes Técnicos

### Arquivos a Modificar

#### Camada Domain
- `src/SmartLanche.Domain/Entities/Order.cs` - Enum de status, validação de transições
- `src/SmartLanche.Domain/Enums/OrderStatus.cs` - Atualizar valores do enum

#### Camada Application
- `src/SmartLanche.Application/DTOs/OrderDto.cs` - Propriedade Status
- `src/SmartLanche.Application/Services/OrderService.cs` - Métodos de transição de status, correção de busca
- `src/SmartLanche.Application/Interfaces/IOrderService.cs` - Novas assinaturas de métodos

#### Camada Infrastructure
- `src/SmartLanche.Infrastructure/Repositories/OrderRepository.cs` - Correção busca por cliente
- `src/SmartLanche.Infrastructure/Data/SmartLancheDbContext.cs` - Mapeamento enum se necessário

#### Camada Presentation
- `src/SmartLanche.Presentation/ViewModels/Orders/OrderListViewModel.cs` - Correção busca, commands de ações
- `src/SmartLanche.Presentation/ViewModels/Orders/OrderFormViewModel.cs` - Carregar produtos na inicialização, filtro categoria, adicionar produto ao pedido
- `src/SmartLanche.Presentation/Views/Orders/OrderListView.xaml` - ComboBox categoria com "Todos"
- `src/SmartLanche.Presentation/Views/Orders/OrderFormView.xaml` - Remover botão "Selecionar Produto", adicionar grid de produtos, filtro categoria, UI fluxo de status

---

## Critérios de Aceitação

### Fluxo de Status
- [ ] Pedido criado com status "Pendente"
- [ ] Pode transicionar: Pendente → Em Preparo
- [ ] Pode transicionar: Em Preparo → Pronto
- [ ] Pode transicionar: Pronto → Finalizado
- [ ] Pode cancelar a partir de: Pendente, Em Preparo, Pronto
- [ ] Não pode cancelar: Finalizado, Cancelado
- [ ] Transições inválidas bloqueadas com feedback ao usuário

### Busca por Cliente
- [ ] Digitar nome/telefone do cliente no campo de busca
- [ ] Clicar "Filtrar" ou pressionar Enter
- [ ] Grid mostra apenas pedidos desse cliente
- [ ] Limpar filtro mostra todos pedidos

### Ações do Pedido
- [ ] Botão "Em Preparo" funciona (pedidos Pendente)
- [ ] Botão "Pronto" funciona (pedidos Em Preparo)
- [ ] Botão "Finalizar" funciona (pedidos Pronto)
- [ ] Botão "Cancelar" funciona (pedidos não finalizados)
- [ ] Botões desabilitados/ocultos para estados inválidos
- [ ] Feedback de sucesso/erro exibido

### Seleção de Produtos
- [ ] Botão "Selecionar Produto" removido
- [ ] Grid de produtos visível no OrderFormView
- [ ] Produtos agrupados por categoria
- [ ] Clicar produto → adiciona aos itens do pedido
- [ ] Quantidade ajustável inline

### Filtro de Categoria
- [ ] ComboBox categoria tem "Todos" como primeiro item
- [ ] "Todos" selecionado por padrão
- [ ] Selecionar categoria filtra grid de produtos
- [ ] Selecionar "Todos" mostra todos produtos

---

## Ordem de Implementação

1. **Domain**: Atualizar enum OrderStatus e lógica de transição
2. **Application**: Corrigir busca, adicionar métodos de transição de status
3. **Infrastructure**: Corrigir query do repositório para busca por cliente
4. **Presentation - ListView**: Corrigir busca, atualizar ComboBox filtro status
5. **Presentation - FormView**: Remover botão seletor produto, adicionar grid produtos, filtro categoria, UI fluxo status
6. **Testes**: Atualizar/adicionar testes unitários para novo comportamento

---

## Observações

- Manter padrão soft-delete (IsActive) para pedidos
- Mudanças de status devem ser auditáveis (considerar entidade OrderStatusHistory)
- Grid de produtos deve usar virtualização para performance
- Formatação moeda: R$ (pt-BR) em todo lugar
- Seguir padrões MVVM existentes (AsyncRelayCommand, INotifyPropertyChanged)