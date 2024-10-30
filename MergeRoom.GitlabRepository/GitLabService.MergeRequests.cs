using MergeRoom.Domain.Entities;
using MergeRoom.Domain.Entities.GitlabEntities;
using MergeRoom.Extensions.Configs;
using MongoDB.Bson;
using System.Text;

namespace MergeRoom.GitlabRepository
{
    public partial class GitLabService
    {
        private const string GET_MERGE_REQUESTS_URL = "/merge_requests";
        private const string GET_MERGE_REQUEST_BY_ID_URL = "/merge_requests/{0}";
        private const string PUT_NOTES_BY_MR_URL = "/merge_requests/{0}";

        public async Task<List<MergeRequest>> GetMergeRequests(Project project, Dictionary<string, object>? queryParams = null, bool withPipeline = true)
        {
            var mergeRequests = new List<MergeRequest>();

            queryParams = AddPageQueryParams(queryParams);

            var url = BuildUrlByProject(GET_MERGE_REQUESTS_URL, project, queryParams);

            var json = await GetAllResponse(url, project.AccessToken);

            foreach (var item in json)
            {
                var mergeRequest = new MergeRequest(
                    (uint)item["id"].AsInt(),
                    (uint)item["iid"].AsInt(),
                    (uint)item["project_id"].AsInt(),
                    item["title"].AsNullableString(),
                    item["description"].AsNullableString(),
                    item["state"].AsNullableString(),
                    BsonDateTime.Create(item["created_at"].AsNullableString()),
                    BsonDateTime.Create(item["updated_at"].AsNullableString()),
                    (uint)item["author"]["id"].AsInt(),
                    item["assignees"].AsBsonArray.Select(x => (uint)x["id"].AsInt()).ToList(),
                    item["reviewers"].AsBsonArray.Select(x => (uint)x["id"].AsInt()).ToList(),
                    item["draft"].AsBoolean,
                    item["merge_status"].AsNullableString(),
                    item["detailed_merge_status"].AsNullableString(),
                    item["web_url"].AsNullableString(),
                    item["has_conflicts"].AsBoolean);

                if (withPipeline)
                {
                    var fullMr = await GetResponse(BuildUrlByProject(string.Format(GET_MERGE_REQUEST_BY_ID_URL, item["iid"].AsInt()), project, null), project.AccessToken);
                    if (fullMr["head_pipeline"] is BsonNull == false)
                    {
                        mergeRequest.HeadPipeline = (uint)fullMr["head_pipeline"]["id"].AsInt();
                    }
                }

                mergeRequests.Add(mergeRequest);
            }

            return mergeRequests;
        }

        public async Task<ApproveInfo> GetApprovesByMergeRequest(Project project, uint mergeRequestId, Dictionary<string, object>? queryParams = null)
        {
            queryParams = AddPageQueryParams(queryParams);

            var url = BuildUrlByProject(string.Format(GET_MERGE_REQUESTS_APPROVALS_BY_MR_URL, mergeRequestId), project, queryParams);

            var json = await GetResponse(url, project.AccessToken);

            return new ApproveInfo(json["approved_by"].AsBsonArray.Select(x => (uint)x["user"]["id"].AsInt()).ToList());
        }

        public async Task<List<ResourceStateEvent>> GetResourceStateEventByMergeRequest(Project project, uint mergeRequestId, Dictionary<string, object>? queryParams = null)
        {
            queryParams = AddPageQueryParams(queryParams);

            var url = BuildUrlByProject(
                string.Format(GET_MERGE_REQUESTS_RESOURCE_STATE_EVENTS_BY_MR_URL, mergeRequestId),
                project,
                queryParams);

            var json = await GetAllResponse(url, project.AccessToken);

            var events = new List<ResourceStateEvent>();
            foreach (var item in json)
            {
                events.Add(new ResourceStateEvent(
                    BsonDateTime.Create(item["created_at"].AsNullableString()),
                    (uint)item["user"]["id"].AsInt(),
                    item["state"].AsNullableString()));
            }

            return events.ToList();
        }


        public async Task SetAssignee(Project project, uint mergeRequestId, string assigneeId)
        {
            // Формирование данных для запроса
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("assignee_ids[]", assigneeId),
            });

            // Формирование URL
            var requestUrl = BuildUrlByProject(string.Format(PUT_NOTES_BY_MR_URL, mergeRequestId), project);

            // Выполнение запроса
            await PutResponseText(requestUrl, project.AccessToken, content);
        }

        private string BuildUrl(string url, Project project, Dictionary<string, object>? queryParams = null)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"https://{project.Host}/api/v4");
            stringBuilder.Append(url);
            if (queryParams is not null)
            {
                stringBuilder.Append(GetQueryString(queryParams));
            }

            return stringBuilder.ToString();
        }
    }
}
