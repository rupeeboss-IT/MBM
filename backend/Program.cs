var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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

builder.Services.AddSingleton<RB_Website_API.Auth.IOtpService, RB_Website_API.Auth.InMemoryOtpService>();
builder.Services.AddSingleton<RB_Website_API.Auth.IEmailSender, RB_Website_API.Auth.ConsoleEmailSender>();
builder.Services.AddSingleton<RB_Website_API.Auth.ISmsSender, RB_Website_API.Auth.ConsoleSmsSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendDev");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
