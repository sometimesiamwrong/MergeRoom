using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MergeRoom.Domain.Entities
{
    public class BaseEntity
    {
        protected BaseEntity(BsonDateTime createdAt, BsonDateTime updatedAt)
        {
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        protected BaseEntity()
        {
        }

        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement]
        public BsonDateTime CreatedAt { get; set; } = null!;

        [BsonElement]
        public BsonDateTime UpdatedAt { get; set; } = null!;
    }
}
