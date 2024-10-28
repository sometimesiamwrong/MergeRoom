using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.Services;

namespace DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling
{
    public static class HandleActionMergeRequest
    {
        public static Dictionary<BaseEntity, ExecuteActionTypes> Handle(HandleData data, DiscordBotConfiguration configuration)
        {
            var entityActions = new Dictionary<BaseEntity, ExecuteActionTypes>();

            var open = CheckToOpen(data, configuration);
            var close = CheckToClose(data);
            var update = ExecuteActionTypes.MrEdit;

            if (close != ExecuteActionTypes.Unknown)
            {
                open = ExecuteActionTypes.Unknown;
                update = ExecuteActionTypes.Unknown;
                if (data.OldMergeRequest is null)
                {
                    close = ExecuteActionTypes.Unknown;
                }
            }
            else if (open != ExecuteActionTypes.Unknown)
            {
                update = ExecuteActionTypes.Unknown;
            }

            var actions = new List<ExecuteActionTypes>
                { open, close, update };


            var action = actions.FirstOrDefault(x => x != ExecuteActionTypes.Unknown);
            entityActions.Add(data.NewMergeRequest, action);

            return entityActions;
        }

        private static ExecuteActionTypes CheckToClose(HandleData data)
        {
            if (data.NewMergeRequest.State == "closed" || data.NewMergeRequest.State == "merged")
            {
                return ExecuteActionTypes.MrMergeOrClosed;
            }

            return ExecuteActionTypes.Unknown;
        }

        private static ExecuteActionTypes CheckToOpen(HandleData data, DiscordBotConfiguration configuration)
        {
            if (data.OldMergeRequest is null)
            {
                return ExecuteActionTypes.MrOpen;
            }

            if (data.Project.PusherKind == configuration.PossiblePusherKinds.Thread && data.NewMergeRequest.ThreadId == 0
                ||
                data.Project.PusherKind == configuration.PossiblePusherKinds.Channel && data.NewMergeRequest.ChannelId == 0)
            {
                return ExecuteActionTypes.MrOpen;
            }

            if (data.OldMergeRequest.State == "merged" ||
                data.OldMergeRequest.State == "closed" &&
                data.NewMergeRequest.State == "opened")
            {
                return ExecuteActionTypes.MrOpen;
            }

            return ExecuteActionTypes.Unknown;
        }
    }
}
