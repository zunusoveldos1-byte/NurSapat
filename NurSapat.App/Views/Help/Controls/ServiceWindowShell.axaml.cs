using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Help.Controls;

public partial class ServiceWindowShell : UserControl
{
    public static readonly StyledProperty<string> WindowTitleProperty =
        AvaloniaProperty.Register<ServiceWindowShell, string>(nameof(WindowTitle), "NurSapat");

    public static readonly StyledProperty<object?> BodyContentProperty =
        AvaloniaProperty.Register<ServiceWindowShell, object?>(nameof(BodyContent));

    public static readonly StyledProperty<object?> FooterContentProperty =
        AvaloniaProperty.Register<ServiceWindowShell, object?>(nameof(FooterContent));

    public static readonly StyledProperty<bool> ShowFooterProperty =
        AvaloniaProperty.Register<ServiceWindowShell, bool>(nameof(ShowFooter), true);

    private ContentPresenter? _bodyPresenter;
    private ContentPresenter? _footerPresenter;

    public ServiceWindowShell()
    {
        InitializeComponent();
        ResolvePresenters();
        SyncPresenters();
    }

    public string WindowTitle
    {
        get => GetValue(WindowTitleProperty);
        set => SetValue(WindowTitleProperty, value);
    }

    public object? BodyContent
    {
        get => GetValue(BodyContentProperty);
        set => SetValue(BodyContentProperty, value);
    }

    public object? FooterContent
    {
        get => GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }

    public bool ShowFooter
    {
        get => GetValue(ShowFooterProperty);
        set => SetValue(ShowFooterProperty, value);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BodyContentProperty)
        {
            UpdateBodyContent(change.NewValue);
        }
        else if (change.Property == FooterContentProperty)
        {
            UpdateFooterContent(change.NewValue);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ResolvePresenters();
        SyncPresenters();
    }

    private void ResolvePresenters()
    {
        _bodyPresenter ??= this.FindControl<ContentPresenter>("BodyPresenter");
        _footerPresenter ??= this.FindControl<ContentPresenter>("FooterPresenter");
    }

    private void SyncPresenters()
    {
        UpdateBodyContent(BodyContent);
        UpdateFooterContent(FooterContent);
    }

    private void UpdateBodyContent(object? content)
    {
        ResolvePresenters();
        if (_bodyPresenter is null)
        {
            return;
        }

        _bodyPresenter.Content = content;
    }

    private void UpdateFooterContent(object? content)
    {
        ResolvePresenters();
        if (_footerPresenter is null)
        {
            return;
        }

        _footerPresenter.Content = content;
    }

    private Window? GetHostWindow() => TopLevel.GetTopLevel(this) as Window;

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var window = GetHostWindow();
        if (window is not null)
        {
            BorderlessChromeHelper.OnTitleBarPointerPressed(window, e);
        }
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        var window = GetHostWindow();
        if (window is not null)
        {
            BorderlessChromeHelper.Minimize(window);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        var window = GetHostWindow();
        if (window is not null)
        {
            BorderlessChromeHelper.Close(window);
        }
    }
}
