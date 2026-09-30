using System.Text.Json;
using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Profiles.Contracts.v1.Profiles;

/// <summary>
/// Replaces a profile's data on behalf of <paramref name="ClientId"/> (a game server), only if the profile belongs to that
/// client's game and is still at <paramref name="ExpectedDataVersion"/>.
/// </summary>
public sealed record ReplaceProfileDataCommand(Guid ProfileId, string ClientId, int ExpectedDataVersion, JsonElement Data)
    : ICommand<Result<ReplaceProfileDataResponse>>;
