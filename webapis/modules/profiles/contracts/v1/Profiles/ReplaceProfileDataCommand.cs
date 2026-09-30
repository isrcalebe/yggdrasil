using System.Text.Json;
using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Profiles.Contracts.v1.Profiles;

public sealed record ReplaceProfileDataCommand(Guid ProfileId, int ExpectedDataVersion, JsonElement Data)
    : ICommand<Result<ReplaceProfileDataResponse>>;
