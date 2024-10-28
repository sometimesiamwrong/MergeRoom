using DiscordMergeRoomBotCsharpEdition.Entities;
using DiscordMergeRoomBotCsharpEdition.GitlabParsing;
using DiscordMergeRoomBotCsharpEdition.Services.PusherServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTests.Services.GitlabParsingServiceTests
{
    public class GitLabParsingServiceTests : BotTestFixture
    {
        [Test]
        public async Task ClearTest()
        {
            Factory.StartApp();

            await WorkerFather.StopAsync(CancellationToken.None);
        }
    }
}
