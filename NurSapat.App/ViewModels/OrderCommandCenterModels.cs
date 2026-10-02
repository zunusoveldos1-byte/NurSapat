using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels;

public enum OrderTimelineState
{
    Completed,
    Active,
    Pending
}

public enum OrderHistoryCategory
{
    Change,
    Payment,
    System,
    Document,
    Stage,
    Comment
}

public enum OrderCheckStatus
{
    Ok,
    Warning,
    Error
}

public sealed partial class OrderAutomationToggle : ObservableObject
{
    public OrderAutomationToggle(string title, bool isEnabled = true)
    {
        Title = title;
        IsEnabled = isEnabled;
    }

    public string Title { get; }

    [ObservableProperty] private bool _isEnabled;
}

public sealed partial class OrderTemplateImpactItem : ObservableObject
{
    public OrderTemplateImpactItem(string title, bool isAffected = true)
    {
        Title = title;
        IsAffected = isAffected;
    }

    public string Title { get; }

    [ObservableProperty] private bool _isAffected;
}

public sealed partial class OrderProfileField : ObservableObject
{
    public OrderProfileField(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    [ObservableProperty] private string _value;
}

public sealed partial class OrderStageCard : ObservableObject
{
    public OrderStageCard(int number, string title, string shortTitle)
    {
        Number = number;
        Title = title;
        ShortTitle = shortTitle;
    }

    public int Number { get; }

    public string ShortTitle { get; }

    [ObservableProperty] private string _title;

    [ObservableProperty] private string _status = "Не начато";

    [ObservableProperty] private DateTime? _startDate;

    [ObservableProperty] private DateTime? _endDate;

    [ObservableProperty] private string _note = "";

    [ObservableProperty] private string _responsible = "—";

    [ObservableProperty] private string _comment = "";

    [ObservableProperty] private bool _isCompleted;

    [ObservableProperty] private bool _isOverdue;

    [ObservableProperty] private bool _isSelected;

    [ObservableProperty] private OrderTimelineState _timelineState = OrderTimelineState.Pending;

    public ObservableCollection<string> AttachedFiles { get; } = [];

    public ObservableCollection<string> AutomationActions { get; } = [];

    public string StartDateText => StartDate?.ToString("dd.MM.yyyy") ?? "—";

    public string EndDateText => EndDate?.ToString("dd.MM.yyyy") ?? "—";

    public string TimelineIcon => TimelineState switch
    {
        OrderTimelineState.Completed => "✓",
        OrderTimelineState.Active => "●",
        _ => "○"
    };

    public string TimelineForeground => TimelineState switch
    {
        OrderTimelineState.Completed => "#27AE60",
        OrderTimelineState.Active => "#F2994A",
        _ => "#CBD5E1"
    };

    public string TimelineLineBrush => TimelineState switch
    {
        OrderTimelineState.Completed => "#27AE60",
        OrderTimelineState.Active => "#2F80ED",
        _ => "#E2E8F0"
    };

    public string StatusForeground => Status switch
    {
        "В работе" => "#2F80ED",
        "Выполнено" => "#27AE60",
        "Ожидание" => "#F2994A",
        "Просрочено" => "#EB5757",
        _ => "#757575"
    };

    public string StatusBackground => Status switch
    {
        "В работе" => "#EAF2FD",
        "Выполнено" => "#E8F7EF",
        "Ожидание" => "#FEF3E8",
        "Просрочено" => "#FDECEA",
        _ => "#F2F2F2"
    };

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(StatusForeground));
        OnPropertyChanged(nameof(StatusBackground));
        UpdateTimelineState();
    }

    partial void OnIsCompletedChanged(bool value)
    {
        if (value)
        {
            Status = "Выполнено";
            EndDate ??= DateTime.Today;
        }

        UpdateTimelineState();
        OnPropertyChanged(nameof(EndDateText));
    }

    partial void OnStartDateChanged(DateTime? value) => OnPropertyChanged(nameof(StartDateText));

    partial void OnEndDateChanged(DateTime? value) => OnPropertyChanged(nameof(EndDateText));

    private void UpdateTimelineState()
    {
        TimelineState = IsCompleted || Status == "Выполнено"
            ? OrderTimelineState.Completed
            : Status is "В работе" or "Ожидание" or "Просрочено"
                ? OrderTimelineState.Active
                : OrderTimelineState.Pending;

        OnPropertyChanged(nameof(TimelineIcon));
        OnPropertyChanged(nameof(TimelineForeground));
        OnPropertyChanged(nameof(TimelineLineBrush));
    }
}

public sealed partial class OrderHistoryEvent : ObservableObject
{
    public OrderHistoryEvent(
        DateTime timestamp,
        string title,
        string details,
        string user,
        OrderHistoryCategory category)
    {
        Timestamp = timestamp;
        Title = title;
        Details = details;
        User = user;
        Category = category;
    }

    public DateTime Timestamp { get; }

    public string Title { get; }

    public string Details { get; }

    public string User { get; }

    public OrderHistoryCategory Category { get; }

    public string TimeText => Timestamp.ToString("HH:mm");

    public string CategoryLabel => Category switch
    {
        OrderHistoryCategory.Change => "Изменение",
        OrderHistoryCategory.Payment => "Оплата",
        OrderHistoryCategory.System => "Система",
        OrderHistoryCategory.Document => "Документ",
        OrderHistoryCategory.Stage => "Этап",
        _ => "Комментарий"
    };

    public string CategoryBackground => Category switch
    {
        OrderHistoryCategory.Change => "#EAF2FD",
        OrderHistoryCategory.Payment => "#E8F7EF",
        OrderHistoryCategory.System => "#F1F5F9",
        OrderHistoryCategory.Document => "#FEF3E8",
        OrderHistoryCategory.Stage => "#EDE9FE",
        _ => "#F8FAFC"
    };

    public string CategoryForeground => Category switch
    {
        OrderHistoryCategory.Change => "#2F80ED",
        OrderHistoryCategory.Payment => "#27AE60",
        OrderHistoryCategory.System => "#64748B",
        OrderHistoryCategory.Document => "#B45309",
        OrderHistoryCategory.Stage => "#7C3AED",
        _ => "#475569"
    };
}

public sealed partial class OrderFileCard : ObservableObject
{
    public OrderFileCard(string name, string extension, string sizeText, string category)
    {
        Name = name;
        Extension = extension;
        SizeText = sizeText;
        Category = category;
    }

    public string Name { get; }

    public string Extension { get; }

    public string SizeText { get; }

    public string Category { get; }

    public string TypeLabel => Extension.ToUpperInvariant();

    public string TypeBackground => Extension.ToUpperInvariant() switch
    {
        "PDF" => "#FDECEA",
        "DWG" or "DXF" => "#EAF2FD",
        "JPG" or "PNG" => "#E8F7EF",
        _ => "#F1F5F9"
    };

    public string TypeForeground => Extension.ToUpperInvariant() switch
    {
        "PDF" => "#EB5757",
        "DWG" or "DXF" => "#2F80ED",
        "JPG" or "PNG" => "#27AE60",
        _ => "#64748B"
    };
}

public sealed partial class OrderCommentMessage : ObservableObject
{
    public OrderCommentMessage(string author, string message, DateTime timestamp, bool isSystem = false)
    {
        Author = author;
        Message = message;
        Timestamp = timestamp;
        IsSystem = isSystem;
    }

    public string Author { get; }

    [ObservableProperty] private string _message;

    public DateTime Timestamp { get; }

    public bool IsSystem { get; }

    public string TimeText => Timestamp.ToString("dd.MM.yyyy HH:mm");

    public string Initials => string.IsNullOrWhiteSpace(Author)
        ? "?"
        : string.Concat(Author.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => part[0])
            .Select(ch => char.ToUpperInvariant(ch)));

    public string AvatarBackground => IsSystem ? "#E2E8F0" : "#EAF2FD";

    public string AvatarForeground => IsSystem ? "#64748B" : "#2F80ED";
}

public sealed partial class OrderReminderItem : ObservableObject
{
    public OrderReminderItem(string title, string dueText, bool isCompleted = false)
    {
        Title = title;
        DueText = dueText;
        IsCompleted = isCompleted;
    }

    public string Title { get; }

    public string DueText { get; }

    [ObservableProperty] private bool _isCompleted;
}

public sealed partial class OrderChecklistItem : ObservableObject
{
    public OrderChecklistItem(string title, OrderCheckStatus status, string details = "")
    {
        Title = title;
        Status = status;
        Details = details;
    }

    public string Title { get; }

    public string Details { get; }

    [ObservableProperty] private OrderCheckStatus _status;

    public string StatusIcon => Status switch
    {
        OrderCheckStatus.Ok => "✔",
        OrderCheckStatus.Warning => "⚠",
        _ => "❌"
    };

    public string StatusForeground => Status switch
    {
        OrderCheckStatus.Ok => "#27AE60",
        OrderCheckStatus.Warning => "#F2994A",
        _ => "#EB5757"
    };
}

public sealed partial class CurrencyRateDisplayModel : ObservableObject
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    [ObservableProperty] private decimal _rate;

    [ObservableProperty] private DateTime _rateDate;

    [ObservableProperty] private string _actionType = string.Empty;

    [ObservableProperty] private decimal _orderAmount;

    public string RateText => $"{Rate:0.####}";

    public string OrderAmountText => $"{OrderAmount:N0}";

    public string PeriodStartText => RateDate.ToString("dd.MM.yyyy");

    partial void OnRateChanged(decimal value)
    {
        OnPropertyChanged(nameof(RateText));
        OnPropertyChanged(nameof(OrderAmountText));
    }

    partial void OnOrderAmountChanged(decimal value) => OnPropertyChanged(nameof(OrderAmountText));
}

public sealed record OrderTemplateOption(string Title, double Width, double Height);
