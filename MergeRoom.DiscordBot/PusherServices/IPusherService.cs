using MergeRoom.Domain.Entities;
using MergeRoom.Parsing;
using MergeRoom.Parsing.Handling;

namespace MergeRoom.DiscordBot.PusherServices
{
    public interface IPusherService
    {
        string Name { get; }

        Task Execute(HandleData data, BaseEntity entity, ExecuteActionTypes action);
    }
}
