namespace RB_Website_API.Data
{
    public interface IAppDbContext
    {
       

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        int SaveChanges();

        int ExecuteSqlRaw(string sql, params object[] parameters);
        Task<int> ExecuteSqlRawAsync(string sql, object[] parameters, CancellationToken cancellationToken = default);
    }
}
