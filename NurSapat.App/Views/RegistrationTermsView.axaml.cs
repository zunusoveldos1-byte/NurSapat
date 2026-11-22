using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace NurSapat.App.Views;

public partial class RegistrationTermsView : UserControl
{
    private static readonly Uri TermsResource = new("avares://NurSapat.App/Assets/Legal/TermsOfUse.ru.txt");

    public RegistrationTermsView()
    {
        InitializeComponent();
    }

    public event EventHandler? DismissRequested;

    internal void ShowTerms()
    {
        if (TermsText.Text is null)
        {
            using var reader = new StreamReader(AssetLoader.Open(TermsResource));
            TermsText.Text = reader.ReadToEnd().TrimEnd();
        }

        IsVisible = true;
        TermsScroll.Offset = default;
        TermsScroll.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            DismissRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) =>
        DismissRequested?.Invoke(this, EventArgs.Empty);
}
