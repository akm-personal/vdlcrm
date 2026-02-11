using Vdlcrm.Interfaces;

namespace Vdlcrm.Services;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly List<T> _entities = [];

    public async Task<T?> GetByIdAsync(int id)
    {
        await Task.Delay(0); // Simulate async operation
        return _entities.FirstOrDefault();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        await Task.Delay(0); // Simulate async operation
        return _entities.AsEnumerable();
    }

    public async Task<T> AddAsync(T entity)
    {
        await Task.Delay(0); // Simulate async operation
        _entities.Add(entity);
        return entity;
    }

    public async Task<T> UpdateAsync(T entity)
    {
        await Task.Delay(0); // Simulate async operation
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await Task.Delay(0); // Simulate async operation
        return true;
    }
}
