using MergeRoom.ChangesService.Workers;
using MergeRoom.DiscordBot;
using MergeRoom.DiscordBot.PusherServices;
using MergeRoom.Domain.Entities;
using MergeRoom.Domain.Entities.GitlabEntities;
using MergeRoom.GitlabRepository;
using MergeRoom.Parsing;
using MergeRoom.Parsing.GitlabParsing;
using MergeRoom.Parsing.Handling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace MergeRoom.IntegrationTests.tests.IntegrationTests
{
    [TestFixture]
    public class BotTestFixture
    {
        [OneTimeSetUp]
        protected void InitAsync()
        {
            ProgramTest.SetupHost(new string[] { });

            SetServices();
        }

        public async Task CleanUpAsync()
        {
            await WorkerFather.StopAsync(CancellationToken.None);
            await GitLabEmulator.DeleteAllData();
            await DeleteDiscordData(_testProject);
            await MongoClient.DropDatabaseAsync("merge-bot-tests");
            await DiscordBot.StopAsync();
            await ProgramTest.StopHost();
        }

        protected async Task DeleteDiscordData(Project project)
        {
            var guild = DataService.GetGuild(project.GuildId);
            var mergeRequests = await MongoRepository.GetAllAsync<MergeRequest>();
            foreach (var mergeRequest in mergeRequests)
            {
                if (project.PusherKind == Configuration.PossiblePusherKinds.Channel)
                {
                    try
                    {
                        var channel = guild.GetChannel(mergeRequest.ChannelId);
                        await channel.DeleteAsync();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                if (project.PusherKind == Configuration.PossiblePusherKinds.Thread)
                {
                    var channel = guild.GetTextChannel(project.ChannelDiscordId);
                    foreach (var thread in channel.Threads)
                    {
                        try
                        {
                            await thread.DeleteAsync();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }
        }

        protected BotTestFixture Fixture => this;

        private Project _testProject;

        public Project TestProject => _testProject;

        protected async Task SetTestProjectThread()
        {
            TestProject.PusherKind = "thread";
            await AddOrUpdateProject();
        }

        protected async Task SetTestProjectNoPusher()
        {
            TestProject.PusherKind = "test";
            await AddOrUpdateProject();
        }

        protected async Task AddOrUpdateProject()
        {
            var existedProject = await MongoRepository.GetAsync<Project>(x => x.Id == TestProject.Id);
            if (existedProject is null)
            {
                await MongoRepository.AddAsync(TestProject);
            }
            else
            {
                await MongoRepository.UpdateAsync(TestProject);
            }
        }

        protected IServiceScope Scope { get; set; }

        public void SetServices()
        {
            var serviceProvider = Factory.ServiceProvider;
            ServiceProvider = serviceProvider;
            Scope = serviceProvider.CreateScope();
            DiscordBot = Scope.ServiceProvider.GetRequiredService<DiscordBot.DiscordBot>();
            DiscordBot.StartAsync(Scope.ServiceProvider).GetAwaiter().GetResult();
            MongoRepository = Scope.ServiceProvider.GetRequiredService<IMongoRepository>();
            MongoClient = Scope.ServiceProvider.GetRequiredService<IMongoClient>();
            DataService = Scope.ServiceProvider.GetRequiredService<DataService>();
            PusherServices = Scope.ServiceProvider.GetServices<IPusherService>();
            PrometheusService = Scope.ServiceProvider.GetRequiredService<PrometheusService>();
            GitLabService = Scope.ServiceProvider.GetRequiredService<GitLabService>();
            GitLabParsingService = Scope.ServiceProvider.GetRequiredService<GitLabParsingService>();
            ChangeHandlerService = Scope.ServiceProvider.GetRequiredService<ChangeHandlerService>();
            GitLabEmulator = Scope.ServiceProvider.GetRequiredService<GitLabEmulator>();
            MockPusherService = ((PusherServices.First(x => x.Name == "test") as MockPusherService)!);
            _testProject = Scope.ServiceProvider.GetRequiredService<Project>();
            var hostedServices = ServiceProvider.GetServices<IHostedService>();

            foreach (var hostedService in hostedServices)
            {
                if (hostedService is WorkerFather father)
                {
                    WorkerFather = father;
                    break;
                }
            }
        }

        public IMongoRepository MongoRepository { get; set; }

        public IMongoClient MongoClient { get; set; }

        public DataService DataService { get; set; }

        public IEnumerable<IPusherService> PusherServices { get; set; }

        public DiscordBot DiscordBot { get; set; }

        public PrometheusService PrometheusService { get; set; }

        public GitLabService GitLabService { get; set; }

        public GitLabParsingService GitLabParsingService { get; set; }

        public ChangeHandlerService ChangeHandlerService { get; set; }

        public IServiceProvider ServiceProvider { get; set; }

        public DiscordBotConfiguration Configuration => ServiceProvider.GetRequiredService<DiscordBotConfiguration>();

        public GitLabEmulator GitLabEmulator { get; set; }

        public WorkerFather WorkerFather { get; set; }

        public MockPusherService MockPusherService { get; set; }

        public void AssertNote(List<int> notesIds, List<PusherServiceSnapshot> notes, ExecuteActionTypes action, int noteCounter)
        {
            Assert.AreEqual(notesIds[noteCounter], ((Note)notes[noteCounter].Entity).GitlabId);
            Assert.AreEqual(action, notes[noteCounter].Action);
        }

        public void AssertMergeRequest(
            List<(int iid, string branch)> mrs,
            List<PusherServiceSnapshot> mergeRequest,
            ExecuteActionTypes action,
            int mrCounter,
            int? mrChangesCounter = null)
        {
            Assert.AreEqual(action, mergeRequest[mrChangesCounter ?? mrCounter].Action);
        }

        public void AssertCounters(int mergeRequestCounter, int noteCounter)
        {
            Assert.AreEqual(MockPusherService.MergeRequestSnapshots.Count, mergeRequestCounter);
            Assert.AreEqual(MockPusherService.NoteSnapshots.Count, noteCounter);
        }
    }
}
