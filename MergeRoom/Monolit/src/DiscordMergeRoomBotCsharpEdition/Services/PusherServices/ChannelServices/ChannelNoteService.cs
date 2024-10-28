using Discord;
using Discord.WebSocket;
using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;

namespace DiscordMergeRoomBotCsharpEdition.Services.PusherServices.ChannelServices
{
    public class ChannelNoteService : DiscordChatNoteService
    {
        public ChannelNoteService(
            DataService dataService,
            DiscordBotConfiguration configuration,
            ILogger<ChannelNoteService> logger) : base(configuration, dataService, logger)
        {
        }

        public Task Delete(HandleData data, Note note)
        {
            throw new NotImplementedException();
        }

        protected override SocketTextChannel GetDiscordChat(SocketGuild? guild, MergeRequest newMergeRequest)
        {
            return guild.GetTextChannel(newMergeRequest.ChannelId);
        }

        protected override async Task TryAddUserToChatAsync(ulong chatId, SocketGuild? guild, ulong userId)
        {
            if (guild.GetTextChannel(chatId).Users.All(x => x.Id != userId))
            {
                var user = guild.GetUser(userId);
                var channel = guild.GetTextChannel(chatId);
                await channel.AddPermissionOverwriteAsync(
                    guild.GetUser(user.Id),
                    new OverwritePermissions(viewChannel: PermValue.Allow));
            }
        }

        public Task Edit(HandleData data, Note note)
        {
            throw new NotImplementedException();
        }
    }
}
