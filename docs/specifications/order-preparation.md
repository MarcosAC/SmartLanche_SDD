# Specificação: Preparação de Pedidos (order-preparation)

## 1. Objetivo
Permitir visualizar e gerenciar pedidos que estão em preparação ("Em Preparação"), com ações para marcar como "Entregue" ou cancelar.

---

## 2. Fluxo de Negócio

```
Novo Pedido (Aberto)
       ↓
[Iniciar Preparação] → Em Preparação
       ↓
[Marcar Entregue] → Entregue
       ↓
[Fechar com Pagamento] → Fechado/Pago
```

**Estados do Pedido (OrderStatus):**
- `Aberto` — Pedido criado, aguardando início da preparação
- `EmPreparacao` — Pedido sendo preparado (foco deste módulo)
- `Entregue` — Pedido entregue ao cliente, aguardando pagamento (se fiado) ou já pago
- `Cancelado` — Pedido cancelado
- `Fechado` — Pedido finalizado e pago

---

## 3. Regras de Negócio

1. **Apenas pedidos com status `Aberto` podem ir para `EmPreparacao`**
2. **Apenas pedidos `EmPreparacao` podem ir para `Entregue`**
3. **Pedidos `Entregue` com forma de pagamento `Fiado` ficam pendentes de pagamento** (módulo payments)
4. **Pedidos `Entregue` com outras formas de pagamento podem ser fechados diretamente**
5. **Pedidos `EmPreparacao` podem ser cancelados** (volta para `Cancelado`)
6. **Exibir tempo decorrido desde que entrou em preparação**

---

## 4. Interface (WPF)

### 4.1 Tela Principal: Lista de Pedidos em Preparação
- **Grid** com colunas:
  - Número do Pedido
  - Data/Hora de Início da Preparação
  - Tempo Decorrido (ex: "15 min")
  - Cliente (se houver)
  - Valor Total
  - Itens (resumo: "2x X-Burger, 1x Batata")
  - Ações: [Ver Detalhes] [Marcar Entregue] [Cancelar]

### 4.2 Filtros
- Todos / Apenas em Preparação / Entregues Hoje
- Busca por número do pedido ou cliente

### 4.3 Ações por Linha
- **Ver Detalhes** → Abre OrderDetailView (read-only)
- **Marcar Entregue** → Muda status para `Entregue`, registra `DeliveredAt`
- **Cancelar** → Muda status para `Cancelado`, registra `CancelledAt`

---

## 5. Casos de Uso (Application)

| Caso de Uso | Entrada | Saída |
|-------------|---------|-------|
| `GetPreparingOrdersAsync()` | — | `IReadOnlyList<OrderPreparationDto>` |
| `StartPreparationAsync(orderId)` | `int orderId` | `OrderDto` atualizado |
| `MarkAsDeliveredAsync(orderId)` | `int orderId` | `OrderDto` atualizado |
| `CancelOrderAsync(orderId)` | `int orderId` | `OrderDto` atualizado |

---

## 6. DTOs

```csharp
// Application/DTOs/OrderPreparationDto.cs
public sealed record OrderPreparationDto(
    int Id,
    DateTime OrderDate,
    DateTime? PreparationStartedAt,
    TimeSpan? ElapsedTime,
    string? CustomerName,
    decimal TotalAmount,
    string ItemsSummary,
    OrderStatus Status
);
```

---

## 7. Serviços (Application)

### `IOrderPreparationService`
```csharp
public interface IOrderPreparationService
{
    Task<IReadOnlyList<OrderPreparationDto>> GetPreparingOrdersAsync();
    Task<OrderDto?> StartPreparationAsync(int orderId);
    Task<OrderDto?> MarkAsDeliveredAsync(int orderId);
    Task<OrderDto?> CancelOrderAsync(int orderId);
}
```

### `OrderPreparationService`
- Orquestra `IOrderRepository` e `IOrderService`
- Valida transições de estado
- Dispara evento `OrderChanged` para atualização em tempo real

---

## 8. Persistência (Infrastructure)

- **Nenhuma migration nova** — usa tabela `Orders` existente
- **Queries otimizadas** com `AsNoTracking()` para listagem
- **Índice** em `Status` + `PreparationStartedAt` (se não existir)

---

## 9. ViewModels (Presentation)

### `OrderPreparationListViewModel`
- Propriedades: `Orders`, `SelectedOrder`, `FilterText`, `IsLoading`
- Commands: `LoadCommand`, `StartPreparationCommand`, `MarkDeliveredCommand`, `CancelCommand`, `ViewDetailCommand`, `RefreshCommand`
- Evento: `RequestViewOrder(OrderDto)`

### `OrderPreparationView` (XAML)
- DataGrid com template para tempo decorrido
- Botões de ação por linha
- Barra de ferramentas: Filtro, Atualizar

---

## 10. Navegação

- Menu lateral: **"Pedidos em Preparação"** (entre "Pedidos" e "Estoque")
- Command no `MainViewModel`: `NavigateOrderPreparationCommand`
- DataTemplate no `MainWindow.xaml`

---

## 11. Critérios de Aceitação

1. [ ] Lista carrega pedidos com status `EmPreparacao`
2. [ ] Tempo decorrido atualiza em tempo real (ex: timer a cada 30s)
3. [ ] "Marcar Entregue" muda status para `Entregue` e registra `DeliveredAt`
4. [ ] "Cancelar" muda status para `Cancelado` e registra `CancelledAt`
5. [ ] "Ver Detalhes" abre tela read-only do pedido
6. [ ] Toast de sucesso/erro nas ações
7. [ ] Filtro por texto funciona (número pedido, cliente)
8. [ ] Navegação pelo menu lateral funciona
9. [ ] Evento `OrderChanged` atualiza lista automaticamente

---

## 12. Testes

- Unit: Transições de estado válidas/Inválidas
- Integration: Fluxo completo Aberto → EmPreparacao → Entregue
- UI: DataGrid binding, commands, navegação