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
        public DbSet<Models.UserRegistrationLead> UserRegistrationLeads => Set<Models.UserRegistrationLead>();
        public DbSet<Models.ReferralLeadOutbox> ReferralLeadOutbox => Set<Models.ReferralLeadOutbox>();
        public DbSet<Models.CustomerReport> CustomerReports => Set<Models.CustomerReport>();
        public DbSet<Models.ReportAuditLog> ReportAuditLogs => Set<Models.ReportAuditLog>();
        public DbSet<Models.ReportChangeRequest> ReportChangeRequests => Set<Models.ReportChangeRequest>();
        public DbSet<Models.ApiExceptionLog> ApiExceptionLogs => Set<Models.ApiExceptionLog>();
        public DbSet<Models.LoanApplication> LoanApplications => Set<Models.LoanApplication>();
        public DbSet<Models.CreditRebuildEnquiry> CreditRebuildEnquiries => Set<Models.CreditRebuildEnquiry>();
        public DbSet<Models.CreditRepairLead> CreditRepairLeads => Set<Models.CreditRepairLead>();
        public DbSet<Models.ContactSubmission> ContactSubmissions => Set<Models.ContactSubmission>();
        public DbSet<Models.SchemeDiscoveryRequest> SchemeDiscoveryRequests => Set<Models.SchemeDiscoveryRequest>();
        public DbSet<Models.Vendor> Vendors => Set<Models.Vendor>();
        public DbSet<Models.VendorPlanMapping> VendorPlanMappings => Set<Models.VendorPlanMapping>();
        public DbSet<Models.VendorAuditLog> VendorAuditLogs => Set<Models.VendorAuditLog>();
        public DbSet<Models.EnquiryStatusHistory> EnquiryStatusHistories => Set<Models.EnquiryStatusHistory>();
        public DbSet<Models.ConnectMemberProfile> ConnectMemberProfiles => Set<Models.ConnectMemberProfile>();
        public DbSet<Models.ConnectAdminListing> ConnectAdminListings => Set<Models.ConnectAdminListing>();
        public DbSet<Models.ConnectRequest> ConnectRequests => Set<Models.ConnectRequest>();
        public DbSet<Models.ConnectContactUnlock> ConnectContactUnlocks => Set<Models.ConnectContactUnlock>();
        public DbSet<Models.CookieConsentLog> CookieConsentLogs => Set<Models.CookieConsentLog>();
        public DbSet<Models.Blog> Blogs => Set<Models.Blog>();
        public DbSet<Models.BlogCategory> BlogCategories => Set<Models.BlogCategory>();
        public DbSet<Models.BlogBadge> BlogBadges => Set<Models.BlogBadge>();
        public DbSet<Models.EventCategory> EventCategories => Set<Models.EventCategory>();
        public DbSet<Models.EventCity> EventCities => Set<Models.EventCity>();
        public DbSet<Models.Event> Events => Set<Models.Event>();
        public DbSet<Models.EventHighlight> EventHighlights => Set<Models.EventHighlight>();
        public DbSet<Models.EventPartner> EventPartners => Set<Models.EventPartner>();
        public DbSet<Models.SchemeCategory> SchemeCategories => Set<Models.SchemeCategory>();
        public DbSet<Models.Scheme> Schemes => Set<Models.Scheme>();
        public DbSet<Models.SchemeBenefit> SchemeBenefits => Set<Models.SchemeBenefit>();
        public DbSet<Models.SchemeCardHighlight> SchemeCardHighlights => Set<Models.SchemeCardHighlight>();
        public DbSet<Models.TeamMember> TeamMembers => Set<Models.TeamMember>();

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

            modelBuilder.Entity<Models.CreditRebuildEnquiry>(e =>
            {
                e.ToTable("CreditRebuildEnquiries", "dbo");
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.FullName).HasMaxLength(160).IsRequired();
                e.Property(x => x.Phone).HasMaxLength(10).IsRequired();
                e.Property(x => x.Email).HasMaxLength(508).IsRequired();
                e.Property(x => x.AdvisorCode).HasMaxLength(50);
                e.Property(x => x.ConsentAccepted).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
            });

            modelBuilder.Entity<Models.CreditRepairLead>(e =>
            {
                e.ToTable("CreditRepair_Lead", "dbo");
                e.Property(x => x.FullName).HasMaxLength(160).IsRequired();
                e.Property(x => x.Phone).HasMaxLength(10).IsRequired();
                e.Property(x => x.Email).HasMaxLength(508);
                e.Property(x => x.ConsentAccepted).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
                e.Property(x => x.Source).HasMaxLength(80).IsRequired();
                e.Property(x => x.CampaignName).HasMaxLength(200).IsRequired();
                e.Property(x => x.LeadId).HasColumnName("lead_id");
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
                e.Property(x => x.CompanyName).HasMaxLength(200);
                e.Property(x => x.Source).HasMaxLength(80).IsRequired();
                e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            });

            modelBuilder.Entity<Models.EnquiryStatusHistory>(e =>
            {
                e.ToTable("EnquiryStatusHistory", "dbo");
                e.Property(x => x.OldStatus).HasMaxLength(20);
                e.Property(x => x.NewStatus).HasMaxLength(20).IsRequired();
                e.Property(x => x.Remarks).HasMaxLength(800);
                e.Property(x => x.ChangedOn).IsRequired();
            });

            modelBuilder.Entity<Models.ConnectContactUnlock>(e =>
            {
                e.ToTable("ConnectContactUnlocks", "dbo");
                e.HasKey(x => new { x.ViewerUserId, x.TargetUserId });
            });

            modelBuilder.Entity<Models.Blog>(e =>
            {
                e.ToTable("Blogs", "dbo");
                e.HasIndex(b => b.Slug).IsUnique();
                e.Property(b => b.Content).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<Models.EventCategory>(e =>
            {
                e.ToTable("EventCategories", "dbo");
                e.HasIndex(c => c.Slug).IsUnique();
            });

            modelBuilder.Entity<Models.EventCity>(e =>
            {
                e.ToTable("EventCities", "dbo");
                e.HasIndex(c => c.Slug).IsUnique();
            });

            modelBuilder.Entity<Models.Event>(e =>
            {
                e.ToTable("Events", "dbo");
                e.HasIndex(x => x.Slug).IsUnique();
                e.Property(x => x.AboutHtml).HasColumnType("nvarchar(max)");
                e.Property(x => x.HighlightsHtml).HasColumnType("nvarchar(max)");
                e.Property(x => x.AssociationHtml).HasColumnType("nvarchar(max)");
                e.Property(x => x.Latitude).HasPrecision(9, 6);
                e.Property(x => x.Longitude).HasPrecision(9, 6);
                e.HasMany(x => x.Highlights)
                    .WithOne(h => h.Event)
                    .HasForeignKey(h => h.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasMany(x => x.Partners)
                    .WithOne(p => p.Event)
                    .HasForeignKey(p => p.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Models.SchemeCategory>(e =>
            {
                e.ToTable("SchemeCategories", "dbo");
                e.HasIndex(c => c.Slug).IsUnique();
            });

            modelBuilder.Entity<Models.Scheme>(e =>
            {
                e.ToTable("Schemes", "dbo");
                e.HasIndex(x => x.Slug).IsUnique();
                e.Property(x => x.ContentHtml).HasColumnType("nvarchar(max)");
                e.HasMany(x => x.Benefits)
                    .WithOne(b => b.Scheme)
                    .HasForeignKey(b => b.SchemeId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasMany(x => x.CardHighlights)
                    .WithOne(h => h.Scheme)
                    .HasForeignKey(h => h.SchemeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Models.TeamMember>(e =>
            {
                e.ToTable("TeamMembers", "dbo");
            });
        }
    }
}
