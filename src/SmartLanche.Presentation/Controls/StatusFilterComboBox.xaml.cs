using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace SmartLanche.Presentation.Controls;

/// <summary>
/// ComboBox reutilizável para filtros de status (Todos/Ativos/Inativos, enums, etc.)
///
/// Uso básico (booleanos):
///     <controls:StatusFilterComboBox SelectedValue="{Binding SelectedFilter, Mode=TwoWay}" />
///
/// Uso com enum (ex: OrderStatus):
///     <controls:StatusFilterComboBox SelectedValue="{Binding SelectedStatus, Mode=TwoWay}"
///                                    EnumType="{x:Type domain:OrderStatus}"
///                                    IncludeAllOption="True"
///                                    AllOptionText="Todos" />
///
/// Uso com items customizados (ViewModel):
///     <controls:StatusFilterComboBox SelectedValue="{Binding SelectedFilter, Mode=TwoWay}"
///                                    Items="{Binding MeusFiltros}" />
/// </summary>
public partial class StatusFilterComboBox : UserControl
{
    #region Dependency Properties

    public static readonly DependencyProperty SelectedValueProperty =
        DependencyProperty.Register(
            nameof(SelectedValue),
            typeof(object),
            typeof(StatusFilterComboBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedValueChanged));

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(IEnumerable<StatusFilterItem>),
            typeof(StatusFilterComboBox),
            new PropertyMetadata(null, OnItemsChanged));

    public static readonly DependencyProperty EnumTypeProperty =
        DependencyProperty.Register(
            nameof(EnumType),
            typeof(Type),
            typeof(StatusFilterComboBox),
            new PropertyMetadata(null, OnEnumTypeChanged));

    public static readonly DependencyProperty IncludeAllOptionProperty =
        DependencyProperty.Register(
            nameof(IncludeAllOption),
            typeof(bool),
            typeof(StatusFilterComboBox),
            new PropertyMetadata(true, OnEnumTypeChanged));

    public static readonly DependencyProperty AllOptionTextProperty =
        DependencyProperty.Register(
            nameof(AllOptionText),
            typeof(string),
            typeof(StatusFilterComboBox),
            new PropertyMetadata("Todos", OnEnumTypeChanged));

    public static readonly DependencyProperty UseEnumDescriptionProperty =
        DependencyProperty.Register(
            nameof(UseEnumDescription),
            typeof(bool),
            typeof(StatusFilterComboBox),
            new PropertyMetadata(true, OnEnumTypeChanged));

    #endregion

    #region Properties

    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public IEnumerable<StatusFilterItem>? Items
    {
        get => (IEnumerable<StatusFilterItem>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <summary>
    /// Tipo do enum para gerar items automaticamente (ex: typeof(OrderStatus)).
    /// Se definido, ignora a propriedade Items.
    /// </summary>
    public Type? EnumType
    {
        get => (Type?)GetValue(EnumTypeProperty);
        set => SetValue(EnumTypeProperty, value);
    }

    /// <summary>
    /// Se true, adiciona item "Todos" com valor null no início da lista.
    /// </summary>
    public bool IncludeAllOption
    {
        get => (bool)GetValue(IncludeAllOptionProperty);
        set => SetValue(IncludeAllOptionProperty, value);
    }

    /// <summary>
    /// Texto do item "Todos".
    /// </summary>
    public string AllOptionText
    {
        get => (string)GetValue(AllOptionTextProperty);
        set => SetValue(AllOptionTextProperty, value);
    }

    /// <summary>
    /// Se true, usa DescriptionAttribute do enum para obter descrição amigável.
    /// Se false, usa o nome do campo do enum.
    /// </summary>
    public bool UseEnumDescription
    {
        get => (bool)GetValue(UseEnumDescriptionProperty);
        set => SetValue(UseEnumDescriptionProperty, value);
    }

    #endregion

    #region Constructors

    public StatusFilterComboBox()
    {
        InitializeComponent();

        // Se nenhum EnumType nem Items forem definidos, usa padrão bool (Todos/Ativos/Inativos)
        Loaded += (_, _) =>
        {
            if (EnumType is null && Items is null)
            {
                Items = CreateDefaultBooleanFilters();
            }
        };
    }

    #endregion

    #region Property Changed Handlers

    private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusFilterComboBox control && control.PART_ComboBox is not null)
        {
            control.PART_ComboBox.SelectedValue = e.NewValue;
        }
    }

    private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Items mudou - o binding no XAML cuida da atualização
    }

    private static void OnEnumTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusFilterComboBox control && control.EnumType is not null)
        {
            control.Items = control.GenerateItemsFromEnum();
        }
    }

    #endregion

    #region Item Generation

    /// <summary>
    /// Cria filtros padrão para booleanos: Todos / Ativos / Inativos
    /// </summary>
    public static IEnumerable<StatusFilterItem> CreateDefaultBooleanFilters()
    {
        return new[]
        {
            new StatusFilterItem("Todos", null),
            new StatusFilterItem("Ativos", true),
            new StatusFilterItem("Inativos", false)
        };
    }

    /// <summary>
    /// Gera items a partir do EnumType definido.
    /// Usa DescriptionAttribute se UseEnumDescription=true, senão usa nome do campo.
    /// </summary>
    private IEnumerable<StatusFilterItem> GenerateItemsFromEnum()
    {
        if (EnumType is null || !EnumType.IsEnum)
            return [];

        var items = new List<StatusFilterItem>();

        if (IncludeAllOption)
        {
            items.Add(new StatusFilterItem(AllOptionText, null));
        }

        var enumValues = Enum.GetValues(EnumType);
        foreach (var value in enumValues)
        {
            string displayName;

            if (UseEnumDescription)
            {
                var field = EnumType.GetField(value.ToString()!);
                var descAttr = field?.GetCustomAttribute<DescriptionAttribute>();
                displayName = descAttr?.Description ?? value.ToString()!;
            }
            else
            {
                displayName = value.ToString()!;
            }

            items.Add(new StatusFilterItem(displayName, value));
        }

        return items;
    }

    /// <summary>
    /// Método estático para ViewModels criarem filtros de enum tipados.
    /// Ex: StatusFilters = StatusFilterComboBox.CreateFromEnum<OrderStatus>("Todos");
    /// </summary>
    public static IEnumerable<StatusFilterItem> CreateFromEnum(Type enumType, string? allOptionText = "Todos")
    {
        if (enumType is null || !enumType.IsEnum)
            return [];

        var items = new List<StatusFilterItem>();

        if (!string.IsNullOrEmpty(allOptionText))
        {
            items.Add(new StatusFilterItem(allOptionText, null));
        }

        foreach (var value in Enum.GetValues(enumType))
        {
            var field = enumType.GetField(value.ToString()!);
            var descAttr = field?.GetCustomAttribute<DescriptionAttribute>();
            var displayName = descAttr?.Description ?? value.ToString()!;
            items.Add(new StatusFilterItem(displayName, value));
        }

        return items;
    }

    /// <summary>
    /// Versão genérica para uso em ViewModels.
    /// Ex: StatusFilters = StatusFilterComboBox.CreateFromEnum<OrderStatus>("Todos");
    /// </summary>
    public static IEnumerable<StatusFilterItem> CreateFromEnum<TEnum>(string? allOptionText = "Todos") where TEnum : struct, Enum
    {
        return CreateFromEnum(typeof(TEnum), allOptionText);
    }

    #endregion
}