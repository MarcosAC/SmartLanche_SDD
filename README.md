# SmartLanche

Sistema de gestão para lanchonetes desenvolvido em **.NET 10** com **WPF** seguindo **Clean Architecture** e **SDD (Spec-Driven Development)**.

## 📋 Visão Geral

O SmartLanche é uma aplicação desktop para gestão completa de lanchonetes, incluindo:
- **Produtos e Categorias** - Cadastro, estoque, combos
- **Clientes** - Cadastro, histórico, fiado (pendências)
- **Pedidos** - Criação, fechamento, cancelamento, preparação
- **Promoções** - Descontos por produto/categoria/data com prioridade
- **Estoque** - Entrada/saída, movimentações, alerta de mínimo
- **Pagamentos** - Controle de fiado, devedores, recebimentos
- **Dashboard** - Métricas em tempo real (pedidos abertos, receita, top produtos)

## 🏗️ Arquitetura

```
src/
├── SmartLanche.Domain          # Entidades, Enums, Regras de negócio puras
├── SmartLanche.Application     # Services, DTOs, Interfaces (Use Cases)
├── SmartLanche.Infrastructure  # EF Core, Repositories, Migrations, SQL Server
└── SmartLanche.Presentation    # WPF MVVM, Views, ViewModels
tests/
├── SmartLanche.Domain.Tests    # 61 testes unitários de domínio
├── SmartLanche.Application.Tests  # 244 testes de aplicação (fakes)
└── SmartLanche.Infrastructure.Tests # Testes de integração com BD
```

### Princípios Aplicados

- **Clean Architecture** - Dependências apontam para o centro (Domain)
- **SDD (Spec-Driven Development)** - Especificações/contratos definidos antes da implementação (interfaces, DTOs, validações)
- **Template Method** - `ListViewModelBase`/`CrudServiceBase` definem fluxo, subclasses customizam
- **Event-based Navigation** - ViewModels disparam eventos (`RequestNewItem`), `MainViewModel` decide navegação
- **Soft Delete** - Todas as entidades têm `IsActive`, query filter global no EF Core
- **Per-operation DbContext** - `IServiceScopeFactory` garante contexto curto e isolado

## 🛠️ Stack Tecnológica

| Camada | Tecnologias |
|--------|-------------|
| **Framework** | .NET 10, C# 13 |
| **UI** | WPF (Windows Presentation Foundation), MVVM |
| **DI** | Microsoft.Extensions.DependencyInjection |
| **ORM** | Entity Framework Core 10, SQL Server |
| **Testes** | xUnit, Fake Repositories (in-memory) |
| **Commands** | `RelayCommand` (ICommand manual) |
| **Notificações** | Toast customizado |

## 🚀 Como Executar

### Pré-requisitos
- .NET 10 SDK
- SQL Server (LocalDB ou instância completa)
- Windows 10/11 (WPF)

### Configuração

```bash
# Clone o repositório
git clone https://github.com/MarcosAC/SmartLanche_SDD.git
cd SmartLanche_SDD/SmartLanche

# Configure connection string em appsettings.json (Infrastructure)
# Ou use variável de ambiente: ConnectionStrings__DefaultConnection

# Execute migrações
dotnet ef database update --project src/SmartLanche.Infrastructure --startup-project src/SmartLanche.Presentation

# Rode a aplicação
dotnet run --project src/SmartLanche.Presentation
```

### Testes

```bash
# Todos os testes
dotnet test

# Apenas aplicação (244 testes)
dotnet test tests/SmartLanche.Application.Tests

# Apenas domínio (61 testes)
dotnet test tests/Smartlanche.Domain.Tests
```

## 📦 Estrutura de Pastas

```
SmartLanche/
├── docs/                          # Documentação
│   ├── viewmodel-structure.md     # Estrutura das ViewModels
│   └── viewmodel-best-practices.md # Avaliação de boas práticas
├── src/
│   ├── SmartLanche.Domain/
│   │   ├── Entities/              # Category, Product, Combo, Order, Customer...
│   │   ├── Enums/                 # OrderStatus, DiscountType, InventoryMovementType...
│   │   └── Interfaces/            # IEntity, IEntityId, IEntityDto
│   ├── SmartLanche.Application/
│   │   ├── DTOs/                  # Requests/Responses
│   │   ├── Interfaces/            # IProductService, IOrderService, IRepository<T>...
│   │   └── Services/              # ProductService, OrderService, ComboService...
│   ├── SmartLanche.Infrastructure/
│   │   ├── Data/                  # ApplicationDbContext, Configurations, Migrations
│   │   └── Repositories/          # EF Core implementations
│   └── SmartLanche.Presentation/
│       ├── ViewModels/            # List, Form, MainViewModel
│       ├── Views/                 # XAML UserControls
│       ├── Commands/              # RelayCommand
│       ├── Services/              # ToastNotificationService
│       └── Converters/            # ValueConverters WPF
└── tests/
    ├── SmartLanche.Domain.Tests/
    ├── SmartLanche.Application.Tests/
    └── SmartLanche.Infrastructure.Tests/
```

## ✨ Funcionalidades Principais

### Produtos & Combos
- CRUD completo com categorias
- Estoque automático (InventoryItem) opcional no cadastro
- Combos com múltiplos itens e validação de disponibilidade
- Preço de combo independente dos componentes

### Pedidos
- Itens avulsos e combos
- Fechamento com cálculo de promoções (prioridade + validade data)
- Cancelamento com estorno de estoque
- Modo preparação (cozinha) com status

### Promoções Inteligentes
- Desconto percentual ou valor fixo
- Aplicável a: todos produtos, produtos específicos, categorias
- Prioridade + vigência (início/fim)
- Cálculo automático no fechamento do pedido

### Fiado & Pagamentos
- Pedidos "Fiado" geram `PendingPayment`
- Tela de devedores com filtros
- Recebimento parcial/total com baixa automática
- Histórico de pagamentos por cliente

### Estoque
- Entrada/Saída manual com motivo
- Alerta de estoque baixo/mínimo
- Movimentações automáticas em pedidos (venda, estorno)
- Valor total do estoque em tempo real

## 🧪 Qualidade

- **305 testes** no total (244 App + 61 Domain)
- Testes de aplicação usam **Fake Repositories** (isolamento total, sem BD)
- Integração contínua pronta (GitHub Actions)
- Zero warnings de build em `TreatWarningsAsErrors`

## 📄 Licença

Proprietário - Desenvolvido por Marcos AC

---

> **Nota sobre SDD:** Este projeto foi desenvolvido seguindo **Spec-Driven Development**. As especificações (interfaces de serviço, DTOs, regras de validação, contratos de repositório) foram definidas **antes** da implementação concreta, servindo como contrato entre camadas e base para testes. Isso garantiu baixo acoplamento, alta testabilidade e clareza nos requisitos desde o início.