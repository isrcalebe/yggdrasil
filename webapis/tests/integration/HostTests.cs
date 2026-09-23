using System.Net;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Persistence;

namespace yggdrasil.Integration.Tests;

/// <summary>Checks that hold for every module, so new modules are covered without new tests.</summary>
public sealed class HostTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task HostIsReadyAgainstRealDatabase()
    {
        using var response = await Client.GetAsync(new Uri("/health/ready", UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ModuleDatabasesAreMigratedAndMatchTheirModel()
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        foreach (var contextType in moduleDbContextTypes())
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);

            Assert.Empty(await context.Database.GetPendingMigrationsAsync(CancellationToken));
            Assert.False(
                context.Database.HasPendingModelChanges(),
                $"{contextType.Name} has model changes without a migration. Run 'dotnet ef migrations add'.");
        }
    }

    [Fact]
    public async Task ModuleDbContextUseTheirSchema()
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        foreach (var contextType in moduleDbContextTypes())
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
            var schema = (string)contextType.GetProperty(nameof(IModuleDbContext.Schema))!.GetValue(null)!;

            Assert.All(context.Model.GetEntityTypes(), entity => Assert.Equal(schema, entity.GetSchema()));
        }
    }

    private static IEnumerable<Type> moduleDbContextTypes()
        => Directory
            .GetFiles(AppContext.BaseDirectory, "yggdrasil.Modules.*.dll")
            .Select(static path => Assembly.Load(Path.GetFileNameWithoutExtension(path)))
            .SelectMany(static assembly => assembly.GetTypes())
            .Where(static type => type is { IsAbstract: false } && typeof(IModuleDbContext).IsAssignableFrom(type));
}
