using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Materials;
using NurSapat.App.Services.Materials;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Stock;

public sealed partial class MaterialPickerViewModel : ObservableObject
{
    private readonly IMaterialsDetailStore _materialsStore;

    public MaterialPickerViewModel(IMaterialsDetailStore materialsStore)
    {
        _materialsStore = materialsStore;
    }

    public string Title { get; private set; } = "Выбор материала";

    private MaterialSection? _sectionFilter;

    public void Configure(MaterialSection? sectionFilter)
    {
        _sectionFilter = sectionFilter;
        Title = sectionFilter == MaterialSection.Glazing
            ? "Выбор материала (остекление)"
            : "Выбор материала";
        OnPropertyChanged(nameof(Title));
    }

    public ObservableCollection<MaterialDetail> FilteredMaterials { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private MaterialDetail? _selectedMaterial;

    public event Action<bool>? RequestClose;

    public Task InitializeAsync()
    {
        ApplyFilter();
        SelectedMaterial = FilteredMaterials.FirstOrDefault();
        return Task.CompletedTask;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedMaterial is not null)
        {
            RequestClose?.Invoke(true);
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<MaterialDetail> query = _materialsStore.GetAll();

        if (_sectionFilter is MaterialSection section)
        {
            query = query.Where(material => material.Section == section);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(material =>
                material.Article.Contains(search, StringComparison.OrdinalIgnoreCase)
                || material.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        FilteredMaterials.Clear();
        foreach (var material in query.OrderBy(item => item.Article))
        {
            FilteredMaterials.Add(material);
        }
    }
}
