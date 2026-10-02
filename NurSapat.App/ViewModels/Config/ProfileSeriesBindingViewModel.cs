using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.App.ViewModels.Config;

public sealed class ProfileSeriesBindingViewModel : ObservableObject
{
    private bool _isLinked;

    public ProfileSeriesBindingViewModel(Guid seriesId, string name, bool isLinked)
    {
        SeriesId = seriesId;
        Name = name;
        _isLinked = isLinked;
    }

    public Guid SeriesId { get; }

    public string Name { get; }

    public bool IsLinked
    {
        get => _isLinked;
        set => SetProperty(ref _isLinked, value);
    }
}
