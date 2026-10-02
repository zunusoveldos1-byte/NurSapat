namespace NurSapat.App.Models.Stock;

public enum FinishedGoodState
{
    OnWarehouse,
    Shipped,
    QualityControl,
    Defect
}

public static class FinishedGoodStateExtensions
{
    public static string ToDisplayName(this FinishedGoodState state) => state switch
    {
        FinishedGoodState.OnWarehouse => "На складе",
        FinishedGoodState.Shipped => "Отгружен",
        FinishedGoodState.QualityControl => "Контроль ОТК",
        FinishedGoodState.Defect => "Брак",
        _ => state.ToString()
    };

    public static IReadOnlyList<FinishedGoodState> All { get; } =
        Enum.GetValues<FinishedGoodState>().ToArray();
}

public sealed class FinishedGoodStateOption
{
    public required FinishedGoodState Value { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public static IReadOnlyList<FinishedGoodStateOption> All { get; } =
        FinishedGoodStateExtensions.All
            .Select(state => new FinishedGoodStateOption
            {
                Value = state,
                DisplayName = state.ToDisplayName()
            })
            .ToArray();
}
