using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Services;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.ViewModels;

public sealed class OrderConstructionLookupItemViewModel : ObservableObject, IDisposable
{
    private const double CardWidthValue = 230;
    private const double CardHeightValue = 360;
    private const double CardInnerPadding = 24;
    private const double PreviewWidth = CardWidthValue - CardInnerPadding;
    private const double PreviewHeight = 260;
    private readonly Order? _order;

    public OrderConstructionLookupItemViewModel(
        OrderConstruction construction,
        bool isActive,
        Order? order = null)
    {
        Construction = construction;
        IsActive = isActive;
        _order = order;

        PreviewEditor = new CadEditorViewModel();
        ConstructionDrawingStore.ApplyPreview(PreviewEditor, construction);
        if (_order is not null)
        {
            _order.Payment.PropertyChanged += OnPaymentChanged;
        }
    }

    public OrderConstruction Construction { get; }

    public CadEditorViewModel PreviewEditor { get; }

    public IReadOnlyList<GeometryElement> DrawingElements => Construction.Geometry.Elements;

    public Guid Id => Construction.Id;

    public string Name => Construction.Name;

    public string DimensionsDisplay => Construction.DimensionsDisplay;

    public string QuantityDisplay => Construction.QuantityDisplay;

    public bool IsActive { get; }

    public ConstructionReadinessStatus EffectiveStatus
    {
        get
        {
            if (Construction.Status == ConstructionReadinessStatus.CancelledOrAbandoned ||
                _order?.Status == OrderStatus.Cancelled)
            {
                return ConstructionReadinessStatus.CancelledOrAbandoned;
            }

            if (Construction.Status == ConstructionReadinessStatus.CompletedAndSold ||
                Construction.DeliveryDate is not null ||
                _order?.Status == OrderStatus.Shipped)
            {
                return ConstructionReadinessStatus.CompletedAndSold;
            }

            if (Construction.Status == ConstructionReadinessStatus.ReadyAndPaid)
            {
                return ConstructionReadinessStatus.ReadyAndPaid;
            }

            var isMade = Construction.ManufactureDate is not null ||
                         _order?.Status is OrderStatus.Assembled or OrderStatus.ReadyForShipment;
            var isPaid = _order is null ||
                         (_order.Payment.AgreedAmount > 0 && _order.Payment.Debt <= 0);
            return isMade && isPaid
                ? ConstructionReadinessStatus.ReadyAndPaid
                : ConstructionReadinessStatus.RequiresPaymentOrSaved;
        }
    }

    public string StatusBackground => EffectiveStatus switch
    {
        ConstructionReadinessStatus.CompletedAndSold => "#2563EB",
        ConstructionReadinessStatus.ReadyAndPaid => "#16A34A",
        ConstructionReadinessStatus.CancelledOrAbandoned => "#DC2626",
        _ => "#CA8A04"
    };

    public string StatusIcon => EffectiveStatus switch
    {
        ConstructionReadinessStatus.CompletedAndSold => "✓",
        ConstructionReadinessStatus.ReadyAndPaid => "✓",
        ConstructionReadinessStatus.CancelledOrAbandoned => "×",
        _ => "!"
    };

    public string StatusTitle => EffectiveStatus switch
    {
        ConstructionReadinessStatus.CompletedAndSold => "Завершен / Продан",
        ConstructionReadinessStatus.ReadyAndPaid =>
            "Сделан / Оплачен (Ожидает доставки)",
        ConstructionReadinessStatus.CancelledOrAbandoned => "Отменен / Заброшен",
        _ => "Требует оплаты / Сохранен"
    };

    public string StatusToolTip => EffectiveStatus switch
    {
        ConstructionReadinessStatus.CompletedAndSold =>
            $"{StatusTitle}\nКонструкция полностью сделана, оплачена и доставлена клиенту.",
        ConstructionReadinessStatus.ReadyAndPaid =>
            $"{StatusTitle}\nИзделие готово и оплачено. Нажмите, чтобы подтвердить доставку.",
        ConstructionReadinessStatus.CancelledOrAbandoned =>
            $"{StatusTitle}\nЗаказ был отменен или помещен в архив.",
        _ =>
            $"{StatusTitle}\nКонструкция сформирована, но не оплачена или ожидает сохранения и оплаты."
    };

    public bool CanConfirmDelivery =>
        EffectiveStatus == ConstructionReadinessStatus.ReadyAndPaid;

    public int ColumnSpan => 1;

    public double CardWidth => CardWidthValue;

    public double CardHeight => CardHeightValue;

    public double PreviewZoneWidth => PreviewWidth;

    public double PreviewZoneHeight => PreviewHeight;

    public int GridRow { get; private set; }

    public int GridColumn { get; private set; }

    public void SetGridPosition(int row, int column)
    {
        GridRow = row;
        GridColumn = column;
    }

    public void ConfirmDelivery(DateTimeOffset deliveredAtUtc)
    {
        if (!CanConfirmDelivery)
        {
            return;
        }

        Construction.Status = ConstructionReadinessStatus.CompletedAndSold;
        Construction.DeliveryDate = deliveredAtUtc;
        NotifyStatusChanged();
    }

    public void Dispose()
    {
        if (_order is not null)
        {
            _order.Payment.PropertyChanged -= OnPaymentChanged;
        }
    }

    private void OnPaymentChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OrderPayment.AgreedAmount)
            or nameof(OrderPayment.PaidAmount)
            or nameof(OrderPayment.Debt))
        {
            NotifyStatusChanged();
        }
    }

    private void NotifyStatusChanged()
    {
        OnPropertyChanged(nameof(EffectiveStatus));
        OnPropertyChanged(nameof(StatusBackground));
        OnPropertyChanged(nameof(StatusIcon));
        OnPropertyChanged(nameof(StatusTitle));
        OnPropertyChanged(nameof(StatusToolTip));
        OnPropertyChanged(nameof(CanConfirmDelivery));
    }
}
