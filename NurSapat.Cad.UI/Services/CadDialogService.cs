using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using NurSapat.Cad.UI.Dialogs;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Calculations;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

public sealed class CadDialogService : ICadDialogService
{
    public async Task<SizeInputResult?> ShowSizeInputAsync(
        double initialWidth,
        double initialHeight,
        double initialX = 0,
        double initialY = 0)
    {
        if (!TryGetOwner(out var owner))
        {
            return null;
        }

        var viewModel = new SizeInputViewModel(initialWidth, initialHeight, initialX, initialY);
        var window = new SizeInputWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return new SizeInputResult(
            viewModel.ResultWidth,
            viewModel.ResultHeight,
            viewModel.ResultX,
            viewModel.ResultY);
    }

    public async Task<NewConstructionSizeInputResult?> ShowNewConstructionSizeInputAsync(NewConstructionOptions options)
    {
        if (!TryGetOwner(out var owner))
        {
            return null;
        }

        var viewModel = new SizeInputViewModel(options);
        var window = new SizeInputWindow
        {
            DataContext = viewModel,
            Title = "Новая коробка"
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return new NewConstructionSizeInputResult(
            viewModel.ResultWidth,
            viewModel.ResultHeight,
            viewModel.ResultX,
            viewModel.ResultY,
            viewModel.ResultAnchor);
    }

    public async Task<ImpostCountDialogResult?> ShowImpostCountAsync(
        Guid targetCellId,
        int initialCount = 1)
    {
        if (!TryGetOwner(out var owner))
        {
            return null;
        }

        var viewModel = new ImpostCountViewModel(targetCellId, initialCount);
        var window = new ImpostCountWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return new ImpostCountDialogResult(
            viewModel.TargetCellId,
            viewModel.ResultCount);
    }

    public async Task<DecorativeSpokeDialogResult?> ShowDecorativeSpokeDialogAsync(bool isArchedGlazing)
    {
        if (!TryGetOwner(out var owner))
        {
            return null;
        }

        var viewModel = new DecorativeSpokeDialogViewModel(isArchedGlazing);
        var window = new DecorativeSpokeDialogWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return new DecorativeSpokeDialogResult(
            viewModel.ResultSpokeType,
            viewModel.ResultThickness,
            viewModel.ResultColor,
            viewModel.ResultRowsCount,
            viewModel.ResultColumnsCount,
            viewModel.ResultRayCount);
    }

    public async Task<SillDialogResult?> ShowSillDialogAsync(
        double frameWidthMm,
        SillElement? existing = null)
    {
        if (!TryGetOwner(out var owner))
        {
            return null;
        }

        var viewModel = new SillDialogViewModel(frameWidthMm, existing);
        var window = new SillDialogWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return new SillDialogResult(
            viewModel.ResultThicknessMm,
            viewModel.ResultWidthMm,
            viewModel.ResultLeftOverhangMm,
            viewModel.ResultRightOverhangMm,
            viewModel.ResultMaterialId,
            viewModel.ResultColor);
    }

    public async Task<CadDrawingTemplate?> ShowTemplatePickerAsync(CadTemplateStore store, CadEditorViewModel editor)
    {
        if (!TryGetOwner(out var owner))
        {
            return null;
        }

        var viewModel = new TemplatePickerViewModel(store, editor);
        var window = new TemplatePickerWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return viewModel.AppliedTemplate;
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        if (!TryGetOwner(out var owner))
        {
            return false;
        }

        var viewModel = new ConfirmDialogViewModel(message);
        var window = new ConfirmDialogWindow
        {
            Title = title,
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        return await window.ShowDialog<bool?>(owner) == true;
    }

    public async Task<VertexCoordinateDialogResult?> ShowVertexCoordinateDialogAsync(
        double initialX,
        double initialY,
        string? title = null)
    {
        if (!TryGetOwner(out var owner))
        {
            return null;
        }

        var viewModel = new VertexCoordinateDialogViewModel(initialX, initialY, title);
        var window = new VertexCoordinateDialogWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return new VertexCoordinateDialogResult(viewModel.ResultX, viewModel.ResultY);
    }

    public async Task<ObjectPropertiesDialogResult?> ShowObjectPropertiesDialogAsync(ObjectPropertiesDialogModel model)
    {
        if (!TryGetOwner(out var owner))
        {
            return null;
        }

        var viewModel = new ObjectPropertiesDialogViewModel(model);
        var window = new ObjectPropertiesDialogWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);

        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return viewModel.Result;
    }

    public async Task ShowConstructionDetailsDialogAsync(
        ConstructionVolumeSummary summary,
        IEnumerable<GeometryElement> elements)
    {
        if (!TryGetOwner(out var owner))
        {
            return;
        }

        var viewModel = new ConstructionDetailsDialogViewModel(summary, elements);
        var window = new ConstructionDetailsDialogWindow
        {
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, _) => window.Close(true);
        await window.ShowDialog<bool?>(owner);
    }

    public async Task<double?> ShowRadiusInputAsync(double initialRadius)
    {
        if (!TryGetOwner(out var owner))
        {
            return null;
        }

        var viewModel = new RadiusInputViewModel(initialRadius);
        var window = new RadiusInputWindow { DataContext = viewModel };
        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);
        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return viewModel.ResultRadius;
    }

    public async Task<string?> ShowTemplateNameInputAsync(string initialName)
    {
        if (!TryGetOwner(out var owner))
        {
            return null;
        }

        var viewModel = new TemplateNameViewModel(initialName);
        var window = new TemplateNameWindow { DataContext = viewModel };
        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);
        if (await window.ShowDialog<bool?>(owner) != true)
        {
            return null;
        }

        return viewModel.ResultName;
    }

    private static bool TryGetOwner(out Window owner)
    {
        owner = null!;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return false;
        }

        owner = desktop.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            ?? desktop.MainWindow!;
        return owner is not null;
    }
}
