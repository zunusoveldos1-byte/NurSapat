using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.ViewModels;

/// <summary>
/// Обёртка над <see cref="OrderConstruction"/> для биндинга параметров конструкции на листе заказа.
/// </summary>
public sealed partial class ConstructionModel : ObservableObject
{
    private readonly OrderConstruction _construction;
    private CadEditorViewModel? _previewEditor;
    private CadEditorViewModel? _subscribedPreviewEditor;

    public ConstructionModel(OrderConstruction construction, CadEditorViewModel? previewEditor = null)
    {
        _construction = construction;
        _previewEditor = previewEditor;
        _construction.Cost.PropertyChanged += OnUnderlyingCostChanged;
    }

    public OrderConstruction Source => _construction;

    public Guid Id => _construction.Id;

    public string Name
    {
        get => _construction.Name;
        set
        {
            if (_construction.Name == value)
            {
                return;
            }

            _construction.Name = value;
            OnPropertyChanged();
        }
    }

    public int Quantity
    {
        get => Math.Max(1, _construction.Cost.Quantity);
        set
        {
            var normalized = Math.Max(1, value);
            if (_construction.Cost.Quantity == normalized)
            {
                return;
            }

            _construction.Cost.Quantity = normalized;
            OnPropertyChanged();
            NotifyAreaChanged();
        }
    }

    public string ProfileSystemName
    {
        get => _construction.ProfileSystemName;
        set => SetConstructionValue(
            _construction.ProfileSystemName,
            value,
            newValue => _construction.ProfileSystemName = newValue);
    }

    public Guid? SelectedMaterialId
    {
        get => _construction.SelectedMaterialId;
        set => SetConstructionValue(
            _construction.SelectedMaterialId,
            value,
            newValue => _construction.SelectedMaterialId = newValue);
    }

    public string MaterialColor
    {
        get => _construction.Appearance.ColorName;
        set => SetConstructionValue(
            _construction.Appearance.ColorName,
            value,
            newValue => _construction.Appearance.ColorName = newValue);
    }

    public string ContourColor
    {
        get => _construction.Appearance.ContourColorName;
        set => SetConstructionValue(
            _construction.Appearance.ContourColorName,
            value,
            newValue => _construction.Appearance.ContourColorName = newValue);
    }

    public string CustomerName
    {
        get => _construction.OrderDetails.CustomerName;
        set => SetConstructionValue(
            _construction.OrderDetails.CustomerName,
            value,
            newValue => _construction.OrderDetails.CustomerName = newValue);
    }

    public string CustomerPhone
    {
        get => _construction.OrderDetails.CustomerPhone;
        set => SetConstructionValue(
            _construction.OrderDetails.CustomerPhone,
            value,
            newValue => _construction.OrderDetails.CustomerPhone = newValue);
    }

    public string ObjectAddress
    {
        get => _construction.OrderDetails.SiteAddress;
        set => SetConstructionValue(
            _construction.OrderDetails.SiteAddress,
            value,
            newValue => _construction.OrderDetails.SiteAddress = newValue);
    }

    public DateTime OrderDate
    {
        get => _construction.OrderDetails.OrderDate;
        set => SetConstructionValue(
            _construction.OrderDetails.OrderDate,
            value,
            newValue => _construction.OrderDetails.OrderDate = newValue);
    }

    public double Width
    {
        get => (double)_construction.Geometry.OuterWidthMm;
        set
        {
            var normalized = Math.Max(0, value);
            var width = (decimal)normalized;
            if (_construction.Geometry.OuterWidthMm == width)
            {
                return;
            }

            _construction.Geometry.OuterWidthMm = width;
            OnPropertyChanged();
            NotifyAreaChanged();
        }
    }

    public double Height
    {
        get => (double)_construction.Geometry.OuterHeightMm;
        set
        {
            var normalized = Math.Max(0, value);
            var height = (decimal)normalized;
            if (_construction.Geometry.OuterHeightMm == height)
            {
                return;
            }

            _construction.Geometry.OuterHeightMm = height;
            OnPropertyChanged();
            NotifyAreaChanged();
        }
    }

    public bool HasArcTopProfile => _previewEditor?.HasArcTopProfile ?? false;

    public double ArcRadius
    {
        get => _previewEditor?.TopProfileArcRadius ?? 0;
        set
        {
            if (_previewEditor is null)
            {
                return;
            }

            _previewEditor.TopProfileArcRadius = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ArcHeight));
        }
    }

    public double ArcHeight
    {
        get => _previewEditor?.TopProfileArcHeight ?? 0;
        set
        {
            if (_previewEditor is null)
            {
                return;
            }

            _previewEditor.TopProfileArcHeight = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ArcRadius));
            OnPropertyChanged(nameof(HasArcTopProfile));
        }
    }

    public DateTimeOffset? ManufactureDate
    {
        get => _construction.ManufactureDate;
        set
        {
            if (_construction.ManufactureDate == value)
            {
                return;
            }

            _construction.ManufactureDate = value;
            OnPropertyChanged();
        }
    }

    public ConstructionReadinessStatus Status
    {
        get => _construction.Status;
        set
        {
            if (_construction.Status == value)
            {
                return;
            }

            _construction.Status = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? DeliveryDate
    {
        get => _construction.DeliveryDate;
        set
        {
            if (_construction.DeliveryDate == value)
            {
                return;
            }

            _construction.DeliveryDate = value;
            if (value is not null)
            {
                _construction.Status = ConstructionReadinessStatus.CompletedAndSold;
                OnPropertyChanged(nameof(Status));
            }

            OnPropertyChanged();
        }
    }

    public string Manager
    {
        get => _construction.Manager;
        set
        {
            if (_construction.Manager == value)
            {
                return;
            }

            _construction.Manager = value;
            OnPropertyChanged();
        }
    }

    public string Supervisor
    {
        get => _construction.Supervisor;
        set
        {
            if (_construction.Supervisor == value)
            {
                return;
            }

            _construction.Supervisor = value;
            OnPropertyChanged();
        }
    }

    public string Notes
    {
        get => _construction.Notes;
        set
        {
            if (_construction.Notes == value)
            {
                return;
            }

            _construction.Notes = value;
            OnPropertyChanged();
        }
    }

    public double Area => (Width * Height / 1_000_000.0) * Quantity;

    public string AreaDisplay => $"{Area:F2} м²";

    public void RefreshArcBindings()
    {
        OnPropertyChanged(nameof(HasArcTopProfile));
        OnPropertyChanged(nameof(ArcRadius));
        OnPropertyChanged(nameof(ArcHeight));
    }

    public object? PreviewCanvas => _previewEditor;

    public void SetPreviewEditor(CadEditorViewModel? previewEditor)
    {
        if (ReferenceEquals(_previewEditor, previewEditor))
        {
            return;
        }

        DetachPreviewEditorSubscription();
        _previewEditor = previewEditor;
        AttachPreviewEditorSubscription();
        OnPropertyChanged(nameof(PreviewCanvas));
        OnPropertyChanged(nameof(HasArcTopProfile));
        OnPropertyChanged(nameof(ArcRadius));
        OnPropertyChanged(nameof(ArcHeight));
    }

    public void Detach()
    {
        DetachPreviewEditorSubscription();
        _construction.Cost.PropertyChanged -= OnUnderlyingCostChanged;
    }

    private void SetConstructionValue<T>(
        T current,
        T value,
        Action<T> apply,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return;
        }

        apply(value);
        OnPropertyChanged(propertyName);
    }

    private void AttachPreviewEditorSubscription()
    {
        if (_previewEditor is null || ReferenceEquals(_subscribedPreviewEditor, _previewEditor))
        {
            return;
        }

        _previewEditor.PropertyChanged += OnPreviewEditorPropertyChanged;
        _subscribedPreviewEditor = _previewEditor;
    }

    private void DetachPreviewEditorSubscription()
    {
        if (_subscribedPreviewEditor is null)
        {
            return;
        }

        _subscribedPreviewEditor.PropertyChanged -= OnPreviewEditorPropertyChanged;
        _subscribedPreviewEditor = null;
    }

    private void OnPreviewEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CadEditorViewModel.HasArcTopProfile)
            or nameof(CadEditorViewModel.TopProfileArcRadius)
            or nameof(CadEditorViewModel.TopProfileArcHeight))
        {
            OnPropertyChanged(nameof(HasArcTopProfile));
            OnPropertyChanged(nameof(ArcRadius));
            OnPropertyChanged(nameof(ArcHeight));
        }
    }

    private void OnUnderlyingCostChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(_construction.Cost.Quantity))
        {
            OnPropertyChanged(nameof(Quantity));
            NotifyAreaChanged();
        }
    }

    private void NotifyAreaChanged()
    {
        OnPropertyChanged(nameof(Area));
        OnPropertyChanged(nameof(AreaDisplay));
    }
}
