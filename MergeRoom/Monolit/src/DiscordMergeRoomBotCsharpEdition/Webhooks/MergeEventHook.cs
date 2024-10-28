using DiscordMergeRoomBotCsharpEdition.Configs;
using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;
using DiscordMergeRoomBotCsharpEdition.MongoDB;
using DiscordMergeRoomBotCsharpEdition.Services;
using MongoDB.Bson;

namespace DiscordMergeRoomBotCsharpEdition.Webhooks
{
    public class MergeEventHook : IEventHook
    {
        private readonly ChangeHandlerService _changeHandlerService;
        private readonly DiscordBotConfiguration _configuration;
        private readonly IMongoRepository _mongoRepository;

        public MergeEventHook(
            DiscordBotConfiguration configuration,
            IMongoRepository mongoRepository,
            ChangeHandlerService changeHandlerService)
        {
            _configuration = configuration;
            _mongoRepository = mongoRepository;
            _changeHandlerService = changeHandlerService;
            Name = configuration.PossibleObjectKinds.MergeRequest;
        }

        public string Name { get; init; }

        public async Task Parse(Project project, BsonDocument body)
        {
            var objectBody = body["object_attributes"];
            var oldMergeRequest = await _mongoRepository.GetAsync<MergeRequest>(
                x => x.GitlabId == (uint)objectBody["id"].AsInt()
                     && x.ProjectId == project.GitlabId);

            var mergeRequest = new MergeRequest(
                (uint)objectBody["id"].AsInt(),
                (uint)objectBody["iid"].AsInt(),
                project.GitlabId,
                objectBody["title"].AsNullableString(),
                objectBody["description"].AsNullableString(),
                objectBody["state"].AsNullableString(),
                objectBody["created_at"].AsNullableString().ToBsonDate(),
                objectBody["updated_at"].AsNullableString().ToBsonDate(),
                (uint)objectBody["author_id"].AsInt(),
                objectBody["assignee_ids"].AsBsonArray.Select(x => (uint)x.AsInt()).ToList(),
                objectBody["reviewers"].AsBsonArray.Select(x => (uint)x.AsInt()).ToList(),
                objectBody["draft"].AsBoolean,
                objectBody["merge_status"].AsNullableString(),
                objectBody["detailed_merge_status"].AsNullableString(),
                objectBody["url"].AsNullableString(),
                objectBody["has_conflicts"].AsBoolean);

            if (oldMergeRequest is null)
            {
                await _mongoRepository.AddAsync(mergeRequest);
            }
            else
            {
                mergeRequest.ApprovesInfo = GetChangeApproves(oldMergeRequest, body);
                mergeRequest.Id = oldMergeRequest.Id;
                mergeRequest.ChannelId = oldMergeRequest.ChannelId;
                mergeRequest.IsClosed = oldMergeRequest.IsClosed;
                await _mongoRepository.UpdateAsync(mergeRequest);
            }

            await _changeHandlerService.HandleMergeRequestChanges(
                mergeRequest,
                new List<Note>(),
                project,
                oldMergeRequest: oldMergeRequest,
                eventKind: _configuration.PossibleObjectKinds.MergeRequest);
        }

        private ApproveInfo GetChangeApproves(MergeRequest mergeRequest, BsonValue body)
        {
            var objectBody = body["object_attributes"];
            var approves = new ApproveInfo
            {
                UserIds = new List<uint>(),
            };

            if (mergeRequest.ApprovesInfo?.UserIds != null)
            {
                approves.UserIds.AddRange(mergeRequest.ApprovesInfo.UserIds);

                var userGitLabId = (uint)body["user"]["id"].AsInt();

                switch (objectBody["action"].AsNullableString())
                {
                    case "approved":
                    {
                        approves.UserIds.Add(userGitLabId);
                        approves.UserIds = approves.UserIds.Distinct().ToList();
                        break;
                    }
                    case "unapproved":
                    {
                        approves.UserIds.ToList().Remove(userGitLabId);
                        break;
                    }
                }
            }

            return approves;
        }
    }
}
