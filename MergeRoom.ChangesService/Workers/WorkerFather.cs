using MergeRoom.Domain.Entities;
using MergeRoom.MongoRepositoryr.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MergeRoom.ChangesService.Workers
{
    public class WorkerFather : IHostedService, IDisposable
    {
        private readonly TimeSpan _checkInterval;
        private readonly ILogger<WorkerFather> _logger;
        private readonly IMongoRepository _mongoRepository;
        private readonly IServiceProvider _serviceProvider;
        private int _chunkNumber = -1;
        private Task? _doWorkParsingTask;
        private CancellationTokenSource _parsingCancellationTokenSource;
        private bool _isFirstStart = true;

        private readonly Timer _timer = null!;

        public WorkerFather(
            IServiceProvider serviceProvider,
            ILogger<WorkerFather> logger,
            IMongoRepository mongoRepository,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _mongoRepository = mongoRepository;
            ProjectPerChunk = int.Parse(configuration["ProjectPerChunk"]);
            _checkInterval = TimeSpan.FromSeconds(double.Parse(configuration["TimeSecondsCheckToStart"]));
            Chunks = new List<ProjectChunk>();
            _parsingCancellationTokenSource = new CancellationTokenSource();
        }

        private int ProjectPerChunk { get; set; }

        private int CurrentChunkNumber => ++_chunkNumber;

        private List<ProjectChunk> Chunks { get; }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker Hosted Service is starting.");
            _parsingCancellationTokenSource = new CancellationTokenSource();
            _doWorkParsingTask = DoWork(_parsingCancellationTokenSource.Token);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _parsingCancellationTokenSource.Cancel();

            await _doWorkParsingTask;
            _logger.LogInformation("Worker Hosted Service is stopping.");
        }

        public event Action OnChunksParsed;

        //TODO: Think about unable chunks at some time.
        private async Task? DoWork(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Chunks.Clear();

                    await UpdateProjectChunks();

                    var parseTask = ParseChunks();
                    var now = DateTimeOffset.UtcNow;
                    var delay = Task.Delay(_checkInterval, token);
                    //var firstTask = await Task.WhenAny(parseTask, delay);

                    /*if (firstTask == delay)
                    {
                        _logger.LogInformation($"Parsing projects took more than {_checkInterval.Seconds} seconds.");
                        await UpdateChunkCount(ProjectPerChunk + 1);
                    }*/

                    await parseTask;
                    _logger.LogDebug($"Parsing projects took {DateTimeOffset.UtcNow - now}");
                    OnChunksParsed?.Invoke();

                    await Task.WhenAll(parseTask, delay);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "Worker has problems: ");
                }

                await Task.Delay(500);
            }

            await Task.CompletedTask;
        }

        private async Task ParseChunks()
        {
            var scope = _serviceProvider.CreateScope();
            foreach (var chunk in Chunks)
            {
                try
                {
                    var workerService = scope.ServiceProvider.GetRequiredService<IWorker>();

                    var scopeProjects = new List<Project>();
                    scopeProjects.AddRange(chunk.Projects);

                    await workerService.ExecuteAsync(scopeProjects);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error while executing worker (CHUNK ID:{chunk.ChunkNumber}) with projects {chunk.Projects.Select(x => x.GitLabLink)}");
                }

                await Task.Delay(100);
            }
        }

        private async Task UpdateProjectChunks()
        {
            var projects = (await _mongoRepository.GetAllAsync<Project>()).ToList();

            if (!projects.Any())
            {
                return;
            }

            if (!Chunks.Any())
            {
                AddChunksForProjects(projects);
            }
            else
            {
                var unchunkedProjects = projects.Where(x => !Chunks.SelectMany(x => x.Projects).Select(x => x.Id).Contains(x.Id)).ToList();

                var unfulledChunks = Chunks.Where(x => x.Projects.Count < ProjectPerChunk).ToList();

                foreach (var unfulledChunk in unfulledChunks)
                {
                    while (unfulledChunk.Projects.Count < ProjectPerChunk && unchunkedProjects.Any())
                    {
                        var project = unchunkedProjects.FirstOrDefault();
                        if (project is not null)
                        {
                            unfulledChunk.Projects.Add(project);
                            unchunkedProjects.Remove(project);
                        }
                    }
                }

                if (unchunkedProjects.Any())
                {
                    AddChunksForProjects(unchunkedProjects);
                }
            }
        }

        private void AddChunksForProjects(List<Project> projects)
        {
            if (!projects.Any())
            {
                return;
            }

            var chunk = new List<Project>();
            if (projects.Count < ProjectPerChunk)
            {
                chunk.AddRange(projects);
                Chunks.Add(new ProjectChunk
                {
                    Projects = chunk,
                });
            }
            else
            {
                for (var i = 1; i <= projects.Count; i++)
                {
                    chunk.Add(projects[i - 1]);
                    if (i % ProjectPerChunk == 0)
                    {
                        Chunks.Add(new ProjectChunk
                        {
                            Projects = chunk,
                            ChunkNumber = CurrentChunkNumber,
                        });
                        chunk = new List<Project>();
                    }
                }
            }
        }

        public Task ResetChunks()
        {
            return SoftUpdateWorker(new Task(() =>
            {
                Chunks.Clear();
                _logger.LogWarning("Chunks have been reset");
            }));
        }

        public Task ResetChunksByNumber(int number)
        {
            return SoftUpdateWorker(new Task(() =>
            {
                Chunks.RemoveAll(x => x.ChunkNumber == number);
                _logger.LogWarning($"Chunk with id {number} have been reset");
            }));
        }

        public Task UpdateChunkCount(int count)
        {
            return SoftUpdateWorker(new Task(() =>
            {
                _logger.LogWarning($"Project per chunk has been changed {ProjectPerChunk} -> {count}");
                ProjectPerChunk = count;
            }));
        }

        private async Task SoftUpdateWorker(Task action)
        {
            await StopAsync(CancellationToken.None);
            action.Start();
            await action;
            await StartAsync(CancellationToken.None);
            _logger.LogWarning("Worker Father has been restarted");
        }
    }
}
