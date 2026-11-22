using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Win32;
using NurSapat.App.ViewModels;
using NurSapat.Infrastructure.Sync.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace NurSapat.App.Views;

public partial class MainWindow : Window
{
    private const string PreferencesKey = @"HKEY_CURRENT_USER\Software\NurSapat";
    private const string LoginThemeValue = "LoginTheme";
    private const double PhotoMinimumAspect = 0.77;
    private const double PhotoMaximumAspect = 1.6;
    private const double PhotoWidthFraction = 0.6;
    private const uint SpiGetClientAreaAnimation = 0x1042;

    private readonly CancellationTokenSource _closed = new();
    private readonly double _minimumWindowHeight;
    private bool? _preferDarkTheme;
    private IPlatformSettings? _platformSettings;
    private MainWindowViewModel? _viewModel;
    private Button? _termsLink;
    private bool _allowMotion;

    public MainWindow()
    {
        InitializeComponent();
        RegistrationTerms.DismissRequested += (_, _) => CloseRegistrationTerms();
        _preferDarkTheme = ReadThemePreference();
        ApplyTheme();

        if (Screens.Primary is { } screen)
        {
            var availableWidth = screen.WorkingArea.Width / screen.Scaling;
            var availableHeight = screen.WorkingArea.Height / screen.Scaling;
            MinWidth = Math.Min(MinWidth, availableWidth);
            MinHeight = Math.Min(MinHeight, availableHeight);
            Width = Math.Min(Width, availableWidth);
            Height = Math.Min(Height, availableHeight);
        }
        _minimumWindowHeight = MinHeight;
        UpdatePanelLayout(Width, Height);
        SizeChanged += (_, e) => UpdatePanelLayout(e.NewSize.Width, e.NewSize.Height);
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.NavigateToLogin();
    }

    internal void ShowRegistrationTerms(Button link)
    {
        _termsLink = link;
        RegistrationTerms.ShowTerms();
        AuthLayout.IsEnabled = false;
    }

    private void CloseRegistrationTerms()
    {
        RegistrationTerms.IsVisible = false;
        AuthLayout.IsEnabled = true;
        _termsLink?.Focus();
        _termsLink = null;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _platformSettings = Application.Current?.PlatformSettings;
        if (_platformSettings is not null)
        {
            _platformSettings.ColorValuesChanged += OnPlatformColorsChanged;
        }

        AuthPages.PageTransition = null;
        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            AuthPages.Content = _viewModel.CurrentView;
        }
        AuthPages.TransitionCompleted += OnFormTransitionCompleted;
        ApplyMotionPreference();
        if (_allowMotion)
        {
            try
            {
                await AuthFormTransition.OpenAsync(AuthPages, _closed.Token);
            }
            catch (OperationCanceledException) when (_closed.IsCancellationRequested)
            {
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_platformSettings is not null)
        {
            _platformSettings.ColorValuesChanged -= OnPlatformColorsChanged;
        }
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        AuthPages.TransitionCompleted -= OnFormTransitionCompleted;
        _closed.Cancel();
        _closed.Dispose();
        base.OnClosed(e);
    }

    private void UpdatePanelLayout(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // Keep the hiker and the airplane in frame as UniformToFill crops the square asset.
        var photoWidth = width * PhotoWidthFraction;
        MaxHeight = Math.Max(_minimumWindowHeight, Math.Floor(photoWidth / PhotoMinimumAspect));
        MinHeight = Math.Max(_minimumWindowHeight, Math.Ceiling(photoWidth / PhotoMaximumAspect));
        Classes.Set("narrow-auth", width < 1000);
        Classes.Set("compact-auth", height < 650);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentView) && _viewModel is not null)
        {
            if (RegistrationTerms.IsVisible)
            {
                CloseRegistrationTerms();
            }
            AuthPages.IsTransitionReversed = _viewModel.CurrentView is LoginViewModel;
            AuthPages.Content = _viewModel.CurrentView;
        }
    }

    private void OnFormTransitionCompleted(object? sender, TransitionCompletedEventArgs e)
    {
        var form = AuthPages.Content;
        Dispatcher.UIThread.Post(() =>
        {
            if (IsActive && ReferenceEquals(form, AuthPages.Content) && !AuthPages.IsKeyboardFocusWithin)
            {
                AuthPages.GetVisualDescendants().OfType<TextBox>()
                    .FirstOrDefault(field => field.IsEffectivelyVisible && field.IsEffectivelyEnabled)?.Focus();
            }
        }, DispatcherPriority.Loaded);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    private void OnResizePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { Tag: WindowEdge edge } &&
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginResizeDrag(edge, e);
            e.Handled = true;
        }
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnThemeClick(object? sender, RoutedEventArgs e)
    {
        _preferDarkTheme = ActualThemeVariant != ThemeVariant.Dark;
        ApplyTheme();
        try
        {
            Registry.SetValue(PreferencesKey, LoginThemeValue, _preferDarkTheme.Value ? "Dark" : "Light");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            StartupTrace.Error("Не удалось сохранить тему окна входа.", ex);
        }
    }

    private void OnPlatformColorsChanged(object? sender, PlatformColorValues e) =>
        Dispatcher.UIThread.Post(ApplyTheme);

    private void ApplyTheme()
    {
        var dark = _preferDarkTheme
            ?? (Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark);
        RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        ThemeButton.Classes.Set("dark", dark);
        var label = dark ? "Включить светлую тему" : "Включить тёмную тему";
        ToolTip.SetTip(ThemeButton, label);
        AutomationProperties.SetName(ThemeButton, label);
    }

    private void ApplyMotionPreference()
    {
        // Match Windows Accessibility > Visual effects > Animation effects.
        _allowMotion = !OperatingSystem.IsWindows()
            || !SystemParametersInfo(SpiGetClientAreaAnimation, 0, out var enabled, 0)
            || enabled;
        Classes.Set("auth-motion", _allowMotion);
        AuthPages.PageTransition = _allowMotion ? new AuthFormTransition(_closed.Token) : null;
    }

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(
        uint action, uint parameter, [MarshalAs(UnmanagedType.Bool)] out bool value, uint flags);

    private static bool? ReadThemePreference()
    {
        try
        {
            return Registry.GetValue(PreferencesKey, LoginThemeValue, null) switch
            {
                "Dark" => true,
                "Light" => false,
                _ => null
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            StartupTrace.Error("Не удалось прочитать тему окна входа.", ex);
            return null;
        }
    }
}
