using System.Buffers.Text;
using System.Security.Cryptography;
using Mediator;
using OpenIddict.Abstractions;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Identity.Contracts.v1.Clients;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace yggdrasil.Modules.Identity.Features.v1.Clients.CreateGameClients;

public sealed class CreateGameClientsCommandHandler(IOpenIddictApplicationManager applications)
    : ICommandHandler<CreateGameClientsCommand, Result<GameClientsResponse>>
{
    private static readonly Uri loopback_redirect_uri = new("http://127.0.0.1/callback");

    public async ValueTask<Result<GameClientsResponse>> Handle(CreateGameClientsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var clientId = command.GameSlug;
        var serverClientId = $"{command.GameSlug}.server";

        if (await applications.FindByClientIdAsync(clientId, cancellationToken) is not null
            || await applications.FindByClientIdAsync(serverClientId, cancellationToken) is not null)
        {
            return Error.Conflict("idenity.game_clients_exist", "OAuth clients already exist for this game.");
        }

        await applications.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = command.GameName,
            ApplicationType = ApplicationTypes.Native,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            RedirectUris = { loopback_redirect_uri },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        }, cancellationToken);

        var secret = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

        await applications.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = serverClientId,
            ClientSecret = secret,
            DisplayName = $"{command.GameName} server",
            ClientType = ClientTypes.Confidential,
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials
            },
        }, cancellationToken);

        return new GameClientsResponse(clientId, serverClientId, secret);
    }
}
