namespace yggdrasil.PublicApis.HealthChecks;

public static class HealthCheckTags
{
    /// <summary>Checks that only verify the process is running and able to serve requests.</summary>
    public const string LIVE = "live";

    /// <summary>Checks that verify external dependencies (databases, brokers, ...) are reachable.</summary>
    public const string READY = "ready";
}
