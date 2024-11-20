using MergeRoom.MongoRepositoryr.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MergeRoom.Domain.Entities.GitlabEntities
{
    /// <summary>
    /// Entity for Gitlab note.
    /// </summary>
    public class Note : BaseEntity
    {
        public Note() { }

        public Note(
            ulong gitlabId,
            uint noteCreatorId,
            string description,
            string webUrl,
            BsonDateTime createAt,
            BsonDateTime updateAt,
            bool isSystem,
            string? type = null,
            string? additionalDescription = null,
            Object? data = null)
            : base(createAt, updateAt)
        {
            GitlabId = gitlabId;
            NoteCreatorId = noteCreatorId;
            AdditionalDescription = additionalDescription;
            Description = description;
            WebUrl = webUrl;
            Type = type;
            IsSystem = isSystem;
            Data = data;
        }

        /// <summary>
        /// Gitlab note Id.
        /// </summary>
        [BsonElement("gitlabId")]
        public ulong GitlabId { get; set; }

        /// <summary>
        /// Gitlab note data.
        /// </summary>
        [BsonIgnore]
        public Object? Data { get; set; }

        /// <summary>
        /// Gitlab user creator note.
        /// </summary>
        [BsonElement("noteCreator")]
        public uint NoteCreatorId { get; set; }

        /// <summary>
        /// Bot Additional description.
        /// </summary>
        [BsonIgnore]
        public string? AdditionalDescription { get; set; }

        /// <summary>
        /// Merge request note.
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gitlab note URL.
        /// </summary>
        [BsonElement("webUrl")]
        public string WebUrl { get; set; }

        /// <summary>
        /// Code changes area.
        /// </summary>
        [BsonIgnore]
        public string? CodeArea { get; set; }

        [BsonElement("type")]
        public string? Type { get; set; }

        [BsonElement("isSystem")]
        public bool IsSystem { get; set; }

        public NoteType NoteType { get; set; }
    }
}
