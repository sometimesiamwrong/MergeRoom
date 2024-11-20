using DiscordMergeRoomBotCsharpEdition.Entities;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Prometheus;

namespace DiscordMergeRoomBotCsharpEdition.Services
{
    public class PrometheusService : IHostedService
    {
        private readonly IMongoClient _mongoClient;

        public PrometheusService(IMongoClient mongoClient)
        {
            _mongoClient = mongoClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Начало сбора метрик MongoDB и других необходимых метрик
            CollectMongoMetrics();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void CollectMongoMetrics()
        {
            var database = _mongoClient.GetDatabase("merge-bot");
            var projects = database.GetCollection<Project>("projects");
            var mergeRequests = database.GetCollection<MergeRequest>("mergeRequests");
            var users = database.GetCollection<User>("users");
            var countProjects = projects.CountDocuments(FilterDefinition<Project>.Empty);
            var countMergeRequests = mergeRequests.CountDocuments(FilterDefinition<MergeRequest>.Empty);
            var countUsers = users.CountDocuments(FilterDefinition<User>.Empty);
            Metrics.CreateGauge("number_projects", "in MongoDB Project collection").Set(countProjects);
            Metrics.CreateGauge("number_merge_requests", "in MongoDB MergeRequests collection").Set(countMergeRequests);
            Metrics.CreateGauge("number_users", "in MongoDB Users collection").Set(countUsers);
        }
    }
}
