using System.Collections.Generic;

namespace CacheUpFront
{
    public interface IEntityCache<T> : IDictionary<string, T> where T : Entity
    {
        void AddOrUpdate(T entity);
        new void Remove(string id);
    }
}
