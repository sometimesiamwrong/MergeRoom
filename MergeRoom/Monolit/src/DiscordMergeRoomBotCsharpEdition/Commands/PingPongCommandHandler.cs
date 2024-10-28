using Discord;
using Discord.Commands;

namespace DiscordMergeRoomBotCsharpEdition.Commands
{
    public class PingPrefixCommandHandler : IPrefixCommandHandler
    {
        public string Name => "ping";

        public Task HandleCommand(SocketCommandContext context, string[] args)
        {
            return context.Message.ReplyAsync("Pong!");
        }
    }
}
