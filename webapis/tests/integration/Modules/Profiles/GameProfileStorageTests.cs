using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Modules.Profiles.Data;
using yggdrasil.Modules.Profiles.Domain;
using yggdrasil.Persistence;

namespace yggdrasil.Integration.Tests.Modules.Profiles;

public sealed class GameProfileStorageTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private static readonly DateTimeOffset created_at = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid account_id = Guid.CreateVersion7();

    private static readonly Guid game_id = Guid.CreateVersion7();

    [Fact]
    public async Task NewProfileStartsWithEmptyDataAtTheFirstVersion()
    {
        var profile = await addAsync(new GameProfile(account_id, game_id, "Player", created_at));

        await using var scope = Factory.Services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<ProfilesModuleDbContext>().Profiles
            .SingleAsync(candidate => candidate.Id == profile.Id, CancellationToken);

        Assert.Equal("Player", stored.DisplayName);
        Assert.Equal(created_at, stored.CreatedAt);
        Assert.Equal(created_at, stored.LastPlayedAt);
        Assert.Equal(GameProfile.EMPTY_DATA, stored.Data);
        Assert.Equal(1, stored.DataVersion);
    }

    [Fact]
    public async Task OneProfilePerAccountAndGame()
    {
        await addAsync(new GameProfile(account_id, game_id, "Player", created_at));
        await addAsync(new GameProfile(account_id, Guid.CreateVersion7(), "Player", created_at));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => addAsync(new GameProfile(account_id, game_id, "Again", created_at)));

        Assert.True(exception.IsUniqueViolation);
    }

    [Fact]
    public async Task DataIsQueryableJson()
    {
        var profile = new GameProfile(account_id, game_id, "Player", created_at);
        profile.ReplaceData("""{"level": 3, "items": ["sword"]}""");
        await addAsync(profile);

        await using var scope = Factory.Services.CreateAsyncScope();
        var level = await scope.ServiceProvider.GetRequiredService<ProfilesModuleDbContext>().Database
            .SqlQuery<string>($"""SELECT data ->> 'level' AS "Value" FROM profiles.profiles""")
            .SingleAsync(CancellationToken);

        Assert.Equal("3", level);
    }

    [Fact]
    public async Task InvalidJsonIsRejectedByTheDatabase()
    {
        var profile = new GameProfile(account_id, game_id, "Player", created_at);
        profile.ReplaceData("not json");

        await Assert.ThrowsAsync<DbUpdateException>(() => addAsync(profile));
    }

    [Fact]
    public async Task WriteBasedOnStaleDataIsRejected()
    {
        var profile = await addAsync(new GameProfile(account_id, game_id, "Player", created_at));

        await using var first = Factory.Services.CreateAsyncScope();
        await using var second = Factory.Services.CreateAsyncScope();
        var firstContext = first.ServiceProvider.GetRequiredService<ProfilesModuleDbContext>();
        var secondContext = second.ServiceProvider.GetRequiredService<ProfilesModuleDbContext>();

        // Both writers read version 1...
        var firstCopy = await firstContext.Profiles.SingleAsync(candidate => candidate.Id == profile.Id, CancellationToken);
        var secondCopy = await secondContext.Profiles.SingleAsync(candidate => candidate.Id == profile.Id, CancellationToken);

        // ...the first one wins and moves the profile to version 2...
        firstCopy.ReplaceData("""{"level": 2}""");
        await firstContext.SaveChangesAsync(CancellationToken);
        Assert.Equal(2, firstCopy.DataVersion);

        // ...so the second one, still based on version 1, must not overwrite it.
        secondCopy.ReplaceData("""{"level": 1}""");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync(CancellationToken));
    }

    private async Task<GameProfile> addAsync(GameProfile profile)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ProfilesModuleDbContext>();

        context.Profiles.Add(profile);
        await context.SaveChangesAsync(CancellationToken);

        return profile;
    }
}
