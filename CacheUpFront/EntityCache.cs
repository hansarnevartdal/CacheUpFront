using CacheUpFront.Models;
using System.Collections.Concurrent;

namespace CacheUpFront
{
    public class EntityCache<TEntity> : ConcurrentDictionary<string, TEntity>, IEntityCache<TEntity> where TEntity : Entity
    {
        public void AddOrUpdate(TEntity entity)
        {
            if (entity == null)
                return;

            AddOrUpdate(entity.Id, entity, (key, oldvalue) => entity);
        }

        public void Remove(string id)
        {
            TEntity entity;
            TryRemove(id, out entity);
        }
    }
}
