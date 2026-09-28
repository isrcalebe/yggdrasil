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
}
