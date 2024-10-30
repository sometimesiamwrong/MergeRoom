namespace MergeRoom.DiscordBot.PusherServices.ThreadServices
{
    public class ThreadNoteService : DiscordChatNoteService
    {
        public ThreadNoteService(
            DataService dataService,
            DiscordBotConfiguration configuration,
            ILogger<ThreadNoteService> logger) : base(configuration, dataService, logger)
        {
        }

        protected override SocketTextChannel GetDiscordChat(SocketGuild? guild, MergeRequest newMergeRequest)
        {
            return guild.GetThreadChannel(newMergeRequest.ThreadId);
        }

        protected override async Task TryAddUserToChatAsync(ulong chatId, SocketGuild? guild, ulong userId)
        {
            var thread = guild.GetThreadChannel(chatId);

            if (thread == null)
            {
                Logger.LogError($"Thread \"{chatId}\" not found.");
                return;
            }

            var users = await thread.GetUsersAsync();

            var user = guild.GetUser(userId);

            if (user == null)
            {
                Logger.LogError($"User not found in Guild \"{userId}\"");
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
                Logger.LogWarning(exception: e, $"{user.GlobalName}\\{user.Nickname} (ID:{user.Id}) has not access to channel or thread");
            }
        }

        public Task Edit(HandleData data, Note note)
        {
            throw new NotImplementedException();
        }

        public Task Delete(HandleData data, Note note)
        {
            throw new NotImplementedException();
        }
    }
}
