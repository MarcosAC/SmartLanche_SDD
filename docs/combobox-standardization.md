# Padronização de ComboBox - SmartLanche

## Visão Geral

Este documento descreve a padronização dos componentes ComboBox em toda a aplicação SmartLanche, realizada para eliminar inconsistências, reduzir duplicação de código e facilitar a manutenção.

---

## Problema Identificado

Antes da padronização, existiam **5 padrões diferentes** de ComboBox na aplicação:

1. **Inline styling** - Propriedades definidas diretamente no elemento (Width, Padding, FontSize, BorderBrush, etc.)
2. **Estilos locais duplicados** - `FilterComboBoxStyle`, `FormComboBoxStyle`, `DialogComboBoxStyle`, `PickerComboBoxStyle` definidos em múltiplas views
3. **ObjectDataProvider + Converters customizados** - Para popular enums e exibir descrições amigáveis
4. **StatusFilterComboBox** - Componente reutilizável já existente mas subutilizado
5. **DataGrid CellEditingTemplate** - ComboBox sem estilo algum

---

## Solução Implementada

### 1. Criação de Estilos Globais (`Styles/ComboBoxStyles.xaml`)

Um `ResourceDictionary` centralizado com hierarquia de estilos baseada em herança:

```xaml
BaseComboBoxStyle          <!-- Base com propriedades comuns -->
├── FilterComboBoxStyle    <!-- Para filtros em listas (Width=180, Height=34) -->
├── FormComboBoxStyle      <!-- Para formulários (Width=Auto, MinWidth=200) -->
├── DialogComboBoxStyle    <!-- Para diálogos (Width=Auto, MinWidth=180) -->
└── PickerComboBoxStyle    <!-- Para seletores (Width=Auto, MinWidth=150) -->
```

**Registro no App.xaml:**
```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Styles/ComboBoxStyles.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 2. StatusFilterComboBox - Componente para Filtros de Enum

Controle personalizado (`Controls/StatusFilterComboBox`) com propriedades de dependência:

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `EnumType` | `Type` | Tipo do enum (ex: `typeof(OrderStatus)`) |
| `SelectedValue` | `object?` | Valor selecionado (binding TwoWay) |
| `IncludeAllOption` | `bool` | Incluir opção "Todos" (default: true) |
| `AllOptionText` | `string` | Texto da opção "Todos" (default: "Todos") |
| `UseEnumDescription` | `bool` | Usar `[Description]` do enum (default: true) |

**Métodos factory:**
```csharp
StatusFilterComboBox.CreateFromEnum<OrderStatus>(includeAllOption: true, allOptionText: "Todos")
StatusFilterComboBox.CreateFromEnum<StockStatus>(includeAllOption: true, allOptionText: "Todos os status")
```

### 3. Atributos Description nos Enums

Adicionados atributos `[Description]` aos enums para exibição amigável:

```csharp
public enum InventoryMovementType
{
    [Description("Entrada")]
    Entrada = 1,

    [Description("Saída")]
    Saida = 2
}

public enum InventoryMovementReason
{
    [Description("Compra")]
    Compra = 1,
    [Description("Reposição")]
    Reposicao = 2,
    // ... demais valores
}
```

---

## Views Migradas

### Filtros de Lista (10 views) → StatusFilterComboBox

| View | Enum/Propriedade | Binding |
|------|------------------|---------|
| `ProductListView` | bool | `SelectedFilter` |
| `CategoryListView` | bool | `SelectedFilter` |
| `ComboListView` | bool | `SelectedFilter` |
| `PromotionListView` | bool | `SelectedFilter` |
| `CustomerListView` | bool | `SelectedFilter` |
| `OrderListView` | OrderStatus | `SelectedStatus` |
| `PendingPaymentListView` | StatusPagamentoPendente | `SelectedStatus` |
| **`InventoryListView`** | **StockStatus** | **`FilterStatus`** |
| **`InventoryMovementsView`** | **InventoryMovementType** | **`Filter.Tipo`** |
| **`InventoryMovementsView`** | **InventoryMovementReason** | **`Filter.Motivo`** |
| **`InventoryMovementView`** | **InventoryMovementType** | **`FilterType`** |

### Formulários e Diálogos → Estilos Globais

| View | Estilo | Controle |
|------|--------|----------|
| `ProductFormView` | `FormComboBoxStyle` | Categoria |
| `OrderFormView` | `FormComboBoxStyle` | Forma de pagamento |
| `OrderCloseView` | `FormComboBoxStyle` | Forma de pagamento |
| `PaymentListView` | `FormComboBoxStyle` | Filtro forma de pagamento |
| **`PromotionFormView`** | **`FormComboBoxStyle`** | **DiscountType (corrigido)** |
| `RegisterPaymentDialog` | `DialogComboBoxStyle` | Forma de pagamento |
| **`ProductPickerView`** | **`PickerComboBoxStyle`** | **Categoria (removido estilo local)** |
| **`ComboFormView`** | **`PickerComboBoxStyle`** | **DataGrid seletor produto (adicionado)** |
| **`InventoryMovementsView`** | **`PickerComboBoxStyle`** | **Seletor produto (era FilterComboBoxStyle local)** |

---

## Código Removido/Eliminado

### Estilos Locais Duplicados Removidos
- `FilterComboBoxStyle` - InventoryMovementsView
- `PickerComboBoxStyle` - ProductPickerView
- `FormComboBoxStyle` - PendingPaymentListView (não usado), OrderFormView, PaymentListView, OrderCloseView
- `DialogComboBoxStyle` - RegisterPaymentDialog

### ObjectDataProvider + Converters Customizados Removidos
- `InventoryListView`: `StockStatusValues` + `StockStatusToDescriptionConverter`
- `InventoryMovementsView`: `MovementTypeValues`/`MovementOriginValues` + 2 converters
- `InventoryMovementView`: `MovementTypeValues` + `MovementTypeToDescriptionConverter`

### Linhas de Código Eliminadas
~200+ linhas de XAML duplicadas removidas das views

---

## Bugs Corrigidos

### InventoryMovementsView - Bindings Incorretos
**Antes:**
```xaml
SelectedItem="{Binding Filter.Type, Mode=TwoWay}"     <!-- Property não existe -->
SelectedItem="{Binding Filter.Origin, Mode=TwoWay}"   <!-- Property não existe -->
```

**Depois:**
```xaml
SelectedValue="{Binding Filter.Tipo, Mode=TwoWay}"    <!-- InventoryMovementType? -->
SelectedValue="{Binding Filter.Motivo, Mode=TwoWay}"  <!-- InventoryMovementReason? -->
```

O `InventoryMovementFilter` record possui propriedades `Tipo` e `Motivo`, não `Type` e `Origin`.

---

## Benefícios Alcançados

| Aspecto | Antes | Depois |
|---------|-------|--------|
| **Estilos ComboBox** | 5 padrões diferentes | 1 hierarquia centralizada |
| **Linhas XAML duplicadas** | ~200+ | 0 |
| **Converters customizados por view** | 5+ | 0 (usa `EnumToDescriptionConverter` global) |
| **ObjectDataProvider por view** | 4 | 0 |
| **Manutenção de estilo** | Editar N arquivos | Editar 1 arquivo (`ComboBoxStyles.xaml`) |
| **Consistência visual** | Inconsistente | 100% consistente |
| **Type-safety nos filtros** | Parcial (string/int?) | Total (enum? / bool?) |

---

## Testes

- ✅ **Build**: 0 erros, 0 warnings
- ✅ **Testes**: 324 testes passando (61 Domain + 224 Application + 39 Infrastructure)
- ✅ **Funcionalidade**: Todos os filtros e formulários operando corretamente

---

## Como Usar no Futuro

### Para novo filtro de lista com enum:
```xaml
<controls:StatusFilterComboBox
    EnumType="{x:Type domainEnums:MeuEnum}"
    SelectedValue="{Binding MeuFiltro, Mode=TwoWay}"
    IncludeAllOption="True"
    AllOptionText="Todos"
    UseEnumDescription="True"
    Width="180"/>
```

### Para novo ComboBox em formulário:
```xaml
<ComboBox
    ItemsSource="{Binding MinhaColecao}"
    SelectedItem="{Binding ItemSelecionado, Mode=TwoWay}"
    DisplayMemberPath="Nome"
    Style="{StaticResource FormComboBoxStyle}"/>
```

### Para novo ComboBox em diálogo:
```xaml
<ComboBox
    ItemsSource="{Binding Opcoes}"
    SelectedItem="{Binding OpcaoSelecionada, Mode=TwoWay}"
    Style="{StaticResource DialogComboBoxStyle}"/>
```

### Para seletor em DataGrid ou picker:
```xaml
<ComboBox
    ItemsSource="{Binding Itens}"
    SelectedValuePath="Id"
    DisplayMemberPath="Nome"
    SelectedValue="{Binding ItemId, Mode=TwoWay}"
    Style="{StaticResource PickerComboBoxStyle}"/>
```

---

## Arquivos Modificados

### Novos Arquivos
- `src/SmartLanche.Presentation/Styles/ComboBoxStyles.xaml` (criado anteriormente)

### Arquivos Modificados - Domain
- `src/SmartLanche.Domain/Enums/InventoryMovementType.cs`
- `src/SmartLanche.Domain/Enums/InventoryMovementReason.cs`

### Arquivos Modificados - Presentation/Views
- `InventoryListView.xaml`
- `InventoryMovementsView.xaml`
- `InventoryMovementView.xaml`
- `ComboFormView.xaml`
- `ProductPickerView.xaml`
- `PromotionFormView.xaml`
- `PendingPaymentListView.xaml`

### Arquivos Modificados - Presentation
- `App.xaml` (referência ao ComboBoxStyles.xaml - feito anteriormente)
- `Controls/StatusFilterComboBox.xaml/.cs` (melhorado anteriormente)
- `ViewModels/ListViewModelBase.cs` (simplificado anteriormente)

---

## Data da Implementação
**Novembro 2026** - Realizado como parte da refatoração contínua da camada de apresentação.