using Discord.Commands;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing.Workers;

namespace DiscordMergeRoomBotCsharpEdition.Commands
{
    public class ResetChunksPrefixCommandHandler : IPrefixCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public ResetChunksPrefixCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string Name { get; } = "ResetChunks";

        public async Task HandleCommand(SocketCommandContext context, string[] args)
        {
            var hostedServices = _serviceProvider.GetServices<IHostedService>();

            WorkerFather? workerFather = null;

            foreach (var hostedService in hostedServices)
            {
                if (hostedService is WorkerFather father)
                {
                    workerFather = father;
                    break;
                }
            }

            if (workerFather is null)
            {
                return;
            }

            if (args.Length == 0)
            {
                await workerFather.ResetChunks();
            }
            else
            {
                int.TryParse(args[0], out var number);

                if (number != -1)
                {
                    await workerFather.ResetChunksByNumber(number);
                }
            }
        }
    }
}
