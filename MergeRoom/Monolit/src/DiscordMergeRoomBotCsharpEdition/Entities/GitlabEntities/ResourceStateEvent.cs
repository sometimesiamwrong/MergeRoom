using MongoDB.Bson;

namespace DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities
{
    public class ResourceStateEvent
    {
        public ResourceStateEvent(BsonDateTime create, ulong authorId, string state)
        {
            CreatedAt = create;
            AuthorId = authorId;
            State = state;
        }

        /// <summary>
        /// Created at.
        /// </summary>
        public BsonDateTime CreatedAt { get; set; }

        /// <summary>
        /// Giltab user id.
        /// </summary>
        public ulong AuthorId { get; set; }

        /// <summary>
        /// Object state.
        /// </summary>
        public string State { get; set; }
    }
}
