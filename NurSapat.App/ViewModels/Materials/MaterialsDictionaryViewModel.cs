using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Materials;
using NurSapat.App.Services.Materials;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Materials;

public sealed partial class MaterialsDictionaryViewModel : ObservableObject
{
    private readonly IMaterialsDetailStore _store;
    private readonly IMaterialEditorService _editorService;
    private readonly IMaterialExportService _exportService;
    private readonly List<MaterialListItemViewModel> _allItems = [];

    public MaterialsDictionaryViewModel(
      IMaterialsDetailStore store,
      IMaterialEditorService editorService,
      IMaterialExportService exportService)
    {
        _store = store;
        _editorService = editorService;
        _exportService = exportService;
        ManufacturersFilter = new ObservableCollection<string> { AllManufacturersLabel };
    }

    public const string AllManufacturersLabel = "Все производители";

    public string Title => "Материалы";

    public ObservableCollection<MaterialListItemViewModel> FilteredItems { get; } = [];

    public ObservableCollection<string> ManufacturersFilter { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    public string SearchQuery
    {
        get => SearchText;
        set => SearchText = value;
    }

    [ObservableProperty]
    private string _selectedManufacturer = AllManufacturersLabel;

    [ObservableProperty]
    private MaterialListItemViewModel? _selectedItem;

    [ObservableProperty]
    private Window? _ownerWindow;

    public bool IsSelected => SelectedItem is not null;

    public IAsyncRelayCommand EditMaterialCommand => EditCommand;

    public IAsyncRelayCommand DeleteMaterialCommand => DeleteCommand;

    public async Task InitializeAsync()
    {
        _allItems.Clear();
        foreach (var material in _store.GetAll())
        {
            _allItems.Add(MaterialListItemViewModel.FromModel(material));
        }

        RefreshManufacturersFilter();
        ApplyFilter();
        await Task.CompletedTask;
    }

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(SearchQuery));
        ApplyFilter();
    }

    partial void OnSelectedManufacturerChanged(string value) => ApplyFilter();

    partial void OnSelectedItemChanged(MaterialListItemViewModel? value)
    {
        OnPropertyChanged(nameof(IsSelected));
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        AddAsCopyCommand.NotifyCanExecuteChanged();
    }

    private void RefreshManufacturersFilter()
    {
        var current = SelectedManufacturer;
        ManufacturersFilter.Clear();
        ManufacturersFilter.Add(AllManufacturersLabel);

        foreach (var manufacturer in _allItems.Select(item => item.Manufacturer).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().OrderBy(name => name))
        {
            ManufacturersFilter.Add(manufacturer);
        }

        SelectedManufacturer = ManufacturersFilter.Contains(current) ? current : AllManufacturersLabel;
    }

    private void ApplyFilter()
    {
        IEnumerable<MaterialListItemViewModel> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SelectedManufacturer) && SelectedManufacturer != AllManufacturersLabel)
        {
            query = query.Where(item => string.Equals(item.Manufacturer, SelectedManufacturer, StringComparison.OrdinalIgnoreCase));
        }

        var search = SearchText.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item =>
                item.Article.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                item.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                item.SuppliersText.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        FilteredItems.Clear();
        var index = 1;
        foreach (var item in query.OrderBy(item => item.Article))
        {
            item.IndexNumber = index++;
            FilteredItems.Add(item);
        }

        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        AddAsCopyCommand.NotifyCanExecuteChanged();
    }

    private bool HasSelection() => SelectedItem is not null;

    [RelayCommand]
    private async Task AddAsync()
    {
        var created = await _editorService.ShowEditorAsync(OwnerWindow, MaterialDetail.CreateDefault(), isClone: true);
        if (created is null)
        {
            return;
        }

        _store.Add(created);
        _allItems.Add(MaterialListItemViewModel.FromModel(created));
        RefreshManufacturersFilter();
        ApplyFilter();
        SelectedItem = FilteredItems.FirstOrDefault(item => item.Id == created.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task AddAsCopyAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var source = _store.GetById(SelectedItem.Id)?.Clone(newId: true) ?? MaterialDetail.CreateDefault();
        source.Article = string.Empty;

        var created = await _editorService.ShowEditorAsync(OwnerWindow, source, isClone: true);
        if (created is null)
        {
            return;
        }

        _store.Add(created);
        _allItems.Add(MaterialListItemViewModel.FromModel(created));
        RefreshManufacturersFilter();
        ApplyFilter();
        SelectedItem = FilteredItems.FirstOrDefault(item => item.Id == created.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task EditAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        await OpenEditorForItemAsync(SelectedItem);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task DeleteAsync()
    {
        if (SelectedItem is null)
        {
            return Task.CompletedTask;
        }

        _store.Delete(SelectedItem.Id);
        _allItems.RemoveAll(item => item.Id == SelectedItem.Id);
        SelectedItem = FilteredItems.FirstOrDefault();
        RefreshManufacturersFilter();
        ApplyFilter();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        var path = await PickSavePathAsync("Excel (*.csv)|*.csv", "materials.csv");
        if (path is null)
        {
            return;
        }

        await _exportService.ExportToExcelAsync(FilteredItems.ToList(), path);
    }

    [RelayCommand]
    private async Task ExportToTxtAsync()
    {
        var path = await PickSavePathAsync("Текстовый файл (*.txt)|*.txt", "materials.txt");
        if (path is null)
        {
            return;
        }

        await _exportService.ExportToTxtAsync(FilteredItems.ToList(), path);
    }

    public async Task OpenEditorForItemAsync(MaterialListItemViewModel item)
    {
        var source = _store.GetById(item.Id);
        if (source is null)
        {
            return;
        }

        var updated = await _editorService.ShowEditorAsync(OwnerWindow, source.Clone(newId: false));
        if (updated is null)
        {
            return;
        }

        _store.Update(updated);
        item.RefreshFrom(updated);
        RefreshManufacturersFilter();
        ApplyFilter();
    }

    private async Task<string?> PickSavePathAsync(string filter, string defaultName)
    {
        if (OwnerWindow?.StorageProvider is not { } storage)
        {
            return null;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить экспорт",
            SuggestedFileName = defaultName,
            FileTypeChoices =
          [
            new FilePickerFileType(filter.Split('|')[0])
        {
          Patterns = [filter.Split('|')[1].Replace("*", "")]
        }
          ]
        });

        return file?.Path.LocalPath;
    }
}
