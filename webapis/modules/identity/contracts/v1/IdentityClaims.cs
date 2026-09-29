using System.Security.Claims;

namespace yggdrasil.Modules.Identity.Contracts.v1;

/// <summary>
/// What other modules read from an access token, without depending on how identity issues it.
/// </summary>
public static class IdentityClaims
{
    extension(ClaimsPrincipal self)
    {
        /// <summary>
        /// The player's account; <see langword="null"/> when the token is not about a player (client credentials).
        /// </summary>
        public Guid? AccountId => Guid.TryParse(self.FindFirst("sub")?.Value, out var accountId)
            ? accountId
            : null;

        /// <summary>
        /// The OAuth client the token was issued to.
        /// </summary>
        public string? ClientId => self.FindFirst("client_id")?.Value;
    }
}
