using MergeRoom.Domain.Entities;

namespace MergeRoom.Parsing.Handling
{
    public interface IHandleAction
    {
        Dictionary<BaseEntity, ExecuteActionTypes> Handle(HandleData data);
    }
}
