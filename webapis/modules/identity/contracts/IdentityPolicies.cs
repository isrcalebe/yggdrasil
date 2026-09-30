namespace yggdrasil.Modules.Identity.Contracts;

/// <summary>
/// Authorization policies other modules can require on their endpoints (<c>.RequireAuthorization(...)</c>).
/// </summary>
public static class IdentityPolicies
{
    /// <summary>
    /// Signed in with an administrator account.
    /// </summary>
    public const string ADMIN = "identity.admin";

    /// <summary>
    /// A valid access token (<c>Authorization: Bearer</c>) carrying the <see cref="IdentityScopes.PROFILES_READ"/> scope.
    /// </summary>
    public const string PROFILES_READ = "identity.scope.profiles.read";
}
