using MergeRoom.MongoRepositoryr.MongoDB;
using MongoDB.Bson.Serialization.Attributes;

namespace MergeRoom.Domain.Entities.GitlabEntities
{
    /// <summary>
    /// Entity for Gitlab DiffNote.
    /// </summary>
    [CollectionName("notes")]
    public class DiffNote : Note
    {
        public DiffNote(Note note)
        {
            GitlabId = note.GitlabId;
            NoteCreatorId = note.NoteCreatorId;
            AdditionalDescription = note.AdditionalDescription;
            Description = note.Description;
            WebUrl = note.WebUrl;
            Type = note.Type;
            IsSystem = note.IsSystem;
            Data = note.Data;
            NoteType = NoteType.Diff;
            CreatedAt = note.CreatedAt;
            UpdatedAt = note.UpdatedAt;
        }

        /// <summary>
        /// Position Data if DiffNote
        /// </summary>
        [BsonElement("position")]
        public Position Position { get; set; }

        /// <summary>
        /// Code changes area.
        /// </summary>
        [BsonIgnore]
        public string CodeArea { get; set; }

        /// <summary>
        /// Comment text from DiffNote
        /// </summary>
        [BsonIgnore]
        public (string? oldText, string? newText) CommentTexts { get; set; }
    }
}
