namespace yggdrasil.Integration.Tests.Infrastructure;

/// <summary>
/// Base class of integration tests: every test starts with an empty database (schema and migrations kept).
/// </summary>
public abstract class IntegrationTest(IntegrationFactory factory) : IClassFixture<IntegrationFactory>, IAsyncLifetime
{
    protected IntegrationFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateClient();

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await Factory.Database.ResetAsync();

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }
}
