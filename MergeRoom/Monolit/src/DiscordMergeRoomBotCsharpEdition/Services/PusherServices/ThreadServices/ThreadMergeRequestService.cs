using Discord;
using Discord.Net;
using Discord.WebSocket;
using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;
using DiscordMergeRoomBotCsharpEdition.MongoDB;
using System.Text;
namespace DiscordMergeRoomBotCsharpEdition.Services.PusherServices.ThreadServices
{
    public class ThreadMergeRequestService
    {
        private const string BlockMrEmoji = "📛";
        private const string ApproveMrEmoji = "\u2705";
        private readonly DataService _dataService;
        private readonly ILogger<ThreadMergeRequestService> _logger;
        private readonly IMongoRepository _repository;

        public ThreadMergeRequestService(
            IMongoRepository repository,
            DataService dataService,
            ILogger<ThreadMergeRequestService> logger)
        {
            _repository = repository;
            _dataService = dataService;
            _logger = logger;
        }

        public async Task Open(HandleData data, MergeRequest mergeRequest)
        {
            var mergeCreator = await _dataService.GetUser(data.Project, mergeRequest.AuthorId);

            var guild = _dataService.GetGuild(data.Project.GuildId);
            var channel = guild.GetTextChannel(data.Project.ChannelDiscordId);

            var title = GetCustomTitle(data.NewMergeRequest, data.NewMergeRequest.Title);

            var thread = await channel.CreateThreadAsync(
                title.Substring(0, Math.Min(99, title.Length)),
                autoArchiveDuration: ThreadArchiveDuration.OneWeek,
                type: ThreadType.PrivateThread);

            await thread.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle(title.Substring(0, Math.Min(256, title.Length)))
                .WithDescription($"[Merge Request]({data.NewMergeRequest.WebUrl})\n[{data.Project.Namespace}/{data.Project.ProjectName}]({data.Project.GitLabLink})")
                .WithColor(Color.Blue)
                .Build());

            if (!string.IsNullOrEmpty(data.NewMergeRequest.Description))
            {
                await thread.SendMessageAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Merge request description")
                        .WithDescription(data.NewMergeRequest.Description)
                        .WithColor(Color.Blue)
                        .Build());
            }

            _logger.LogInformation($"Thread \"{data.NewMergeRequest.Title}:{thread.Id}\" created in channel \"{data.Project.ChannelDiscordName}:{channel.Id}\".");

            guild.GetThreadChannel(thread.Id);

            await AddUsersToMergeRequest(mergeCreator, thread, guild, data);

            mergeRequest.Title = FilterEscapedCharacters(mergeRequest.Title);
            mergeRequest.ThreadId = thread.Id;
            mergeRequest.IsClosed = false;

            await _repository.UpdateAsync(mergeRequest);

        }
        
        private async Task AddUsersToMergeRequest(User? mergeCreator, SocketThreadChannel thread, SocketGuild guild, HandleData data)
        {
            if (mergeCreator is not null && mergeCreator.DiscordId.HasValue)
            {
                await TryAddUserToThreadAsync(thread.Id, guild, mergeCreator.DiscordId.Value);
            }

            if (data.NewMergeRequest.ReviewerIds.Any())
            {
                foreach (var reviewerId in data.NewMergeRequest.ReviewerIds)
                {
                    var reviewer = await _dataService.GetUser(data.Project, reviewerId);
                    if (reviewer.DiscordId.HasValue)
                    {
                        await TryAddUserToThreadAsync(thread.Id, guild, reviewer.DiscordId.Value);
                    }
                }
            }

            if (data.NewMergeRequest.AdditionalUsers.Any())
            {
                foreach (var additionalUserId in data.NewMergeRequest.AdditionalUsers)
                {
                    var additionalUser = await _dataService.GetUser(data.Project, additionalUserId);
                    if (additionalUser.DiscordId.HasValue)
                    {
                        await TryAddUserToThreadAsync(thread.Id, guild, additionalUser.DiscordId.Value);
                    }
                }
            }
        }

        private async Task TryAddUserToThreadAsync(ulong chatId, SocketGuild? guild, ulong userId)
        {
            var thread = guild.GetThreadChannel(chatId);

            if (thread == null)
            {
                _logger.LogError($"Thread \"{chatId}\" not found.");
                return;
            }

            var users = await thread.GetUsersAsync();

            var user = guild.GetUser(userId);

            if (user == null)
            {
                return;
            }

            if (users.Any(x => x.Id == userId))
            {
                return;
            }

            try
            {
                await thread.AddUserAsync(user);
            }
            catch (Exception e)
            {
                _logger.LogWarning(exception: e, $"{user.GlobalName}\\{user.Nickname} (ID:{user.Id}) has not access to channel or thread");
            }
        }

        public async Task Close(HandleData data, MergeRequest mergeRequest)
        {
            var guild = _dataService.GetGuild(data.Project.GuildId);

            var thread = guild.GetThreadChannel(data.NewMergeRequest.ThreadId);

            if (thread is null)
            {
                _logger.LogError($"Thread not by id {data.NewMergeRequest.Title}.");
                _logger.LogWarning($"Trying to delete by merge request name: {data.NewMergeRequest.Title}");
                thread = guild.ThreadChannels.FirstOrDefault(x => x.Name == data.NewMergeRequest.Title);
                if (thread is null)
                {
                    throw new ArgumentException($"Thread not found by id {data.NewMergeRequest.ThreadId} and name {data.NewMergeRequest.Title}");
                }

                await thread.DeleteAsync();
            }
            else
            {
                await thread.DeleteAsync();
            }

            _logger.LogInformation($"Thread \"{thread.Name}:{thread.Id}\" delete because: {mergeRequest.State}");
            mergeRequest.IsClosed = true;
            await _repository.UpdateAsync(mergeRequest);
        }

        public async Task Edit(HandleData data, MergeRequest mergeRequest)
        {
            var guild = _dataService.GetGuild(data.Project.GuildId);

            var thread = guild.GetThreadChannel(data.NewMergeRequest.ThreadId);

            if (thread is null)
            {
                _logger.LogError($"Thread not by id {data.NewMergeRequest.Title}.");
                _logger.LogWarning($"Trying to edit by merge request name: {data.NewMergeRequest.Title}");
                thread = guild.ThreadChannels.FirstOrDefault(x => x.Name == data.NewMergeRequest.Title);
                if (thread is null)
                {
                    throw new ArgumentException($"Thread not found by id {data.NewMergeRequest.ThreadId} and name {data.NewMergeRequest.Title}");
                }
            }

            await AddUsersToMergeRequest(null, thread, guild, data);

            var newTitle = GetCustomTitle(mergeRequest, thread.Name);
            if (thread.Name != newTitle)
            {
                _logger.LogInformation($"Thread \"{thread.Name}:{thread.Id}\" edited -> \"{newTitle}\".");
                try
                {
                    await thread.ModifyAsync(properties => properties.Name = newTitle);
                }
                catch (RateLimitedException ex)
                {
                    _logger.LogWarning(ex, $"RateLimit {data.Project.Namespace}/{data.Project.ProjectName} {thread.Name}, {mergeRequest.Title}");
                }
            }
        }

        private string FilterEscapedCharacters(string str)
        {
            return str.Replace("\"", "\\\"").Replace("'", "\\'");
        }

        private string GetCustomTitle(MergeRequest mergeRequest, string threadName)
        {
            var titleBuilder = new StringBuilder();

            if (mergeRequest.HasConflicts)
            {
                titleBuilder.Append(BlockMrEmoji);
            }
            else
            {
                if (threadName.Contains(BlockMrEmoji) && mergeRequest.MergeStatus.Contains("check"))
                {
                    titleBuilder.Append(BlockMrEmoji);
                }
            }

            if (mergeRequest.ApprovesInfo is not null)
            {
                for (var i = 0; i < mergeRequest.ApprovesInfo.UserIds.Count; i++)
                {
                    titleBuilder.Append(ApproveMrEmoji);
                }
            }

            titleBuilder.Append(FilterEscapedCharacters(mergeRequest.Title));

            var title = titleBuilder.ToString();
            return title.Substring(0, Math.Min(99, title.Length));
        }
    }
}
