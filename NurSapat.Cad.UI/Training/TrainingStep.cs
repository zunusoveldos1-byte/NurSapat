namespace NurSapat.Cad.UI.Training;

public sealed record TrainingStep(DateTimeOffset Timestamp, string Tool, string Action, string Status)
{
    public string Display => $"[{Timestamp:HH:mm:ss}] | Инструмент: {Tool} | Действие: {Action} | Статус: {Status}";
}
