using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Workers;
using DiscordMergeRoomBotCsharpEdition.Services.PusherServices;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [NonParallelizable]
    public class WorkFlowTests : BotTestFixture
    {
        [Test]
        public async Task DefaultWorkFlow_2Mrs2Notes()
        {
            var mrs = new List<(int IId, string branchName)>();
            for (var i = 0; i < 2; i++)
            {
                mrs.Add(await GitLabEmulator.OpenMergeRequest());
            }

            var notes = new List<int>
            {
                await GitLabEmulator.AddCommentToMergeRequest(mrs[0].IId),
                await GitLabEmulator.AddCommentToMergeRequest(mrs[0].IId),
                await GitLabEmulator.AddCommentToMergeRequest(mrs[1].IId),
                await GitLabEmulator.AddCommentToMergeRequest(mrs[1].IId),
            };

            await SetTestProjectNoPusher();

            Factory.StartApp();

            var testTask = new Task(() => { });
            WorkerFather.OnChunksParsed += () =>
            {
                testTask.Start();
            };

            var eventCounter = 0;
            var noteCounter = 0;
            var mergeRequestCounter = 0;

            var assertTasks = new List<Task>()
            {
                new Task(() =>
                {
                    AssertCounters(1, 0);
                    AssertMergeRequest(mrs, MockPusherService.MergeRequestSnapshots, ExecuteActionTypes.MrOpen, mergeRequestCounter);
                    mergeRequestCounter++;
                }),
                new Task(() =>
                {
                    AssertCounters(1, 1);
                    AssertNote(notes, MockPusherService.NoteSnapshots, ExecuteActionTypes.NoteNew, noteCounter);
                    noteCounter++;
                }),
                new Task(() =>
                {
                    AssertCounters(1, 2);
                    AssertNote(notes, MockPusherService.NoteSnapshots, ExecuteActionTypes.NoteNew, noteCounter);
                    noteCounter++;
                }),
                new Task(() =>
                {
                    AssertCounters(2, 2);
                    AssertMergeRequest(mrs, MockPusherService.MergeRequestSnapshots, ExecuteActionTypes.MrOpen, mergeRequestCounter);
                    mergeRequestCounter++;
                }),
                new Task(() =>
                {
                    AssertCounters(2, 3);
                    AssertNote(notes, MockPusherService.NoteSnapshots, ExecuteActionTypes.NoteNew, noteCounter);
                    noteCounter++;
                }),
                new Task(() =>
                {
                    AssertCounters(2, 4);
                    AssertNote(notes, MockPusherService.NoteSnapshots, ExecuteActionTypes.NoteNew, noteCounter);
                    noteCounter++;
                }),
            };

            MockPusherService.ExecuteEvent += async (sender, e) =>
            {
                assertTasks[eventCounter].Start();
                await assertTasks[eventCounter];
                eventCounter++;
            };

            await testTask;
            await CleanUpAsync();
        }

        [Test]
        public async Task NoteAddAfterParsingBefore1Minute_1Mr3Notes()
        {
            try
            {
                await MongoClient.DropDatabaseAsync("merge-bot-tests");
                await GitLabEmulator.DeleteAllData();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            var mrs = new List<(int IId, string branchName)>
            {
                await GitLabEmulator.OpenMergeRequest(),
            };

            var notes = new List<int>
            {
                await GitLabEmulator.AddCommentToMergeRequest(mrs[0].IId),
                await GitLabEmulator.AddCommentToMergeRequest(mrs[0].IId),
            };

            await Task.Delay(1000);

            await SetTestProjectNoPusher();
            await Task.Delay(1000);

            Factory.StartApp();

            var parsingTaskWait = new List<Task>
            {
                new Task(() => { }),
                new Task(() => { }),
            };

            var taskWaitCounter = 0;

            WorkerFather.OnChunksParsed += startParsingTask;

            void startParsingTask()
            {
                parsingTaskWait[taskWaitCounter].Start();
                taskWaitCounter++;
            }

            var eventCounter = 0;
            var noteCounter = 0;
            var mergeRequestCounter = 0;

            var assertTasks = new List<Task>()
            {
                new Task(() =>
                {
                    AssertCounters(1, 0);
                    AssertMergeRequest(mrs, MockPusherService.MergeRequestSnapshots, ExecuteActionTypes.MrOpen, mergeRequestCounter);
                    mergeRequestCounter++;
                }),
                new Task(() =>
                {
                    AssertCounters(1, 1);
                    AssertNote(notes, MockPusherService.NoteSnapshots, ExecuteActionTypes.NoteNew, noteCounter);
                    noteCounter++;
                }),
                new Task(() =>
                {
                    AssertCounters(1, 2);
                    AssertNote(notes, MockPusherService.NoteSnapshots, ExecuteActionTypes.NoteNew, noteCounter);
                    noteCounter++;
                }),
            };

            MockPusherService.ExecuteEvent += StartAssertTask;

            async Task StartAssertTask(object sender, EventArgs e)
            {
                assertTasks[eventCounter].Start();
                await assertTasks[eventCounter];
                eventCounter++;
            }

            await parsingTaskWait[taskWaitCounter];
            await Task.Delay(1000);
            notes.Add(await GitLabEmulator.AddCommentToMergeRequest(mrs[0].IId));

            assertTasks.AddRange(new List<Task>()
            {
                new Task(() =>
                {
                    AssertCounters(2, 2);
                    AssertMergeRequest(mrs, MockPusherService.MergeRequestSnapshots, ExecuteActionTypes.MrEdit, mergeRequestCounter);
                }),
                new Task(() =>
                {
                    AssertCounters(2, 3);
                    AssertNote(notes, MockPusherService.NoteSnapshots, ExecuteActionTypes.NoteNew, noteCounter);
                })
            });

            await parsingTaskWait[taskWaitCounter];

            Assert.IsTrue(assertTasks.All(x => x.IsCompletedSuccessfully));
            WorkerFather.OnChunksParsed -= startParsingTask;
            MockPusherService.ExecuteEvent -= StartAssertTask;
            await CleanUpAsync();
        }

        [Test]
        public async Task Conflict_2MrBy1notes()
        {
            var mrs = new List<(int IId, string branchName)>
            {
                await GitLabEmulator.OpenMergeRequest(),
                await GitLabEmulator.OpenMergeRequest(),
            };

            await GitLabEmulator.UpdateBranchFile(mrs[0].branchName);
            await GitLabEmulator.UpdateBranchFile(mrs[1].branchName);

            var notes = new List<int>
            {
                await GitLabEmulator.AddCommentToMergeRequest(mrs[0].IId),
                await GitLabEmulator.AddCommentToMergeRequest(mrs[1].IId),
            };

            await Task.Delay(1000);
            await SetTestProjectNoPusher();
            await Task.Delay(1000);

            Factory.StartApp();

            var parsingTaskWait = new List<Task>
            {
                new Task(() => { }),
                new Task(() => { }),
            };

            var taskWaitCounter = 0;

            WorkerFather.OnChunksParsed += startParsingTask;

            void startParsingTask()
            {
                parsingTaskWait[taskWaitCounter].Start();
                taskWaitCounter++;
            }

            var eventCounter = 0;
            var noteCounter = 0;
            var mergeRequestCounter = 0;

            var assertTasks = new List<Task>()
            {
                new Task(() =>
                {
                    AssertCounters(1, 0);
                    AssertMergeRequest(mrs, MockPusherService.MergeRequestSnapshots, ExecuteActionTypes.MrOpen, mergeRequestCounter);
                    mergeRequestCounter++;
                }),
                new Task(() =>
                {
                    AssertCounters(1, 1);
                    AssertNote(notes, MockPusherService.NoteSnapshots, ExecuteActionTypes.NoteNew, noteCounter);
                    noteCounter++;
                }),
                new Task(() =>
                {
                    AssertCounters(2, 1);
                    AssertMergeRequest(mrs, MockPusherService.MergeRequestSnapshots, ExecuteActionTypes.MrOpen, mergeRequestCounter);
                }),
                new Task(() =>
                {
                    AssertCounters(2, 2);
                    AssertNote(notes, MockPusherService.NoteSnapshots, ExecuteActionTypes.NoteNew, noteCounter);
                    noteCounter++;
                }),
            };

            MockPusherService.ExecuteEvent += StartAssertTask;

            async Task StartAssertTask(object sender, EventArgs e)
            {
                assertTasks[eventCounter].Start();
                await assertTasks[eventCounter];
                eventCounter++;
            }

            await parsingTaskWait[taskWaitCounter];
            await GitLabEmulator.MergeMergeRequest(mrs[0].IId);
            await Task.Delay(1000);

            //Trigger check mr
            try
            {
                await GitLabEmulator.TriggerCheckMerge(mrs[1].IId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            var mrChangesCounter = mergeRequestCounter + 1;

            assertTasks.AddRange(new List<Task>()
            {
                new Task(() =>
                {
                    AssertCounters(3, 2);
                    AssertMergeRequest(mrs, MockPusherService.MergeRequestSnapshots, ExecuteActionTypes.MrEdit, mergeRequestCounter, mrChangesCounter);
                    Assert.AreEqual(((MergeRequest)MockPusherService.MergeRequestSnapshots[mrChangesCounter].Entity).HasConflicts, true);
                    mrChangesCounter++;
                }),
                new Task(() =>
                {
                    AssertCounters(4, 2);
                    AssertMergeRequest(mrs, MockPusherService.MergeRequestSnapshots, ExecuteActionTypes.MrMergeOrClosed, mergeRequestCounter, mrChangesCounter);
                    Assert.AreEqual(((MergeRequest)MockPusherService.MergeRequestSnapshots[mrChangesCounter].Entity).State, "merged");
                    mrChangesCounter++;
                }),
            });

            await parsingTaskWait[taskWaitCounter];

            Assert.IsTrue(assertTasks.All(x => x.IsCompletedSuccessfully));
            WorkerFather.OnChunksParsed -= startParsingTask;
            MockPusherService.ExecuteEvent -= StartAssertTask;
            await CleanUpAsync();
        }
    }
}
