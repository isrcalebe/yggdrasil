namespace yggdrasil.Modules.Identity.Contracts.v1.Clients;

public sealed record GameClientsResponse(string ClientId, string ServerClientId, string ServerClientSecret);
