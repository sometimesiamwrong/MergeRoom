using Discord;
using Discord.WebSocket;
using DiscordMergeRoomBotCsharpEdition.Commands;
using DiscordMergeRoomBotCsharpEdition.Configs;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Handling;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Workers;
using DiscordMergeRoomBotCsharpEdition.MongoDB;
using DiscordMergeRoomBotCsharpEdition.Services;
using DiscordMergeRoomBotCsharpEdition.Services.PusherServices;
using DiscordMergeRoomBotCsharpEdition.Services.PusherServices.ChannelServices;
using DiscordMergeRoomBotCsharpEdition.Services.PusherServices.ThreadServices;
using DiscordMergeRoomBotCsharpEdition.Webhooks;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Prometheus;

namespace DiscordMergeRoomBotCsharpEdition
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHealthChecks();
            services.AddLogging(configure => configure.AddConsole());

            services.AddSingleton<DiscordBot>();
            services.AddSingleton<IMongoClient>(sp =>
            {
                var settings = MongoClientSettings.FromConnectionString(Configuration["ConnectionString"]);
                return new MongoClient(settings);
            });

            services.AddSingleton<DataService>();
            services.AddSingleton<PrometheusService>();
            services.AddSingleton<IMongoRepository, MongoRepository>(provider => new MongoRepository(Configuration["ConnectionString"], Configuration["DatabaseName"]));

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
            services.AddHttpClient<GitLabService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            });
            services.AddScoped<IEventHook, MergeEventHook>();
            services.AddScoped<IEventHook, NoteEventHook>();
            services.AddScoped<IPusherService, ChannelService>();
            services.AddTransient<ChannelMergeRequestService>();
            services.AddTransient<ChannelNoteService>();
            services.AddTransient<ThreadNoteService>();
            services.AddTransient<ThreadMergeRequestService>();
            services.AddScoped<IPusherService, ThreadService>();
            services.AddScoped<ChangeHandlerService>();
            services.AddScoped<GitLabParsingService>();
            services.AddScoped<IWorker, Worker>();
            services.AddSingleton<IHostedService, WorkerFather>();
            services.AddSingleton<ISlashCommandHandler, SetParseDefaultBranchesSlashCommandHandler>();
            services.AddSingleton<ISlashCommandHandler, RegisterSlashCommandHandler>();
            services.AddSingleton<IPrefixCommandHandler, ResetChunksPrefixCommandHandler>();
            services.AddSingleton<IPrefixCommandHandler, PingPrefixCommandHandler>();

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

            services.AddHostedService<PrometheusService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseHttpMetrics();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
            });

            // Использование Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = "v1/swagger"; // Для отображения Swagger UI на главной странице
            });

            var bot = app.ApplicationServices.GetRequiredService<DiscordBot>();

            bot.StartAsync(app.ApplicationServices).GetAwaiter().GetResult();
        }
    }
}
