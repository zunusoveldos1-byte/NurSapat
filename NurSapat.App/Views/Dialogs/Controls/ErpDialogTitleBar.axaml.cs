using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Dialogs.Controls;

public partial class ErpDialogTitleBar : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ErpDialogTitleBar, string>(nameof(Title));

    public static readonly StyledProperty<bool> IsWarningProperty =
        AvaloniaProperty.Register<ErpDialogTitleBar, bool>(nameof(IsWarning));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsWarning
    {
        get => GetValue(IsWarningProperty);
        set => SetValue(IsWarningProperty, value);
    }

    public event EventHandler? CloseRequested;

    public ErpDialogTitleBar()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsWarningProperty)
        {
            ApplyWarningStyle();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ApplyWarningStyle();
    }

    private void ApplyWarningStyle()
    {
        if (TitleBarBorder is null)
        {
            return;
        }

        if (IsWarning)
        {
            if (!TitleBarBorder.Classes.Contains("warning"))
            {
                TitleBarBorder.Classes.Add("warning");
            }
        }
        else
        {
            TitleBarBorder.Classes.Remove("warning");
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is Window window)
        {
            BorderlessChromeHelper.OnTitleBarPointerPressed(window, e);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);
}
