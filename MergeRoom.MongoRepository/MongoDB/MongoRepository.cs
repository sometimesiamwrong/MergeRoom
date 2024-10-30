using MergeRoom.MongoRepository.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MergeRoom.MongoRepositoryr.MongoDB
{
    public class MongoRepository : IMongoRepository
    {
        private readonly IMongoDatabase _database;

        public MongoRepository(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }


        public IMongoCollection<T> GetCollection<T>() where T : BaseEntity, new()
        {
            return _database.GetCollection<T>(GetCollectionName(typeof(T)));
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>(Expression<Func<T, bool>>? expression = null) where T : BaseEntity, new()
        {
            if (expression is null)
            {
                expression = x => true;
            }

            return await GetCollection<T>().AsQueryable().Where(expression).ToListAsync();
        }

        public async Task<T?> GetAsync<T>(Expression<Func<T, bool>> expression) where T : BaseEntity, new()
        {
            return await GetCollection<T>().AsQueryable().Where(expression).FirstOrDefaultAsync();
        }

        public async Task<T?> GetAsync<T>(ObjectId id) where T : BaseEntity, new()
        {
            return await GetCollection<T>().Find(GetFilterById<T>(id)).FirstOrDefaultAsync();
        }

        public async Task AddAsync<T>(T entity) where T : BaseEntity, new()
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await GetCollection<T>().InsertOneAsync(entity);
        }

        public async Task UpdateAsync<T>(T entity) where T : BaseEntity, new()
        {
            entity.UpdatedAt = DateTime.UtcNow;
            await GetCollection<T>().ReplaceOneAsync(GetFilterByEntity(entity), entity);
        }

        public async Task DeleteAsync<T>(T entity) where T : BaseEntity, new()
        {
            await GetCollection<T>().DeleteOneAsync(GetFilterByEntity(entity));
        }

        public async Task DeleteAsync<T>(ObjectId id) where T : BaseEntity, new()
        {
            await GetCollection<T>().DeleteOneAsync(GetFilterById<T>(id));
        }

        public IClientSessionHandle StartSession()
        {
            return _database.Client.StartSession();
        }

        private string GetCollectionName(Type type)
        {
            var collectionAttribute = type.GetCustomAttribute<CollectionNameAttribute>();
            if (collectionAttribute == null)
            {
                throw new InvalidOperationException($"No collection name defined for {type.Name}");
            }

            return collectionAttribute.Name;
        }

        public async Task ExecuteInTransactionAsync(Func<IClientSessionHandle, Task> transactionalOperations)
        {
            using (var session = StartSession())
            {
                session.StartTransaction();
                try
                {
                    await transactionalOperations(session);
                    await session.CommitTransactionAsync();
                }
                catch (Exception)
                {
                    await session.AbortTransactionAsync();
                    throw;
                }
            }
        }

        private FilterDefinition<T> GetFilterByEntity<T>(T entity) where T : BaseEntity, new()
        {
            return Builders<T>.Filter.Eq(e => e.Id, entity.Id);
        }

        private FilterDefinition<T> GetFilterById<T>(ObjectId id) where T : BaseEntity, new()
        {
            return Builders<T>.Filter.Eq(e => e.Id, id);
        }
    }
}
