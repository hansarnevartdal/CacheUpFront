using CacheUpFront.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CacheUpFront.Services
{
    public interface ICentralizedCacheService<T> where T : IEntity
    {
        Task AddOrUpdate(T entity);
        Task AddOrUpdate(IList<T> entities);
        Task Remove(string id);
    }
}
