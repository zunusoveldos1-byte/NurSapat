namespace NurSapat.App.Models.Stock;

public sealed class FinishedGoodItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OrderNumber { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string CellAddress { get; set; } = string.Empty;

    public FinishedGoodState State { get; set; } = FinishedGoodState.OnWarehouse;

    public DateTime DateIncoming { get; set; } = DateTime.Today;

    public string UserName { get; set; } = string.Empty;

    public FinishedGoodItem Clone() => new()
    {
        Id = Id,
        OrderNumber = OrderNumber,
        ProductCode = ProductCode,
        CellAddress = CellAddress,
        State = State,
        DateIncoming = DateIncoming,
        UserName = UserName
    };
}
