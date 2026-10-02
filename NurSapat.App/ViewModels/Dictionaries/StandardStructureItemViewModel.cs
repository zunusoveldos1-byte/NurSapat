using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Structures;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class StandardStructureItemViewModel : ObservableObject
{
    public Guid Id { get; init; }

    public Guid GroupId { get; init; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _thumbnailPath = string.Empty;

    public bool HasThumbnail => !string.IsNullOrWhiteSpace(ThumbnailPath);

    partial void OnThumbnailPathChanged(string value) => OnPropertyChanged(nameof(HasThumbnail));

    public static StandardStructureItemViewModel FromModel(StandardStructureItem item) => new()
    {
        Id = item.Id,
        GroupId = item.GroupId,
        Name = item.Name,
        ThumbnailPath = item.ThumbnailPath
    };

    public void RefreshFrom(StandardStructureItem item)
    {
        Name = item.Name;
        ThumbnailPath = item.ThumbnailPath;
    }
}
