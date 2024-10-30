using Discord.Commands;

namespace MergeRoom.DiscordBot.Commands
{
    public interface IPrefixCommandHandler
    {
        string Name { get; }

        Task HandleCommand(SocketCommandContext context, string[] args);
    }
}
