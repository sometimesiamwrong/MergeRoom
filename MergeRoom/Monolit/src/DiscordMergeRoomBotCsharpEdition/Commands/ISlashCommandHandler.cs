using Discord.WebSocket;

namespace DiscordMergeRoomBotCsharpEdition.Commands
{
    public interface ISlashCommandHandler
    {
        string Name { get; }

        Task HandleCommand(SocketSlashCommand command, string[] args);
    }
}
