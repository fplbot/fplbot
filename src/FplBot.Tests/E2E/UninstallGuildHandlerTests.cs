using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E;

[Collection("App")]
public class UninstallGuildHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task UninstallGuild_WhenInstallationAlreadyGone_StillClearsMemberCount()
    {
        // Simulates a retry after a first attempt deleted the installation but faulted before
        // clearing the member-count entry - the retry must not skip that cleanup step.
        var guildId = "orphaned-" + Guid.NewGuid().ToString("N");
        await fixture.GuildMemberCountRepo.SetApproximateMemberCount(guildId, 500);

        await fixture.Bus.Publish(new UninstallGuild(guildId, UninstallReason.AdminDeleted), TestContext.Current.CancellationToken);

        await AppFixture.WaitUntil(async () => !(await fixture.GuildMemberCountRepo.GetAll()).ContainsKey(guildId));
    }
}
