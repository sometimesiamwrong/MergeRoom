namespace MergeRoom.IntegrationTests.tests.IntegrationTests.Services
{
    public interface IGitLabHttpRepository
    {
        Task<BsonDocument> SendGet(string apiUrl, Project project);

        Task<List<BsonDocument>> SendGetAll(string apiUrl, Project project);

        Task<BsonValue> SendPost(object requestBody, string apiUrl, Project project);

        Task<BsonValue> SendPut(object requestBody, string apiUrl, Project project);

        Task SendDelete(string apiUrl, Project project);

        string BuildUrlByProject(string url, Project project, Dictionary<string, object>? queryParams = null);

        string GetQueryString(Dictionary<string, object>? queryParams = null);

        string BuildUrl(string url, Project project, Dictionary<string, object>? queryParams = null);
    }
}
