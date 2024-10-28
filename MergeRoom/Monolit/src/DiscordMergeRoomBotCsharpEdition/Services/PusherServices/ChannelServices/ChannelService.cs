using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;

namespace DiscordMergeRoomBotCsharpEdition.Services.PusherServices.ChannelServices
{
    public class ChannelService : IPusherService
    {
        private readonly ChannelMergeRequestService _channelMergeRequestService;
        private readonly ChannelNoteService _channelNoteService;
        private readonly ILogger<ChannelService> _logger;

        public ChannelService(
            DiscordBotConfiguration configuration,
            ChannelMergeRequestService channelMergeRequestService,
            ChannelNoteService channelNoteService,
            ILogger<ChannelService> logger)
        {
            _channelMergeRequestService = channelMergeRequestService;
            _channelNoteService = channelNoteService;
            _logger = logger;
            Name = configuration.PossiblePusherKinds.Channel;
        }

        public string Name { get; init; }

        public async Task Execute(HandleData data, BaseEntity entity, ExecuteActionTypes action)
        {
            switch (action)
            {
                case ExecuteActionTypes.MrEdit:
                    try
                    {
                        await _channelMergeRequestService.Edit(data, (entity as MergeRequest)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error editing merge request chat: {ex}");
                    }

                    break;
                case ExecuteActionTypes.MrOpen:
                    try
                    {
                        await _channelMergeRequestService.Open(data, (entity as MergeRequest)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error opening merge request chat: {ex}");
                    }

                    break;
                case ExecuteActionTypes.MrMergeOrClosed:
                    try
                    {
                        await _channelMergeRequestService.Close(data, (entity as MergeRequest)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error closing merge request chat: {ex}");
                    }

                    break;

                case ExecuteActionTypes.NoteEdit:
                    try
                    {
                        await _channelNoteService.Edit(data, (entity as Note)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error editing note chat: {ex}");
                    }

                    break;
                case ExecuteActionTypes.NoteNew:
                    try
                    {
                        await _channelNoteService.Send(data, (entity as Note)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error opening note chat: {ex}");
                    }

                    break;
                case ExecuteActionTypes.NoteDelete:
                    try
                    {
                        await _channelNoteService.Delete(data, (entity as Note)!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error closing note chat: {ex}");
                    }

                    break;
            }
        }
    }
}
