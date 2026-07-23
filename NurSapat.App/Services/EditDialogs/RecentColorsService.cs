namespace NurSapat.App.Services.EditDialogs;

public interface IRecentColorsService
{
    IReadOnlyList<string> GetRecentColors();

    void Remember(string colorHex, int maxItems = 8);
}

public sealed class RecentColorsService : IRecentColorsService
{
    private readonly List<string> _recent = [];

    public IReadOnlyList<string> GetRecentColors() => _recent.AsReadOnly();

    public void Remember(string colorHex, int maxItems = 8)
    {
        var normalized = NurSapat.App.Models.EditDialogs.StandardColorCatalog.NormalizeHex(colorHex);
        _recent.RemoveAll(c => string.Equals(c, normalized, StringComparison.OrdinalIgnoreCase));
        _recent.Insert(0, normalized);

        while (_recent.Count > maxItems)
        {
            _recent.RemoveAt(_recent.Count - 1);
        }
    }
}
