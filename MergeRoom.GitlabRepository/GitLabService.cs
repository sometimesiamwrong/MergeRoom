using MergeRoom.Domain.Entities;
using MergeRoom.Extensions.Configs;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MergeRoom.GitlabRepository
{
    /// <summary>
    /// GitLab service for common
    /// </summary>
    public partial class GitLabService
    {
        private readonly HttpClient _client;

        private readonly Dictionary<string, object> _defaultPageParams = new Dictionary<string, object>
        {
            ["page"] = 0,
            ["per_page"] = 99999,
        };
        
        private Dictionary<string, object> _mrNewestQuery = new()
        {
            ["order_by"] = "updated_at",
            ["sort"] = "desc",
            ["updated_after"] = null!,
        };

        private Dictionary<string, object> _noteQuery = new()
        {
            ["order_by"] = "updated_at",
            ["sort"] = "desc",
            ["updated_after"] = null!,
            ["page"] = 0,
            ["per_page"] = 30,
        };

        private Dictionary<string, object> _onlyOpened = new()
        {
            ["order_by"] = "updated_at",
            ["sort"] = "desc",
            ["state"] = "opened",
        };

        public GitLabService(HttpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Get project id and url.
        /// </summary>
        public async Task<Project> GetProjectDataByUrl(string accessToken, string url)
        {
            var pattern = @"https://(?<host>[^/]+)/(?<namespace>[^/]+)/(?<project>.+)";

            // Use Regex to match the pattern in the URL
            var match = Regex.Match(url, pattern);

            if (!match.Success)
            {
                Console.WriteLine("The URL format is incorrect. Please check the URL\n" +
                                  "Example: (https://HOST/NAMESPACE/(?GROUP?)/PROJECT_NAME).");
            }

            var namespaceName = match.Groups["namespace"].Value;
            var projectName = match.Groups["project"].Value;
            var hostName = match.Groups["host"].Value;
            projectName = projectName.Replace("/", "%2f");

            var project = new Project
            {
                Host = hostName,
                AccessToken = accessToken,
            };
            var projectUrl = $"/projects/{namespaceName}%2F{projectName}";

            var projectJson = await GetResponse(BuildUrl(projectUrl, project), project.AccessToken);
            var userJson = await GetResponse(BuildUrl("/user", project), project.AccessToken);
            var userId = (uint)userJson["id"].AsInt();

            project.GitlabId = (uint)projectJson["id"].AsInt();
            project.GitLabLink = projectJson["web_url"].AsNullableString();
            project.Namespace = namespaceName;
            project.ProjectName = projectName;

            var members = await GetAllResponse(BuildUrlByProject("/members", project), project.AccessToken);
            foreach (var member in members)
            {
                if ((uint)member["id"].AsInt() == userId)
                {
                    if (member["access_level"].AsInt() < 40)
                    {
                        throw new ArgumentException("You do not have enough rights to access this project.");
                    }
                }
            }

            return project;
        }

        private string BuildUrlByProject(string url, Project project, Dictionary<string, object>? queryParams = null)
        {
            return BuildUrl($"/projects/{project.GitlabId}" + url, project, queryParams);
        }

        private async Task<BsonDocument> GetResponse(string requestUrl, string accessToken)
        {
            try
            {
                return BsonDocument.Parse(await GetResponseText(requestUrl, accessToken));
            }
            catch (Exception hre)
            {
                throw new ArgumentException($"Cannot parse to Json (URL: {requestUrl}). {hre.Message}", hre);
            }
        }

        private async Task<List<BsonDocument>> GetAllResponse(string requestUrl, string accessToken)
        {
            try
            {
                var jsonArray = JArray.Parse(await GetResponseText(requestUrl, accessToken));
                var bsonDocuments = new List<BsonDocument>();
                foreach (var jsonObject in jsonArray)
                {
                    var bsonDocument = BsonDocument.Parse(jsonObject.ToString());
                    bsonDocuments.Add(bsonDocument);
                }

                return bsonDocuments;
            }
            catch (Exception hre)
            {
                throw new ArgumentException($"Cannot parse to Json (URL: {requestUrl}). {hre.Message}", hre);
            }
        }

        private async Task<string> GetResponseText(string requestUrl, string accessToken)
        {
            try
            {
                // Установите заголовки запроса
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("PRIVATE-TOKEN", accessToken);
                // Отправьте запрос и получите ответ
                var response = await _client.SendAsync(request);

                // Проверьте успешность ответа
                response.EnsureSuccessStatusCode();

                // Прочитайте содержимое ответа
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception hre)
            {
                throw new ArgumentException($"Cannot access to project by (Url:{requestUrl}). {hre.Message}", hre);
            }
        }

        private string GetQueryString(Dictionary<string, object> queryParams)
        {
            var stringBuilder = new StringBuilder("?");
            stringBuilder.AppendJoin("&", queryParams.Select(x => $"{x.Key}={x.Value}"));
            return stringBuilder.ToString();
        }

        private Dictionary<string, object> AddPageQueryParams(Dictionary<string, object>? queryParams = null)
        {
            if (queryParams is null)
            {
                queryParams = _defaultPageParams;
            }
            else
            {
                foreach (var param in _defaultPageParams)
                {
                    queryParams.TryAdd(param.Key, param.Value);
                }
            }

            return queryParams;
        }

        private async Task<string> PutResponseText(string requestUrl, string accessToken, FormUrlEncodedContent content)
        {
            try
            {
                // Выполнение запроса
                var request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
                request.Headers.Add("PRIVATE-TOKEN", accessToken);

                request.Content = content;

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Assignee has been successfully updated.");
                }

                // Прочитайте содержимое ответа
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception hre)
            {
                throw new ArgumentException($"Cannot access to project by (Url:{requestUrl}). {hre.Message}", hre);
            }
        }
    }
}
