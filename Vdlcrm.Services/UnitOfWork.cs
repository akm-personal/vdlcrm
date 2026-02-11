using Vdlcrm.Interfaces;

namespace Vdlcrm.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.ContainsKey(type))
        {
            _repositories[type] = new Repository<T>();
        }
        return (IRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync()
    {
        await Task.Delay(0); // Simulate async save operation
        return 0;
    }

    public void Dispose()
    {
        _repositories.Clear();
    }
}
