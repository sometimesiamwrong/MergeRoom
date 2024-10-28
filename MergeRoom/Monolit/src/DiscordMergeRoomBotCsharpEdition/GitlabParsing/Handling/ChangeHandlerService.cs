using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities;
using DiscordMergeRoomBotCsharpEdition.Services;
using DiscordMergeRoomBotCsharpEdition.Services.PusherServices;

namespace DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling
{
    public class ChangeHandlerService
    {
        private readonly DiscordBotConfiguration _configuration;
        private readonly IEnumerable<IPusherService> _pusherService;
        private readonly ILogger<ChangeHandlerService> _logger;

        public ChangeHandlerService(
            IEnumerable<IPusherService> pusherService,
            DiscordBotConfiguration configuration,
            ILogger<ChangeHandlerService> logger)
        {
            _pusherService = pusherService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task HandleMergeRequestChanges(
            MergeRequest newMergeRequest,
            List<Note> notes,
            Project project,
            MergeRequest? oldMergeRequest = null,
            string? eventKind = null)
        {
            var data = new HandleData
            {
                OldMergeRequest = oldMergeRequest,
                NewMergeRequest = newMergeRequest,
                Notes = notes,
                Project = project,
            };
            var actions = new Dictionary<BaseEntity, ExecuteActionTypes>();

            if (eventKind is null || eventKind == _configuration.PossibleObjectKinds.MergeRequest)
            {
                foreach (var item in HandleActionMergeRequest.Handle(data, _configuration))
                {
                    actions.Add(item.Key, item.Value);
                    if (item.Value != ExecuteActionTypes.MrEdit)
                    {
                        _logger.LogInformation($"Parsing {((MergeRequest)item.Key).Title} as {item.Value} (ID {data.NewMergeRequest.GitlabIId}) in project {data.Project.Namespace}/{data.Project.ProjectName}");
                    }
                }
            }

            if (eventKind is null || eventKind == _configuration.PossibleObjectKinds.Note)
            {
                foreach (var item in HandleActionNotes.Handle(data, _configuration))
                {
                    actions.Add(item.Key, item.Value);
                    var note = item.Key as Note;
                    if (note.NoteType != NoteType.Unknown)
                    {
                        _logger.LogInformation($"Parsed {note!.Description} Gitlab AUTHOR: {note.NoteCreatorId} ACTION {item.Value} TYPE {note.NoteType} " +
                                               $"(MR: {data.NewMergeRequest.Title} ID {data.NewMergeRequest.GitlabIId}) in project {data.Project.Namespace}/{data.Project.ProjectName}");
                    }
                }
            }

            await HandleAction(data, actions);
        }

        public async Task HandleProjectChanges(MergeRequest mockNewMergeRequest, List<Note> failedJobNotes, Project project, MergeRequest mockOldMergeRequest)
        {
            var data = new HandleData
            {
                Project = project,
                NewMergeRequest = mockNewMergeRequest,
                Notes = failedJobNotes,
                OldMergeRequest = mockOldMergeRequest,
            };
            var actions = new Dictionary<BaseEntity, ExecuteActionTypes>();

            foreach (var item in HandleActionNotes.Handle(data, _configuration))
            {
                actions.Add(item.Key, item.Value);
                var note = item.Key as Note;
                if (note.NoteType != NoteType.Unknown)
                {
                    _logger.LogInformation($"Parsed {note!.Description} ACTION {item.Value} TYPE {note.NoteType} (ID {data.NewMergeRequest.GitlabId}) in project {data.Project.Namespace}/{data.Project.ProjectName}");
                }
            }

            await HandleAction(data, actions);
        }

        private async Task HandleAction(HandleData data, Dictionary<BaseEntity, ExecuteActionTypes> actions)
        {
            var pusherService = _pusherService.FirstOrDefault(x => x.Name == data.Project.PusherKind);

            if (pusherService is null)
            {
                return;
            }

            foreach (var action in actions.Where(x => x.Value != ExecuteActionTypes.Unknown))
            {
                await pusherService.Execute(data, action.Key, action.Value);
            }
        }
    }
}
