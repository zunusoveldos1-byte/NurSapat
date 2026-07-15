using NurSapat.App.Models.Materials;

namespace NurSapat.App.Services.Materials;

public interface IMaterialsDetailStore
{
    IReadOnlyList<MaterialDetail> GetAll();

    MaterialDetail? GetById(Guid id);

    MaterialDetail? GetByArticle(string article);

    void Add(MaterialDetail material);

    void Update(MaterialDetail material);

    void Delete(Guid id);
}

public sealed class MaterialsDetailStore : IMaterialsDetailStore
{
    private readonly List<MaterialDetail> _items = [];

    public IReadOnlyList<MaterialDetail> GetAll() => _items;

    public MaterialDetail? GetById(Guid id) => _items.FirstOrDefault(item => item.Id == id);

    public MaterialDetail? GetByArticle(string article) =>
        _items.FirstOrDefault(item => string.Equals(item.Article, article, StringComparison.OrdinalIgnoreCase));

    public void Add(MaterialDetail material) => _items.Add(material);

    public void Update(MaterialDetail material)
    {
        var index = _items.FindIndex(item => item.Id == material.Id);
        if (index >= 0)
        {
            _items[index] = material;
        }
    }

    public void Delete(Guid id) => _items.RemoveAll(item => item.Id == id);
}
