namespace yggdrasil.Modules.Identity.Contracts;

/// <summary>
/// OAuth scopes of the Yggdrasil APIs. Identity is the authorization server, so it keeps the catalog: it registers these
/// scopes and grants them to the game clients.
/// </summary>
public static class IdentityScopes
{
    /// <summary>
    /// A game reads the signed-in player's profile.
    /// </summary>
    public const string PROFILES_READ = "profiles.read";

    /// <summary>
    /// A game server replaces the data of its players' profiles (client credentials only: players never write).
    /// </summary>
    public const string PROFILES_WRITE = "profiles.write";
}
