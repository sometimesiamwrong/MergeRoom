using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.Entities.AdditionalEntityes;
using DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;
using DiscordMergeRoomBotCsharpEdition.MongoDB;
using DiscordMergeRoomBotCsharpEdition.Services;
using MongoDB.Bson;
using System.Text;
using System.Text.RegularExpressions;

namespace DiscordMergeRoomBotCsharpEdition.GitlabParsing
{
    public class GitLabParsingService
    {
        private readonly GitLabService _gitLabService;
        private readonly ILogger<GitLabParsingService> _logger;
        private readonly IMongoRepository _mongoRepository;
        private readonly IServiceProvider _serviceProvider;

        private Dictionary<string, object> _mrNewestQuery = new()
        {
            ["order_by"] = "updated_at",
            ["sort"] = "desc",
            ["updated_after"] = null!,
        };

        private Dictionary<string, object> _noteQuery = new()
        {
            ["order_by"] = "updated_at",
            ["sort"] = "desc",
            ["updated_after"] = null!,
            ["page"] = 0,
            ["per_page"] = 30,
        };

        private Dictionary<string, object> _onlyOpened = new()
        {
            ["order_by"] = "updated_at",
            ["sort"] = "desc",
            ["state"] = "opened",
        };

        public GitLabParsingService(
            GitLabService gitLabService,
            IMongoRepository mongoRepository,
            IServiceProvider serviceProvider,
            ILogger<GitLabParsingService> logger)
        {
            _gitLabService = gitLabService;
            _mongoRepository = mongoRepository;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

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

            openedUpdatedMergeRequests.Sort((x, y) => y.GitlabIId >= x.GitlabIId ? -1 : 1);
            updatedMergeRequests.Sort((x, y) => y.GitlabIId >= x.GitlabIId ? -1 : 1);

            updatedMergeRequests = updatedMergeRequests.Where(x => openedUpdatedMergeRequests.All(y => x.GitlabIId != y.GitlabIId)).ToList();

            var jobsMrTasks = new List<Task<List<Job>>>();

            foreach (var openedMergeRequest in openedMergeRequests)
            {
                if (openedMergeRequest.HeadPipeline.HasValue)
                {
                    jobsMrTasks.Add(_gitLabService.GetJobsByPipeline(project, openedMergeRequest.HeadPipeline.Value, new Dictionary<string, object>()
                    {
                        ["scope"] = "failed"
                    }));
                }
            }

            if (openedUpdatedMergeRequests.Any())
            {
                var notesMrTasks = new List<Task<List<Note>>>();
                var approveMrTasks = new List<Task<ApproveInfo>>();

                foreach (var mergeRequest in openedUpdatedMergeRequests)
                {
                    notesMrTasks.Add(_gitLabService.GetNotesByMergeRequests(project, mergeRequest.GitlabIId, _noteQuery));
                    approveMrTasks.Add(_gitLabService.GetApprovesByMergeRequest(project, mergeRequest.GitlabIId));
                }

                await Task.WhenAll(notesMrTasks);
                await Task.WhenAll(approveMrTasks);
                await Task.WhenAll(jobsMrTasks);

                var notesMr = notesMrTasks.Select(x => x.Result).ToList();
                var approvesMr = approveMrTasks.Select(x => x.Result).ToList();
                var jobsMr = jobsMrTasks.Select(x => x.Result).ToList();

                for (var i = 0; i < openedUpdatedMergeRequests.Count; i++)
                {
                    var mr = openedUpdatedMergeRequests[i];
                    await SyncDataForMergeRequest(mr, project);
                    var additionalNotes = new List<Note>();

                    foreach (var jobList in jobsMr.Where(x => x.Any(y => y.MergeRequestIId == mr.GitlabIId)))
                    {
                        var filteredJobs = FilterJobsByParsingTime(jobList, projectParsedAt);
                        additionalNotes.AddRange(filteredJobs.Select(ConvertJobToNote));
                    }

                    mr.AdditionalUsers.AddRange(await GetUserIdsFromDescription(mr.Description, project));

                    var notes = FilterNotesByParsingTime(notesMr[i], projectParsedAt);

                    var texts = new List<(Task<(string?, string?)> task, int index)>();
                    for (int j = 0; j < notes.Count; j++)
                    {
                        if (!notes[j].IsSystem)
                        {
                            if (notes[j] is DiffNote diffNote)
                            {
                                if (mr.DiffNoteHashes.TryAdd(diffNote.Position.GetHashCode().ToString(), (long)notes[j].GitlabId))
                                {
                                    _logger.LogInformation($"New diff note added to MR (Hash {diffNote.Position.GetHashCode()}, Id {notes[j].GitlabId})");
                                    texts.Add((GetFilesContentFromDiffNote(mr, diffNote, project), j));
                                }
                                else
                                {
                                    _logger.LogInformation($"Comment to existed diffNote (Hash {diffNote.Position.GetHashCode()}, Id {notes[j].GitlabId})");
                                }
                            }
                            mr.AdditionalUsers.AddRange(await GetUserIdsFromDescription(notes[j].Description, project));
                        }
                    }

                    await Task.WhenAll(texts.Select(x => x.task));

                    foreach (var tuple in texts)
                    {
                        var diffNote = notes[tuple.index] as DiffNote;
                        diffNote.CommentTexts = tuple.task.Result;
                        diffNote.CodeArea = GetCodeAreaFromDiffPosition(diffNote);
                    }

                    notes.AddRange(additionalNotes);

                    mr.ApprovesInfo = approvesMr[i];

                    await ParseNotes(
                        mr,
                        notes,
                        project);
                }
            }

            foreach (var mr in openedMergeRequests
                .Where(x => openedUpdatedMergeRequests.All(y => y.GitlabIId != x.GitlabIId)))
            {
                await SyncDataForMergeRequest(mr, project);
                await Task.WhenAll(jobsMrTasks);
                var jobsMr = jobsMrTasks.Select(x => x.Result).ToList();
                var additionalNotes = new List<Note>();
                foreach (var jobList in jobsMr.Where(x => x.Any(y => y.MergeRequestIId == mr.GitlabIId)))
                {
                    var filteredJobs = FilterJobsByParsingTime(jobList, projectParsedAt);
                    additionalNotes.AddRange(filteredJobs.Select(ConvertJobToNote));
                }

                mr.AdditionalUsers.AddRange(await GetUserIdsFromDescription(mr.Description, project));
                await ParseNotes(
                    mr,
                    additionalNotes,
                    project);
            }

            foreach (var mr in updatedMergeRequests)
            {
                await SyncDataForMergeRequest(mr, project);
                await ParseNotes(
                    mr,
                    new List<Note>(),
                    project);
            }
        }

        private string GetCodeAreaFromDiffPosition(DiffNote diffNote)
        {
            var oldTextRows = new List<ChangeRow>();
            var newTextRows = new List<ChangeRow>();

            if (diffNote.CommentTexts.oldText is not null)
            {
                oldTextRows = GetSplitTextWithNumber(
                    diffNote.CommentTexts.oldText,
                    new Range(
                        diffNote.Position.Start.Range.OldNumber,
                        diffNote.Position.End.Range.OldNumber + 1));
            }
            if (diffNote.CommentTexts.newText is not null)
            {
                newTextRows = GetSplitTextWithNumber(
                    diffNote.CommentTexts.newText, new Range(
                        diffNote.Position.Start.Range.NewNumber,
                        diffNote.Position.End.Range.NewNumber + 1));
            }

            if (!oldTextRows.Any() && !newTextRows.Any())
            {
                return null;
            }

            var numberArea = new List<string>();
            var textArea = new List<string>();

            var fixedOldIx = 0;
            var fixedNewIx = 0;
            var currentOldIx = 0;
            var currentNewIx = 0;

            if (!oldTextRows.Any())
            {
                AddNewChangesFromText(0, newTextRows.Count);
                return GetResultCodeArea();
            }

            if (!newTextRows.Any())
            {
                AddDeletedChangesFromText(0, oldTextRows.Count);
                return GetResultCodeArea();
            }

            while (fixedOldIx < oldTextRows.Count || fixedNewIx < newTextRows.Count)
            {
                var isSyncRowFound = SetSyncRowIndexes(ref currentOldIx, ref currentNewIx);

                AddDeletedChangesFromText(fixedOldIx, currentOldIx);
                AddNewChangesFromText(fixedNewIx, currentNewIx);

                if (isSyncRowFound)
                {
                    AddSyncRowChanges(ref currentOldIx, ref currentNewIx);
                }

                fixedOldIx = currentOldIx;
                fixedNewIx = currentNewIx;
            }

            bool SetSyncRowIndexes(ref int currentOldIx, ref int currentNewIx)
            {
                for (int i = currentOldIx; i < oldTextRows.Count; i++)
                {
                    for (int j = currentNewIx; j < newTextRows.Count; j++)
                    {
                        if (oldTextRows[i].TextHash == newTextRows[j].TextHash)
                        {
                            currentOldIx = i;
                            currentNewIx = j;
                            return true;
                        }
                    }
                }

                currentOldIx = oldTextRows.Count;
                currentNewIx = newTextRows.Count;

                return false;
            }

            void AddDeletedChangesFromText(int fixedOldIx, int currentOldIx)
            {
                for (; fixedOldIx < currentOldIx; fixedOldIx++)
                {
                    numberArea.Add($"{oldTextRows[fixedOldIx].Number.ToString(),-4} {string.Empty,-4} - ");
                    textArea.Add(oldTextRows[fixedOldIx].Text);
                }
            }

            void AddNewChangesFromText(int fixedNewIx, int currentNewIx)
            {
                for (; fixedNewIx < currentNewIx; fixedNewIx++)
                {
                    numberArea.Add($"{string.Empty,-4} {newTextRows[fixedNewIx].Number.ToString(),-4} + ");
                    textArea.Add(newTextRows[fixedNewIx].Text);
                }
            }

            void AddSyncRowChanges(ref int currentOldIx, ref int currentNewIx)
            {
                numberArea.Add($"{oldTextRows[currentOldIx].Number.ToString(),-4} {newTextRows[currentNewIx].Number.ToString(),-4} \0 ");
                textArea.Add(newTextRows[currentNewIx].Text);
                currentOldIx++;
                currentNewIx++;
            }

            string GetResultCodeArea()
            {
                var codeAreaBuilder = new StringBuilder();
                textArea = RemoveLeadingSpaces(textArea);
                for (int i = 0; i < numberArea.Count; i++)
                {
                    codeAreaBuilder.AppendLine($"{numberArea[i]} {textArea[i]}");
                }
                
                return codeAreaBuilder.ToString();
            }
            
            List<string> RemoveLeadingSpaces(List<string> lines)
            {
                // Найти минимальное количество ведущих пробелов
                int minLeadingSpaces = lines
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.TakeWhile(char.IsWhiteSpace).Count())
                    .DefaultIfEmpty(0)
                    .Min();

                // Удалить минимальное количество ведущих пробелов из каждой строки
                return lines.Select(line => line.Length >= minLeadingSpaces ? line.Substring(minLeadingSpaces) : line).ToList();
            }

            return GetResultCodeArea();
        }

        private List<ChangeRow> GetSplitTextWithNumber(string text, Range range)
        {
            var textRows = text.Split("\n")[range];
            var counter = range.Start.Value + 1;
            var changeRows = new List<ChangeRow>();
            foreach (var row in textRows)
            {
                changeRows.Add(new ChangeRow()
                {
                    Text = row,
                    Number = counter,
                });
                counter++;
            }
            return changeRows;
        }

        private async Task ParseNotes(
            MergeRequest newMergeRequest,
            List<Note> notes,
            Project project)
        {
            var oldMergeRequest = await _mongoRepository.GetAsync<MergeRequest>(
                x => x.GitlabId == newMergeRequest.GitlabId
                     && x.ProjectId == project.GitlabId);

            await _mongoRepository.UpdateAsync(newMergeRequest);

            using (var scope = _serviceProvider.CreateScope())
            {
                var changeHandlerService = scope.ServiceProvider.GetRequiredService<ChangeHandlerService>();
                await changeHandlerService.HandleMergeRequestChanges(newMergeRequest, notes, project, oldMergeRequest);
            }
        }

        private async Task SyncDataForMergeRequest(MergeRequest newMergeRequest, Project project)
        {
            var oldMergeRequest = await _mongoRepository.GetAsync<MergeRequest>(
                x => x.GitlabId == newMergeRequest.GitlabId
                     && x.ProjectId == project.GitlabId);

            if (oldMergeRequest is null)
            {
                if (newMergeRequest.State is not "opened")
                {
                    return;
                }

                if (!newMergeRequest.AssigneeIds.Any())
                {
                    await AddDefaultAssignee(newMergeRequest, project);
                    _logger.LogInformation($"Add assignee to {newMergeRequest.Title}\nProject {project.Namespace}/{project.ProjectName} (ID {project.Id})");
                }

                await _mongoRepository.AddAsync(newMergeRequest);
            }
            else
            {
                newMergeRequest.Id = oldMergeRequest.Id;
                newMergeRequest.ChannelId = oldMergeRequest.ChannelId;
                newMergeRequest.ThreadId = oldMergeRequest.ThreadId;
                newMergeRequest.IsClosed = oldMergeRequest.IsClosed;
                newMergeRequest.ApprovesInfo ??= oldMergeRequest?.ApprovesInfo;
                newMergeRequest.DiffNoteHashes = oldMergeRequest.DiffNoteHashes;

                await _mongoRepository.UpdateAsync(newMergeRequest);
            }
        }

        private Task AddDefaultAssignee(MergeRequest newMergeRequest, Project project)
        {
            return _gitLabService.SetAssignee(project, newMergeRequest.GitlabIId, newMergeRequest.AuthorId.ToString());
        }

        private List<Note> FilterNotesByParsingTime(List<Note> notes, BsonDateTime projectLastUpdate)
        {
            if (!notes.Any())
            {
                return new List<Note>();
            }

            var filteredNotes = notes
                .Where(x => x.CreatedAt.MillisecondsSinceEpoch > projectLastUpdate.MillisecondsSinceEpoch) // Сортируем по UpdatedAt
                .OrderBy(x => x.CreatedAt)
                .ToList();
            return filteredNotes;
        }

        private List<Job> FilterJobsByParsingTime(List<Job> jobs, BsonDateTime projectLastUpdate)
        {
            if (!jobs.Any())
            {
                return new List<Job>();
            }

            var filteredJobs = jobs
                .Where(x => x.FinishedAt.MillisecondsSinceEpoch > projectLastUpdate.MillisecondsSinceEpoch) // Сортируем по FinishedAt
                .OrderBy(x => x.FinishedAt)
                .ToList();
            return filteredJobs;
        }

        private Note ConvertJobToNote(Job job)
        {
            return new Note(
                0,
                job.AuthorId,
                $"JobToNoteConverted {job.Stage}/{job.Name}",
                null,
                job.CreatedAt,
                job.FinishedAt != null ? job.FinishedAt.Value : BsonDateTime.Create(DateTimeOffset.UtcNow),
                true,
                data: job);
        }

        private async Task<List<uint>> GetUserIdsFromDescription(string description, Project project)
        {
            var users = GetUsersFromDescription(description);
            var userIds = new List<uint>();

            foreach (var user in users)
            {
                var userEntity = await _gitLabService.GetUserByUserName(project, user);

                if (userEntity is not null)
                {
                    userIds.Add(userEntity.GitlabId);
                }
            }

            return userIds;
        }

        private List<string> GetUsersFromDescription(string description)
        {
            var regex = @"@([a-zA-Z0-9._-]+)";

            var matches = Regex.Matches(description, regex);

            if (matches.Count > 0)
            {
                return matches.Select(x => x.Groups[1].Value).ToList();
            }

            return new List<string>();
        }

        private async Task<(string? oldPath, string? newPath)> GetFilesContentFromDiffNote(MergeRequest mergeRequest, DiffNote note, Project project)
        {
            var startRange = note.Position.Start.Range;
            var endRange = note.Position.End.Range;

            try
            {
                if (note.Position.Start.Type == "new" &&
                    note.Position.End.Type == "new" &&
                    startRange.OldNumber == endRange.OldNumber &&
                    note.Position.Start.OldLine == null &&
                    note.Position.End.OldLine == null)
                {
                    return (null, await _gitLabService.GetRawFileByBranchName(project, note.Position.NewPath, note.Position.HeadSha));
                }

                if (note.Position.Start.Type == "old" &&
                    note.Position.End.Type == "old" &&
                    startRange.NewNumber == endRange.NewNumber &&
                    note.Position.Start.NewLine == null &&
                    note.Position.End.NewLine == null)
                {
                    return (await _gitLabService.GetRawFileByBranchName(project, note.Position.OldPath, note.Position.BaseSha), null);
                }

                return (oldPath: await _gitLabService.GetRawFileByBranchName(project, note.Position.OldPath, note.Position.BaseSha),
                    newPath: await _gitLabService.GetRawFileByBranchName(project, note.Position.NewPath, note.Position.HeadSha));
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Cannot get raw from file {project.Namespace}/{project.ProjectName} {mergeRequest.Title} (IID: {mergeRequest.GitlabIId}) {e.Message}");
                return (null, null);
            }
        }
    }
}
