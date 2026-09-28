using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Modules.Games.Data;
using yggdrasil.Modules.Games.Domain;
using yggdrasil.Persistence;

namespace yggdrasil.Integration.Tests.Modules.Games;

public sealed class GameStorageTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private static readonly DateTimeOffset created_at = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GameIsStoredAndReadBack()
    {
        var game = new Game("my-game", "My Game", created_at);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GamesModuleDbContext>();
            context.Games.Add(game);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<GamesModuleDbContext>().Games
                .SingleAsync(candidate => candidate.Id == game.Id, CancellationToken);

            Assert.Equal("my-game", stored.Slug);
            Assert.Equal("My Game", stored.Name);
            Assert.Equal(created_at, stored.CreatedAt);
        }
    }

    [Fact]
    public async Task SlugIsUniqueAcrossTheCatalog()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<GamesModuleDbContext>();

        context.Games.Add(new Game("my-game", "My Game", created_at));
        await context.SaveChangesAsync(CancellationToken);

        context.Games.Add(new Game("my-game", "Another Game", created_at));
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(CancellationToken));

        Assert.True(exception.IsUniqueViolation);
    }
}
