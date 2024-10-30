using MongoDB.Bson;

namespace MergeRoom.Domain.Entities.GitlabEntities
{
    public class Job : BaseEntity
    {
        public Job(
            ulong gitLabId,
            uint? mergeRequestIId,
            uint authorId,
            string @ref,
            string stage,
            string status,
            string name,
            string webUrl,
            string pipelineWebUrl,
            bool allowFailure,
            BsonDateTime createdAt,
            BsonDateTime? finishedAt)
            : base(createdAt, null!)
        {
            GitLabId = gitLabId;
            MergeRequestIId = mergeRequestIId;
            AuthorId = authorId;
            Ref = @ref;
            Stage = stage;
            Status = status;
            Name = name;
            WebUrl = webUrl;
            PipelineWebUrl = pipelineWebUrl;
            AllowFailure = allowFailure;
            FinishedAt = finishedAt;
        }

        public ulong GitLabId { get; set; }

        public uint? MergeRequestIId { get; set; }

        public uint AuthorId { get; set; }

        public string Ref { get; set; }

        public string Stage { get; set; }

        public string Status { get; set; }

        public string Name { get; set; }

        public string WebUrl { get; set; }

        public string PipelineWebUrl { get; set; }

        public bool AllowFailure { get; set; }

        public BsonDateTime? FinishedAt { get; set; }
    }
}
