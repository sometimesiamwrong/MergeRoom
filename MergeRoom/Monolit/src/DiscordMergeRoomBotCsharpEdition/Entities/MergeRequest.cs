using DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities;
using DiscordMergeRoomBotCsharpEdition.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DiscordMergeRoomBotCsharpEdition.Entities
{
    [CollectionName("mergeRequests")]
    public class MergeRequest : BaseEntity
    {
        public MergeRequest(
            uint gitlabId,
            uint gitlabIId,
            uint projectId,
            string title,
            string description,
            string state,
            BsonDateTime gitlabCreatedAt,
            BsonDateTime gitlabUpdatedAt,
            uint authorId,
            List<uint> assigneeIds,
            List<uint> reviewerIds,
            bool draft,
            string mergeStatus,
            string detailedMergeStatus,
            string webUrl,
            bool hasConflicts)
            : base(gitlabCreatedAt, gitlabUpdatedAt)
        {
            GitlabId = gitlabId;
            GitlabIId = gitlabIId;
            ProjectId = projectId;
            Title = title;
            Description = description;
            State = state;
            GitlabCreatedAt = gitlabCreatedAt;
            GitlabUpdatedAt = gitlabUpdatedAt;
            AuthorId = authorId;
            AssigneeIds = assigneeIds;
            ReviewerIds = reviewerIds;
            Draft = draft;
            MergeStatus = mergeStatus;
            DetailedMergeStatus = detailedMergeStatus;
            WebUrl = webUrl;
            HasConflicts = hasConflicts;
        }

        public MergeRequest()
        {
        }

        /// <summary>
        /// Gitlab merge request Id.
        /// </summary>
        [BsonElement("id")]
        public uint GitlabId { get; set; }

        /// <summary>
        /// Gitlab merge request IId.
        /// </summary>
        [BsonElement("iid")]
        public uint GitlabIId { get; set; }

        /// <summary>
        /// Merge request project Id.
        /// </summary>
        [BsonElement("projectId")]
        public uint ProjectId { get; set; }

        /// <summary>
        /// Merge request title.
        /// </summary>
        [BsonElement("title")]
        public string Title { get; set; } = null!;

        /// <summary>
        /// Merge request description.
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Merge request state.
        /// </summary>
        [BsonElement("state")]
        public string State { get; set; } = null!;

        /// <summary>
        /// Merge request created at.
        /// </summary>
        [BsonElement("gitlabCreatedAt")]
        public BsonDateTime GitlabCreatedAt { get; set; } = null!;

        /// <summary>
        /// Merge request updated at.
        /// </summary>
        [BsonElement("gitlabUpdatedAt")]
        public BsonDateTime GitlabUpdatedAt { get; set; } = null!;

        /// <summary>
        /// Gitlab author Id.
        /// </summary>
        [BsonElement("authorId")]
        public uint AuthorId { get; set; }

        /// <summary>
        /// Merge request assignee Ids.
        /// </summary>
        [BsonElement("assigneeIds")]
        public List<uint> AssigneeIds { get; set; } = new List<uint>();

        /// <summary>
        /// Merge request reviewer Ids.
        /// </summary>
        [BsonElement("reviewerIds")]
        public List<uint> ReviewerIds { get; set; } = new List<uint>();

        /// <summary>
        /// Additional users Ids.
        /// </summary>
        [BsonElement("additionalUsers")]
        public List<uint> AdditionalUsers { get; set; } = new List<uint>();

        /// <summary>
        /// Merge request draft state.
        /// </summary>
        [BsonElement("draft")]
        public bool Draft { get; set; }

        /// <summary>
        /// Merge request merge status.
        /// </summary>
        [BsonElement("mergeStatus")]
        public string MergeStatus { get; set; } = null!;

        /// <summary>
        /// Merge request detailed merge status.
        /// </summary>
        [BsonElement("detailedMergeStatus")]
        public string DetailedMergeStatus { get; set; } = null!;

        /// <summary>
        /// Merge request web url.
        /// </summary>
        [BsonElement("webUrl")]
        public string WebUrl { get; set; } = null!;

        /// <summary>
        /// Discord channel Id.
        /// </summary>
        [BsonElement("channelId")]
        public ulong ChannelId { get; set; }

        /// <summary>
        /// Discord channel Id.
        /// </summary>
        [BsonElement("threadId")]
        public ulong ThreadId { get; set; }

        /// <summary>
        /// Merge request close state.
        /// </summary>
        [BsonElement("isClosed")]
        public bool IsClosed { get; set; }

        /// <summary>
        /// Merge request close state.
        /// </summary>
        [BsonElement("approves")]
        public ApproveInfo? ApprovesInfo { get; set; }

        /// <summary>
        /// Merge request close state.
        /// </summary>
        [BsonElement("hasConflicts")]
        public bool HasConflicts { get; set; }

        /// <summary>
        /// Hashes of Position for DiffNotes (Hash of Position, noteId)
        /// </summary>
        [BsonElement("diffNoteHashes")]
        public Dictionary<string, long> DiffNoteHashes { get; set; } = new Dictionary<string,long>();

        /// <summary>
        /// Head pipeline id
        /// </summary>
        [BsonIgnore]
        public uint? HeadPipeline { get; set; }
    }
}
