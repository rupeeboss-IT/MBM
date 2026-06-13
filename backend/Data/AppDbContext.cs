using Microsoft.EntityFrameworkCore;

namespace RB_Website_API.Data
{
    public class AppDbContext :DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Models.User> Users => Set<Models.User>();
        public DbSet<Models.MemberIdSequence> MemberIdSequences => Set<Models.MemberIdSequence>();
        public DbSet<Models.UserStatusAudit> UserStatusAudit => Set<Models.UserStatusAudit>();
        public DbSet<Models.UserStatusHistory> UserStatusHistory => Set<Models.UserStatusHistory>();
        public DbSet<Models.UserAuditLog> UserAuditLog => Set<Models.UserAuditLog>();
        public DbSet<Models.Plan> Plans => Set<Models.Plan>();
        public DbSet<Models.PaymentOrder> PaymentOrders => Set<Models.PaymentOrder>();
        public DbSet<Models.Payment> Payments => Set<Models.Payment>();
        public DbSet<Models.UserPlan> UserPlans => Set<Models.UserPlan>();
        public DbSet<Models.PaymentOrderReferral> PaymentOrderReferrals => Set<Models.PaymentOrderReferral>();
        public DbSet<Models.ReferralLeadOutbox> ReferralLeadOutbox => Set<Models.ReferralLeadOutbox>();
        public DbSet<Models.CustomerReport> CustomerReports => Set<Models.CustomerReport>();
        public DbSet<Models.ReportAuditLog> ReportAuditLogs => Set<Models.ReportAuditLog>();
        public DbSet<Models.ReportChangeRequest> ReportChangeRequests => Set<Models.ReportChangeRequest>();
        public DbSet<Models.ApiExceptionLog> ApiExceptionLogs => Set<Models.ApiExceptionLog>();
        public DbSet<Models.LoanApplication> LoanApplications => Set<Models.LoanApplication>();
        public DbSet<Models.ContactSubmission> ContactSubmissions => Set<Models.ContactSubmission>();
        public DbSet<Models.SchemeDiscoveryRequest> SchemeDiscoveryRequests => Set<Models.SchemeDiscoveryRequest>();

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MBM.dbo.LoanApplications — Id = RBMAIN.dbo.lead_data.Lead_id (int, not identity in MBM)
            modelBuilder.Entity<Models.LoanApplication>(e =>
            {
                e.ToTable("LoanApplications", "dbo");
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.FullName).HasMaxLength(160).IsRequired();
                e.Property(x => x.Phone).HasMaxLength(10).IsRequired();
                e.Property(x => x.Email).HasMaxLength(508);
                e.Property(x => x.Pincode).HasMaxLength(6).IsRequired();
                e.Property(x => x.LoanTypeId).IsRequired();
                e.Property(x => x.LoanAmount).IsRequired();
                e.Property(x => x.ConsentAccepted).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
            });

            modelBuilder.Entity<Models.ContactSubmission>(e =>
            {
                e.ToTable("ContactSubmissions", "dbo");
                e.Property(x => x.FullName).HasMaxLength(160).IsRequired();
                e.Property(x => x.Phone).HasMaxLength(10).IsRequired();
                e.Property(x => x.Email).HasMaxLength(508).IsRequired();
                e.Property(x => x.Message).HasMaxLength(4000).IsRequired();
                e.Property(x => x.ConsentAccepted).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
            });
        }
    }
}
