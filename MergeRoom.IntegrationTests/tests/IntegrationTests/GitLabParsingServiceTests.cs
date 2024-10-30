using NUnit.Framework;

namespace MergeRoom.IntegrationTests.tests.IntegrationTests
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
