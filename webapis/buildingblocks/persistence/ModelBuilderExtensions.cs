using Microsoft.EntityFrameworkCore;

namespace yggdrasil.Persistence;

public static class ModelBuilderExtensions
{
    extension(ModelBuilder self)
    {
        /// <summary>
        /// Applies the module conventions: every table in <typeparamref name="TContext"/>'s schema and the entity
        /// configurations of the module assembly. Call it from <c>OnModelCreating</c>, after <c>base.OnModelCreating</c>.
        /// </summary>
        public ModelBuilder ApplyModuleConventions<TContext>()
            where TContext : DbContext, IModuleDbContext
        {
            self.HasDefaultSchema(TContext.Schema);
            self.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly);

            return self;
        }
    }
}
