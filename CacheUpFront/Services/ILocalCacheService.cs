using CacheUpFront.Models;
using System.Threading.Tasks;

namespace CacheUpFront.Services
{
    public interface ILocalCacheService<T> where T : IEntity
    {
        Task LoadAndSubscribe();
    }
}
