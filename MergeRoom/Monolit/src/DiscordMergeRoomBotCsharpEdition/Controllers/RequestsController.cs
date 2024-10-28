using DiscordMergeRoomBotCsharpEdition.Configs;
using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.MongoDB;
using DiscordMergeRoomBotCsharpEdition.Services;
using DiscordMergeRoomBotCsharpEdition.Webhooks;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DiscordMergeRoomBotCsharpEdition.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RequestsController : ControllerBase
    {
        private readonly DiscordBotConfiguration _discordBotConfiguration;
        private readonly IEnumerable<IEventHook> _eventHooks;
        private readonly IMongoRepository _mongoRepository;

        public RequestsController(
            IEnumerable<IEventHook> eventHooks,
            DiscordBotConfiguration discordBotConfiguration,
            IMongoRepository mongoRepository)
        {
            _discordBotConfiguration = discordBotConfiguration;
            _mongoRepository = mongoRepository;
            _eventHooks = eventHooks;
        }

        [HttpPost]
        public async Task<IActionResult> ParsePoint([FromBody] JsonElement request)
        {
            var json = request.ToBd();

            json.TryGetValue("object_kind", out var objectKind);

            var objectKindName = objectKind.ToString();

            if (_discordBotConfiguration.PossibleObjectKinds.AllKinds.All(x => x != objectKindName))
            {
                return Ok($"Object kind not found '{objectKind}'");
            }

            var projectId = json["project"]["id"].AsInt();
            var project = await _mongoRepository.GetAsync<Project>(x => x.GitlabId == projectId!);

            if (project == null)
            {
                return NotFound("Project not found!");
            }

            await _eventHooks.First(e => e.Name == objectKindName).Parse(project, json);

            return Ok("WebHook parsed");
        }
    }
}
