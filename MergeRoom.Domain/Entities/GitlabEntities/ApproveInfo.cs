using MongoDB.Bson.Serialization.Attributes;

namespace MergeRoom.Domain.Entities.GitlabEntities
{
    public class ApproveInfo
    {
        public ApproveInfo(List<uint>? userIds)
        {
            UserIds = userIds;
        }

        public ApproveInfo()
        {
        }

        [BsonElement("userIds")]
        public List<uint> UserIds { get; set; } = new List<uint>();
    }
}
