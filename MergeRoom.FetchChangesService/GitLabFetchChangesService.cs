using MergeRoom.Domain.Entities;
using MergeRoom.Domain.Entities.GitlabEntities;
using MergeRoom.Parsing.Handling;
using MongoDB.Bson;

namespace MergeRoom.ChangesService
{
    public class GitLabFetchChangesService
    {
        public async Task ParseProjects(List<Project> projects)
        {
            foreach (var project in projects)
            {
                var projectParsedAt = project.ParsedAt;
                project.ParsedAt = BsonDateTime.Create(DateTimeOffset.UtcNow);
                try
                {
                    await ParseMergeRequests(project, projectParsedAt);
                    await ParseProject(project, projectParsedAt);
                    _logger.LogDebug($"Project {project.Namespace}/{project.ProjectName} (ID {project.Id}) parsed.");
                }
                catch (Exception e)
                {
                    project.ParsedAt = projectParsedAt;
                    _logger.LogCritical(message: $"Error parsing {project.Namespace}/{project.ProjectName}", exception: e);
                    throw;
                }
                finally
                {
                    await _mongoRepository.UpdateAsync(project);
                }
            }
        }
        
        private async Task ParseProject(Project project, BsonDateTime projectParsedAt)
        {
            if (!project.IsNeedParseDefaultBranches)
            {
                return;
            }

            var headBranchesPipelinesJobs = await _gitLabService.GetProtectedBranchesPipelinesJobs(project, new Dictionary<string, object>()
            {
                ["updated_after"] = projectParsedAt,
            });

            var failedJobNotes = new List<Note>();

            foreach (var headBranchesPipelinesJob in headBranchesPipelinesJobs)
            {
                foreach (var job in headBranchesPipelinesJob)
                {
                    if (job.Status == "failed")
                    {
                        failedJobNotes.Add(ConvertJobToNote(job));
                    }
                }
            }

            var mockOldMergeRequest = new MergeRequest();
            var mockNewMergeRequest = new MergeRequest
            {
                ThreadId = project.ChannelDiscordId,
                ChannelId = project.ChannelDiscordId,
            };

            project.PusherKind = "channel";

            using (var scope = _serviceProvider.CreateScope())
            {
                var changeHandlerService = scope.ServiceProvider.GetRequiredService<ChangeHandlerService>();
                await changeHandlerService.HandleProjectChanges(mockNewMergeRequest, failedJobNotes, project, mockOldMergeRequest);
            }

            project.PusherKind = "thread";
        }

        private async Task ParseMergeRequests(Project project, BsonDateTime projectParsedAt)
        {
            _noteQuery["updated_after"] = projectParsedAt;
            _mrNewestQuery["updated_after"] = projectParsedAt;

            var justNowDelay = BsonDateTime.Create(DateTimeOffset.UtcNow.AddMinutes(-1.01));

            var updatedMergeRequests = await _gitLabService.GetMergeRequests(project, _mrNewestQuery, withPipeline: false);
            var openedMergeRequests = await _gitLabService.GetMergeRequests(project, _onlyOpened);

            var openedUpdatedMergeRequests = updatedMergeRequests.Where(x => x.State == "opened").ToList();
            updatedMergeRequests = updatedMergeRequests
                .Where(x => openedMergeRequests.All(y => y.GitlabIId != x.GitlabIId))
                .ToList();

            var justNowUpdatedOpenedMergeRequests = openedMergeRequests
                .Where(x => openedUpdatedMergeRequests.All(y => y.GitlabIId != x.GitlabIId))
                .Where(x => x.UpdatedAt > justNowDelay)
                .ToList();

            openedUpdatedMergeRequests.AddRange(justNowUpdatedOpenedMergeRequests);

            openedUpdatedMergeRequests.Sort((x, y) => y.GitlabIId >= x.GitlabIId
                ? -1
                : 1);
            updatedMergeRequests.Sort((x, y) => y.GitlabIId >= x.GitlabIId
                ? -1
                : 1);

            updatedMergeRequests = updatedMergeRequests.Where(x => openedUpdatedMergeRequests.All(y => x.GitlabIId != y.GitlabIId)).ToList();
        }
    }
}
