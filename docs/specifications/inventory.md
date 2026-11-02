# Especificação: Estoque (inventory)

## 1. Objetivo
Gerenciar o estoque de produtos da lanchonete, permitindo controle de quantidades, entradas/saídas manuais, alertas de estoque baixo, histórico de movimentações e atualização automática após realização de pedidos.

---

## 2. Fluxo de Negócio

```
Produto Cadastrado
       ↓
[Entrada Inicial] → Estoque com Quantidade Inicial
       ↓
[Pedidos Realizados] → Saída Automática (baixa no estoque)
       ↓
[Entrada/Saída Manual] → Ajuste Manual
       ↓
[Alerta] → Quantidade ≤ Quantidade Mínima
```

---

## 3. Regras de Negócio

1. **Todo produto deve ter um registro de estoque** (InventoryItem) ao ser cadastrado
2. **Quantidade nunca pode ser negativa** — validação no domínio
3. **Entrada manual** aumenta a quantidade (ex: reposição)
4. **Saída manual** diminui a quantidade (ex: perda, doação) — valida se há estoque suficiente
5. **Baixa automática** ao confirmar pedido (status `Aberto` → `EmPreparacao`)
6. **Alerta de estoque baixo** quando `Quantidade <= QuantidadeMinima`
7. **Toda movimentação gera registro no histórico** (InventoryMovement)
8. **Valor unitário** para cálculo de valor total do estoque

---

## 4. Interface (WPF)

### 4.1 Tela Principal: Lista de Itens do Estoque
- **Grid** com colunas:
  - Produto
  - Categoria
  - Quantidade Atual
  - Quantidade Mínima
  - Valor Unitário
  - Valor Total (Qtd × Valor Unitário)
  - Status (Normal / Baixo / Zerado)
  - Ações: [Entrada] [Saída] [Histórico]

### 4.2 Filtros
- Todos / Estoque Baixo / Zerados
- Busca por nome do produto
- Filtro por categoria

### 4.3 Ações por Linha
- **Entrada** → Modal para registrar entrada (quantidade, observação)
- **Saída** → Modal para registrar saída (quantidade, observação, validação)
- **Histórico** → Abre tela de movimentações do produto

### 4.4 Tela de Movimentações (InventoryMovementView)
- Grid com: Data, Tipo (Entrada/Saída), Quantidade, Quantidade Anterior, Quantidade Posterior, Origem (Pedido/Manual), Observação
- Filtros por período, tipo, produto

---

## 5. Casos de Uso (Application)

| Caso de Uso | Entrada | Saída |
|-------------|---------|-------|
| `GetAllInventoryAsync()` | — | `IReadOnlyList<InventoryItemDto>` |
| `GetLowStockItemsAsync()` | — | `IReadOnlyList<InventoryItemDto>` |
| `GetInventoryByProductIdAsync(productId)` | `int productId` | `InventoryItemDto?` |
| `AddStockAsync(productId, quantity, note)` | `int, int, string?` | `InventoryMovementDto` |
| `RemoveStockAsync(productId, quantity, note)` | `int, int, string?` | `InventoryMovementDto` |
| `GetMovementsAsync(productId, dateFrom, dateTo)` | `int, DateTime?, DateTime?` | `IReadOnlyList<InventoryMovementDto>` |
| `UpdateMinStockAsync(productId, minQuantity)` | `int, int` | `InventoryItemDto` |

---

## 6. DTOs

```csharp
// Application/DTOs/InventoryItemDto.cs
public sealed record InventoryItemDto(
    int ProductId,
    string ProductName,
    string CategoryName,
    int Quantity,
    int MinQuantity,
    decimal UnitPrice,
    decimal TotalValue,
    StockStatus Status,
    DateTime? LastMovementAt
);

// Application/DTOs/InventoryMovementDto.cs
public sealed record InventoryMovementDto(
    int Id,
    int ProductId,
    string ProductName,
    MovementType Type,
    int Quantity,
    int PreviousQuantity,
    int NewQuantity,
    MovementOrigin Origin,
    string? ReferenceId,       // OrderId se originou de pedido
    string? Note,
    DateTime CreatedAt
);

// Application/DTOs/StockStatus.cs (enum)
public enum StockStatus
{
    Normal,     // Quantidade > QuantidadeMinima
    Low,        // Quantidade <= QuantidadeMinima e > 0
    OutOfStock  // Quantidade == 0
}
```

---

## 7. Serviços (Application)

### `IInventoryService`
```csharp
public interface IInventoryService
{
    Task<IReadOnlyList<InventoryItemDto>> GetAllInventoryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItemDto>> GetLowStockItemsAsync(CancellationToken ct = default);
    Task<InventoryItemDto?> GetByProductIdAsync(int productId, CancellationToken ct = default);
    
    Task<InventoryMovementDto> AddStockAsync(int productId, int quantity, string? note, CancellationToken ct = default);
    Task<InventoryMovementDto> RemoveStockAsync(int productId, int quantity, string? note, CancellationToken ct = default);
    
    Task<IReadOnlyList<InventoryMovementDto>> GetMovementsAsync(int productId, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<InventoryItemDto> UpdateMinStockAsync(int productId, int minQuantity, CancellationToken ct = default);
    
    // Chamado pelo OrderService ao iniciar preparação
    Task<bool> ReserveStockForOrderAsync(int orderId, CancellationToken ct = default);
    Task ReleaseStockForOrderAsync(int orderId, CancellationToken ct = default);
}
```

### `InventoryService`
- Orquestra `IInventoryRepository` e `IProductRepository`
- Valida regras de negócio (quantidade negativa, estoque suficiente)
- Emite evento `InventoryChanged` para atualização em tempo real
- Coordena com `OrderService` para baixa automática

---

## 8. Domínio (Domain)

### Entidades

#### `InventoryItem`
```csharp
public class InventoryItem : Entity
{
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public int MinQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public DateTime? LastMovementAt { get; private set; }
    
    // Navigation
    public Product Product { get; private set; }
    public ICollection<InventoryMovement> Movements { get; private set; }
    
    // Domain methods
    public void AddStock(int quantity, string? note, MovementOrigin origin, string? referenceId = null)
    public void RemoveStock(int quantity, string? note, MovementOrigin origin, string? referenceId = null)
    public void UpdateMinQuantity(int minQuantity)
    public StockStatus GetStatus()
    public decimal GetTotalValue()
}
```

#### `InventoryMovement`
```csharp
public class InventoryMovement : Entity
{
    public int InventoryItemId { get; private set; }
    public int ProductId { get; private set; }
    public MovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public int PreviousQuantity { get; private set; }
    public int NewQuantity { get; private set; }
    public MovementOrigin Origin { get; private set; }
    public string? ReferenceId { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Navigation
    public InventoryItem InventoryItem { get; private set; }
    public Product Product { get; private set; }
}
```

### Enums
```csharp
public enum MovementType
{
    Entrada = 1,
    Saida = 2
}

public enum MovementOrigin
{
    Manual = 1,
    Pedido = 2,
    Ajuste = 3
}

public enum StockStatus
{
    Normal,
    Low,
    OutOfStock
}
```

---

## 9. Persistência (Infrastructure)

### Tabelas
- `InventoryItems` — 1:1 com `Products` (ProductId é PK e FK)
- `InventoryMovements` — N:1 com `InventoryItems`

### Configurações EF Core
- `InventoryItemConfiguration`: PK = ProductId, FK para Product, índice em MinQuantity
- `InventoryMovementConfiguration`: PK = Id, FK para InventoryItem, índice em CreatedAt, ProductId

### Migration
- Criar tabelas `InventoryItems` e `InventoryMovements`
- Popular `InventoryItems` para produtos existentes (quantidade = 0, min = 5 padrão)
- Índices para performance em consultas de estoque baixo e movimentações

---

## 10. ViewModels (Presentation)

### `InventoryListViewModel`
- Propriedades: `Items`, `SelectedItem`, `FilterText`, `SelectedCategory`, `FilterStatus`, `IsLoading`
- Commands: `LoadCommand`, `AddStockCommand`, `RemoveStockCommand`, `ViewHistoryCommand`, `EditMinStockCommand`, `RefreshCommand`
- Eventos: `RequestAddStock`, `RequestRemoveStock`, `RequestViewHistory`, `RequestEditMinStock`

### `AddStockViewModel` / `RemoveStockViewModel` (Modals)
- Propriedades: `ProductName`, `CurrentQuantity`, `Quantity`, `Note`, `IsSaving`
- Commands: `SaveCommand`, `CancelCommand`

### `InventoryMovementViewModel`
- Propriedades: `Movements`, `FilterProductId`, `FilterFrom`, `FilterTo`, `FilterType`, `IsLoading`
- Commands: `LoadCommand`, `RefreshCommand`

---

## 11. Navegação

- Menu lateral: **"Estoque"** (entre "Pedidos em Preparação" e "Resumo de Vendas")
- Command no `MainViewModel`: `NavigateInventoryCommand`
- DataTemplate no `MainWindow.xaml`
- Submenu ou aba para "Movimentações"

---

## 12. Critérios de Aceitação

1. [ ] Lista carrega todos itens do estoque com dados corretos
2. [ ] Status visual: Verde (Normal), Amarelo (Baixo), Vermelho (Zerado)
3. [ ] "Entrada" abre modal, salva movimento, atualiza quantidade
4. [ ] "Saída" valida estoque suficiente, salva movimento, atualiza quantidade
5. [ ] "Histórico" abre tela de movimentações do produto
6. [ ] Filtros funcionam (texto, categoria, status)
7. [ ] Alerta visual para itens com estoque baixo
8. [ ] Baixa automática ao iniciar preparação de pedido
9. [ ] Toast de sucesso/erro nas ações
10. [ ] Navegação pelo menu lateral funciona
11. [ ] Evento `InventoryChanged` atualiza lista automaticamente
12. [ ] Edição de quantidade mínima funciona

---

## 13. Testes

- Unit: `InventoryItem.AddStock`, `RemoveStock`, validações de quantidade
- Unit: `InventoryService` regras de negócio
- Integration: Fluxo completo Entrada → Saída → Pedido (baixa automática)
- Integration: Alerta estoque baixo
- UI: DataGrid binding, modais, navegação

---

## 14. Integração com Módulos Existentes

### OrderService → InventoryService
Ao chamar `StartPreparationAsync(orderId)`:
1. `OrderService` valida transição Aberto → EmPreparacao
2. `OrderService` chama `InventoryService.ReserveStockForOrderAsync(orderId)`
3. `InventoryService` baixa estoque dos itens do pedido (cria movimentos com Origin = Pedido)
4. Se algum item não tem estoque suficiente → falha toda a operação

### ProductService → InventoryService
Ao cadastrar novo produto:
1. `ProductService` cria produto
2. `ProductService` chama `InventoryService.InitializeInventoryAsync(productId, minQuantity: 5)`
3. Cria `InventoryItem` com quantidade 0

---

## 15. Conversores Necessários (Presentation)

- `StockStatusToColorConverter` — Normal=Verde, Amarelo=Amarelo, Vermelho=Vermelho
- `StockStatusToDescriptionConverter` — "Normal", "Baixo", "Zerado"
- `MovementTypeToDescriptionConverter` — "Entrada", "Saída"
- `MovementOriginToDescriptionConverter` — "Manual", "Pedido", "Ajuste"