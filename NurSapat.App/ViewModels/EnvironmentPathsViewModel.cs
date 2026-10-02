using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Core.Settings;

namespace NurSapat.App.ViewModels;

public sealed partial class EnvironmentPathsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _dbPath;

    [ObservableProperty]
    private string _documentsPath;

    [ObservableProperty]
    private string _sketchesPath;

    [ObservableProperty]
    private string _logsPath;

    public EnvironmentPathsViewModel(EnvironmentSettings settings)
    {
        DbPath = settings.DbPath;
        DocumentsPath = settings.DocumentsPath;
        SketchesPath = settings.SketchesPath;
        LogsPath = settings.LogsPath;
    }
}
