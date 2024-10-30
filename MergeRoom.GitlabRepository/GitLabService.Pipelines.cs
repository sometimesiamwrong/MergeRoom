using MergeRoom.Domain.Entities;
using MergeRoom.Domain.Entities.GitlabEntities;
using MergeRoom.Extensions.Configs;
using MongoDB.Bson;
using System.Text.RegularExpressions;

namespace MergeRoom.GitlabRepository
{
    /// <summary>
    /// GitLab service for pipelines
    /// </summary>
    public partial class GitLabService
    {
        private const string GET_JOBS_BY_PIPELINE_URL = "/pipelines/{0}/jobs";
        private const string GET_PIPELINES_URL = "/pipelines";
        private const string GET_PROTECTED_BRANCHES_URL = "/protected_branches";
        private const string GET_MERGE_REQUESTS_RESOURCE_STATE_EVENTS_BY_MR_URL = "/merge_requests/{0}/resource_state_events";
        private const string GET_MERGE_REQUESTS_APPROVALS_BY_MR_URL = "/merge_requests/{0}/approvals";

        public async Task<List<List<Job>>> GetProtectedBranchesPipelinesJobs(Project project, Dictionary<string, object>? queryParams = null)
        {
            var pipelineIds = await GetBranchHeadPipelineId(project, queryParams);

            var branchJobs = new List<List<Job>>();
            foreach (var id in pipelineIds)
            {
                branchJobs.Add(await GetJobsByPipeline(project, id));
            }

            return branchJobs;
        }

        public async Task<List<uint>> GetBranchHeadPipelineId(Project project, Dictionary<string, object>? queryParams = null)
        {
            queryParams = AddPageQueryParams(queryParams);

            var protectedBranchesUrl = BuildUrlByProject(GET_PROTECTED_BRANCHES_URL, project, queryParams);

            var protectedBranchesJson = await GetAllResponse(protectedBranchesUrl, project.AccessToken);

            var headPipelineIds = new List<uint>();

            foreach (var item in protectedBranchesJson)
            {
                var branchName = item["name"].AsNullableString();
                queryParams.Add("ref", branchName);

                var pipelinesByBranchUrl = BuildUrlByProject(GET_PIPELINES_URL, project, queryParams);

                var pipelinesJson = await GetAllResponse(pipelinesByBranchUrl, project.AccessToken);
                if (pipelinesJson.Any())
                {
                    headPipelineIds.Add((uint)pipelinesJson.FirstOrDefault()["id"].AsInt());
                }
            }

            return headPipelineIds;
        }


        public async Task<List<Job>> GetJobsByPipeline(Project project, uint pipelineId, Dictionary<string, object>? queryParams = null)
        {
            var jobs = new List<Job>();

            queryParams = AddPageQueryParams(queryParams);

            var url = BuildUrlByProject(string.Format(GET_JOBS_BY_PIPELINE_URL, pipelineId), project, queryParams);

            var json = await GetAllResponse(url, project.AccessToken);

            foreach (var item in json)
            {
                var @ref = item["ref"].AsNullableString();

                string mrPattern = @"merge-requests/(\d+)/head";

                var match = Regex.Match(@ref, mrPattern);

                uint? mrIId = null;

                if (match.Success)
                {
                    mrIId = uint.Parse(match.Groups[1].Value);
                }

                var note = new Job(
                    (ulong)item["id"].AsLong(),
                    mrIId,
                    (uint)item["user"]["id"].AsInt(),
                    @ref,
                    item["stage"].AsNullableString(),
                    item["status"].AsNullableString(),
                    item["name"].AsNullableString(),
                    item["web_url"].AsNullableString(),
                    item["pipeline"]["web_url"].AsNullableString(),
                    item["allow_failure"].AsBoolean,
                    BsonDateTime.Create(item["created_at"].AsNullableString()),
                    !string.IsNullOrWhiteSpace(item["finished_at"].AsNullableString()) ? BsonDateTime.Create(item["finished_at"].AsNullableString()) : null);

                jobs.Add(note);
            }

            return jobs;
        }
    }
}
