using Mediator;
using Microsoft.EntityFrameworkCore;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Games.Contracts.v1.Games;
using yggdrasil.Modules.Games.Data;
using yggdrasil.Modules.Games.Domain;
using yggdrasil.Modules.Identity.Contracts.v1.Clients;
using yggdrasil.Persistence;

namespace yggdrasil.Modules.Games.Features.v1.Games.RegisterGame;

public sealed class RegisterGameCommandHandler(GamesModuleDbContext context, IMediator mediator, TimeProvider clock)
    : ICommandHandler<RegisterGameCommand, Result<RegisterGameResponse>>
{
    private static readonly Error slug_taken = Error.Conflict("games.slug_taken", "A game with this slug already exists.");

    public async ValueTask<Result<RegisterGameResponse>> Handle(RegisterGameCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await context.Games.AnyAsync(game => game.Slug == command.Slug, cancellationToken))
            return slug_taken;

        var clients = await mediator.Send(new CreateGameClientsCommand(command.Slug, command.Name), cancellationToken);

        if (!clients.IsSuccess)
            return clients.Error!;

        var game = new Game(command.Slug, command.Name, clients.Value.ClientId, clients.Value.ServerClientId, clock.GetUtcNow());
        context.Games.Add(game);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            return slug_taken;
        }

        return new RegisterGameResponse(game.Id, game.Slug, game.ClientId, game.ServerClientId, clients.Value.ServerClientSecret);
    }
}
