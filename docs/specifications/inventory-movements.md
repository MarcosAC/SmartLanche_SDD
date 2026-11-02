# Especificação: Relatório de Movimentações de Estoque (inventory-movements)

## 1. Objetivo
Fornecer visão consolidada de **todas as movimentações de estoque** (entradas, saídas, ajustes, baixas de pedidos) com filtros globais por período, tipo, origem e produto. Complementa a tela `InventoryMovementView` (histórico por produto) com relatório gerencial.

---

## 2. Fluxo de Negócio

```
[Usuário acessa Movimentações]
         ↓
[Seleciona filtros: Data Inicial/Fim, Tipo, Origem, Produto]
         ↓
[Grid carrega todas as movimentações do período]
         ↓
[Pode exportar / visualizar detalhes]
```

---

## 3. Regras de Negócio

1. **Movimentações não podem ser editadas/excluídas** — apenas visualizadas (auditoria)
2. **Filtro de data obrigatório** — padrão: últimos 30 dias
3. **Tipos de movimentação**: Entrada / Saída
4. **Origens**: Manual / Pedido / Ajuste
5. **Ordenação padrão**: Mais recentes primeiro (CreatedAt DESC)
6. **Paginação** — carregar em lotes de 100 para performance

---

## 4. Interface (WPF)

### 4.1 Tela: Relatório de Movimentações (`InventoryMovementsView`)

**Header:**
- Título: "Movimentações de Estoque"
- Botão "Atualizar"

**Barra de Filtros (acima do grid):**
| Filtro | Tipo | Padrão |
|--------|------|--------|
| Data Inicial | DatePicker | 30 dias atrás |
| Data Final | DatePicker | Hoje |
| Tipo | ComboBox (Todos / Entrada / Saída) | Todos |
| Origem | ComboBox (Todos / Manual / Pedido / Ajuste) | Todos |
| Produto | ComboBox (busca + lista produtos) | Todos |
| Botão "Filtrar" | Button | — |
| Botão "Limpar" | Button | — |

**Grid Principal (DataGrid):**
| Coluna | Binding | Formato |
|--------|---------|---------|
| Data/Hora | `CreatedAt` | dd/MM/yyyy HH:mm |
| Produto | `ProductName` | — |
| Tipo | `Type` | "Entrada" / "Saída" (cores) |
| Origem | `Origin` | "Manual" / "Pedido" / "Ajuste" |
| Quantidade | `Quantity` | inteiro |
| Qtd. Anterior | `PreviousQuantity` | inteiro |
| Qtd. Atual | `NewQuantity` | inteiro |
| Valor Unit. | `UnitPrice` | R$ {0:N2} |
| Valor Total | `Quantity * UnitPrice` | R$ {0:N2} |
| Referência | `ReferenceId` | OrderId ou — |
| Observação | `Note` | texto |

**Totais no Rodapé (opcional):**
- Total Entradas (soma de quantidades onde Type=Entrada)
- Total Saídas (soma onde Type=Saída)
- Saldo do Período (Entradas - Saídas)

**Loading / Empty State:**
- Loading: overlay "Carregando..." durante consulta
- Empty: "Nenhuma movimentação encontrada para os filtros selecionados"

---

## 5. Casos de Uso (Application)

| Caso de Uso | Entrada | Saída |
|-------------|---------|-------|
| `GetAllMovementsAsync(filter)` | `InventoryMovementFilter` | `IReadOnlyList<InventoryMovementDto>` |
| `GetMovementSummaryAsync(filter)` | `InventoryMovementFilter` | `InventoryMovementSummaryDto` |

### `InventoryMovementFilter`
```csharp
public sealed record InventoryMovementFilter(
    DateTime? DateFrom,
    DateTime? DateTo,
    MovementType? Type,
    MovementOrigin? Origin,
    int? ProductId,
    int Page = 1,
    int PageSize = 100
);
```

### `InventoryMovementSummaryDto`
```csharp
public sealed record InventoryMovementSummaryDto(
    int TotalEntries,
    int TotalExits,
    int NetQuantity,
    decimal TotalEntryValue,
    decimal TotalExitValue
);
```

---

## 6. DTOs (existentes — reutilizar)

- `InventoryMovementDto` (já definido em `inventory.md`)
- `MovementType` enum
- `MovementOrigin` enum

---

## 7. Serviços (Application)

### `IInventoryMovementService` (novo)
```csharp
public interface IInventoryMovementService
{
    Task<IReadOnlyList<InventoryMovementDto>> GetAllAsync(
        InventoryMovementFilter filter, 
        CancellationToken ct = default);
    
    Task<InventoryMovementSummaryDto> GetSummaryAsync(
        InventoryMovementFilter filter, 
        CancellationToken ct = default);
}
```

### `InventoryMovementService`
- Usa `IInventoryRepository.GetMovementsAsync` com filtros globais (sem productId obrigatório)
- Agrega totais para summary
- Performance: queries com índices em `CreatedAt`, `ProductId`, `Type`, `Origin`

---

## 8. Domínio (Domain)

**Nenhuma nova entidade** — reutiliza `InventoryMovement` existente.

Adicionar método no repositório:
```csharp
// IInventoryRepository
Task<IReadOnlyList<InventoryMovement>> GetAllAsync(
    DateTime? from, DateTime? to, 
    MovementType? type, MovementOrigin? origin, 
    int? productId,
    int skip, int take,
    CancellationToken ct = default);
```

---

## 9. Persistência (Infrastructure)

- Query otimizada com `AsNoTracking()`
- Índices existentes em `InventoryMovements`: `CreatedAt`, `ProductId`, `Type`, `Origin`
- Paginação via `Skip/Take`

---

## 10. ViewModels (Presentation)

### `InventoryMovementsViewModel`
**Propriedades:**
- `Movements` — `ObservableCollection<InventoryMovementDto>`
- `Filter` — `InventoryMovementFilter` (bindable)
- `Products` — `IReadOnlyList<ProductDto>` (para combo de produto)
- `IsLoading` — bool
- `Summary` — `InventoryMovementSummaryDto?`
- `StatusText` — string

**Commands:**
- `LoadCommand` — carrega com filtros atuais
- `RefreshCommand` — recarrega
- `ClearFiltersCommand` — reseta filtros para padrão

**Construtor:**
```csharp
public InventoryMovementsViewModel(
    IInventoryMovementService movementService,
    IProductService productService,
    IToastNotificationService toastService)
```

---

## 11. Navegação

- Menu lateral: **"Movimentações"** (subitem de "Estoque" ou item separado)
- Command no `MainViewModel`: `NavigateInventoryMovementsCommand`
- DataTemplate no `MainWindow.xaml`:
  ```xml
  <DataTemplate DataType="{x:Type inventoryVm:InventoryMovementsViewModel}">
      <viewsInventory:InventoryMovementsView/>
  </DataTemplate>
  ```

---

## 12. Critérios de Aceitação

1. [ ] Tela abre com filtros padrão (últimos 30 dias)
2. [ ] Grid carrega movimentações ordenadas por data decrescente
3. [ ] Filtros funcionam individualmente e combinados
3. [ ] Tipo mostra "Entrada" (verde) / "Saída" (vermelho)
4. [ ] Origem mostra "Manual" / "Pedido" / "Ajuste"
5. [ ] Coluna Referência mostra OrderId quando Origin=Pedido
6. [ ] Totais (Entradas, Saídas, Saldo) exibidos no rodapé
7. [ ] Loading overlay durante consulta
8. [ ] Empty state quando sem resultados
9. [ ] Botão "Limpar" reseta filtros
10. [ ] Navegação pelo menu funciona
11. [ ] Performance: < 2s para 10k registros
12. [ ] Toast de erro se consulta falhar

---

## 13. Testes

- Unit: `InventoryMovementService.GetAllAsync` com vários filtros
- Unit: `InventoryMovementService.GetSummaryAsync` cálculos corretos
- Integration: Query com filtros combinados no repositório
- UI: DataGrid binding, filtros, loading, empty state, navegação

---

## 14. Integração com Módulos Existentes

- Reutiliza `InventoryMovementDto`, `MovementType`, `MovementOrigin`
- Usa `IInventoryRepository` existente (novo método `GetAllAsync`)
- `ProductService` para popular combo de produtos
- Conversores existentes: `MovementTypeToDescriptionConverter`, `MovementOriginToDescriptionConverter`

---

## 15. Conversores (Presentation) — já existem

- `MovementTypeToDescriptionConverter` — "Entrada" / "Saída"
- `MovementOriginToDescriptionConverter` — "Manual" / "Pedido" / "Ajuste"
- `BoolToVisibilityConverter` — loading/empty states
- Novo: `MovementTypeToColorConverter` — Verde/Vermelho para badges no grid