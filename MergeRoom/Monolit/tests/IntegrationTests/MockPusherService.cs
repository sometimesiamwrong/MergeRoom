using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.Entities.GitlabEntities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;
using DiscordMergeRoomBotCsharpEdition.MongoDB;
using DiscordMergeRoomBotCsharpEdition.Services;
using DiscordMergeRoomBotCsharpEdition.Services.PusherServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntegrationTests
{
    public class MockPusherService : IPusherService
    {
        private IMongoRepository _mongoRepository;

        public delegate Task AsyncEventHandler(object sender, EventArgs e);

        public MockPusherService(DiscordBotConfiguration configuration, IMongoRepository mongoRepository)
        {
            _mongoRepository = mongoRepository;
            HashCode = GetHashCode();
            Name = "test";
        }

        public List<PusherServiceSnapshot> MergeRequestSnapshots { get; } = new List<PusherServiceSnapshot>();
        public List<PusherServiceSnapshot> NoteSnapshots { get; } = new List<PusherServiceSnapshot>();
        public HandleData LastHandleData { get; private set; }

        public int HashCode { get; }

        public string Name { get; }

        public async Task Execute(HandleData data, BaseEntity entity, ExecuteActionTypes action)
        {
            var mrCount = MergeRequestSnapshots.Count;
            var noteCount = NoteSnapshots.Count;
            switch (action)
            {
                case ExecuteActionTypes.MrEdit:
                    MergeRequestSnapshots.Add(new PusherServiceSnapshot
                    {
                        Action = action,
                        Entity = entity as MergeRequest,
                    });
                    break;
                case ExecuteActionTypes.MrOpen:
                    if (entity is MergeRequest mr)
                    {
                        mr.ChannelId = 1;
                        mr.ThreadId = 1;
                        await _mongoRepository.UpdateAsync(mr);
                        MergeRequestSnapshots.Add(new PusherServiceSnapshot
                        {
                            Action = action,
                            Entity = mr,
                        });
                    }
                    break;
                case ExecuteActionTypes.MrMergeOrClosed:
                    MergeRequestSnapshots.Add(new PusherServiceSnapshot
                    {
                        Action = action,
                        Entity = entity as MergeRequest,
                    });
                    break;
                case ExecuteActionTypes.NoteEdit:
                    NoteSnapshots.Add(new PusherServiceSnapshot
                    {
                        Action = action,
                        Entity = entity as Note,
                    });
                    break;
                case ExecuteActionTypes.NoteNew:
                    if (entity is Note note)
                    {
                        if (note.NoteType != NoteType.Unknown)
                        {
                            NoteSnapshots.Add(new PusherServiceSnapshot
                            {
                                Action = action,
                                Entity = entity,
                            });
                        }
                    }
                    break;
                case ExecuteActionTypes.NoteDelete:
                    NoteSnapshots.Add(new PusherServiceSnapshot
                    {
                        Action = action,
                        Entity = entity as Note,
                    });
                    break;
            }
            LastHandleData = data;

            if (noteCount != NoteSnapshots.Count || mrCount != MergeRequestSnapshots.Count)
            {
                await RaiseMyEventAsync(this, EventArgs.Empty);
            }
        }

        public event AsyncEventHandler ExecuteEvent;


        public async Task RaiseMyEventAsync(object sender, EventArgs e)
        {
            if (ExecuteEvent != null)
            {
                // Получаем всех подписчиков события
                var eventHandlers = ExecuteEvent.GetInvocationList();

                // Создаем список задач для всех подписчиков
                var tasks = eventHandlers.Select(handler => ((AsyncEventHandler)handler)(sender, e));

                // Ожидаем выполнения всех задач
                await Task.WhenAll(tasks);
            }
        }
    }

    public struct PusherServiceSnapshot
    {
        public ExecuteActionTypes Action { get; set; }
        public BaseEntity Entity { get; set; }
    }
}
