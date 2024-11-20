using MergeRoom.Domain.Entities;
using MergeRoom.Parsing.GitlabParsing;

namespace MergeRoom.ChangesService.Workers
{
    public class Worker : IWorker
    {
        private readonly GitLabParsingService _gitLabParsingService;

        public Worker(GitLabParsingService gitLabParsingService)
        {
            _gitLabParsingService = gitLabParsingService;
        }

        public async Task ExecuteAsync(List<Project> projects)
        {
            await _gitLabParsingService.ParseProjects(projects);
        }
    }
}
