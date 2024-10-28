using DiscordMergeRoomBotCsharpEdition.Entities;

namespace DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling
{
    public interface IHandleAction
    {
        Dictionary<BaseEntity, ExecuteActionTypes> Handle(HandleData data);
    }
}
