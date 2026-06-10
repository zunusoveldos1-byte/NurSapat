using NurSapat.Core.Models;

namespace NurSapat.Core.Interfaces;

public interface IMaterialRepository
{
    Task<IReadOnlyList<Material>> GetAllAsync();

    Task<Material?> GetByIdAsync(Guid id);

    Task AddAsync(Material material);

    Task UpdateAsync(Material material);

    Task DeleteAsync(Guid id);
}
