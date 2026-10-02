using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Stock;

namespace NurSapat.App.ViewModels.Stock;

public sealed partial class StockDateRangeDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Период отчёта";

    [ObservableProperty]
    private DateTime _period1Start = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime _period1End = DateTime.Today;

    [ObservableProperty]
    private DateTime _period2Start = DateTime.Today.AddMonths(-2);

    [ObservableProperty]
    private DateTime _period2End = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private bool _isDualPeriod;

    public event Action<StockDateRangeResult?>? RequestClose;

    public void Configure(string title, StockDateDialogMode mode)
    {
        Title = title;
        IsDualPeriod = mode == StockDateDialogMode.DualPeriod;
    }

    [RelayCommand]
    private void Confirm()
    {
        if (Period1End.Date < Period1Start.Date)
        {
            return;
        }

        if (IsDualPeriod && Period2End.Date < Period2Start.Date)
        {
            return;
        }

        RequestClose?.Invoke(new StockDateRangeResult(
            Period1Start.Date,
            Period1End.Date,
            IsDualPeriod ? Period2Start.Date : null,
            IsDualPeriod ? Period2End.Date : null));
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(null);
}
