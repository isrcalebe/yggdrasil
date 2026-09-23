using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using yggdrasil.Modules.Identity.Domain;

namespace yggdrasil.Modules.Identity.Data.Configurations;

public sealed class IdentityTablesConfiguration :
    IEntityTypeConfiguration<Account>,
    IEntityTypeConfiguration<IdentityRole<Guid>>,
    IEntityTypeConfiguration<IdentityUserRole<Guid>>,
    IEntityTypeConfiguration<IdentityUserClaim<Guid>>,
    IEntityTypeConfiguration<IdentityUserLogin<Guid>>,
    IEntityTypeConfiguration<IdentityUserToken<Guid>>,
    IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<Account> builder)
        => builder.ToTable("accounts");

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
        => builder.ToTable("roles");

    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
        => builder.ToTable("account_roles");

    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
        => builder.ToTable("account_claims");

    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
        => builder.ToTable("account_logins");

    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
        => builder.ToTable("account_tokens");

    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
        => builder.ToTable("role_claims");
}
