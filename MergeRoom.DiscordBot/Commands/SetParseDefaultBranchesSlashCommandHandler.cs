using Discord.WebSocket;
using MergeRoom.Domain.Entities;

namespace MergeRoom.DiscordBot.Commands
{
    public class SetParseDefaultBranchesSlashCommandHandler : ISlashCommandHandler
    {
        public string Name { get; } = "set_parse_default_branches";

        private readonly IMongoRepository _mongoRepository;

        public SetParseDefaultBranchesSlashCommandHandler(IMongoRepository mongoRepository)
        {
            _mongoRepository = mongoRepository;
        }

        public async Task HandleCommand(SocketSlashCommand command, string[] args)
        {
            var boolOption = command.Data.Options.FirstOrDefault(o => o.Name == "bool_value");
            var projectIdOption = command.Data.Options.FirstOrDefault(o => o.Name == "project_id");

            if (boolOption == null || projectIdOption == null)
            {
                await command.RespondAsync("Missing required options.");
                return;
            }

            var boolValue = boolOption.Value as bool?;
            var projectId = projectIdOption.Value as long?;

            var project = command.GuildId.HasValue ? await _mongoRepository.GetAsync<Project>(p => p.GuildId == command.GuildId.Value && p.GitlabId == projectId) : null;

            if (project is null)
            {
                await command.RespondAsync("Project are not registered in the system.");
                return;
            }

            project.IsNeedParseDefaultBranches = boolValue.Value;

            await _mongoRepository.UpdateAsync(project);
        }
    }
}
