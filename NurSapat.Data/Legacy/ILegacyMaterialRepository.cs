namespace NurSapat.Data.Legacy;

public interface ILegacyMaterialRepository
{
    Task<IReadOnlyList<LegacyMaterial>> GetAllAsync();

    Task<LegacyMaterial?> GetByIdAsync(Guid id);

    Task AddAsync(LegacyMaterial material);

    Task UpdateAsync(LegacyMaterial material);

    Task DeleteAsync(Guid id);
}
