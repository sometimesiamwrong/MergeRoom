using DiscordMergeRoomBotCsharpEdition.MongoDB;
using MongoDB.Bson.Serialization.Attributes;

namespace DiscordMergeRoomBotCsharpEdition.Entities
{
    /// <summary>
    /// Gitlab entity.
    /// </summary>
    /// <remarks>Get by http to gitlab. Use to connect GitlabUser and DiscordUser</remarks>
    [CollectionName("users")]
    public class User : BaseEntity
    {
        public User(
            uint gitlabId,
            string name,
            string username,
            string webUrl,
            ulong? discordId,
            string avatarUrl,
            uint projectId)
        {
            GitlabId = gitlabId;
            Username = username;
            Name = name;
            WebUrl = webUrl;
            DiscordId = discordId;
            AvatarUrl = avatarUrl;
            GitlabProjectId = projectId;
        }

        public User()
        {
        }

        /// <summary>
        /// Gitlab name.
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gitlab username.
        /// </summary>
        [BsonElement("username")]
        public string Username { get; set; } = null!;

        /// <summary>
        /// Gitlab avatar URL.
        /// </summary>
        [BsonElement("avatarUrl")]
        public string AvatarUrl { get; set; } = null!;

        /// <summary>
        /// Link to Gitlab profile.
        /// </summary>
        [BsonElement("webUrl")]
        public string WebUrl { get; set; } = null!;

        /// <summary>
        /// Discord Id.
        /// </summary>
        [BsonElement("discordId")]
        public ulong? DiscordId { get; set; }

        /// <summary>
        /// Gitlab Id.
        /// </summary>
        [BsonElement("GitlabId")]
        public uint GitlabId { get; set; }

        /// <summary>
        /// Gitlab Id.
        /// </summary>
        [BsonElement("GitlabProjectId")]
        public uint GitlabProjectId { get; set; }
    }
}
