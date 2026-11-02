# Estrutura das ViewModels - SmartLanche

## Hierarquia de Herança

```
ViewModelBase (base comum - INotifyPropertyChanged, comandos, loading, mensagens)
    └── ListViewModelBase<TDto, TService> (listagens genéricas)
            ├── NewItemCommand → RequestNewItem (evento)
            ├── EditItemCommand → RequestEditItem (evento)
            ├── LoadCommand, SearchText, SortColumn, SortDescending
            ├── Items (ObservableCollection), SelectedItem
            └── GetItemsAsync(), ApplySearchFilter(), ApplySort() (virtual)
                    └── CrudListViewModelBase<TDto, TService> (com activate/deactivate)
                            ├── DeactivateItemCommand
                            ├── ReactivateItemCommand
                            ├── GetItemsAsync() usa Service.GetAllAsync(isActive)
                            └── GetActiveFilter() (virtual)
                                    └── ViewModels concretas (ProductListViewModel, CategoryListViewModel, etc.)
```

## ViewModels de Listagem (herdam de `CrudListViewModelBase`)

| ViewModel | Service | DTO | Eventos expostos |
|-----------|---------|-----|------------------|
| `ProductListViewModel` | `IProductService` | `ProductDto` | `RequestNewItem`, `RequestEditItem` |
| `CategoryListViewModel` | `ICategoryService` | `CategoryDto` | `RequestNewItem`, `RequestEditItem` |
| `CustomerListViewModel` | `ICustomerService` | `CustomerDto` | `RequestNewItem`, `RequestEditItem` |
| `PromotionListViewModel` | `IPromotionService` | `PromotionDto` | `RequestNewItem`, `RequestEditItem` |
| `ComboListViewModel` | `IComboService` | `ComboDto` | `RequestNewItem`, `RequestEditItem` |

## ViewModels Especiais

| ViewModel | Herda de | Comandos próprios |
|-----------|----------|-------------------|
| `OrderListViewModel` | `ListViewModelBase` (não Crud) | `CloseOrderCommand`, `CancelOrderCommand`, `ViewOrderCommand` + mapeia `RequestNewItem→RequestNewOrder`, `RequestEditItem→RequestViewOrder` |
| `InventoryListViewModel` | `ListViewModelBase` | `AddStockCommand`, `RemoveStockCommand`, `ViewHistoryCommand`, `EditMinStockCommand` |

## ViewModels de Formulário

| ViewModel | Propósito |
|-----------|-----------|
| `ProductFormViewModel` | CRUD de produto + `InventoryRequest` opcional |
| `CategoryFormViewModel` | CRUD de categoria |
| `CustomerFormViewModel` | CRUD de cliente |
| `PromotionFormViewModel` | CRUD de promoção + produtos/categorias aplicáveis |
| `ComboFormViewModel` | CRUD de combo + itens do combo |
| `OrderFormViewModel` | Criação/edição de pedido com itens |
| `OrderDetailViewModel` | Visualização read-only de pedido |

## Navegação (MainViewModel)

O `MainViewModel` é o orquestrador:
- Mantém instâncias singleton de todos os ListViewModels
- No `InitializeListViewModels()` **inscreve-se nos eventos** de cada um:
  ```csharp
  _productListViewModel.RequestNewItem += OnRequestNewProduct;
  _productListViewModel.RequestEditItem += OnRequestEditProduct;
  // ... etc para cada entidade
  ```
- Ao receber evento, cria o FormViewModel correspondente via DI e troca `CurrentViewModel`
- DataTemplates no `MainWindow.xaml` mapeiam ViewModel → View automaticamente

## Fluxo "Novo Produto"

1. Usuário clica botão "Novo produto" → `NewItemCommand` (base)
2. `NewItemCommand` executa: `ClearMessages(); RequestNewItem?.Invoke();`
3. `MainViewModel.OnRequestNewProduct()` chamado
4. `OpenProductFormAsync(null)` cria `ProductFormViewModel` via DI
5. `CurrentViewModel = viewModel` → DataTemplate mostra `ProductFormView`
6. `viewModel.InitializeAsync(null)` carrega categorias para dropdown
7. Usuário preenche e salva → `WasSaved = true` → `MainViewModel` volta para lista

## Princípio Chave

**As ListViewModels NÃO conhecem navegação** - apenas disparam eventos. O `MainViewModel` (ou host) decide o que fazer. Isso mantém as listagens testáveis e desacopladas.

## Correção Recente (Botão "Novo")

As views estavam vinculando a comandos inexistentes (`NewProductCommand`, `NewCategoryCommand`, etc.). O correto é usar os comandos da base:
- `NewItemCommand` (não `NewProductCommand`)
- `EditItemCommand` (não `EditProductCommand`)
- `DeactivateItemCommand` / `ReactivateItemCommand` (não `DeactivateProductCommand`)

Corrigido em: ProductListView, CategoryListView, CustomerListView, PromotionListView, ComboListView, OrderListView.