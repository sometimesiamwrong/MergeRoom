using MergeRoom.Domain.Entities;
using MergeRoom.Extensions.Configs;
using MongoDB.Bson;

namespace MergeRoom.GitlabRepository
{
    /// <summary>
    /// GitLab service for users
    /// </summary>
    public partial class GitLabService
    {
        private const string GET_USERS_BY_ID = "/users/{0}";
        private const string GET_USERS = "/users";

        /// <summary>
        /// Get Gitlab user by Gitlab ID.
        /// </summary>
        public async Task<User> GetUser(Project project, ulong id)
        {
            var json = await GetResponse(BuildUrl(string.Format(GET_USERS_BY_ID, id), project), project.AccessToken);

            return new User(
                (uint)json["id"].AsInt(),
                json["name"].AsNullableString(),
                json["username"].AsNullableString(),
                json["web_url"].AsNullableString(),
                GetDiscordIdFromBson(json["discord"]),
                json["avatar_url"].AsNullableString(),
                project.GitlabId);
        }

        /// <summary>
        /// Get Gitlab user by host.
        /// </summary>
        public async Task<List<User>> GetUsersByHost(Project project)
        {
            return await GetNewUsers(project, BuildUrl(GET_USERS, project, _defaultPageParams));
        }

        /// <summary>
        /// Get Gitlab user by username.
        /// </summary>
        public async Task<User> GetUserByUserName(Project project, string userName)
        {
            try
            {
                var existedUser = await _repository.GetAsync<User>(x =>
                    x.Username == userName &&
                    x.GitlabProjectId == project.GitlabId);

                if (existedUser is not null)
                {
                    return existedUser;
                }

                var jsonArray = await GetAllResponse(BuildUrl(
                        GET_USERS,
                        project,
                        new Dictionary<string, object>
                        {
                            ["username"] = userName
                        }),
                    project.AccessToken);

                var json = jsonArray.First();

                return new User(
                    (uint)json["id"].AsInt(),
                    json["name"].AsNullableString(),
                    json["username"].AsNullableString(),
                    json["web_url"].AsNullableString(),
                    null,
                    json["avatar_url"].AsNullableString(),
                    project.GitlabId);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Get Gitlab user by host.
        /// </summary>
        public async Task<List<User>> GetUsersByProject(Project project)
        {
            return await GetNewUsers(project, BuildUrlByProject(GET_USERS, project, _defaultPageParams));
        }

        private async Task<List<User>> GetNewUsers(Project project, string uri)
        {
            var json = await GetAllResponse(uri, project.AccessToken);
            var users = new List<User>();
            foreach (var item in json)
            {
                var existedUser = await _repository.GetAsync<User>(x =>
                    x.GitlabId == (uint)item["id"].AsInt() &&
                    x.GitlabProjectId == project.GitlabId);

                if (existedUser is not null)
                {
                    continue;
                }

                users.Add(new User(
                    (uint)item["id"].AsInt(),
                    item["name"].AsNullableString(),
                    item["username"].AsNullableString(),
                    item["web_url"].AsNullableString(),
                    null,
                    item["avatar_url"].AsNullableString(),
                    project.GitlabId));
            }

            return users;
        }

        private ulong? GetDiscordIdFromBson(BsonValue bsonDiscordValue)
        {
            if (bsonDiscordValue is not BsonNull && !string.IsNullOrEmpty(bsonDiscordValue.AsNullableString()))
            {
                return ulong.Parse(bsonDiscordValue.AsNullableString());
            }

            return null;
        }
    }
}
