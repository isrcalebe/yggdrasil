using Microsoft.AspNetCore.Identity;

namespace yggdrasil.Modules.Identity.Domain;

public sealed class Account : IdentityUser<Guid>
{
    public Account()
        => Id = Guid.CreateVersion7();
}
