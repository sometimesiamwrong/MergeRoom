using System.Text;

namespace MergeRoom.IntegrationTests.tests.IntegrationTests.Services
{
    public class GitLabHttpRepository : IGitLabHttpRepository
    {
        public HttpClient _client;

        public GitLabHttpRepository(HttpClient client)
        {
            _client = client;
        }

        public async Task<BsonValue> SendPost(object requestBody, string apiUrl, Project project)
        {
            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = content,
            };

            request.Headers.Add("PRIVATE-TOKEN", project.AccessToken);

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return BsonDocument.Parse(await response.Content.ReadAsStringAsync());
            }

            throw new Exception($"Error while sending POST request {apiUrl}");
        }

        public async Task<BsonValue> SendPut(object requestBody, string apiUrl, Project project)
        {
            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Put, apiUrl)
            {
                Content = content,
            };

            request.Headers.Add("PRIVATE-TOKEN", project.AccessToken);

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return BsonDocument.Parse(await response.Content.ReadAsStringAsync());
            }

            throw new Exception($"Error while sending POST request {apiUrl}");
        }

        public async Task SendDelete(string apiUrl, Project project)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, apiUrl);

            request.Headers.Add("PRIVATE-TOKEN", project.AccessToken);

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            throw new Exception($"Error while sending POST request {apiUrl}");
        }

        public async Task<BsonDocument> SendGet(string apiUrl, Project project)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

            request.Headers.Add("PRIVATE-TOKEN", project.AccessToken);

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return BsonDocument.Parse(await response.Content.ReadAsStringAsync());
            }

            throw new Exception($"Error while sending POST request {apiUrl}");
        }

        public async Task<List<BsonDocument>> SendGetAll(string apiUrl, Project project)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

            request.Headers.Add("PRIVATE-TOKEN", project.AccessToken);

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonArray = JArray.Parse(await response.Content.ReadAsStringAsync());
                var bsonDocuments = new List<BsonDocument>();
                foreach (var jsonObject in jsonArray)
                {
                    var bsonDocument = BsonDocument.Parse(jsonObject.ToString());
                    bsonDocuments.Add(bsonDocument);
                }

                return bsonDocuments;
            }

            throw new Exception($"Error while sending POST request {apiUrl}");
        }

        public string BuildUrlByProject(string url, Project project, Dictionary<string, object>? queryParams = null)
        {
            return BuildUrl($"/projects/{project.GitlabId}" + url, project, queryParams);
        }

        public string GetQueryString(Dictionary<string, object>? queryParams = null)
        {
            if (queryParams is null)
            {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder("?");
            stringBuilder.AppendJoin("&", queryParams.Select(x => $"{x.Key}={x.Value}"));
            return stringBuilder.ToString();
        }

        public string BuildUrl(string url, Project project, Dictionary<string, object>? queryParams = null)
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
