using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MergeRoom.Domain.Entities
{
    public class BaseEntity
    {
        public required long Id { get; set; }

        public required DateTimeOffset CreatedAt { get; set; }

        public required DateTimeOffset UpdatedAt { get; set; }
    }
}
