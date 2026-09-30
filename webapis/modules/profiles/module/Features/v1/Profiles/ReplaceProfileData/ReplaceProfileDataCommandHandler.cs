using Mediator;
using Microsoft.EntityFrameworkCore;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Games.Contracts.v1.Games;
using yggdrasil.Modules.Profiles.Contracts.v1.Profiles;
using yggdrasil.Modules.Profiles.Data;

namespace yggdrasil.Modules.Profiles.Features.v1.Profiles.ReplaceProfileData;

public sealed class ReplaceProfileDataCommandHandler(ProfilesModuleDbContext context, IMediator mediator)
    : ICommandHandler<ReplaceProfileDataCommand, Result<ReplaceProfileDataResponse>>
{
    private static readonly Error stale = Error.PreconditionFailed(
        "profiles.stale_data",
        "The profile changed since you read it: read it again and reapply your change."
    );

    public async ValueTask<Result<ReplaceProfileDataResponse>> Handle(ReplaceProfileDataCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var game = await mediator.Send(new GetGameByClientIdQuery(command.ClientId), cancellationToken);

        if (!game.IsSuccess)
            return Error.Forbidden("profiles.not_a_game", "The token was not issued to a registered game.");

        var profile = await context.Profiles.SingleOrDefaultAsync(profile =>
            profile.Id == command.ProfileId &&
            profile.GameId == game.Value.GameId,
            cancellationToken
        );

        if (profile is null)
            return Error.NotFound("profiles.not_found", "No profile has this id.");

        if (profile.DataVersion != command.ExpectedDataVersion)
            return stale;

        profile.ReplaceData(command.Data.GetRawText());

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return stale;
        }

        return new ReplaceProfileDataResponse(profile.DataVersion);
    }
}
