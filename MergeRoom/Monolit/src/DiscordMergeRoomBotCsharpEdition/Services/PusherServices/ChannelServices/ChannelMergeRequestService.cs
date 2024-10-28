using Discord;
using Discord.WebSocket;
using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;
using DiscordMergeRoomBotCsharpEdition.MongoDB;
using System.Text;

namespace DiscordMergeRoomBotCsharpEdition.Services.PusherServices.ChannelServices
{
    public class ChannelMergeRequestService
    {
        private const string BlockMrEmoji = "📛";
        private const string ApproveMrEmoji = "\u2705";
        private readonly DataService _dataService;
        private readonly IMongoRepository _repository;

        public ChannelMergeRequestService(
            IMongoRepository repository,
            DataService dataService)
        {
            _repository = repository;
            _dataService = dataService;
        }

        public async Task Open(HandleData data, MergeRequest mergeRequest)
        {
            var mergeCreator = await _dataService.GetUser(data.Project, mergeRequest.AuthorId);

            var guild = _dataService.GetGuild(data.Project.GuildId);
            var permissionOverwrites = new List<Overwrite>();

            var roles = guild.Roles;
            if (roles is not null)
            {
                foreach (var role in roles)
                {
                    permissionOverwrites.Add(new Overwrite(role.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)));
                }
            }

            permissionOverwrites.Add(new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)));

            if (mergeCreator.DiscordId.HasValue)
            {
                permissionOverwrites.Add(new Overwrite(mergeCreator.DiscordId.Value, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow)));
            }

            var channel = await guild.CreateTextChannelAsync(GetCustomTitle(mergeRequest, mergeRequest.Title), properties =>
            {
                properties.CategoryId = data.Project.CategoryDiscordId;
                properties.PermissionOverwrites = permissionOverwrites;
            });

            guild.GetTextChannel(channel.Id);

            mergeRequest.Title = FilterEscapedCharacters(mergeRequest.Title);
            mergeRequest.ChannelId = channel.Id;
            mergeRequest.IsClosed = false;

            await _repository.UpdateAsync(mergeRequest);
        }

        public async Task Close(HandleData data, MergeRequest mergeRequest)
        {
            var guild = _dataService.GetGuild(data.Project.GuildId);

            var channel = guild.GetTextChannel(mergeRequest.ChannelId);
            await channel.DeleteAsync();

            mergeRequest.IsClosed = true;

            await _repository.UpdateAsync(mergeRequest);
        }

        public async Task Edit(HandleData data, MergeRequest mergeRequest)
        {
            var guild = _dataService.GetGuild(data.Project.GuildId);

            var channel = guild.GetTextChannel(mergeRequest.ChannelId);
            await channel.ModifyAsync(properties => properties.Name = GetCustomTitle(mergeRequest, channel.Name));
        }

        private string FilterEscapedCharacters(string str)
        {
            return str.Replace("\"", "\\\"").Replace("'", "\\'");
        }

        private string GetCustomTitle(MergeRequest mergeRequest, string channelName)
        {
            var titleBuilder = new StringBuilder();

            if (mergeRequest.HasConflicts)
            {
                titleBuilder.Append(BlockMrEmoji);
            }
            else
            {
                if (channelName.Contains(BlockMrEmoji) && mergeRequest.MergeStatus.Contains("check"))
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


            return titleBuilder.ToString();
        }
    }
}
