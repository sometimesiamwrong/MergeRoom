using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;

namespace DiscordMergeRoomBotCsharpEdition.Services.PusherServices
{
    public interface IPusherService
    {
        string Name { get; }

        Task Execute(HandleData data, BaseEntity entity, ExecuteActionTypes action);
    }
}
