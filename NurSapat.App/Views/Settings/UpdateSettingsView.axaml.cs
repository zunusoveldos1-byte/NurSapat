using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NurSapat.App.Views.Settings;

public partial class UpdateSettingsView : UserControl
{
    public UpdateSettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
