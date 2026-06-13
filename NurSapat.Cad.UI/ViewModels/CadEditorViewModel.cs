using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.Core.Commands;
using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Core.Models.Orders;
using NurSapat.Cad.Core.Snapping;
using NurSapat.Cad.Core.Viewport;
using NurSapat.Cad.Tools;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.ObjectModel;
using NurSapat.Cad.UI.Dialogs;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel : ObservableObject
{
    private readonly CadToolFactory _toolFactory = new();

    public CadDocument Document { get; } = new();

    public CadViewport Viewport { get; } = new();

    public CadSnappingEngine Snapping { get; } = new();

    public CadCommandManager Commands { get; }

    public event EventHandler? SceneInvalidated;

    /// <summary>
    /// Обработчик клика по холсту для режимов проектирования (импост, створка и т.д.).
    /// Возвращает true, если клик обработан и стандартный инструмент не должен срабатывать.
    /// </summary>
  public Func<CadPoint, bool>? DesignPlacement { get; set; }

  public IReadOnlyList<GeometryElement> DesignElements { get; private set; } = [];

  public void SetDesignElements(IReadOnlyList<GeometryElement> elements)
  {
      DesignElements = elements;
      RequestSceneRefresh();
  }

  public Action<DesignPlacementCommit>? OnDesignPlacementCommit { get; set; }

  public string? ActiveDesignToolId { get; set; }

  public string? ActiveDesignToolLabel { get; set; }

  public ObservableCollection<CadToolButtonViewModel> Tools { get; } = new(
        new CadToolFactory().Tools.Select(t => new CadToolButtonViewModel(t.Id, t.DisplayName)));

    [ObservableProperty]
    private string _activeToolId = "select";

    public IRelayCommand<string> ActivateToolCommand { get; }

    [ObservableProperty]
    private ICadTool _activeTool;

    [ObservableProperty]
    private string _statusText = "Готово";

    [ObservableProperty]
    private double _cursorX;

    [ObservableProperty]
    private double _cursorY;

    [ObservableProperty]
    private bool _isGridEnabled = true;

    [ObservableProperty]
    private bool _isSnapEnabled = true;

    [ObservableProperty]
    private CadRect? _selectedRectangle;

    // ============ ЗАГЛУШКИ ДЛЯ ПАНЕЛЕЙ ============
    [ObservableProperty]
    private bool _isLeftPanelVisible = true;

    [ObservableProperty]
    private bool _isRightPanelVisible = true;

    [ObservableProperty]
    private bool _isBottomPanelVisible = true;

    [ObservableProperty]
    private bool _isTopPanelVisible = false;

    public IRelayCommand ToggleLeftPanelCommand => new RelayCommand(() => IsLeftPanelVisible = !IsLeftPanelVisible);
    public IRelayCommand ToggleRightPanelCommand => new RelayCommand(() => IsRightPanelVisible = !IsRightPanelVisible);
    public IRelayCommand ToggleBottomPanelCommand => new RelayCommand(() => IsBottomPanelVisible = !IsBottomPanelVisible);
    public IRelayCommand ToggleTopPanelCommand => new RelayCommand(() => IsTopPanelVisible = !IsTopPanelVisible);
    // ==============================================

    public CadEditorViewModel()
    {
        Commands = new CadCommandManager(Document);
        ActivateToolCommand = new RelayCommand<string>(ActivateTool);
        ActiveTool = _toolFactory.Create("rectangle");
        FitDocumentToViewport();
    }

    public void SetViewportSize(double width, double height)
    {
        if (Math.Abs(Viewport.Width - Math.Max(1, width)) < CadMath.Epsilon &&
            Math.Abs(Viewport.Height - Math.Max(1, height)) < CadMath.Epsilon)
        {
            return;
        }

        Viewport.Resize(width, height);
        RequestSceneRefresh();
    }

    public void RequestSceneRefresh() => SceneInvalidated?.Invoke(this, EventArgs.Empty);

    public CadRect GetWorldBounds()
    {
        var rects = Document.Rectangles;
        if (rects.Count == 0) return new CadRect(-200, 200, 400, 400);

        var minX = rects.Min(r => r.X);
        var maxX = rects.Max(r => r.X + r.Width);
        var minY = rects.Min(r => r.Y - r.Height);
        var maxY = rects.Max(r => r.Y);

        return new CadRect(minX - 100, maxY + 100, maxX - minX + 200, maxY - minY + 200);
    }

    public void FitDocumentToViewport()
    {
        Viewport.FitToBounds(GetWorldBounds());
        OnPropertyChanged(nameof(ZoomPercent));
        RequestSceneRefresh();
    }

    public CadToolContext CreateToolContext()
    {
        return new CadToolContext(Document, Commands, Snapping)
        {
            Viewport = Viewport,
            Invalidate = () => RequestSceneRefresh(),
            InvokeSizeInput = point => _ = HandleSizeInputAsync(point),
            CommitPlacement = commit => OnDesignPlacementCommit?.Invoke(commit),
            ActiveDesignToolId = ActiveDesignToolId,
            ActiveDesignToolLabel = ActiveDesignToolLabel,
            SnapPoint = SnapWorldPoint
        };
    }

    private CadPoint SnapWorldPoint(CadPoint point)
    {
        if (!IsSnapEnabled)
        {
            return point;
        }

        return Snapping.Snap(point, BuildSnapLines()).Point;
    }

    private IEnumerable<CadLine> BuildSnapLines()
    {
        foreach (var rect in Document.Rectangles)
        {
            var topLeft = new CadPoint(rect.X, rect.Y);
            var topRight = new CadPoint(rect.X + rect.Width, rect.Y);
            var bottomLeft = new CadPoint(rect.X, rect.Y - rect.Height);
            var bottomRight = new CadPoint(rect.X + rect.Width, rect.Y - rect.Height);
            yield return new CadLine(topLeft, topRight);
            yield return new CadLine(topRight, bottomRight);
            yield return new CadLine(bottomRight, bottomLeft);
            yield return new CadLine(bottomLeft, topLeft);
        }
    }

    private async Task HandleSizeInputAsync(CadPoint point)
    {
        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;

        var window = new SizeInputWindow();
        window.SetInitialValues(1000, 1000);

        var result = await window.ShowDialog<bool?>(mainWindow);
        if (result == true)
        {
            var rect = new CadRect(
                point.X - window.ResultWidth / 2,
                point.Y + window.ResultHeight / 2,
                window.ResultWidth,
                window.ResultHeight
            );
            var command = new CadActionCommand(
                name: "Добавить прямоугольник",
                execute: doc => doc.AddRectangle(rect),
                undo: doc => doc.RemoveRectangle(rect)
            );
            Commands.Execute(command);
            RequestSceneRefresh();
        }
    }

    public void RefreshSelection()
    {
        OnPropertyChanged(nameof(SelectedRectangle));
    }

    public string ZoomPercent => $"{Viewport.ZoomFactor * 100:0}%";

    public string ActiveToolDisplayName => ActiveTool.DisplayName;

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedRectangle != null)
        {
            Document.RemoveRectangle(SelectedRectangle.Value);
            SelectedRectangle = null;
            RefreshSelection();
            RequestSceneRefresh();
            StatusText = "Выбранный объект удален";
        }
    }

    [RelayCommand]
    private void Undo()
    {
        Commands.Undo();
        RefreshSelection();
        RequestSceneRefresh();
        StatusText = "Отменить";
    }

    [RelayCommand]
    private void Redo()
    {
        Commands.Redo();
        RefreshSelection();
        RequestSceneRefresh();
        StatusText = "Повторить";
    }

    [RelayCommand]
    private void FitAll()
    {
        FitDocumentToViewport();
        StatusText = "Показать всё";
    }

    [RelayCommand]
    private async Task ClearAll()
    {
        if (!await ConfirmClearAsync()) return;

        Document.ClearRectangles();
        SelectedRectangle = null;
        RefreshSelection();
        FitDocumentToViewport();
        StatusText = "Чертеж очищен";
    }

    private void ActivateTool(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        ActiveTool = _toolFactory.Create(id);
        ActiveToolId = id;
        OnPropertyChanged(nameof(ActiveToolDisplayName));
        StatusText = ActiveTool.DisplayName;
    }

    public void UpdateCursor(CadPoint point)
    {
        CursorX = point.X;
        CursorY = point.Y;
        OnPropertyChanged(nameof(ZoomPercent));
    }

    private static async Task<bool> ConfirmClearAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not Window owner)
        {
            return true;
        }

        var dialog = new Window
        {
            Title = "Очистить чертеж",
            Width = 460,
            Height = 210,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var title = new TextBlock
        {
            Text = "Вы уверены, что хотите очистить чертеж?",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };
        var message = new TextBlock
        {
            Text = "Все несохраненные изменения будут потеряны.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#5F6368"))
        };
        var buttons = CreateDialogButtons(dialog);
        Grid.SetRow(message, 1);
        Grid.SetRow(buttons, 2);

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };
        grid.Children.Add(title);
        grid.Children.Add(message);
        grid.Children.Add(buttons);
        dialog.Content = new Border
        {
            Padding = new Avalonia.Thickness(22),
            Background = Avalonia.Media.Brushes.White,
            Child = grid
        };

        return await dialog.ShowDialog<bool>(owner);

        static Panel CreateDialogButtons(Window dialog)
        {
            var cancel = new Button
            {
                Content = "Отмена",
                Width = 110,
                Height = 34,
                Margin = new Avalonia.Thickness(0, 0, 8, 0)
            };
            var clear = new Button
            {
                Content = "Очистить",
                Width = 120,
                Height = 34,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D93025")),
                Foreground = Avalonia.Media.Brushes.White
            };
            cancel.Click += (_, _) => dialog.Close(false);
            clear.Click += (_, _) => dialog.Close(true);

            var panel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };
            panel.Children.Add(cancel);
            panel.Children.Add(clear);
            return panel;
        }
    }

    private void SetActiveToolState(string activeId)
    {
        foreach (var tool in Tools)
            tool.IsActive = tool.Id == activeId;
    }
}