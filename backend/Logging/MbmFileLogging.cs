using Serilog;
using Serilog.Events;

namespace RB_Website_API.Logging;

/// <summary>
/// Centralized Serilog configuration for daily rolling file logs under /Logs.
/// </summary>
public static class MbmFileLogging
{
    public const string LogFolder = "Logs";
    public const string LogFilePattern = "Application-.log";

    public static void Configure(WebApplicationBuilder builder)
    {
        var logsDirectory = Path.Combine(builder.Environment.ContentRootPath, LogFolder);
        TryCreateLogsDirectory(logsDirectory);

        var logFilePath = Path.Combine(logsDirectory, LogFilePattern);

        builder.Host.UseSerilog((context, _, loggerConfiguration) =>
        {
            // All sinks configured in code — do not use ReadFrom.Configuration (it loads sink DLLs at runtime).
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authorization", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.With(new ShortSourceContextEnricher())
                .Filter.With<SensitiveDataLogFilter>()
                .WriteTo.Async(
                    sink => sink.File(
                        formatter: new MbmLogTextFormatter(),
                        path: logFilePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 31,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1),
                        restrictedToMinimumLevel: LogEventLevel.Information),
                    bufferSize: 10_000,
                    blockWhenFull: false);

            if (context.HostingEnvironment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console(
                    formatter: new MbmLogTextFormatter(),
                    restrictedToMinimumLevel: LogEventLevel.Information);
            }
        }, writeToProviders: false);
    }

    public static void CreateBootstrapLogger()
    {
        // No sink here — avoids Serilog.Sinks.Console before the host is built (IIS deploy safety).
        // Full file logging is wired in Configure() once ContentRootPath is known.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .CreateBootstrapLogger();
    }

    private static void TryCreateLogsDirectory(string logsDirectory)
    {
        try
        {
            Directory.CreateDirectory(logsDirectory);
        }
        catch
        {
            // File sink creation is best-effort; logging failures must not crash startup.
        }
    }
}
