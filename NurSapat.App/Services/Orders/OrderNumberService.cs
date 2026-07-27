namespace NurSapat.App.Services.Orders;

public interface IOrderNumberService
{
    string AllocateNext();

    string PeekNext();
}

/// <summary>Последовательные номера заказа: 000001, 000002, …</summary>
public sealed class OrderNumberService : IOrderNumberService
{
    private readonly object _sync = new();
    private int _next = 1;

    public string AllocateNext()
    {
        lock (_sync)
        {
            var value = _next++;
            return value.ToString("D6");
        }
    }

    public string PeekNext()
    {
        lock (_sync)
        {
            return _next.ToString("D6");
        }
    }

    public void EnsureAtLeast(int number)
    {
        lock (_sync)
        {
            if (number >= _next)
            {
                _next = number + 1;
            }
        }
    }
}
