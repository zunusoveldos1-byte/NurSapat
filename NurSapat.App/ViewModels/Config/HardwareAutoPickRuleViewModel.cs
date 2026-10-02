using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Config;

namespace NurSapat.App.ViewModels.Config;

public sealed class HardwareAutoPickRuleViewModel : ObservableObject
{
    private double _minWidthFalzMm;
    private double _maxWidthFalzMm;
    private double _minHeightFalzMm;
    private double _maxHeightFalzMm;
    private int _scissorsCount = 1;
    private int _middleLockCount;

    public HardwareAutoPickRuleViewModel(HardwareAutoPickRule source)
    {
        MinWidthFalzMm = source.MinWidthFalzMm;
        MaxWidthFalzMm = source.MaxWidthFalzMm;
        MinHeightFalzMm = source.MinHeightFalzMm;
        MaxHeightFalzMm = source.MaxHeightFalzMm;
        ScissorsCount = source.ScissorsCount;
        MiddleLockCount = source.MiddleLockCount;
    }

    public double MinWidthFalzMm
    {
        get => _minWidthFalzMm;
        set => SetProperty(ref _minWidthFalzMm, value);
    }

    public double MaxWidthFalzMm
    {
        get => _maxWidthFalzMm;
        set => SetProperty(ref _maxWidthFalzMm, value);
    }

    public double MinHeightFalzMm
    {
        get => _minHeightFalzMm;
        set => SetProperty(ref _minHeightFalzMm, value);
    }

    public double MaxHeightFalzMm
    {
        get => _maxHeightFalzMm;
        set => SetProperty(ref _maxHeightFalzMm, value);
    }

    public int ScissorsCount
    {
        get => _scissorsCount;
        set => SetProperty(ref _scissorsCount, value);
    }

    public int MiddleLockCount
    {
        get => _middleLockCount;
        set => SetProperty(ref _middleLockCount, value);
    }

    public HardwareAutoPickRule ToModel() => new()
    {
        MinWidthFalzMm = MinWidthFalzMm,
        MaxWidthFalzMm = MaxWidthFalzMm,
        MinHeightFalzMm = MinHeightFalzMm,
        MaxHeightFalzMm = MaxHeightFalzMm,
        ScissorsCount = ScissorsCount,
        MiddleLockCount = MiddleLockCount
    };
}
