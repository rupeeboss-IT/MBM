using Microsoft.EntityFrameworkCore;
using RB_Website_API.Referrals.Models;

namespace RB_Website_API.Data;

/// <summary>
/// DB2 context (anotherdb) for employee + lead tables.
/// Kept separate from the main AppDbContext for isolation.
/// </summary>
public sealed class ReferralDbContext : DbContext
{
    public ReferralDbContext(DbContextOptions<ReferralDbContext> options) : base(options) { }

    public DbSet<EmployeeMaster> EmployeeMaster => Set<EmployeeMaster>();
    public DbSet<LeadData> LeadData => Set<LeadData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EmployeeMaster>().ToTable("employee_master");

        // lead_data has triggers; EF must not use OUTPUT on INSERT (SQL Server restriction).
        modelBuilder.Entity<LeadData>(entity =>
        {
            entity.ToTable("lead_data", tb => tb.UseSqlOutputClause(false));
            entity.Property(e => e.Lead_id).ValueGeneratedOnAdd();
        });
    }
}

