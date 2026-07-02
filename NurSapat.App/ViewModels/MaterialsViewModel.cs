using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NurSapat.App.ViewModels;

public sealed partial class MaterialsViewModel : ObservableObject
{
    private readonly IMaterialRepository _materialRepo;

    // ИСПОЛЬЗУЕМ ПОЛНОЕ ИМЯ ТИПА, ЧТОБЫ ИЗБЕЖАТЬ КОНФЛИКТА
    [ObservableProperty]
    private ObservableCollection<NurSapat.Core.Models.Material> _materials = new();

    [ObservableProperty]
    private NurSapat.Core.Models.Material? _selectedMaterial;

    public MaterialsViewModel(IMaterialRepository materialRepo)
    {
        _materialRepo = materialRepo;
        LoadMaterialsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadMaterials()
    {
        var list = await _materialRepo.GetAllAsync();
        Materials = new ObservableCollection<NurSapat.Core.Models.Material>(list);
    }

    [RelayCommand]
    private async Task AddMaterial()
    {
        var newMaterial = new NurSapat.Core.Models.Material
        {
            Article = "Новый артикул",
            Name = "Новый материал",
            // ИСПРАВЛЕНИЕ: Используем полное имя enum
            Category = NurSapat.Core.Models.MaterialCategory.Profile,
            UnitOfMeasure = "м/п",
            Price = 0
        };
        await _materialRepo.AddAsync(newMaterial);
        await LoadMaterials();
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {
        if (SelectedMaterial is null) return;
        await _materialRepo.DeleteAsync(SelectedMaterial.Id);
        await LoadMaterials();
    }
}
