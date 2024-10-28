using DiscordMergeRoomBotCsharpEdition.Entities;
using MongoDB.Bson;

namespace DiscordMergeRoomBotCsharpEdition.Webhooks
{
    public interface IEventHook
    {
        string Name { get; init; }

        Task Parse(Project project, BsonDocument body);
    }
}
