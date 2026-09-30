using Mediator;
using Microsoft.EntityFrameworkCore;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Games.Contracts.v1.Games;
using yggdrasil.Modules.Games.Data;

namespace yggdrasil.Modules.Games.Features.v1.Games.GetGameByClientId;

public sealed class GetGameByClientIdQueryHandler(GamesModuleDbContext context)
    : IQueryHandler<GetGameByClientIdQuery, Result<GameResponse>>
{
    public async ValueTask<Result<GameResponse>> Handle(GetGameByClientIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var game = await context.Games
            .AsNoTracking()
            .Where(game => game.ClientId == query.ClientId)
            .Select(static game => new GameResponse(game.Id, game.Slug, game.Name))
            .SingleOrDefaultAsync(cancellationToken);

        return game is null
            ? Error.NotFound("games.unknown_client", "No game has this OAuth client.")
            : game;
    }
}
