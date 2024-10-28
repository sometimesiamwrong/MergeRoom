using DiscordMergeRoomBotCsharpEdition.Entities;

namespace DiscordMergeRoomBotCsharpEdition.GitlabParsing.Workers
{
    public interface IWorker
    {
        Task ExecuteAsync(List<Project> project);
    }
}
