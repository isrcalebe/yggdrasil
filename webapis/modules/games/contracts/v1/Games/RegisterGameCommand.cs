using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Games.Contracts.v1.Games;

public sealed record RegisterGameCommand(string Slug, string Name)
    : ICommand<Result<RegisterGameResponse>>;
