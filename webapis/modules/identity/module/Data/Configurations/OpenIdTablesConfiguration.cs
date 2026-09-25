using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIddict.EntityFrameworkCore.Models;

namespace yggdrasil.Modules.Identity.Data.Configurations;

public sealed class OpenIdTablesConfiguration :
    IEntityTypeConfiguration<OpenIddictEntityFrameworkCoreApplication<Guid>>,
    IEntityTypeConfiguration<OpenIddictEntityFrameworkCoreAuthorization<Guid>>,
    IEntityTypeConfiguration<OpenIddictEntityFrameworkCoreScope<Guid>>,
    IEntityTypeConfiguration<OpenIddictEntityFrameworkCoreToken<Guid>>
{
    public void Configure(EntityTypeBuilder<OpenIddictEntityFrameworkCoreApplication<Guid>> builder)
        => builder.ToTable("oauth_applications");

    public void Configure(EntityTypeBuilder<OpenIddictEntityFrameworkCoreAuthorization<Guid>> builder)
        => builder.ToTable("oauth_authorizations");

    public void Configure(EntityTypeBuilder<OpenIddictEntityFrameworkCoreScope<Guid>> builder)
        => builder.ToTable("oauth_scopes");

    public void Configure(EntityTypeBuilder<OpenIddictEntityFrameworkCoreToken<Guid>> builder)
        => builder.ToTable("oauth_tokens");
}
