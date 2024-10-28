using Discord.WebSocket;
using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.MongoDB;

namespace DiscordMergeRoomBotCsharpEdition.Services
{
    public class DataService
    {
        private readonly DiscordSocketClient _client;
        private readonly GitLabService _gitLabService;
        private readonly IMongoRepository _repository;
        private readonly ILogger<DataService> _logger;

        public DataService(
            DiscordSocketClient client,
            IMongoRepository repository,
            GitLabService gitLabService,
            ILogger<DataService> logger)
        {
            _client = client;
            _repository = repository;
            _gitLabService = gitLabService;
            _logger = logger;
        }

        public SocketGuild GetGuild(ulong guildId, int tryCounter = 0)
        {
            try
            {
                var guild = _client.GetGuild(guildId);
                if (guild is null)
                {
                    throw new Exception("Guild is null");
                }

                return guild;
            }
            catch (Exception e)
            {
                Task.Delay(1000).GetAwaiter().GetResult();
                GetGuild(guildId, tryCounter + 1);
                if (tryCounter > 5)
                {
                    _logger.LogError(e, "Get guild is failed");
                    throw;
                }
            }

            return null;
        }

        public async Task<User> GetUser(Project project, uint authorId)
        {
            var existedUser = await _repository.GetAsync<User>(u =>
                u.GitlabId == authorId &&
                u.GitlabProjectId == project.GitlabId);

            if (existedUser is not null)
            {
                if (existedUser.DiscordId.HasValue)
                {
                    return existedUser;
                }

                existedUser = await _gitLabService.GetUser(project, authorId);
                await _repository.UpdateAsync(existedUser);

                return existedUser;
            }

            existedUser = await _gitLabService.GetUser(project, authorId);
            await _repository.AddAsync(existedUser);

            return existedUser;
        }
    }
}
