using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Controls;

/// <summary>
/// Плавающий TextBox поверх холста для редактирования размерной надписи in-place.
/// </summary>
public sealed class DimensionEditorOverlay : Canvas
{
    private readonly TextBox _textBox;
    private DimensionHitInfo? _activeHit;

    public CadEditorViewModel? Editor
    {
        get => _editor;
        set
        {
            if (_editor == value)
            {
                return;
            }

            if (_editor != null)
            {
                _editor.PropertyChanged -= OnEditorPropertyChanged;
            }

            _editor = value;

            if (_editor != null)
            {
                _editor.PropertyChanged += OnEditorPropertyChanged;
            }
        }
    }

    private CadEditorViewModel? _editor;

    public DimensionEditorOverlay()
    {
        Background = Brushes.Transparent;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        IsHitTestVisible = false;

        _textBox = new TextBox
        {
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#3B82F6")),
            Background = Brushes.White,
            FontSize = GeometryElementRenderer.DimensionLabelFontSize,
            Padding = new Thickness(12, 4),
            TextAlignment = TextAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinWidth = 44,
            MinHeight = 18,
            IsHitTestVisible = true
        };
        _textBox.LostFocus += OnTextBoxLostFocus;
        _textBox.KeyDown += OnTextBoxKeyDown;
        Children.Add(_textBox);
        IsVisible = false;
    }

    protected override Size MeasureOverride(Size availableSize) => availableSize;

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            var width = double.IsNaN(child.Width) ? child.DesiredSize.Width : child.Width;
            var height = double.IsNaN(child.Height) ? child.DesiredSize.Height : child.Height;
            child.Arrange(new Rect(new Point(Canvas.GetLeft(child), Canvas.GetTop(child)), new Size(width, height)));
        }

        return finalSize;
    }

    public void OpenAt(DimensionHitInfo hit)
    {
        _activeHit = hit;
        var textBounds = hit.TextBounds.Width > 0 && hit.TextBounds.Height > 0
            ? hit.TextBounds
            : hit.ScreenRect;

        var displayText = hit.ValueMm.ToString("0");
        _textBox.FontSize = GeometryElementRenderer.DimensionLabelFontSize;
        _textBox.Text = displayText;

        var (textWidth, textHeight) = GeometryElementRenderer.MeasureDimensionLabelText(displayText);
        const double horizontalPadding = 28;
        const double verticalPadding = 8;
        var editorWidth = Math.Max(textWidth + horizontalPadding, _textBox.MinWidth);
        var editorHeight = Math.Max(textHeight + verticalPadding, _textBox.MinHeight);
        _textBox.Width = editorWidth;
        _textBox.Height = editorHeight;

        var centerX = textBounds.X + textBounds.Width / 2;
        var centerY = textBounds.Y + textBounds.Height / 2;
        Canvas.SetLeft(_textBox, centerX - editorWidth / 2);
        Canvas.SetTop(_textBox, centerY - editorHeight / 2);

        IsVisible = true;
        _textBox.IsHitTestVisible = true;
        _textBox.Focus();
        _textBox.SelectAll();
        InvalidateArrange();
    }

    public void Close()
    {
        _activeHit = null;
        IsVisible = false;
        _textBox.IsHitTestVisible = false;
    }

    private void OnEditorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CadEditorViewModel.ActiveDimensionHit))
        {
            if (_editor?.ActiveDimensionHit is { } hit)
            {
                OpenAt(hit);
            }
            else
            {
                Close();
            }
        }
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                if (_activeHit is null || _editor?.ActiveDimensionHit is null)
                {
                    return;
                }

                Commit();
            },
            DispatcherPriority.Background);
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Commit();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _editor?.CancelDimensionEdit();
            e.Handled = true;
        }
    }

    private void Commit()
    {
        if (_editor == null || _activeHit is null)
        {
            return;
        }

        _editor.TryCommitDimensionEdit(_textBox.Text);
    }
}
