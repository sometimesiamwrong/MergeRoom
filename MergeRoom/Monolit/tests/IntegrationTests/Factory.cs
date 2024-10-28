using Discord;
using Discord.WebSocket;
using DiscordMergeRoomBotCsharpEdition;
using DiscordMergeRoomBotCsharpEdition.Configs;
using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Workers;
using DiscordMergeRoomBotCsharpEdition.MongoDB;
using DiscordMergeRoomBotCsharpEdition.Services;
using DiscordMergeRoomBotCsharpEdition.Services.PusherServices;
using DiscordMergeRoomBotCsharpEdition.Services.PusherServices.ChannelServices;
using DiscordMergeRoomBotCsharpEdition.Services.PusherServices.ThreadServices;
using DiscordMergeRoomBotCsharpEdition.Webhooks;
using IntegrationTests.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace IntegrationTests
{
    public static class Factory
    {
        public static IServiceProvider ServiceProvider { get; set; }

        public static void StartApp()
        {
            ProgramTest.StartHost();
        }

        public static Task StopApp()
        {
            return ProgramTest.StopHost();
        }

        public static string GetEvironment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Test";
        }

        public static IConfiguration GetConfiguration()
        {
            var env = GetEvironment();

            return new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{env}.json")
                .AddJsonFile($"Configs/serilog.{env}.json", true)
                .Build();
        }

        public static void SetupServiceCollection(IServiceCollection services)
        {
            var configuration = GetConfiguration();
            services.AddSingleton(configuration);
            services.AddControllers();
            services.AddHealthChecks();
            services.AddLogging(configure => configure.AddConsole());

            services.AddSingleton<DiscordBot>();
            services.AddSingleton<IMongoClient>(sp =>
            {
                var settings = MongoClientSettings.FromConnectionString(configuration["ConnectionString"]);
                return new MongoClient(settings);
            });

            services.AddScoped<DataService>();
            services.AddSingleton<PrometheusService>();
            services.AddSingleton<IMongoRepository, MongoRepository>(provider => new MongoRepository(configuration["ConnectionString"], configuration["DatabaseName"]));

            services.AddSingleton(sp =>
            {
                var clientConfig = new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.Guilds |
                                     GatewayIntents.GuildMembers |
                                     GatewayIntents.GuildEmojis |
                                     GatewayIntents.GuildIntegrations |
                                     GatewayIntents.GuildWebhooks |
                                     GatewayIntents.GuildInvites |
                                     GatewayIntents.GuildVoiceStates |
                                     GatewayIntents.GuildPresences |
                                     GatewayIntents.MessageContent |
                                     GatewayIntents.GuildMessageReactions |
                                     GatewayIntents.GuildMessageTyping |
                                     GatewayIntents.DirectMessages |
                                     GatewayIntents.DirectMessageReactions |
                                     GatewayIntents.DirectMessageTyping |
                                     GatewayIntents.All,
                };
                return new DiscordSocketClient(clientConfig);
            });

            // Регистрация GitLabService
            services.AddSingleton<HttpClient>(_ =>
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                return client;
            });

            services.AddSingleton<GitLabService>();

            services.AddScoped<IEventHook, MergeEventHook>();
            services.AddScoped<IEventHook, NoteEventHook>();
            services.AddScoped<IPusherService, ChannelService>();
            services.AddTransient<ChannelNoteService>();
            services.AddTransient<ChannelMergeRequestService>();
            services.AddScoped<IPusherService, ThreadService>();
            services.AddTransient<ThreadNoteService>();
            services.AddTransient<ThreadMergeRequestService>();
            services.AddSingleton<IPusherService, MockPusherService>();
            services.AddScoped<ChangeHandlerService>();
            services.AddScoped<GitLabParsingService>();
            services.AddScoped<IWorker, Worker>();
            services.AddSingleton<IHostedService, WorkerFather>();

            services.AddSingleton<DiscordBotConfiguration>(sp => new DiscordBotConfiguration(
                new PossibleEventKinds
                {
                    MergeRequest = "merge_request",
                    Note = "note",
                },
                new PossiblePusherKinds
                {
                    Channel = "channel",
                    Thread = "thread",
                },
                new PossibleSystemNoteKinds
                {
                    ResolvedAllThreads = "resolved all threads",
                    Approve = "approved",
                    Unapprove = "unapproved",
                }));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Merge bot",
                    Version = "v1",
                });
            });

            services.AddSingleton<Project>(provider => new Project
            {
                CreatedAt = BsonDateTime.Create(DateTime.UtcNow),
                UpdatedAt = BsonDateTime.Create(DateTime.UtcNow),
                GuildId = 1188839818076098570,
                GitlabId = 59141501,
                GitLabLink = "https://gitlab.com/Tekra/testgitlabparsing",
                CategoryDiscordName = "test_chanel_mr",
                CategoryDiscordId = 1250457612764319764,
                ChannelDiscordName = "test_chanel",
                ChannelDiscordId = 1255048953943162972,
                AccessToken = "glpat-LoRPKG9boyxyGqD6D-yn",
                Host = "gitlab.com",
                Namespace = "Tekra",
                ProjectName = "testgitlabparsing",
                ParsedAt = BsonDateTime.Create(DateTime.UtcNow.AddDays(-1)),
                PusherKind = "thread",
            });

            services.AddSingleton<GitLabEmulator>();
            services.AddSingleton<IGitLabHttpRepository, GitLabHttpRepository>();
        }
    }
}
