using Microsoft.EntityFrameworkCore;

namespace RB_Website_API.Data
{
    public class AppDbContext :DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Models.User> Users => Set<Models.User>();

        public new DbSet<T> Set<T>() where T : class
        {
            return base.Set<T>();
        }
        public int ExecuteSqlRaw(string sql, params object[] parameters)
        {
            return base.Database.ExecuteSqlRaw(sql, parameters);
        }

        public async Task<int> ExecuteSqlRawAsync(string sql, object[] parameters, CancellationToken cancellationToken = default)
        {
            return await base.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
        }

    }
}
