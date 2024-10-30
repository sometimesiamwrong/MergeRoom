using Discord;
using Discord.Net;
using Discord.WebSocket;
using MergeRoom.Domain.Entities;
using MergeRoom.Domain.Entities.GitlabEntities;
using MergeRoom.Parsing;
using MergeRoom.Parsing.Handling;
using Microsoft.Extensions.Logging;

namespace MergeRoom.DiscordBot.PusherServices
{
    public abstract class DiscordChatNoteService
    {
        private readonly DiscordBotConfiguration _configuration;
        private readonly DataService _dataService;
        private readonly Dictionary<KeyValuePair<ulong, Project>, User> _users = new Dictionary<KeyValuePair<ulong, Project>, User>();
        protected readonly ILogger<DiscordChatNoteService> Logger;

        public DiscordChatNoteService(
            DiscordBotConfiguration configuration,
            DataService dataService,
            ILogger<DiscordChatNoteService> logger)
        {
            _configuration = configuration;
            _dataService = dataService;
            Logger = logger;
        }

        public async Task Send(HandleData data, Note note)
        {
            _users.TryGetValue(new KeyValuePair<ulong, Project>(note.NoteCreatorId, data.Project), out var noteCreator);
            if (noteCreator is null)
            {
                noteCreator = await _dataService.GetUser(data.Project, note.NoteCreatorId);
                _users.Add(new KeyValuePair<ulong, Project>(note.NoteCreatorId, data.Project), noteCreator);
            }

            var guild = _dataService.GetGuild(data.Project.GuildId);
            var chat = GetDiscordChat(guild, data.NewMergeRequest);

            var embed = GetEmbed(note, noteCreator, data);

            if (embed is null)
            {
                return;
            }

            var text = GetText(note);

            try
            {
                await chat.SendMessageAsync(text: text, embed: embed);
            }
            catch (RateLimitedException ex)
            {
                Logger.LogWarning($"Rate limit hit: Retry after {ex.Request.TimeoutAt} seconds");
            }

            if (noteCreator.DiscordId.HasValue)
            {
                await TryAddUserToChatAsync(data.NewMergeRequest.ThreadId, guild, noteCreator.DiscordId.Value);
            }

            if (data.NewMergeRequest.ReviewerIds.Any())
            {
                foreach (var reviewerId in data.NewMergeRequest.ReviewerIds)
                {
                    var reviewer = await _dataService.GetUser(data.Project, reviewerId);
                    if (reviewer.DiscordId.HasValue)
                    {
                        await TryAddUserToChatAsync(data.NewMergeRequest.ThreadId, guild, reviewer.DiscordId.Value);
                    }
                }
            }

            if (data.NewMergeRequest.AdditionalUsers.Any())
            {
                foreach (var additionalUserId in data.NewMergeRequest.AdditionalUsers)
                {
                    var additionalUser = await _dataService.GetUser(data.Project, additionalUserId);
                    if (additionalUser.DiscordId.HasValue)
                    {
                        await TryAddUserToChatAsync(data.NewMergeRequest.ThreadId, guild, additionalUser.DiscordId.Value);
                    }
                }
            }
        }

        private string? GetText(Note note)
        {
            if (note.IsSystem)
            {
                switch (note.NoteType)
                {
                }
            }
            else
            {
                switch (note.NoteType)
                {
                    case NoteType.Diff:
                        return GetDiffText(note as DiffNote);
                }
            }

            return null;
        }

        private string? GetDiffText(DiffNote note)
        {
            if (note.CodeArea == null)
            {
                return null;
            }

            return $"```\n{note.Position.NewPath}\n```\n```{note.Position.NewPath.Split('.').Last()}\n{note.CodeArea}\n```";
        }

        protected abstract SocketTextChannel GetDiscordChat(SocketGuild? guild, MergeRequest newMergeRequest);

        protected abstract Task TryAddUserToChatAsync(ulong chatId, SocketGuild? guild, ulong userId);

        private Embed? GetEmbed(Note note, User noteCreator, HandleData data)
        {
            if (note.IsSystem)
            {
                switch (note.NoteType)
                {
                    case NoteType.Approve:
                        return GetEmbedApprove(Color.Green, "approved", data, note, noteCreator);
                    case NoteType.Unapprove:
                        return GetEmbedApprove(Color.Red, "unapproved", data, note, noteCreator);
                    case NoteType.ResolvedAllThreads:
                        return GetEmbedBlockingDiscussionsResolved(data, note);
                    case NoteType.JobFailed:
                        return GetFailedJobEmbed(note, noteCreator, data.Project);
                }
            }
            else
            {
                switch (note.NoteType)
                {
                    case NoteType.Basic:
                    case NoteType.Diff:
                        return GetBasicEmbed(note, noteCreator, data.Project);
                }
            }

            return null;
        }

        private Embed? GetFailedJobEmbed(Note note, User noteCreator, Project project)
        {
            var job = note.Data as Job;

            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithAuthor(noteCreator.Name, noteCreator.AvatarUrl, noteCreator.WebUrl)
                .WithTitle($"Job failed in {job.Ref}")
                .WithDescription($"Name: {job!.Stage}/{job.Name}\n[Job link]({job.WebUrl})\n[Pipeline link]({job.PipelineWebUrl})")
                .AddField("Project", $"[{project.ProjectName}]({project.GitLabLink})", true)
                .WithTimestamp(note.UpdatedAt.ToLocalTime())
                .Build();
        }

        private Embed? GetEmbedBlockingDiscussionsResolved(HandleData data, Note note)
        {
            return new EmbedBuilder()
                .WithColor(Color.Green)
                .WithDescription($"## All blocking discussions resolved\n[Note link]({note.WebUrl})")
                .AddField("Project", $"[{data.Project.ProjectName}]({data.Project.GitLabLink})", true)
                .WithTimestamp(note.UpdatedAt.ToLocalTime())
                .Build();
        }

        private Embed? GetEmbedApprove(Color color, string eventType, HandleData data, Note note, User user)
        {
            return new EmbedBuilder()
                .WithColor(color)
                .WithAuthor(user.Name, user.AvatarUrl, user.WebUrl)
                .WithDescription($"## {user.Name} {eventType} (Count {data.NewMergeRequest.ApprovesInfo?.UserIds.Count ?? 0})\n[Note link]({note.WebUrl})")
                .AddField("Project", $"[{data.Project.ProjectName}]({data.Project.GitLabLink})", true)
                .WithTimestamp(note.UpdatedAt.ToLocalTime())
                .Build();
        }

        private Embed? GetBasicEmbed(Note note, User noteCreator, Project project)
        {
            var id = noteCreator.DiscordId ?? (ulong)new Random().NextInt64();
            var embedBuilder = new EmbedBuilder()
                .WithColor(new Color((uint)(id % 16777215)))
                .WithAuthor(noteCreator.Name, noteCreator.AvatarUrl, noteCreator.WebUrl)
                .WithDescription($"{note.AdditionalDescription}{note.Description}\n[Note link]({note.WebUrl})")
                .AddField("Project", $"[{project.ProjectName}]({project.GitLabLink})", true)
                .WithTimestamp(note.UpdatedAt.ToLocalTime());

            return embedBuilder.Build();
        }
    }
}
