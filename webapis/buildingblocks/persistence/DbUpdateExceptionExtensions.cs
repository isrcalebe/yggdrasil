using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace yggdrasil.Persistence;

public static class DbUpdateExceptionExtensions
{
    extension(DbUpdateException self)
    {
        public bool IsUniqueViolation => self.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
    }
}
