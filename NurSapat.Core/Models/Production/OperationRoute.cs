namespace NurSapat.Core.Models.Production;

public sealed class OperationRoute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public decimal LaborCostPerHour { get; set; }
    public decimal StandardMinutesPerUnit { get; set; }
}
