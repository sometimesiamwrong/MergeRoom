using DiscordMergeRoomBotCsharpEdition.Configs;
using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;
using DiscordMergeRoomBotCsharpEdition.MongoDB;
using DiscordMergeRoomBotCsharpEdition.Services;
using MongoDB.Bson;

namespace DiscordMergeRoomBotCsharpEdition.Webhooks
{
    public class NoteEventHook : IEventHook
    {
        private readonly ChangeHandlerService _changeHandlerService;
        private readonly DiscordBotConfiguration _configuration;
        private readonly IMongoRepository _mongoRepository;

        public NoteEventHook(
            IMongoRepository mongoRepository,
            DiscordBotConfiguration configuration,
            ChangeHandlerService changeHandlerService)
        {
            _mongoRepository = mongoRepository;
            _configuration = configuration;
            _changeHandlerService = changeHandlerService;
            Name = configuration.PossibleObjectKinds.Note;
        }

        public string Name { get; init; }

        public async Task Parse(Project project, BsonDocument body)
        {
            var mergeRequest = await _mongoRepository.GetAsync<MergeRequest>(
                x => x.GitlabId == (uint)body["merge_request"]["id"].AsInt()
                     && x.ProjectId == project.GitlabId);

            if (mergeRequest == null)
            {
                return;
            }

            var objectBody = body["object_attributes"];

            var note = new Note(
                (uint)objectBody["id"].AsInt(),
                (uint)objectBody["author_id"].AsInt(),
                objectBody["note"].AsNullableString(),
                objectBody["url"].AsNullableString(),
                objectBody["created_at"].AsNullableString().ToBsonDate(),
                objectBody["updated_at"].AsNullableString().ToBsonDate(),
                objectBody["system"].AsBoolean);

            await _changeHandlerService.HandleMergeRequestChanges(
                mergeRequest,
                new List<Note> { note },
                project,
                oldMergeRequest: null,
                eventKind: _configuration.PossibleObjectKinds.Note);
        }
    }
}
