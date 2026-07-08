using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RB_Website_API.Logging;
using RB_Website_API.Middleware;
using Serilog;
using System.Text;

MbmFileLogging.CreateBootstrapLogger();

try
{
var builder = WebApplication.CreateBuilder(args);

MbmFileLogging.Configure(builder);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.Converters.Add(new RB_Website_API.Auth.LocalDateTimeJsonConverter());
    o.JsonSerializerOptions.Converters.Add(new RB_Website_API.Auth.NullableLocalDateTimeJsonConverter());
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var bad = new BadRequestObjectResult(new
        {
            success = false,
            message = "Please check your input and try again.",
        });
        return bad;
    };
});

builder.Services.AddExceptionHandler<GlobalApiExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        if (!ctx.HttpContext.Request.Path.StartsWithSegments("/api")) return;
        ctx.ProblemDetails.Title = null;
        ctx.ProblemDetails.Detail = null;
        ctx.ProblemDetails.Extensions.Clear();
        ctx.ProblemDetails.Type = null;
        ctx.ProblemDetails.Instance = null;
    };
});

builder.Services.AddSingleton<RB_Website_API.Services.IApiExceptionLogService, RB_Website_API.Services.ApiExceptionLogService>();

builder.Services.AddDbContext<RB_Website_API.Data.AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("ConnectionString")
             ?? builder.Configuration["ConnectionStrings:ConnectionString"];
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Missing ConnectionStrings:ConnectionString in configuration.");
    options.UseSqlServer(cs);
});

builder.Services.AddDbContext<RB_Website_API.Data.ReferralDbContext>(options =>
{
    // Prefer ConnectionStrings:ReferralDb, fallback to ConnectionStrings:rbConnectionString if present.
    var cs = builder.Configuration.GetConnectionString("ReferralDb")
             ?? builder.Configuration["ConnectionStrings:ReferralDb"]
             ?? builder.Configuration.GetConnectionString("rbConnectionString")
             ?? builder.Configuration["ConnectionStrings:rbConnectionString"];

    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Missing ConnectionStrings:ReferralDb in configuration.");

    options.UseSqlServer(cs);
});

builder.Services.Configure<RB_Website_API.Auth.ApplicationUrlsSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.ApplicationUrlsSettings.SectionName));
builder.Services.Configure<RB_Website_API.Auth.JwtSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.JwtSettings.SectionName));
builder.Services.AddSingleton<RB_Website_API.Auth.IJwtTokenService, RB_Website_API.Auth.JwtTokenService>();

builder.Services.Configure<RB_Website_API.Auth.AdminSeedSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.AdminSeedSettings.SectionName));
builder.Services.AddHostedService<RB_Website_API.Auth.AdminSeederHostedService>();

var jwt = builder.Configuration.GetSection(RB_Website_API.Auth.JwtSettings.SectionName).Get<RB_Website_API.Auth.JwtSettings>()
          ?? new RB_Website_API.Auth.JwtSettings();
if (string.IsNullOrWhiteSpace(jwt.Key))
    throw new InvalidOperationException("Missing Jwt:Key in configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwt.Issuer),
            ValidateAudience = !string.IsNullOrWhiteSpace(jwt.Audience),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminAccess", policy => policy.RequireRole("admin", "superadmin"));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("superadmin"));
    options.AddPolicy("MemberAccess", policy => policy.RequireRole("member", "partner"));
});

builder.Services.AddScoped<RB_Website_API.Services.PaymentActivationService>();
builder.Services.AddScoped<RB_Website_API.Services.MembershipEmailService>();
builder.Services.AddScoped<RB_Website_API.Services.RegistrationWelcomeEmailService>();
builder.Services.AddScoped<RB_Website_API.Services.InvoicePdfService>();
builder.Services.Configure<RB_Website_API.Auth.InvoiceSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.InvoiceSettings.SectionName));
builder.Services.Configure<RB_Website_API.Auth.CustomerReportSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.CustomerReportSettings.SectionName));
builder.Services.Configure<RB_Website_API.Auth.SdrReportSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.SdrReportSettings.SectionName));
builder.Services.Configure<RB_Website_API.Auth.ContactSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.ContactSettings.SectionName));
builder.Services.AddHostedService<RB_Website_API.Auth.DateTimeDefaultsSchemaHostedService>();
builder.Services.AddHostedService<RB_Website_API.Auth.UserPlanSchemaHostedService>();
builder.Services.AddHostedService<RB_Website_API.Auth.ReferralSchemaHostedService>();
builder.Services.AddHostedService<RB_Website_API.Auth.CustomerReportSchemaHostedService>();
builder.Services.AddHostedService<RB_Website_API.Auth.ApiExceptionLogSchemaHostedService>();
builder.Services.AddHostedService<RB_Website_API.Auth.MemberIdSchemaHostedService>();
builder.Services.AddHostedService<RB_Website_API.Auth.LoanApplicationSchemaHostedService>();
builder.Services.AddHostedService<RB_Website_API.Auth.ContactSchemaHostedService>();
builder.Services.AddHostedService<RB_Website_API.Auth.SchemeDiscoveryBootstrapHostedService>();
builder.Services.AddHostedService<RB_Website_API.Services.SubscriptionExpiryHostedService>();
builder.Services.AddHostedService<RB_Website_API.Services.SubscriptionReminderHostedService>();

var corsOrigins = builder.Configuration
    .GetSection(RB_Website_API.Auth.ApplicationUrlsSettings.SectionName)
    .Get<RB_Website_API.Auth.ApplicationUrlsSettings>()?
    .CorsOrigins?
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .ToArray() ?? [];

if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
    });
}
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RB_Website_API.Auth.EmailSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.EmailSettings.SectionName));
builder.Services.Configure<RB_Website_API.Auth.SmsSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.SmsSettings.SectionName));
builder.Services.Configure<RB_Website_API.Auth.RazorpaySettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.RazorpaySettings.SectionName));
builder.Services.Configure<RB_Website_API.Auth.ReferralSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.ReferralSettings.SectionName));
builder.Services.Configure<RB_Website_API.Auth.ZohoFlowSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.ZohoFlowSettings.SectionName));
builder.Services.AddHttpClient(
    RB_Website_API.Auth.HttpSmsSender.HttpClientName,
    client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient("Razorpay", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("Saarthi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.UserAgent.Add(
        new System.Net.Http.Headers.ProductInfoHeaderValue("MSME-Bharat-Manch", "1.0"));
});
builder.Services.AddHttpClient(RB_Website_API.Services.Webhooks.ZohoFlowWebhookClient.HttpClientName, client =>
{
    var timeoutSeconds = builder.Configuration.GetValue<int?>($"{RB_Website_API.Auth.ZohoFlowSettings.SectionName}:TimeoutSeconds") ?? 15;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 60));
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.UserAgent.Add(
        new System.Net.Http.Headers.ProductInfoHeaderValue("MSME-Bharat-Manch", "1.0"));
});
builder.Services.AddSingleton<RB_Website_API.Auth.IOtpRateLimiter, RB_Website_API.Auth.OtpRateLimiter>();
builder.Services.AddSingleton<RB_Website_API.Auth.IOtpEmailTemplateService, RB_Website_API.Auth.OtpEmailTemplateService>();
builder.Services.AddSingleton<RB_Website_API.Auth.IOtpService, RB_Website_API.Auth.InMemoryOtpService>();
builder.Services.AddSingleton<RB_Website_API.Auth.IPasswordResetService, RB_Website_API.Auth.InMemoryPasswordResetService>();
builder.Services.AddSingleton<RB_Website_API.Auth.IEmailSender, RB_Website_API.Auth.SmtpEmailSender>();
builder.Services.AddSingleton<RB_Website_API.Auth.ISmsSender, RB_Website_API.Auth.HttpSmsSender>();
builder.Services.AddHostedService<RB_Website_API.Auth.SmtpWarmupHostedService>();

builder.Services.AddScoped<RB_Website_API.Referrals.Services.IEmployeeValidationService, RB_Website_API.Referrals.Services.EmployeeValidationService>();
builder.Services.AddScoped<RB_Website_API.Services.IReferralService, RB_Website_API.Services.ReferralService>();
builder.Services.AddScoped<RB_Website_API.Services.ILeadPushService, RB_Website_API.Services.LeadPushService>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.IReportChangeRequestRepository, RB_Website_API.Services.Repository.ReportChangeRequestRepository>();
builder.Services.AddScoped<RB_Website_API.Services.IReportChangeRequestService, RB_Website_API.Services.ReportChangeRequestService>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.ICustomerReportRepository, RB_Website_API.Services.Repository.CustomerReportRepository>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.IReportAuditRepository, RB_Website_API.Services.Repository.ReportAuditRepository>();
builder.Services.AddScoped<RB_Website_API.Services.ICustomerReportService, RB_Website_API.Services.CustomerReportService>();
builder.Services.AddScoped<RB_Website_API.Services.IReportAuditService, RB_Website_API.Services.ReportAuditService>();
builder.Services.AddScoped<RB_Website_API.Services.IReportEmailService, RB_Website_API.Services.ReportEmailService>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.IMemberIdSequenceRepository, RB_Website_API.Services.Repository.MemberIdSequenceRepository>();
builder.Services.AddScoped<RB_Website_API.Services.IMemberIdGeneratorService, RB_Website_API.Services.MemberIdGeneratorService>();
builder.Services.AddScoped<RB_Website_API.Services.ILoanApplicationService, RB_Website_API.Services.LoanApplicationService>();
builder.Services.AddScoped<RB_Website_API.Services.ICreditRebuildService, RB_Website_API.Services.CreditRebuildService>();
builder.Services.AddScoped<RB_Website_API.Services.IContactEmailService, RB_Website_API.Services.ContactEmailService>();
builder.Services.AddScoped<RB_Website_API.Services.IContactService, RB_Website_API.Services.ContactService>();
builder.Services.AddScoped<RB_Website_API.Services.ISchemeDiscoveryService, RB_Website_API.Services.SchemeDiscoveryService>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.ISdrReportRepository, RB_Website_API.Services.Repository.SdrReportRepository>();
builder.Services.AddScoped<RB_Website_API.Services.ISaarthiApiClient, RB_Website_API.Services.SaarthiApiClient>();
builder.Services.AddScoped<RB_Website_API.Services.ISdrReportService, RB_Website_API.Services.SdrReportService>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.IUserManagementRepository, RB_Website_API.Services.Repository.UserManagementRepository>();
builder.Services.AddScoped<RB_Website_API.Services.IUserManagementService, RB_Website_API.Services.UserManagementService>();
builder.Services.AddScoped<RB_Website_API.Services.Webhooks.IZohoFlowWebhookClient, RB_Website_API.Services.Webhooks.ZohoFlowWebhookClient>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.ICreditRepairLeadRepository, RB_Website_API.Services.Repository.CreditRepairLeadRepository>();
builder.Services.AddScoped<RB_Website_API.Services.ICreditRepairLeadService, RB_Website_API.Services.CreditRepairLeadService>();
builder.Services.AddScoped<RB_Website_API.Features.CreditRepair.ISubmitCreditRepairLeadHandler, RB_Website_API.Features.CreditRepair.SubmitCreditRepairLeadHandler>();
builder.Services.AddScoped<RB_Website_API.Services.BulkImportWelcomeEmailService>();
builder.Services.AddScoped<RB_Website_API.Services.IBulkMemberImportService, RB_Website_API.Services.BulkMemberImportService>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.IVendorManagementRepository, RB_Website_API.Services.Repository.VendorManagementRepository>();
builder.Services.AddScoped<RB_Website_API.Services.IVendorManagementService, RB_Website_API.Services.VendorManagementService>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.ILeadAttributionRepository, RB_Website_API.Services.Repository.LeadAttributionRepository>();
builder.Services.AddScoped<RB_Website_API.Services.ILeadAttributionService, RB_Website_API.Services.LeadAttributionService>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.IConnectRepository, RB_Website_API.Services.Repository.ConnectRepository>();
builder.Services.AddScoped<RB_Website_API.Services.IConnectService, RB_Website_API.Services.ConnectService>();
builder.Services.Configure<RB_Website_API.Auth.ConnectSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.ConnectSettings.SectionName));
builder.Services.AddScoped<RB_Website_API.Services.IRepository.IEnquiryManagementRepository, RB_Website_API.Services.Repository.EnquiryManagementRepository>();
builder.Services.AddScoped<RB_Website_API.Services.IEnquiryManagementService, RB_Website_API.Services.EnquiryManagementService>();
builder.Services.AddScoped<RB_Website_API.Services.IRepository.ICreditRepairManagementRepository, RB_Website_API.Services.Repository.CreditRepairManagementRepository>();
builder.Services.AddScoped<RB_Website_API.Services.ICreditRepairManagementService, RB_Website_API.Services.CreditRepairManagementService>();

var app = builder.Build();

app.Logger.LogInformation(
    "MBM API started Environment={Environment} Urls={Urls} LogFolder={LogFolder}",
    app.Environment.EnvironmentName,
    string.Join(", ", app.Urls),
    MbmFileLogging.LogFolder);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


if (corsOrigins.Length > 0)
    app.UseCors("Frontend");

app.UseMiddleware<HttpContextLogEnrichmentMiddleware>();

// In Development, the Angular proxy uses http://localhost:5228. HTTPS redirection would 307 to another
// port and can break POST bodies / proxy handling for /api calls.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseLegacyUrlRedirects();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Angular SPA (published under wwwroot): /login, /profile, etc.
app.MapFallbackToFile("index.html");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MBM API terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("MBM API shutting down");
    Log.CloseAndFlush();
}
