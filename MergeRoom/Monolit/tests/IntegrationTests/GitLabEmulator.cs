using DiscordMergeRoomBotCsharpEdition.Configs;
using DiscordMergeRoomBotCsharpEdition.Entities;
using IntegrationTests.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntegrationTests
{
    public class GitLabEmulator
    {
        private static int _counter;
        private readonly IGitLabHttpRepository _gitlabHttp;
        public readonly LinkedList<string> BranchNames = new LinkedList<string>();

        public readonly LinkedList<int> MergeRequestIds = new LinkedList<int>();
        private readonly Project _project;

        public GitLabEmulator(IGitLabHttpRepository gitlabHttp, Project project)
        {
            _gitlabHttp = gitlabHttp;
            _project = project;
        }

        public int Counter => ++_counter;

        public int LastMrCount { get; private set; }

        public async Task<int> CreateMergeRequest(string sourceBranch, string targetBranch = "main")
        {
            var url = "/merge_requests";

            var requestBody = new
            {
                source_branch = sourceBranch,
                target_branch = targetBranch,
                title = $"{LastMrCount + 1} {Counter} {DateTime.UtcNow:HH-mm-ss} Merge request",
            };
            Console.WriteLine(requestBody.title);
            var response = await _gitlabHttp.SendPost(requestBody, _gitlabHttp.BuildUrlByProject(url, _project), _project);
            LastMrCount = response["iid"].AsInt();
            MergeRequestIds.AddFirst(LastMrCount);
            return LastMrCount;
        }

        /// <summary>
        /// Create test branch
        /// </summary>
        /// <returns>Branch name</returns>
        public async Task<string> CreateBranch()
        {
            var url = "/repository/branches";

            var requestQuery = new Dictionary<string, object>
            {
                ["branch"] = $"{Counter}-{DateTime.UtcNow:HH-mm-ss}-Branch",
                ["ref"] = "main",
            };

            var response = await _gitlabHttp.SendPost(new object(), _gitlabHttp.BuildUrlByProject(url, _project, requestQuery), _project);
            var name = response["name"].AsNullableString();
            BranchNames.AddFirst(name);
            return name;
        }

        public async Task UpdateBranchFile(string branchName, string filePath = "README.md")
        {
            var apiUrl = $"/repository/files/{Uri.EscapeDataString(filePath)}";

            var requestBody = new
            {
                branch = branchName,
                content = $"{Counter} {DateTime.UtcNow:HH-mm-ss}  Content",
                commit_message = $"{Counter} Update README.md through API",
            };

            await _gitlabHttp.SendPut(requestBody, _gitlabHttp.BuildUrlByProject(apiUrl, _project), _project);
        }

        public async Task CreateBranchFile(string branchName, string? filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = $"{Counter}-{DateTime.UtcNow:HH-mm-ss}-file";
            }

            var apiUrl = $"/repository/files/{Uri.EscapeDataString(filePath)}";

            var requestBody = new
            {
                branch = branchName,
                content = $"{Counter} {DateTime.UtcNow:HH-mm-ss}  Content",
                commit_message = $"{Counter} Update README.md through API",
            };

            await _gitlabHttp.SendPost(requestBody, _gitlabHttp.BuildUrlByProject(apiUrl, _project), _project);
        }

        public async Task<int> AddCommentToMergeRequest(int mergeRequestIId, DateTimeOffset? createdAt = null)
        {
            var url = $"/merge_requests/{mergeRequestIId}/notes";

            var requestQuery = new Dictionary<string, object>
            {
                ["body"] = $"{Counter} {DateTime.UtcNow.TimeOfDay} Note",
            };

            if (createdAt.HasValue)
            {
                requestQuery.Add("created_at", createdAt);
            }

            var response = await _gitlabHttp.SendPost(null, _gitlabHttp.BuildUrlByProject(url, _project, requestQuery), _project);
            return response["id"].AsInt();
        }

        public async Task<(int mrIId, string branchName)> OpenMergeRequest()
        {
            var branchName = await CreateBranch();
            await CreateBranchFile(branchName);
            return (await CreateMergeRequest(branchName), branchName);
        }

        public async Task CloseMergeRequest(int iid)
        {
            var branchName = await CreateBranch();
            await UpdateBranchFile(branchName);
            await CreateMergeRequest(branchName);
        }

        public async Task MergeMergeRequest(int iid)
        {
            var url = $"/merge_requests/{iid}/merge";
            await _gitlabHttp.SendPut(null, _gitlabHttp.BuildUrlByProject(url, _project), _project);
        }

        public async Task TriggerCheckMerge(int iid)
        {
            var url = $"/merge_requests/{iid}";
            var response = await _gitlabHttp.SendGet(_gitlabHttp.BuildUrlByProject(url, _project, new Dictionary<string, object>()
            {
                ["render_html"] = true
            }), _project);
        }

        public async Task DeleteData()
        {
            foreach (var mergeRequestId in MergeRequestIds)
            {
                await _gitlabHttp.SendDelete(_gitlabHttp.BuildUrlByProject($"/merge_requests/{mergeRequestId}", _project), _project);
            }

            foreach (var branchName in BranchNames)
            {
                await _gitlabHttp.SendDelete(_gitlabHttp.BuildUrlByProject($"/repository/branches/{branchName}", _project), _project);
            }
        }

        public async Task DeleteAllData()
        {
            try
            {
                var mrs = await _gitlabHttp.SendGetAll(
                    _gitlabHttp.BuildUrlByProject(
                        "/merge_requests",
                        _project,
                        new Dictionary<string, object>
                        {
                            ["page"] = 0,
                            ["per_page"] = 9999,
                        }),
                    _project);

                foreach (var mergeRequestId in mrs.Select(x => x["iid"].AsInt()))
                {
                    await _gitlabHttp.SendDelete(
                        _gitlabHttp.BuildUrlByProject($"/merge_requests/{mergeRequestId}", _project),
                        _project);
                }

                var branches = await _gitlabHttp.SendGetAll(
                    _gitlabHttp.BuildUrlByProject(
                        "/repository/branches",
                        _project,
                        new Dictionary<string, object>
                        {
                            ["page"] = 0,
                            ["per_page"] = 9999,
                        }),
                    _project);

                foreach (var branchName in branches.Select(x => x["name"].AsNullableString()))
                {
                    if (branchName == "main")
                    {
                        continue;
                    }

                    await _gitlabHttp.SendDelete(
                        _gitlabHttp.BuildUrlByProject($"/repository/branches/{branchName}", _project),
                        _project);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Deleting data from gitlab error", e);
            }
        }
    }
}
