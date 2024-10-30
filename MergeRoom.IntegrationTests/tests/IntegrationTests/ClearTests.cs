using NUnit.Framework;

namespace MergeRoom.IntegrationTests.tests.IntegrationTests
{
    public class ClearTests : BotTestFixture
    {
        [Test]
        public async Task ClearTest()
        {
            Factory.StartApp();
            await Task.Delay(6000);
            await SetTestProjectThread();
            var guild = DataService.GetGuild(TestProject.GuildId);
            var channel = guild.GetTextChannel(1253791749562957996);
            foreach (var thread in channel.Threads)
            {
                await thread.DeleteAsync();
            }
            await CleanUpAsync();
        }
    }
}
