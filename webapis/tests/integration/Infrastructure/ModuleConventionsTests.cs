using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using yggdrasil.Persistence;

namespace yggdrasil.Integration.Tests.Infrastructure;

public sealed class ModuleConventionsTests
{
    [Fact]
    public void IdentityTablesGoToTheModuleSchema()
    {
        using var context = new TestIdentityContext(
            new DbContextOptionsBuilder<TestIdentityContext>()
                .UseNpgsql("Host=unused")
                .Options
        );

        Assert.All(context.Model.GetEntityTypes(), entity => Assert.Equal("test", entity.GetSchema()));
    }

    private sealed class TestIdentityContext(DbContextOptions<TestIdentityContext> options)
        : IdentityDbContext(options), IModuleDbContext
    {
        public static string Schema => "test";

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyModuleConventions<TestIdentityContext>();
        }
    }
}
