using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Controls;

public sealed class CadSceneControl : Grid
{
    private readonly CadCanvasControl _canvas = new();
    private readonly RulerControl _horizontalRuler = new() { Orientation = RulerOrientation.Horizontal, Background = new SolidColorBrush(Color.Parse("#F8F9FA")), ClipToBounds = true, Margin = new Thickness(0, 0, 0, 0) };
    private readonly RulerControl _verticalRuler = new() { Orientation = RulerOrientation.Vertical, Background = new SolidColorBrush(Color.Parse("#F8F9FA")), ClipToBounds = true, Margin = new Thickness(0, 0, -8, 0) };
    private readonly Border _corner = new()
    {
        Background = new SolidColorBrush(Color.Parse("#F8F9FA")),
        BorderBrush = new SolidColorBrush(Color.Parse("#DCDCDC")),
        BorderThickness = new Thickness(0, 0, 1, 1)
    };

    public static readonly StyledProperty<CadEditorViewModel?> EditorProperty =
        AvaloniaProperty.Register<CadSceneControl, CadEditorViewModel?>(nameof(Editor));

    public CadEditorViewModel? Editor
    {
        get => GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public CadSceneControl()
    {
        Background = Brushes.White;
        RowDefinitions.Add(new RowDefinition(GridLength.Parse("30")));
        RowDefinitions.Add(new RowDefinition(GridLength.Star));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("30")));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        SetRow(_corner, 0);
        SetColumn(_corner, 0);
        SetRow(_horizontalRuler, 0);
        SetColumn(_horizontalRuler, 1);
        SetRow(_verticalRuler, 1);
        SetColumn(_verticalRuler, 0);
        SetRow(_canvas, 1);
        SetColumn(_canvas, 1);

        Children.Add(_corner);
        Children.Add(_horizontalRuler);
        Children.Add(_verticalRuler);
        Children.Add(_canvas);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == EditorProperty)
        {
            if (change.OldValue is CadEditorViewModel oldEditor)
            {
                oldEditor.SceneInvalidated -= OnSceneInvalidated;
            }

            if (change.NewValue is CadEditorViewModel newEditor)
            {
                newEditor.SceneInvalidated += OnSceneInvalidated;
            }

            _canvas.Editor = Editor;
            _horizontalRuler.Editor = Editor;
            _verticalRuler.Editor = Editor;
            InvalidateCadScene();
        }

        base.OnPropertyChanged(change);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);
        UpdateViewportSize();
        return arranged;
    }

    private void UpdateViewportSize()
    {
        if (Editor == null)
        {
            return;
        }

        var width = Math.Max(1, _canvas.Bounds.Width);
        var height = Math.Max(1, _canvas.Bounds.Height);
        Editor.SetViewportSize(width, height);
    }

    private void OnSceneInvalidated(object? sender, EventArgs e) => InvalidateCadScene();

    private void InvalidateCadScene()
    {
        _canvas.InvalidateVisual();
        _horizontalRuler.InvalidateVisual();
        _verticalRuler.InvalidateVisual();
    }
}
