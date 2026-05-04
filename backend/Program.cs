using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddDbContext<RB_Website_API.Data.AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("ConnectionString")
             ?? builder.Configuration["ConnectionStrings:ConnectionString"];
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Missing ConnectionStrings:ConnectionString in configuration.");
    options.UseSqlServer(cs);
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RB_Website_API.Auth.EmailSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.EmailSettings.SectionName));
builder.Services.Configure<RB_Website_API.Auth.SmsSettings>(
    builder.Configuration.GetSection(RB_Website_API.Auth.SmsSettings.SectionName));
builder.Services.AddHttpClient(
    RB_Website_API.Auth.HttpSmsSender.HttpClientName,
    client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddSingleton<RB_Website_API.Auth.IOtpRateLimiter, RB_Website_API.Auth.OtpRateLimiter>();
builder.Services.AddSingleton<RB_Website_API.Auth.IOtpService, RB_Website_API.Auth.InMemoryOtpService>();
builder.Services.AddSingleton<RB_Website_API.Auth.IEmailSender, RB_Website_API.Auth.SmtpEmailSender>();
builder.Services.AddSingleton<RB_Website_API.Auth.ISmsSender, RB_Website_API.Auth.HttpSmsSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendDev");

// In Development, the Angular proxy uses http://localhost:5228. HTTPS redirection would 307 to another
// port and can break POST bodies / proxy handling for /api calls.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
