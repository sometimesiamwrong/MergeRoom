namespace MergeRoom.DiscordBot.PusherServices.ThreadServices
{
    public class ThreadService : IPusherService
    {
        private readonly ILogger<ThreadService> _logger;
        private readonly ThreadMergeRequestService _threadMergeRequestService;
        private readonly ThreadNoteService _threadNoteService;

        public ThreadService(
            DiscordBotConfiguration configuration,
            ThreadMergeRequestService threadMergeRequestService,
            ThreadNoteService threadNoteService,
            ILogger<ThreadService> logger)
        {
            _threadMergeRequestService = threadMergeRequestService;
            _threadNoteService = threadNoteService;
            _logger = logger;
            Name = configuration.PossiblePusherKinds.Thread;
        }

        public string Name { get; init; }

        public async Task Execute(HandleData data, BaseEntity entity, ExecuteActionTypes action)
        {
            switch (action)
            {
                case ExecuteActionTypes.MrEdit:
                    try
                    {
                        await _threadMergeRequestService.Edit(data, (entity as MergeRequest)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error editing merge request chat: {AdditionalLogInfo(data)} {ex}");
                    }

                    break;
                case ExecuteActionTypes.MrOpen:
                    try
                    {
                        await _threadMergeRequestService.Open(data, (entity as MergeRequest)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error opening merge request chat: {AdditionalLogInfo(data)} {ex}");
                    }

                    break;
                case ExecuteActionTypes.MrMergeOrClosed:
                    try
                    {
                        await _threadMergeRequestService.Close(data, (entity as MergeRequest)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error closing merge request chat: {AdditionalLogInfo(data)} {ex}");
                    }

                    break;

                case ExecuteActionTypes.NoteEdit:
                    try
                    {
                        await _threadNoteService.Edit(data, (entity as Note)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error editing note in thread: {AdditionalLogInfo(data)} {ex}");
                    }

                    break;
                case ExecuteActionTypes.NoteNew:
                    try
                    {
                        await _threadNoteService.Send(data, (entity as Note)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error send note to thread: {AdditionalLogInfo(data)} {ex}");
                    }

                    break;
                case ExecuteActionTypes.NoteDelete:
                    try
                    {
                        await _threadNoteService.Delete(data, (entity as Note)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error delete note in thread: {AdditionalLogInfo(data)} {ex}");
                    }

                    break;
            }
        }

        private string AdditionalLogInfo(HandleData data)
        {
            return $"\n(Project :{data.Project.Namespace}/{data.Project.ProjectName}" +
                   $"\nMr {data?.NewMergeRequest?.Title})";
        }
    }
}
