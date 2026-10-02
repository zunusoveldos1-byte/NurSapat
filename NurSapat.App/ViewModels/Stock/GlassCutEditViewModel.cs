using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Materials;
using NurSapat.App.Models.Stock;
using NurSapat.App.Services.Dictionaries;
using NurSapat.App.Services.Stock;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Stock;

public sealed partial class GlassCutEditViewModel : ObservableObject
{
    private readonly IMaterialPickerService _materialPickerService;
    private readonly IWindowColorsStore _colorsStore;
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public GlassCutEditViewModel(
        IMaterialPickerService materialPickerService,
        IWindowColorsStore colorsStore)
    {
        _materialPickerService = materialPickerService;
        _colorsStore = colorsStore;
    }

    public string WindowTitle => _isNew ? "Новый обрезок стекла" : "Редактирование обрезка стекла";

    public ObservableCollection<string> AvailableColors { get; } = [];

    public Window? OwnerWindow { get; set; }

    [ObservableProperty]
    private string _article = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _selectedColor;

    [ObservableProperty]
    private decimal _widthMm = 800m;

    [ObservableProperty]
    private decimal _heightMm = 1200m;

    public event Action<bool>? RequestClose;

    public Task InitializeAsync()
    {
        AvailableColors.Clear();
        foreach (var color in _colorsStore.GetAll().Select(item => item.Name).OrderBy(name => name))
        {
            AvailableColors.Add(color);
        }

        return Task.CompletedTask;
    }

    public void Load(GlassCutItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;

        Article = source.Article;
        Name = source.Name;
        SelectedColor = string.IsNullOrWhiteSpace(source.Color)
            ? AvailableColors.FirstOrDefault()
            : source.Color;
        WidthMm = (decimal)source.WidthMm;
        HeightMm = (decimal)source.HeightMm;

        OnPropertyChanged(nameof(WindowTitle));
    }

    public GlassCutItem BuildResult() => new()
    {
        Id = _id,
        Article = Article,
        Name = Name,
        Color = SelectedColor ?? string.Empty,
        WidthMm = (double)WidthMm,
        HeightMm = (double)HeightMm
    };

    [RelayCommand]
    private async Task PickMaterial()
    {
        var material = await _materialPickerService.PickMaterialAsync(OwnerWindow, MaterialSection.Glazing);
        if (material is null)
        {
            return;
        }

        Article = material.Article;
        Name = material.Name;
    }

    [RelayCommand]
    private void Save() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}
