using Discord;
using Discord.WebSocket;
using MergeRoom.Domain.Entities;
using MergeRoom.GitlabRepository;
using MergeRoom.MongoRepositoryr.MongoDB;

namespace MergeRoom.DiscordBot.Commands
{
    public class RegisterSlashCommandHandler : ISlashCommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordBotConfiguration _configuration;
        private readonly GitLabService _gitLabService;
        private readonly IMongoRepository _mongoRepository;

        public RegisterSlashCommandHandler(
            DiscordSocketClient client,
            GitLabService gitLabService,
            IMongoRepository mongoRepository,
            DiscordBotConfiguration configuration)
        {
            _client = client;
            _gitLabService = gitLabService;
            _mongoRepository = mongoRepository;
            _configuration = configuration;
        }

        public string Name => "register_project";

        public async Task HandleCommand(SocketSlashCommand command, string[] args)
        {
            try
            {
                var linkOption = command.Data.Options.FirstOrDefault(o => o.Name == "link");
                var nameOption = command.Data.Options.FirstOrDefault(o => o.Name == "name");
                var accessTokenOption = command.Data.Options.FirstOrDefault(o => o.Name == "access_token");
                var pusherKindOption = command.Data.Options.FirstOrDefault(o => o.Name == "pusher_kind");

                if (linkOption?.Value == null || nameOption?.Value == null || accessTokenOption?.Value == null || pusherKindOption?.Value == null)
                {
                    await command.RespondAsync("Missing required options.");
                    return;
                }

                var link = linkOption.Value as string;
                var name = nameOption.Value as string;
                var accessToken = accessTokenOption.Value as string;
                var pusherKind = pusherKindOption.Value as string;

                if (!_configuration.PossiblePusherKinds.AllKinds.Contains(pusherKind))
                {
                    await command.RespondAsync($"Pusher kind: {pusherKind} NOT CORRECT!");
                    return;
                }

                if (!IsValidUrl(link))
                {
                    await command.RespondAsync($"Link: {link} NOT CORRECT!");
                    return;
                }

                var gitlabProject = await _gitLabService.GetProjectDataByUrl(accessToken, link);
                var existProject = await _mongoRepository.GetAsync<Project>(p => p.GitlabId == gitlabProject.GitlabId);

                if (existProject is not null)
                {
                    await command.RespondAsync($"Project with: {existProject.GitLabLink} already registered.");
                    return;
                }

                var guild = _client.GetGuild(command.GuildId!.Value);
                if (guild == null)
                {
                    await command.RespondAsync("Guild not found.");
                    return;
                }

                var channel = guild.TextChannels.FirstOrDefault(c => c.Name == name);

                if (channel == null)
                {
                    await command.RespondAsync($"Channel with name: {name} not found.");
                    return;
                }

                var permissionOverwrites = new List<Overwrite>
                {
                    new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                };

                string? categoryName = null;
                ICategoryChannel? category = null;
                if (pusherKind == _configuration.PossiblePusherKinds.Channel)
                {
                    categoryName = name + "_mr";

                    category = await guild.CreateCategoryChannelAsync(categoryName, properties =>
                    {
                        properties.PermissionOverwrites = permissionOverwrites;
                    });
                }

                var project = new Project
                {
                    GuildId = guild.Id,
                    GitlabId = gitlabProject.GitlabId,
                    GitLabLink = gitlabProject.GitLabLink,
                    Host = gitlabProject.Host,
                    Namespace = gitlabProject.Namespace,
                    ProjectName = gitlabProject.ProjectName,
                    AccessToken = accessToken,
                    ChannelDiscordName = channel.Name,
                    ChannelDiscordId = channel.Id,
                    CategoryDiscordName = categoryName,
                    CategoryDiscordId = category?.Id,
                    ParsedAt = DateTime.UtcNow.AddDays(-7),
                    PusherKind = pusherKind,
                    IsNeedParseDefaultBranches = false,
                };
                await _mongoRepository.AddAsync(project);

                await CacheAllUsers(project);
                await command.RespondAsync($"Link: {project.GitLabLink} registered. To '{categoryName}'");
            }
            catch (Exception ex)
            {
                await command.RespondAsync($"Error: {ex.Message}");
            }
        }

        private async Task CacheAllUsers(Project project)
        {
            var users = await _gitLabService.GetUsersByProject(project);
            foreach (var user in users)
            {
                await _mongoRepository.AddAsync(user);
            }
        }

        private bool IsValidUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
