using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Config;
using NurSapat.App.Services;
using NurSapat.App.Services.Config;
using NurSapat.App.Services.Dictionaries;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Config;

public sealed partial class ConstructionElementsViewModel : ViewModelBase
{
    private readonly IConstructionElementsStore _elementsStore;
    private readonly IManufacturersStore _manufacturersStore;
    private readonly IProfileSeriesStore _seriesStore;
    private readonly IAppMessageService _messageService;
    private readonly List<ConstructionElement> _workingElements = [];
    private ConstructionElement? _selectedElementModel;
    private bool _isLoadingElement;

    public ConstructionElementsViewModel(
        IConstructionElementsStore elementsStore,
        IManufacturersStore manufacturersStore,
        IProfileSeriesStore seriesStore,
        IAppMessageService messageService)
    {
        _elementsStore = elementsStore;
        _manufacturersStore = manufacturersStore;
        _seriesStore = seriesStore;
        _messageService = messageService;

        foreach (var section in ConstructionSectionTypeExtensions.TerminalSections)
        {
            TerminalSections.Add(new ConstructionSectionItemViewModel(section));
        }

        foreach (var section in ConstructionSectionTypeExtensions.BasicSections)
        {
            BasicSections.Add(new ConstructionSectionItemViewModel(section));
        }

        ManufacturerOptions.Add(ManufacturerFilterOption.All);
        foreach (var manufacturer in _manufacturersStore.GetAll())
        {
            ManufacturerOptions.Add(new ManufacturerFilterOption
            {
                ManufacturerId = manufacturer.Id,
                Name = manufacturer.Name
            });
        }

        AddElementCommand = new RelayCommand(AddElement, () => SelectedSection is not null);
        DeleteElementCommand = new RelayCommand(DeleteElement, () => SelectedElement is not null);
        CopyElementCommand = new RelayCommand(CopyElement, () => SelectedElement is not null);
        AddSeriesToBindingCommand = new RelayCommand(AddSeriesToBinding, CanModifySeriesBinding);
        ExcludeSeriesFromBindingCommand = new RelayCommand(ExcludeSeriesFromBinding, CanModifySeriesBinding);
        AddHardwareRuleCommand = new RelayCommand(AddHardwareRule, () => SelectedElement is not null);
        DeleteHardwareRuleCommand = new RelayCommand(DeleteHardwareRule, () => SelectedHardwareRule is not null);
        OkCommand = new RelayCommand(SaveAndClose);
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
    }

    public string Title => "Элементы конструкций";

    public ObservableCollection<ManufacturerFilterOption> ManufacturerOptions { get; } = [];

    public ObservableCollection<ConstructionSectionItemViewModel> TerminalSections { get; } = [];

    public ObservableCollection<ConstructionSectionItemViewModel> BasicSections { get; } = [];

    public ObservableCollection<ConstructionElementItemViewModel> FilteredElements { get; } = [];

    public ObservableCollection<ProfileSeriesBindingViewModel> SeriesBindings { get; } = [];

    public ObservableCollection<HardwareAutoPickRuleViewModel> HardwareRules { get; } = [];

    public IRelayCommand AddElementCommand { get; }

    public IRelayCommand DeleteElementCommand { get; }

    public IRelayCommand CopyElementCommand { get; }

    public IRelayCommand AddSeriesToBindingCommand { get; }

    public IRelayCommand ExcludeSeriesFromBindingCommand { get; }

    public IRelayCommand AddHardwareRuleCommand { get; }

    public IRelayCommand DeleteHardwareRuleCommand { get; }

    public IRelayCommand OkCommand { get; }

    public IRelayCommand CloseCommand { get; }

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    [ObservableProperty]
    private ManufacturerFilterOption? _selectedManufacturer;

    [ObservableProperty]
    private ConstructionSectionItemViewModel? _selectedSection;

    [ObservableProperty]
    private ConstructionElementItemViewModel? _selectedElement;

    [ObservableProperty]
    private ProfileSeriesBindingViewModel? _selectedSeriesBinding;

    [ObservableProperty]
    private HardwareAutoPickRuleViewModel? _selectedHardwareRule;

    [ObservableProperty]
    private string _elementName = string.Empty;

    [ObservableProperty]
    private string _article = string.Empty;

    [ObservableProperty]
    private double _widthMm;

    [ObservableProperty]
    private double _heightMm;

    [ObservableProperty]
    private double _wallThicknessMm;

    [ObservableProperty]
    private double _weightKg;

    [ObservableProperty]
    private string _glassFormula = string.Empty;

    [ObservableProperty]
    private double _glassThicknessMm;

    [ObservableProperty]
    private string _glassStructure = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    public bool IsProfilePanelVisible =>
        SelectedSection?.SectionType.UsesProfilePanel() == true;

    public bool IsGlazingPanelVisible =>
        SelectedSection?.SectionType.UsesGlazingPanel() == true;

    public bool IsHardwarePanelVisible =>
        SelectedSection?.SectionType.UsesHardwarePanel() == true;

    public bool IsGenericPanelVisible =>
        SelectedSection is not null &&
        !IsProfilePanelVisible &&
        !IsGlazingPanelVisible &&
        !IsHardwarePanelVisible;

    public Task InitializeAsync()
    {
        _workingElements.Clear();
        _workingElements.AddRange(_elementsStore.GetAll().Select(item => item.Clone()));

        SelectedManufacturer = ManufacturerOptions.FirstOrDefault();
        SelectedSection = TerminalSections.FirstOrDefault();
        RefreshFilteredElements();
        return Task.CompletedTask;
    }

    partial void OnSelectedManufacturerChanged(ManufacturerFilterOption? value)
    {
        CommitCurrentElement();
        RefreshFilteredElements();
        RefreshSeriesBindings();
    }

    partial void OnSelectedSectionChanged(ConstructionSectionItemViewModel? value)
    {
        CommitCurrentElement();
        RefreshFilteredElements();
        NotifyPanelVisibility();
        NotifyElementCommands();
    }

    partial void OnSelectedElementChanged(ConstructionElementItemViewModel? value)
    {
        LoadSelectedElement();
        NotifyElementCommands();
        NotifySeriesCommands();
    }

    partial void OnElementNameChanged(string value) => UpdateSelectedModel(model => model.Name = value);

    partial void OnArticleChanged(string value) => UpdateSelectedModel(model => model.Article = value);

    partial void OnWidthMmChanged(double value) => UpdateSelectedModel(model => model.WidthMm = value);

    partial void OnHeightMmChanged(double value) => UpdateSelectedModel(model => model.HeightMm = value);

    partial void OnWallThicknessMmChanged(double value) => UpdateSelectedModel(model => model.WallThicknessMm = value);

    partial void OnWeightKgChanged(double value) => UpdateSelectedModel(model => model.WeightKg = value);

    partial void OnGlassFormulaChanged(string value) => UpdateSelectedModel(model => model.GlassFormula = value);

    partial void OnGlassThicknessMmChanged(double value) => UpdateSelectedModel(model => model.GlassThicknessMm = value);

    partial void OnGlassStructureChanged(string value) => UpdateSelectedModel(model => model.GlassStructure = value);

    partial void OnNotesChanged(string value) => UpdateSelectedModel(model => model.Notes = value);

    private void RefreshFilteredElements()
    {
        CommitCurrentElement();

        var selectedId = SelectedElement?.Id;
        FilteredElements.Clear();

        foreach (var element in _workingElements
                     .Where(MatchesCurrentFilter)
                     .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            FilteredElements.Add(new ConstructionElementItemViewModel(element));
        }

        SelectedElement = FilteredElements.FirstOrDefault(item => item.Id == selectedId)
            ?? FilteredElements.FirstOrDefault();
    }

    private bool MatchesCurrentFilter(ConstructionElement element)
    {
        if (SelectedSection is not null && element.Section != SelectedSection.SectionType)
        {
            return false;
        }

        if (SelectedManufacturer?.ManufacturerId is Guid manufacturerId &&
            element.ManufacturerId != manufacturerId)
        {
            return false;
        }

        return true;
    }

    private void LoadSelectedElement()
    {
        _isLoadingElement = true;

        if (SelectedElement is null)
        {
            _selectedElementModel = null;
            ElementName = string.Empty;
            Article = string.Empty;
            WidthMm = 0;
            HeightMm = 0;
            WallThicknessMm = 0;
            WeightKg = 0;
            GlassFormula = string.Empty;
            GlassThicknessMm = 0;
            GlassStructure = string.Empty;
            Notes = string.Empty;
            HardwareRules.Clear();
            SeriesBindings.Clear();
            _isLoadingElement = false;
            return;
        }

        _selectedElementModel = _workingElements.FirstOrDefault(item => item.Id == SelectedElement.Id);
        if (_selectedElementModel is null)
        {
            _isLoadingElement = false;
            return;
        }

        ElementName = _selectedElementModel.Name;
        Article = _selectedElementModel.Article;
        WidthMm = _selectedElementModel.WidthMm;
        HeightMm = _selectedElementModel.HeightMm;
        WallThicknessMm = _selectedElementModel.WallThicknessMm;
        WeightKg = _selectedElementModel.WeightKg;
        GlassFormula = _selectedElementModel.GlassFormula;
        GlassThicknessMm = _selectedElementModel.GlassThicknessMm;
        GlassStructure = _selectedElementModel.GlassStructure;
        Notes = _selectedElementModel.Notes;

        HardwareRules.Clear();
        foreach (var rule in _selectedElementModel.HardwareRules)
        {
            HardwareRules.Add(new HardwareAutoPickRuleViewModel(rule));
        }

        RefreshSeriesBindings();
        _isLoadingElement = false;
    }

    private void RefreshSeriesBindings()
    {
        if (_selectedElementModel is null)
        {
            SeriesBindings.Clear();
            return;
        }

        var manufacturerId = ResolveSeriesManufacturerId();
        var selectedSeriesId = SelectedSeriesBinding?.SeriesId;

        SeriesBindings.Clear();
        foreach (var series in _seriesStore.GetAll()
                     .Where(item => item.ManufacturerId == manufacturerId)
                     .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var isLinked = _selectedElementModel.AssociatedSeriesIds.Contains(series.Id);
            SeriesBindings.Add(new ProfileSeriesBindingViewModel(series.Id, series.Name, isLinked));
        }

        SelectedSeriesBinding = SeriesBindings.FirstOrDefault(item => item.SeriesId == selectedSeriesId)
            ?? SeriesBindings.FirstOrDefault();
    }

    private Guid ResolveSeriesManufacturerId()
    {
        if (SelectedManufacturer?.ManufacturerId is Guid manufacturerId)
        {
            return manufacturerId;
        }

        if (_selectedElementModel is not null)
        {
            return _selectedElementModel.ManufacturerId;
        }

        return _manufacturersStore.GetAll().FirstOrDefault()?.Id ?? Guid.Empty;
    }

    private void CommitCurrentElement()
    {
        if (_isLoadingElement || _selectedElementModel is null)
        {
            return;
        }

        _selectedElementModel.Name = ElementName;
        _selectedElementModel.Article = Article;
        _selectedElementModel.WidthMm = WidthMm;
        _selectedElementModel.HeightMm = HeightMm;
        _selectedElementModel.WallThicknessMm = WallThicknessMm;
        _selectedElementModel.WeightKg = WeightKg;
        _selectedElementModel.GlassFormula = GlassFormula;
        _selectedElementModel.GlassThicknessMm = GlassThicknessMm;
        _selectedElementModel.GlassStructure = GlassStructure;
        _selectedElementModel.Notes = Notes;
        _selectedElementModel.HardwareRules = HardwareRules.Select(rule => rule.ToModel()).ToList();

        if (SelectedElement is not null && SelectedElement.Name != ElementName)
        {
            SelectedElement.Name = ElementName;
        }

        foreach (var binding in SeriesBindings)
        {
            if (binding.IsLinked)
            {
                if (!_selectedElementModel.AssociatedSeriesIds.Contains(binding.SeriesId))
                {
                    _selectedElementModel.AssociatedSeriesIds.Add(binding.SeriesId);
                }
            }
            else
            {
                _selectedElementModel.AssociatedSeriesIds.Remove(binding.SeriesId);
            }
        }
    }

    private void UpdateSelectedModel(Action<ConstructionElement> update)
    {
        if (_isLoadingElement || _selectedElementModel is null)
        {
            return;
        }

        update(_selectedElementModel);
    }

    private Guid ResolveManufacturerIdForNewElement()
    {
        if (SelectedManufacturer?.ManufacturerId is Guid manufacturerId)
        {
            return manufacturerId;
        }

        return _manufacturersStore.GetAll().FirstOrDefault()?.Id ?? Guid.Empty;
    }

    private void AddElement()
    {
        if (SelectedSection is null)
        {
            return;
        }

        CommitCurrentElement();

        var element = new ConstructionElement
        {
            Name = $"Новый элемент ({SelectedSection.DisplayName})",
            ManufacturerId = ResolveManufacturerIdForNewElement(),
            Section = SelectedSection.SectionType
        };

        _workingElements.Add(element);
        RefreshFilteredElements();
        SelectedElement = FilteredElements.FirstOrDefault(item => item.Id == element.Id);
    }

    private void DeleteElement()
    {
        if (SelectedElement is null)
        {
            return;
        }

        CommitCurrentElement();
        _workingElements.RemoveAll(item => item.Id == SelectedElement.Id);
        RefreshFilteredElements();
    }

    private void CopyElement()
    {
        if (SelectedElement is null)
        {
            return;
        }

        CommitCurrentElement();

        var source = _workingElements.FirstOrDefault(item => item.Id == SelectedElement.Id);
        if (source is null)
        {
            return;
        }

        var copy = source.Clone();
        copy.Id = Guid.NewGuid();
        copy.Name = $"{source.Name} (копия)";
        _workingElements.Add(copy);
        RefreshFilteredElements();
        SelectedElement = FilteredElements.FirstOrDefault(item => item.Id == copy.Id);
    }

    private bool CanModifySeriesBinding() =>
        SelectedElement is not null && SelectedSeriesBinding is not null;

    private void AddSeriesToBinding()
    {
        if (SelectedSeriesBinding is null)
        {
            return;
        }

        SelectedSeriesBinding.IsLinked = true;
        CommitCurrentElement();
    }

    private void ExcludeSeriesFromBinding()
    {
        if (SelectedSeriesBinding is null)
        {
            return;
        }

        SelectedSeriesBinding.IsLinked = false;
        CommitCurrentElement();
    }

    private void AddHardwareRule()
    {
        if (_selectedElementModel is null)
        {
            return;
        }

        var rule = new HardwareAutoPickRuleViewModel(new HardwareAutoPickRule());
        HardwareRules.Add(rule);
        SelectedHardwareRule = rule;
        CommitCurrentElement();
    }

    private void DeleteHardwareRule()
    {
        if (SelectedHardwareRule is null)
        {
            return;
        }

        HardwareRules.Remove(SelectedHardwareRule);
        SelectedHardwareRule = HardwareRules.FirstOrDefault();
        CommitCurrentElement();
    }

    private void SaveAndClose()
    {
        CommitCurrentElement();
        _elementsStore.ReplaceAll(_workingElements);
        _elementsStore.Save();
        _messageService.LogInfo("Элементы конструкций сохранены.");
        RequestClose?.Invoke();
    }

    private void NotifyPanelVisibility()
    {
        OnPropertyChanged(nameof(IsProfilePanelVisible));
        OnPropertyChanged(nameof(IsGlazingPanelVisible));
        OnPropertyChanged(nameof(IsHardwarePanelVisible));
        OnPropertyChanged(nameof(IsGenericPanelVisible));
    }

    private void NotifyElementCommands()
    {
        AddElementCommand.NotifyCanExecuteChanged();
        DeleteElementCommand.NotifyCanExecuteChanged();
        CopyElementCommand.NotifyCanExecuteChanged();
        AddHardwareRuleCommand.NotifyCanExecuteChanged();
        DeleteHardwareRuleCommand.NotifyCanExecuteChanged();
    }

    private void NotifySeriesCommands()
    {
        AddSeriesToBindingCommand.NotifyCanExecuteChanged();
        ExcludeSeriesFromBindingCommand.NotifyCanExecuteChanged();
    }
}
