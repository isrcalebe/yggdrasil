using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using yggdrasil.Modules.Identity.Domain;
using yggdrasil.Persistence;

namespace yggdrasil.Modules.Identity.Data;

public sealed class IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options)
    : IdentityDbContext<Account, IdentityRole<Guid>, Guid>(options), IModuleDbContext
{
    public static string Schema => IdentityModule.NAME;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseOpenIddict<Guid>();

        builder.ApplyModuleConventions<IdentityModuleDbContext>();
    }
}
