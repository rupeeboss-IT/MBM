using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RB_Website_API.DTO;
using RB_Website_API.Models;
using RB_Website_API.Models.Lead;
using RB_Website_API.ViewModels;

namespace RB_Website_API.Data
{
    public class AppDbContext :DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserOtp> UserOtp { get; set; }
        public DbSet<OtpRequestLog> OtpRequestLog { get; set; }
        public DbSet<ProfileViewModel> ProfileViewModel { get; set; }
        public DbSet<DashboardViewModel> DashboardViewModel { get; set; }
        public DbSet<CompanyStats> CompanyStats { get; set; }
        public DbSet<LoanRates> LoanRates { get; set; }
        public DbSet<lenderMaster> lenderMaster { get; set; }
        public DbSet<LoanProducts> LoanProducts { get; set; }
        public DbSet<LenderProductRates> LenderProductRates { get; set; }
        public DbSet<RB_ContactRequests> RB_ContactRequests { get; set; }
        public DbSet<RB_OfficeLocations> RB_OfficeLocations { get; set; }
        public DbSet<Pincode_Master_ID> Pincode_Master_ID { get; set; }
        public DbSet<RB_Events> RB_Events { get; set; }
        public DbSet<RB_TestimonialMedia> RB_TestimonialMedia { get; set; }
        public DbSet<Lead_data> lead_Data { get; set; }
        public DbSet<CreditRepairLead> CreditRepairLeads { get; set; }
        public DbSet<RB_JobDto> RB_JobDto { get; set; }
        public DbSet<RB_JobApplications> RB_JobApplications { get; set; }
        public DbSet<RB_Vacancy_Master> RB_Vacancy_Master { get; set; }
        public DbSet<RB_Vacancy_Details> RB_Vacancy_Details { get; set; }
        public DbSet<Designation_Master> Designation_Master { get; set; }

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
