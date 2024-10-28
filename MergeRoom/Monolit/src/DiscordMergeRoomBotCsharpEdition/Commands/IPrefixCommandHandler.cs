using Discord.Commands;

namespace DiscordMergeRoomBotCsharpEdition.Commands
{
    public interface IPrefixCommandHandler
    {
        string Name { get; }

        Task HandleCommand(SocketCommandContext context, string[] args);
    }
}
