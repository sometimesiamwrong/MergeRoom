using Discord.WebSocket;

namespace MergeRoom.DiscordBot.Commands
{
    public interface ISlashCommandHandler
    {
        string Name { get; }

        Task HandleCommand(SocketSlashCommand command, string[] args);
    }
}
