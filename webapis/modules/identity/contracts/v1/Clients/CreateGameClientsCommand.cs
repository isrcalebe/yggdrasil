using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Identity.Contracts.v1.Clients;

public sealed record CreateGameClientsCommand(string GameSlug, string GameName)
    : ICommand<Result<GameClientsResponse>>;
