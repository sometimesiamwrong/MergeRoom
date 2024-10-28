using DiscordMergeRoomBotCsharpEdition.Configs;
using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities;
using MongoDB.Bson;
using System.Web;

namespace DiscordMergeRoomBotCsharpEdition.Services
{
    /// <summary>
    /// GitLab service for Note
    /// </summary>
    public partial class GitLabService
    {
        private const string GET_NOTES_BY_MR_URL = "/merge_requests/{0}/notes";
        private const string GET_RAW_FILE_FROM_REPOSITORY = "/repository/files";

        private readonly Dictionary<string, object> _notePageParams = new Dictionary<string, object>
        {
            ["page"] = 0,
            ["per_page"] = 30,
        };

        public async Task<List<Note>> GetNotesByMergeRequests(Project project, uint mergeRequestId, Dictionary<string, object>? queryParams = null)
        {
            var notes = new List<Note>();

            queryParams = AddPageQueryParams(queryParams);

            var url = BuildUrlByProject(string.Format(GET_NOTES_BY_MR_URL, mergeRequestId), project, queryParams);

            var json = await GetAllResponse(url, project.AccessToken);

            foreach (var item in json)
            {
                var note = new Note(
                    (uint)item["id"].AsInt(),
                    (uint)item["author"]["id"].AsInt(),
                    item["body"].AsNullableString(),
                    $"https://{project.Host}/{project.Namespace}/{project.ProjectName}/merge_requests/{mergeRequestId}#note_{(uint)item["id"].AsInt()}",
                    BsonDateTime.Create(item["created_at"].AsNullableString()),
                    BsonDateTime.Create(item["updated_at"].AsNullableString()),
                    type: item["type"].AsBsonValue is BsonNull ? null : item["type"].AsNullableString(),
                    isSystem: item["system"].AsBoolean);

                if (note.Type == "DiffNote" &&
                    item["position"]["position_type"].AsNullableString() == "text")
                {
                    var diffNote = new DiffNote(note);
                    diffNote.Position = GetPositionFromBson(item["position"].AsBsonDocument);
                    note = diffNote;
                }

                notes.Add(note);
            }

            return notes;
        }

        private Position? GetPositionFromBson(BsonDocument bsonPosition)
        {
            var position = new Position
            {
                BaseSha = bsonPosition["base_sha"].AsNullableString(),
                StartSha = bsonPosition["start_sha"].AsNullableString(),
                HeadSha = bsonPosition["head_sha"].AsNullableString(),
                OldPath = bsonPosition["old_path"].AsNullableString(),
                NewPath = bsonPosition["new_path"].AsNullableString(),
            };

            var lineRange = bsonPosition["line_range"].AsBsonDocument;

            position.Start = new LineRange()
            {
                LineCode = lineRange["start"]["line_code"].AsString,
                Type = lineRange["start"]["type"].AsNullableString(),
                OldLine = lineRange["start"]["old_line"].AsNullableInt(),
                NewLine = lineRange["start"]["new_line"].AsNullableInt(),
            };

            position.End = new LineRange()
            {
                LineCode = lineRange["end"]["line_code"].AsString,
                Type = lineRange["end"]["type"].AsNullableString(),
                OldLine = lineRange["end"]["old_line"].AsNullableInt(),
                NewLine = lineRange["end"]["new_line"].AsNullableInt(),
            };

            if (position.Start.Range.OldNumber != position.End.Range.OldNumber &&
                position.End.Type == "new")
            {
                position.End.Range = (position.End.Range.OldNumber - 1, position.End.Range.NewNumber);
            }

            if (position.Start.Range.NewNumber != position.End.Range.NewNumber &&
                position.End.Type == "old")
            {
                position.End.Range = (position.End.Range.OldNumber, position.End.Range.NewNumber - 1);
            }

            return position;
        }

        /// <summary>
        /// Get text of file by branch
        /// </summary>
        public async Task<string> GetRawFileByBranchName(Project project, string filePath, string @ref)
        {
            var requestUrl = BuildUrlByProject(
                $"{GET_RAW_FILE_FROM_REPOSITORY}/{HttpUtility.UrlEncode(filePath)}/raw/",
                project, new Dictionary<string, object>
                {
                    ["ref"] = @ref,
                });

            return await GetResponseText(requestUrl, project.AccessToken);
        }
    }
}
