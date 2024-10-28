using DiscordMergeRoomBotCsharpEdition.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DiscordMergeRoomBotCsharpEdition.Entities
{
    /// <summary>
    /// Project mongo-entity.
    /// </summary>
    [CollectionName("projects")]
    public class Project : BaseEntity
    {
        /// <summary>
        /// Discord guild ID.
        /// </summary>
        [BsonElement("guildId")]
        public ulong GuildId { get; set; }

        /// <summary>
        /// Gitlab project ID.
        /// </summary>
        [BsonElement("projectId")]
        public uint GitlabId { get; set; }

        /// <summary>
        /// Gitlab project link.
        /// </summary>
        [BsonElement("gitlabLink")]
        public string GitLabLink { get; set; } = null!;

        /// <summary>
        /// Discord project category name.
        /// </summary>
        [BsonElement("categoryDiscordName")]
        public string? CategoryDiscordName { get; set; }

        /// <summary>
        /// Discord project category ID.
        /// </summary>
        [BsonElement("categoryDiscordId")]
        public ulong? CategoryDiscordId { get; set; }

        /// <summary>
        /// Discord project channel name.
        /// </summary>
        [BsonElement("channelDiscordName")]
        public string ChannelDiscordName { get; set; } = null!;

        /// <summary>
        /// Discord project channel id.
        /// </summary>
        [BsonElement("channelDiscordId")]
        public ulong ChannelDiscordId { get; set; }

        /// <summary>
        /// Gitlab access token.
        /// </summary>
        [BsonElement("accessToken")]
        public string AccessToken { get; set; } = null!;

        /// <summary>
        /// Https:/{HOST}/NAMESPACE/PROJECT
        /// </summary>
        [BsonElement("host")]
        public string Host { get; set; } = null!;

        /// <summary>
        /// Https:/HOST/{NAMESPACE}/PROJECT
        /// </summary>
        [BsonElement("namespace")]
        public string Namespace { get; set; } = null!;

        /// <summary>
        /// Https:/HOST/NAMESPACE/{PROJECT_NAME}
        /// </summary>
        [BsonElement("projectName")]
        public string ProjectName { get; set; } = null!;

        /// <summary>
        /// Last time parse Gitlab project.
        /// </summary>
        [BsonElement("parsedAt")]
        public BsonDateTime ParsedAt { get; set; } = null!;

        /// <summary>
        /// Last time parse Gitlab project.
        /// </summary>
        [BsonElement("pusherKind")]
        public string PusherKind { get; set; } = null!;

        /// <summary>
        /// Flag for parsing default branches at failed jobs.
        /// </summary>
        [BsonElement("isNeedParseDefaultBranches")]
        public bool IsNeedParseDefaultBranches { get; set; }
    }
}
