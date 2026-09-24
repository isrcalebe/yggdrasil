namespace yggdrasil.Modules.Identity.Contracts.v1.Sessions;

public sealed record CurrentSessionResponse(Guid AccountId, string Email);
