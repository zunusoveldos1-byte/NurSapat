using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Controls;

public enum HorizontalRulerPlacement
{
    Top,
    Bottom
}

public enum HorizontalTickEdge
{
    Top,
    Bottom
}

public sealed class CadSceneControl : Grid
{
    public const double VerticalRulerThickness = 30;

    private readonly Grid _canvasHost = new();
    private readonly CadCanvasControl _canvas = new();
    private readonly DimensionEditorOverlay _dimensionOverlay = new();
    private readonly RulerControl _horizontalRuler = new()
    {
        Orientation = RulerOrientation.Horizontal,
        Background = new SolidColorBrush(Color.Parse("#F8FAFC")),
        ClipToBounds = true,
        HorizontalTickEdge = HorizontalTickEdge.Bottom
    };
    private readonly RulerControl _verticalRuler = new()
    {
        Orientation = RulerOrientation.Vertical,
        Background = new SolidColorBrush(Color.Parse("#F8FAFC")),
        ClipToBounds = true
    };
    private readonly Border _corner = new()
    {
        Background = new SolidColorBrush(Color.Parse("#F8FAFC")),
        BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
        BorderThickness = new Thickness(0, 0, 1, 1)
    };

    public static readonly StyledProperty<CadEditorViewModel?> EditorProperty =
        AvaloniaProperty.Register<CadSceneControl, CadEditorViewModel?>(nameof(Editor));

    public static readonly StyledProperty<bool> ShowHorizontalRulerProperty =
        AvaloniaProperty.Register<CadSceneControl, bool>(nameof(ShowHorizontalRuler), true);

    public CadEditorViewModel? Editor
    {
        get => GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public bool ShowHorizontalRuler
    {
        get => GetValue(ShowHorizontalRulerProperty);
        set => SetValue(ShowHorizontalRulerProperty, value);
    }

    public CadCanvasControl Canvas => _canvas;

    public CadSceneControl()
    {
        Background = Brushes.White;
        _canvasHost.Children.Add(_canvas);
        _canvasHost.Children.Add(_dimensionOverlay);
        RebuildLayout();
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

            BindEditor();
            InvalidateCadScene();
        }
        else if (change.Property == ShowHorizontalRulerProperty)
        {
            RebuildLayout();
        }

        base.OnPropertyChanged(change);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);
        UpdateViewportSize();
        return arranged;
    }

    private void RebuildLayout()
    {
        Children.Clear();
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();

        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse($"{VerticalRulerThickness}")));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        if (!ShowHorizontalRuler)
        {
            RowDefinitions.Add(new RowDefinition(GridLength.Star));
            SetRow(_verticalRuler, 0);
            SetColumn(_verticalRuler, 0);
            SetRow(_canvasHost, 0);
            SetColumn(_canvasHost, 1);
            Children.Add(_verticalRuler);
            Children.Add(_canvasHost);
            BindEditor();
            return;
        }

        RowDefinitions.Add(new RowDefinition(GridLength.Parse($"{VerticalRulerThickness}")));
        RowDefinitions.Add(new RowDefinition(GridLength.Star));

        SetRow(_verticalRuler, 0);
        SetColumn(_verticalRuler, 0);
        SetRow(_canvasHost, 0);
        SetColumn(_canvasHost, 1);
        SetRow(_corner, 1);
        SetColumn(_corner, 0);
        SetRow(_horizontalRuler, 1);
        SetColumn(_horizontalRuler, 1);

        Children.Add(_corner);
        Children.Add(_horizontalRuler);
        Children.Add(_verticalRuler);
        Children.Add(_canvasHost);
        BindEditor();
    }

    private void BindEditor()
    {
        _canvas.Editor = Editor;
        _dimensionOverlay.Editor = Editor;
        _horizontalRuler.Editor = Editor;
        _verticalRuler.Editor = Editor;
    }

    private void UpdateViewportSize()
    {
        if (Editor == null)
        {
            return;
        }

        var width = Math.Max(1, _canvas.Bounds.Width);
        var height = Math.Max(1, _canvas.Bounds.Height);
        Editor.NotifyDrawingCanvasSized(width, height);
    }

    private void OnSceneInvalidated(object? sender, EventArgs e) => InvalidateCadScene();

    private void InvalidateCadScene()
    {
        _canvas.InvalidateVisual();
        _horizontalRuler.InvalidateVisual();
        _verticalRuler.InvalidateVisual();
    }
}
