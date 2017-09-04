using System.Threading.Tasks;

namespace CacheUpFront.Services
{
    public interface ILocalCacheService<T> where T : Entity
    {
        Task LoadAndSubscribe();
    }
}
