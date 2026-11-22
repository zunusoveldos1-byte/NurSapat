using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.View;

namespace NurSapat.App.ViewModels;

public sealed partial class MessageItemViewModel : ObservableObject
{
    public MessageItemViewModel(AppMessage message)
    {
        Id = message.Id;
        Severity = message.Severity;
        Text = message.Text;
        TimeDisplay = message.TimeDisplay;
        MarkerBrush = CreateMarkerBrush(message.Severity);
    }

    public Guid Id { get; }

    public AppMessageSeverity Severity { get; }

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _timeDisplay = string.Empty;

    [ObservableProperty]
    private IBrush _markerBrush = Brushes.Gray;

    private static IBrush CreateMarkerBrush(AppMessageSeverity severity) => severity switch
    {
        AppMessageSeverity.Error => new SolidColorBrush(Color.Parse("#DC2626")),
        AppMessageSeverity.Warning => new SolidColorBrush(Color.Parse("#F97316")),
        _ => new SolidColorBrush(Color.Parse("#16A34A"))
    };
}
