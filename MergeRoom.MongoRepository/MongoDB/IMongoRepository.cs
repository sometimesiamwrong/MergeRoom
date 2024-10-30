using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using MergeRoom.Domain;

namespace MergeRoom.MongoRepository.MongoDB
{
    public interface IMongoRepository
    {
        IMongoCollection<T> GetCollection<T>() where T : BaseEntity, new();

        Task<IEnumerable<T>> GetAllAsync<T>(Expression<Func<T, bool>>? expression = null) where T : BaseEntity, new();

        Task<T?> GetAsync<T>(Expression<Func<T, bool>> expression) where T : BaseEntity, new();

        Task<T?> GetAsync<T>(ObjectId id) where T : BaseEntity, new();

        Task AddAsync<T>(T entity) where T : BaseEntity, new();

        Task UpdateAsync<T>(T entity) where T : BaseEntity, new();

        Task DeleteAsync<T>(T entity) where T : BaseEntity, new();

        Task DeleteAsync<T>(ObjectId id) where T : BaseEntity, new();
    }
}
