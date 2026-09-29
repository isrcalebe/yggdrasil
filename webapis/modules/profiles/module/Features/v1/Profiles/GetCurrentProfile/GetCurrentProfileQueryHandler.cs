using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Games.Contracts.v1.Games;
using yggdrasil.Modules.Profiles.Contracts.v1.Profiles;
using yggdrasil.Modules.Profiles.Data;
using yggdrasil.Modules.Profiles.Domain;
using yggdrasil.Persistence;

namespace yggdrasil.Modules.Profiles.Features.v1.Profiles.GetCurrentProfile;

public sealed class GetCurrentProfileQueryHandler(
    ProfilesModuleDbContext context,
    IMediator mediator,
    TimeProvider clock
) : IQueryHandler<GetCurrentProfileQuery, Result<ProfileResponse>>
{
    public async ValueTask<Result<ProfileResponse>> Handle(GetCurrentProfileQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var game = await mediator.Send(new GetGameByClientIdQuery(query.ClientId), cancellationToken);

        if (!game.IsSuccess)
            return Error.Forbidden("profiles.not_a_game", "The token was not issued to a registered game.");

        var profile = await findAsync(query.AccountId, game.Value.GameId, cancellationToken)
            ?? await createAsync(query.AccountId, game.Value.GameId, cancellationToken);

        return new ProfileResponse(
            profile.Id,
            profile.GameId,
            profile.DisplayName,
            profile.CreatedAt,
            profile.LastPlayedAt,
            JsonDocument.Parse(profile.Data).RootElement.Clone(),
            profile.DataVersion);
    }

    private Task<GameProfile?> findAsync(Guid accountId, Guid gameId, CancellationToken cancellationToken)
        => context.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.AccountId == accountId && profile.GameId == gameId, cancellationToken);

    private async Task<GameProfile> createAsync(Guid accountId, Guid gameId, CancellationToken cancellationToken)
    {
        var profile = new GameProfile(accountId, gameId, defaultDisplayName(accountId), clock.GetUtcNow());
        context.Profiles.Add(profile);

        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return profile;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            context.Entry(profile).State = EntityState.Detached;

            return (await findAsync(accountId, gameId, cancellationToken))!;
        }
    }

    private static string defaultDisplayName(Guid accountId)
        => $"Player-{accountId.ToString("N")[^6..]}";
}
