using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordMergeRoomBotCsharpEdition.Commands;

namespace DiscordMergeRoomBotCsharpEdition
{
    public class DiscordBot
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<DiscordBot> _logger;
        private IEnumerable<IPrefixCommandHandler> _prefixCommandHandlers;
        private IEnumerable<ISlashCommandHandler> _slashCommandHandlers;

        public DiscordBot(
            DiscordSocketClient client,
            ILogger<DiscordBot> logger,
            IEnumerable<IPrefixCommandHandler> prefixCommandHandlers,
            IEnumerable<ISlashCommandHandler> slashCommandHandlers)
        {
            _client = client;
            _logger = logger;
            _prefixCommandHandlers = prefixCommandHandlers;
            _slashCommandHandlers = slashCommandHandlers;
        }

        public async Task StartAsync(IServiceProvider services)
        {
            var config = services.GetRequiredService<IConfiguration>();
            var botToken = config["BotToken"];

            _client.Log += Log;
            _client.Ready += async () =>
            {
                _logger.LogInformation($"Connected as {_client.CurrentUser.Username}#{_client.CurrentUser.Discriminator}");

                var guildIdStr = config["GuildId"];
                if (string.IsNullOrEmpty(guildIdStr))
                {
                    throw new ArgumentException("GuildId is not set in the configuration.");
                }

                if (!ulong.TryParse(guildIdStr, out var guildId))
                {
                    throw new ArgumentException("GuildId is not a valid ulong.");
                }

                var guild = _client.GetGuild(guildId);
                if (guild == null)
                {
                    _logger.LogError($"Guild with ID {guildId} not found. Make sure the bot is invited to the server.");
                    return;
                }

                _logger.LogInformation($"Guild with ID {guildId} found.");

                // Регистрация команд
                var commands = new List<SlashCommandBuilder>
                {
                    new SlashCommandBuilder().WithName("set_parse_default_branches").WithDescription("Set need parse default branches")
                        .AddOption("project_id", ApplicationCommandOptionType.Integer, "Project ID", true)
                        .AddOption("bool_value", ApplicationCommandOptionType.Boolean, "Value to change", true),
                    new SlashCommandBuilder().WithName("register_project").WithDescription("Register project")
                        .AddOption("link", ApplicationCommandOptionType.String, "Link to project", true)
                        .AddOption("name", ApplicationCommandOptionType.String, "Name of project", true)
                        .AddOption("access_token", ApplicationCommandOptionType.String, "Access token", true)
                        .AddOption("pusher_kind", ApplicationCommandOptionType.String, "Pusher kind", true, choices: new List<ApplicationCommandOptionChoiceProperties>()
                        {
                            new ApplicationCommandOptionChoiceProperties()
                            {
                                Name = "thread",
                                Value = "thread"
                            },
                            new ApplicationCommandOptionChoiceProperties()
                            {
                                Name = "channel",
                                Value = "channel"
                            }
                        }.ToArray()),
                };

                var existingGlobalCommands = await _client.GetGlobalApplicationCommandsAsync();

                // Удаление всех существующих глобальных команд
                foreach (var command in existingGlobalCommands)
                {
                    await command.DeleteAsync();
                }

                foreach (var command in commands)
                {
                    await guild.CreateApplicationCommandAsync(command.Build());
                }
            };

            _client.MessageReceived += async interaction => { await HandlePrefixInteraction(interaction); };

            _client.SlashCommandExecuted += async interaction => { await HandleSlashInteraction(interaction); };

            await _client.LoginAsync(TokenType.Bot, botToken);
            await _client.StartAsync();
        }

        public Task StopAsync()
        {
            return _client.StopAsync();
        }

        private Task Log(LogMessage arg)
        {
            _logger.LogInformation(arg.ToString());
            return Task.CompletedTask;
        }

        private async Task HandlePrefixInteraction(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message is null)
            {
                return;
            }

            if (message.Author.IsBot)
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);

            var argPos = 0;
            if (message.HasCharPrefix('!', ref argPos))
            {
                var command = message.Content.Substring(1);
                var paths = command.Split(' ');
                var commandHandler = _prefixCommandHandlers.FirstOrDefault(h => h.Name == paths[0]);
                if (commandHandler != null)
                {
                    await commandHandler.HandleCommand(context, paths.Skip(1).ToArray());
                }
                else
                {
                    await context.Message.ReplyAsync("Command not found.");
                }
            }
        }

        private async Task HandleSlashInteraction(SocketSlashCommand command)
        {
            var commandHandler = _slashCommandHandlers.FirstOrDefault(h => h.Name == command.CommandName);
            if (commandHandler != null)
            {
                await commandHandler.HandleCommand(command, null);
            }
            else
            {
                await command.RespondAsync("Command not found.");
            }
        }
    }
}
